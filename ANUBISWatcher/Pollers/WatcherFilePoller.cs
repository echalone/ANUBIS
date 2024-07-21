using ANUBISWatcher.Entities;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Pollers
{
    public class WatcherFilePoller
    {
        #region Fields

        #region Constants

        private readonly object lock_TimestampLastUpdate = new();
        private readonly object lock_PollerCount = new();
        private readonly object lock_PollerStatus = new();
        private const long c_maxFileAccessTimeInMilliseconds = 15000;
        private const long c_maxFileFirstAccessTimeInMilliseconds = 180000;

        #endregion

        #region Unchangable fields

        private readonly TimeSpan _sleepTimespan = TimeSpan.Zero;
        private readonly TimeSpan _lockTimeout = TimeSpan.Zero;
        private long _defaultAlertTimeInMilliseconds = 0;

        #endregion

        #region Changable fields

        private Thread? _pollingThread = null;
        private CancellationTokenSource? _cancellationTokenSource = null;
        private DateTime _timestampLastUpdate;
        private ulong _pollerCount = 0;
        private volatile PollerStatus _pollerStatus = PollerStatus.Stopped;

        #endregion

        #endregion

        #region Properties

        public WatcherFilePollerOptions Options { get; init; }

        private volatile bool _hasPanicked = false;
        public bool HasPanicked { get { return _hasPanicked; } private set { _hasPanicked = value; } }

        private volatile bool _hasLoopPanic = false;
        public bool HasLoopPanic { get { return _hasLoopPanic; } private set { _hasLoopPanic = value; } }

        private volatile bool _hasCurrentMailPriority = false;
        public bool HasCurrentMailPriority { get { return _hasCurrentMailPriority; } private set { _hasCurrentMailPriority = value; } }


        public bool TriggerShutDownOnError
        {
            get
            {
                return (Status == PollerStatus.Armed || Status == PollerStatus.SafeMode); // make sure poller is in correct mode
            }
        }

        public bool CanBeArmed
        {
            get
            {
                return (Status == PollerStatus.Monitoring || Status == PollerStatus.Holdback) && // make sure poller is in correct mode
                        (PollerCount >= (Options?.MinPollerCountToArm ?? 0)) && // make sure we ran through enough poller loops
                        !HasLoopPanic && !HasPanicked && // make sure poller has no panics
                         (!(Options?.Files?.Any(itm => !itm.CanBeArmed) ?? false)); // make sure switches don't have panic and are in correct mode
            }
        }

        private ILogger? Logging { get { return Options?.Logger; } }

        public DateTime LastUpdateTimestamp
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_TimestampLastUpdate, _lockTimeout);
                    if (lockTaken)
                        return _timestampLastUpdate;
                    else
                        throw new LockTimeoutException(nameof(LastUpdateTimestamp), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_TimestampLastUpdate);
                    }
                }
            }
            private set
            {
                lock (lock_TimestampLastUpdate)
                {
                    _timestampLastUpdate = value;
                }
            }
        }

        public ulong PollerCount
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_PollerCount, _lockTimeout);
                    if (lockTaken)
                        return _pollerCount;
                    else
                        throw new LockTimeoutException(nameof(PollerCount), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_PollerCount);
                    }
                }
            }
            private set
            {
                lock (lock_PollerCount)
                {
                    _pollerCount = value;
                }
            }
        }

        public PollerStatus Status
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_PollerStatus, _lockTimeout);
                    if (lockTaken)
                        return _pollerStatus;
                    else
                        throw new LockTimeoutException(nameof(Status), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_PollerStatus);
                    }
                }
            }
            private set
            {
                lock (lock_PollerStatus)
                {
                    _pollerStatus = value;
                }
            }
        }

        public long AlertTimeInMilliseconds
        {
            get
            {
                return Options.AlertTimeInMilliseconds ?? _defaultAlertTimeInMilliseconds;
            }
        }

        public long MillisecondsSinceLastUpdate
        {
            get
            {
                return (long)Math.Floor((DateTime.UtcNow - LastUpdateTimestamp).TotalMilliseconds);
            }
        }

        public bool IsPollerUnresponsive
        {
            get
            {
                try
                {
                    return MillisecondsSinceLastUpdate > AlertTimeInMilliseconds;
                }
                catch (Exception ex)
                {
                    Logging?.LogCritical(ex, "While trying to check for unresponsive watcher file poller: {message}", ex.Message);
                    Logging?.LogDebug("Mimicking unresponsive behaviour so we catch this problem");
                    return true;
                }
            }
        }

        #endregion

        #region Constructors

        public WatcherFilePoller(WatcherFilePollerOptions options)
        {
            Options = options;
            HasCurrentMailPriority = true;
            _lockTimeout = TimeSpan.FromMilliseconds(Options.LockTimeoutInMilliseconds);
            _sleepTimespan = TimeSpan.FromMilliseconds(Options.SleepTimeInMilliseconds);
            InitDefaultAlertTimeInMilliseconds();
        }

        #endregion

        #region Helper methods

        private static long OneCommandMaxMilliseconds(WatcherPollerFileOptions fileOptions)
        {
            return ((fileOptions.FileAccessRetryMillisecondsMax + c_maxFileAccessTimeInMilliseconds) *
                        fileOptions.FileAccessRetryCountMax) + c_maxFileFirstAccessTimeInMilliseconds;
        }

        private void InitDefaultAlertTimeInMilliseconds()
        {
            using (Logging?.BeginScope("InitDefaultAlertTimeInMilliseconds"))
            {
                var fileOptions = Options.Files.FirstOrDefault()?.Options;
                var anyWriteStateOnPanic = Options.Files.Any(itm => itm.Options.WriteStateOnPanic);
                _defaultAlertTimeInMilliseconds = ((Options.SleepTimeInMilliseconds * 3) +
                                                        (fileOptions != null ?
                                                            ((Options.WriteTo && anyWriteStateOnPanic ? 2 : 1) * OneCommandMaxMilliseconds(fileOptions)) : 0
                                                        )
                                                    );
                if (Options.AlertTimeInMilliseconds.HasValue)
                {
                    if (Options.WriteTo)
                    {
                        Logging?.LogInformation("Using the set alert time of {alerttime} milliseconds for local file writer poller. One poller cycle must not exceed this time or it will be considered stuck.", Options.AlertTimeInMilliseconds.Value);
                    }
                    else
                    {
                        Logging?.LogInformation("Using the set alert time of {alerttime} milliseconds for remote file reader poller. One poller cycle must not exceed this time or it will be considered stuck.", Options.AlertTimeInMilliseconds.Value);
                    }
                }
                else
                {
                    if (Options.WriteTo)
                    {
                        Logging?.LogInformation("Calculated a default alert time of {defaultalerttime} milliseconds for local file writer poller. One poller cycle must not exceed this time or it will be considered stuck.", _defaultAlertTimeInMilliseconds);
                    }
                    else
                    {
                        Logging?.LogInformation("Calculated a default alert time of {defaultalerttime} milliseconds for remote file reader poller. One poller cycle must not exceed this time or it will be considered stuck.", _defaultAlertTimeInMilliseconds);
                    }
                }
            }
        }

        private void CheckThreadCancellation()
        {
            (_cancellationTokenSource?.Token)?.ThrowIfCancellationRequested();
        }

        private void UpdateTimestamp()
        {
            using (Logging?.BeginScope("UpdateTimestamp"))
            {
                try
                {
                    DateTime now = DateTime.UtcNow;
                    LastUpdateTimestamp = now;

                    if (Options.WriteTo)
                    {
                        Logging?.LogTrace("Updated LastUpdateTimestamp for local write file poller to {timestamp}", now);
                    }
                    else
                    {
                        Logging?.LogTrace("Updated LastUpdateTimestamp for remote read file poller to {timestamp}", now);
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Watcher file polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Watcher file polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to set last update timestamp: {message}", ex.Message);
                }
            }
        }

        private void WatcherFileReadPollerLoop()
        {
            using (Logging?.BeginScope("WatcherFileReadPollerLoop"))
            {
                HasPanicked = false;
                HasLoopPanic = false;
                HasCurrentMailPriority = true;
                CancellationToken? token = _cancellationTokenSource?.Token;
                if (token.HasValue)
                {
                    Options.Files.ForEach(itm => itm?.SetCancellationToken(token.Value));
                }
                Logging?.LogInformation("Starting watcher file read polling loop");
                try
                {
                    InitializeFiles();

                    while (!HasLoopPanic && !(token?.IsCancellationRequested ?? false))
                    {
                        try
                        {
                            bool filesPanic = false;
                            bool hasCurrentMailPriority = true;
                            UpdateTimestamp();
                            foreach (var singleSwitch in Options.Files)
                            {
                                if (singleSwitch?.ReadFile() ?? false)
                                {
                                    UpdateTimestamp();
                                    CheckThreadCancellation();

                                    filesPanic = true;
                                    if (!(singleSwitch?.HoldBack ?? false))
                                    {
                                        UniversalPanicReason reason = Generator.GetUniversalPanicReason(singleSwitch?.Panic);
                                        Generator.TriggerShutDown(UniversalPanicType.ReaderFile, singleSwitch?.Options.WatcherFileName, reason);

                                        if (Options.CalculatedAutoSafeMode)
                                        {
                                            Logging?.LogInformation("Automatically entering safe mode in poller after panic of watcher files to read because AutoSafeMode was set to true");
                                            EnterSafeMode(false);
                                            SharedData.EnterSafeMode(true);
                                        }
                                    }
                                    else
                                    {
                                        if (Options.CalculatedAutoSafeMode)
                                        {
                                            Logging?.LogDebug("Not entering safe mode in poller after panic of watcher files to read because we are in holdback mode");
                                        }
                                    }

                                    if (singleSwitch?.Options?.MailPriority ?? false)
                                    {
                                        if (singleSwitch?.Panic == WatcherFilePanicReason.Panic)
                                        {
                                            // there's at least one machine with MailPriority responsive, even though it has some sort of panic
                                            hasCurrentMailPriority = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (singleSwitch?.Options?.MailPriority ?? false)
                                    {
                                        if (singleSwitch != null && singleSwitch.CurrentState != WatcherFileState.Stopped &&
                                            (singleSwitch.Panic == WatcherFilePanicReason.Panic || singleSwitch.Panic == WatcherFilePanicReason.NoPanic))
                                        {
                                            // there's at least one machine with MailPriority responsive 
                                            hasCurrentMailPriority = false;
                                        }
                                    }
                                }
                                UpdateTimestamp();
                                CheckThreadCancellation();
                            }
                            HasCurrentMailPriority = hasCurrentMailPriority;
                            UpdateTimestamp();
                            HasPanicked = HasPanicked || filesPanic;

                            if (filesPanic)
                                Logging?.LogWarning("One of the watcher files to read panicked");
                        }
                        catch (OperationCanceledException)
                        {
                            Logging?.LogWarning("Recieved watcher file read polling loop cancelation");
                        }
                        catch (ThreadInterruptedException)
                        {
                            Logging?.LogWarning("Recieved watcher file read polling loop interruption");
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogCritical(ex, "While running watcher file read polling loop, will end watcher file read polling loop due to this error: {message}", ex.Message);
                            HasLoopPanic = HasPanicked = true;
                            HasCurrentMailPriority = true;
                            if (TriggerShutDownOnError)
                                Generator.TriggerShutDown(UniversalPanicType.ReaderFilePoller, ConstantTriggerIDs.ID_GeneralError, UniversalPanicReason.GeneralError);
                        }
                        PollerCount++;
                        Logging?.LogDebug("Watcher file read poller count is now at {pollercount}", PollerCount);
                        if (!(token?.IsCancellationRequested ?? false))
                        {
                            Logging?.LogDebug("Sleeping for {sleepTime} milliseconds before requesting new information on all watcher read files", _sleepTimespan);
                            if (token.HasValue)
                            {
                                if (token.Value.WaitHandle.WaitOne(_sleepTimespan))
                                    Logging?.LogTrace("Watcher file read poller recieved thread cancellation request during sleep");
                            }
                            else
                                Thread.Sleep(_sleepTimespan);
                        }
                    }
                }
                finally
                {
                    Options.Files.ForEach(itm => itm?.RemoveCancellationToken());
                }
                Logging?.LogInformation("Ending watcher file read polling loop");
            }
        }

        private void WatcherFileWritePollerLoop()
        {
            using (Logging?.BeginScope("WatcherFileReadPollerLoop"))
            {
                HasPanicked = false;
                HasLoopPanic = false;
                CancellationToken? token = _cancellationTokenSource?.Token;
                if (token.HasValue)
                {
                    Options.Files.ForEach(itm => itm?.SetCancellationToken(token.Value));
                }
                Logging?.LogInformation("Starting watcher file write polling loop");
                try
                {
                    InitializeFiles();

                    while (!HasLoopPanic && !(token?.IsCancellationRequested ?? false))
                    {
                        try
                        {
                            bool filesPanic = false;
                            UpdateTimestamp();
                            foreach (var singleSwitch in Options.Files)
                            {
                                if (singleSwitch?.WriteFile() ?? false)
                                {
                                    UpdateTimestamp();
                                    CheckThreadCancellation();

                                    filesPanic = true;
                                    if (!(singleSwitch?.HoldBack ?? false))
                                    {
                                        UniversalPanicReason reason = Generator.GetUniversalPanicReason(singleSwitch?.Panic);
                                        Generator.TriggerShutDown(UniversalPanicType.WriterFile, singleSwitch?.Options.WatcherFileName, reason);

                                        if (Options.CalculatedAutoSafeMode)
                                        {
                                            Logging?.LogInformation("Automatically entering safe mode in poller after panic of watcher files to write because AutoSafeMode was set to true");
                                            EnterSafeMode(false);
                                            SharedData.EnterSafeMode(true);
                                        }
                                    }
                                    else
                                    {
                                        if (Options.CalculatedAutoSafeMode)
                                        {
                                            Logging?.LogDebug("Not entering safe mode in poller after panic of watcher files to write because we are in holdback mode");
                                        }
                                    }
                                }
                                UpdateTimestamp();
                                CheckThreadCancellation();
                            }
                            UpdateTimestamp();
                            HasPanicked = HasPanicked || filesPanic;

                            if (filesPanic)
                                Logging?.LogWarning("One of the watcher files to write panicked");
                        }
                        catch (OperationCanceledException)
                        {
                            Logging?.LogWarning("Recieved watcher file write polling loop cancelation");
                        }
                        catch (ThreadInterruptedException)
                        {
                            Logging?.LogWarning("Recieved watcher file write polling loop interruption");
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogCritical(ex, "While running watcher file write polling loop, will end watcher file write polling loop due to this error: {message}", ex.Message);
                            HasLoopPanic = HasPanicked = true;
                            if (TriggerShutDownOnError)
                                Generator.TriggerShutDown(UniversalPanicType.WriterFilePoller, ConstantTriggerIDs.ID_GeneralError, UniversalPanicReason.GeneralError);
                        }
                        PollerCount++;
                        Logging?.LogDebug("Watcher file write poller count is now at {pollercount}", PollerCount);
                        if (!(token?.IsCancellationRequested ?? false))
                        {
                            Logging?.LogDebug("Sleeping for {sleepTime} before requesting new information on all watcher write files", _sleepTimespan);
                            if (token.HasValue)
                            {
                                if (token.Value.WaitHandle.WaitOne(_sleepTimespan))
                                    Logging?.LogTrace("Watcher file write poller recieved thread cancellation request during sleep");
                            }
                            else
                                Thread.Sleep(_sleepTimespan);
                        }
                    }
                }
                finally
                {
                    Options.Files.ForEach(itm => itm?.RemoveCancellationToken());
                }
                Logging?.LogInformation("Ending watcher file write polling loop");
            }
        }

        #endregion

        #region Control methods

        public bool StopPollingThread(bool setStoppedState)
        {
            using (Logging?.BeginScope("StopPollingThread"))
            {
                if (_pollingThread != null)
                {
                    bool blStopMonitoring = StopMonitoring(false, setStoppedState);

                    if (_pollingThread.IsAlive)
                        _cancellationTokenSource?.Cancel();
                    _pollingThread.Join(_sleepTimespan * 2);

                    if (_pollingThread.IsAlive)
                        throw new WatcherPollerException("Watcher file polling loop is still running after shutdown command, this was unexpected");

                    Logging?.LogDebug("Watcher file polling loop ended gracefully");

                    _cancellationTokenSource?.Dispose();
                    _pollingThread = null;
                    _cancellationTokenSource = null;
                    UpdateTimestamp();

                    return blStopMonitoring;
                }
                else
                {
                    return StopMonitoring(false, setStoppedState);
                }
            }
        }

        public bool StartPollingThread()
        {
            using (Logging?.BeginScope("StartPollingThread"))
            {
                if (_pollingThread == null)
                {
                    UpdateTimestamp();
                    PollerCount = 0;
                    _cancellationTokenSource = Generator.GetLinkedCancellationTokenSource();

                    bool blStartMonitoring = StartMonitoring(false);
                    if (blStartMonitoring)
                    {
                        if (Options.WriteTo)
                        {
                            _pollingThread = new Thread(new ThreadStart(WatcherFileWritePollerLoop));
                        }
                        else
                        {
                            _pollingThread = new Thread(new ThreadStart(WatcherFileReadPollerLoop));
                        }
                        _pollingThread.Start();
                        return true;
                    }
                    else
                    {
                        if (Options.WriteTo)
                            Logging?.LogWarning("Cannot start write file polling due to wrong poller mode");
                        else
                            Logging?.LogWarning("Cannot start read file polling due to wrong poller mode");
                        return false;
                    }
                }
                else
                {
                    throw new FritzPollerException("Polling thread already started, please stop thread before starting anew");
                }
            }
        }

        public void InitializeFiles()
        {
            using (Logging?.BeginScope("InitializeFiles"))
            {
                if (Options.WriteTo)
                {
                    Logging?.LogInformation("Initializing watcher write files");
                    Options.Files.ForEach(itm => itm?.WriteInitialStatus());
                }
                else
                {
                    Logging?.LogInformation("Initializing watcher read files");
                    Options.Files.ForEach(itm => itm?.ReadFile(true));
                }
            }
        }

        public void UpdateState(WatcherFileState state)
        {
            using (Logging?.BeginScope("UpdateState"))
            {
                if (Options.WriteTo)
                    Logging?.LogInformation("Updating state for all watcher write files to {state}", Enum.GetName(state));
                else
                    Logging?.LogInformation("Updating state for all watcher read files to {state}", Enum.GetName(state));
                Options.Files.ForEach(itm => itm?.UpdateState(state));
            }
        }

        public void UpdateStateTimestamp()
        {
            using (Logging?.BeginScope("UpdateStateTimestamp"))
            {
                Options.Files.ForEach(itm => itm?.UpdateStateTimestamp());
            }
        }

        public bool ArmPanicMode(bool whatIf)
        {
            using (Logging?.BeginScope("ArmPanicMode"))
            {
                if (Status == PollerStatus.Monitoring || Status == PollerStatus.Holdback)
                {
                    if (CanBeArmed)
                    {
                        if (!whatIf)
                        {
                            if (Options.WriteTo)
                                Logging?.LogInformation("Arming panic mode for all watcher write files");
                            else
                                Logging?.LogInformation("Arming panic mode for all watcher read files");
                            Options?.Files?.ForEach(itm => itm?.ArmPanicMode());
                            Status = PollerStatus.Armed;
                        }
                        else
                        {
                            if (Options.WriteTo)
                                Logging?.LogTrace("Only simulated arming panic mode for all watcher write files");
                            else
                                Logging?.LogTrace("Only simulated arming panic mode for all watcher read files");
                        }

                        return true;
                    }
                    else
                    {
                        if (Options.WriteTo)
                            Logging?.LogWarning("Unable to arm all watcher write files");
                        else
                            Logging?.LogWarning("Unable to arm all watcher read files");

                        if (Options.WriteTo)
                        {
                            if (PollerCount < (Options?.MinPollerCountToArm ?? 0))
                                Logging?.LogWarning("Cannot arm all watcher write files because only {currentcount} of a minimum of {mincount} poller loops have been ran through", PollerCount, (Options?.MinPollerCountToArm ?? 0));
                            if (HasLoopPanic)
                                Logging?.LogWarning("Cannot arm all watcher write files because of loop panic");
                            if (HasPanicked)
                                Logging?.LogWarning("Cannot arm all watcher write files because one of the files panicked");
                        }
                        else
                        {
                            if (PollerCount < (Options?.MinPollerCountToArm ?? 0))
                                Logging?.LogWarning("Cannot arm all watcher read files because only {currentcount} of a minimum of {mincount} poller loops have been ran through", PollerCount, (Options?.MinPollerCountToArm ?? 0));
                            if (HasLoopPanic)
                                Logging?.LogWarning("Cannot arm all watcher read files because of loop panic");
                            if (HasPanicked)
                                Logging?.LogWarning("Cannot arm all watcher read files because one of the switches panicked");
                        }

                        return false;
                    }
                }
                else
                {
                    if (Options.WriteTo)
                        Logging?.LogWarning("Cannot arm panic mode for all watcher write files, must be in monitoring or holdback mode");
                    else
                        Logging?.LogWarning("Cannot arm panic mode for all watcher read files, must be in monitoring or holdback mode");

                    return false;
                }
            }
        }

        public bool DisarmPanicMode(bool whatIf)
        {
            using (Logging?.BeginScope("DisarmPanicMode"))
            {
                if (Status == PollerStatus.Armed || Status == PollerStatus.Holdback || Status == PollerStatus.SafeMode)
                {
                    if (!whatIf)
                    {
                        if (Options.WriteTo)
                            Logging?.LogInformation("Disarming panic mode for all watcher write files");
                        else
                            Logging?.LogInformation("Disarming panic mode for all watcher read files");
                        Options.Files.ForEach(itm => itm?.DisarmPanicMode());
                        Status = PollerStatus.Monitoring;
                    }
                    else
                    {
                        if (Options.WriteTo)
                            Logging?.LogTrace("Only simulated disarming panic mode for all watcher write files");
                        else
                            Logging?.LogTrace("Only simulated disarming panic mode for all watcher read files");
                    }

                    return true;
                }
                else
                {
                    if (Options.WriteTo)
                        Logging?.LogWarning("Cannot disarm panic mode for all watcher write files, must be in armed, holdback or safe mode");
                    else
                        Logging?.LogWarning("Cannot disarm panic mode for all watcher read files, must be in armed, holdback or safe mode");

                    return false;
                }
            }
        }

        public bool StartMonitoring(bool whatIf)
        {
            using (Logging?.BeginScope("StartMonitoring"))
            {
                if (Status == PollerStatus.Stopped)
                {
                    if (!whatIf)
                    {
                        if (Options.WriteTo)
                            Logging?.LogInformation("Start monitoring all watcher write files");
                        else
                            Logging?.LogInformation("Start monitoring all watcher read files");

                        Options.Files.ForEach(itm => itm?.StartMonitoring());
                        Status = PollerStatus.Monitoring;
                    }
                    else
                    {
                        if (Options.WriteTo)
                            Logging?.LogTrace("Only simulated start monitoring for all watcher write files");
                        else
                            Logging?.LogTrace("Only simulated start monitoring for all watcher read files");
                    }

                    return true;
                }
                else
                {
                    if (Options.WriteTo)
                        Logging?.LogWarning("Cannot start monitoring for all watcher write files, must be in stopped mode");
                    else
                        Logging?.LogWarning("Cannot start monitoring for all watcher read files, must be in stopped mode");

                    return false;
                }
            }
        }

        public bool StopMonitoring(bool whatIf, bool setStoppedState)
        {
            using (Logging?.BeginScope("StopMonitoring"))
            {
                if (Status != PollerStatus.Stopped)
                {
                    if (!whatIf)
                    {
                        if (Options.WriteTo)
                            Logging?.LogInformation("Stop monitoring all watcher write files");
                        else
                            Logging?.LogInformation("Stop monitoring all watcher read files");
                        Options.Files.ForEach(itm => itm?.StopMonitoring(setStoppedState));
                        Status = PollerStatus.Stopped;

                        if (Options.WriteTo)
                        {
                            // ignore errors or cancellation token here
                            Options.Files.ForEach(itm => itm?.WriteEndStatus());
                        }
                    }
                    else
                    {
                        if (Options.WriteTo)
                            Logging?.LogTrace("Only simulated stop monitoring for all watcher write files");
                        else
                            Logging?.LogTrace("Only simulated stop monitoring for all watcher read files");
                    }

                    return true;
                }
                else
                {
                    if (Options.WriteTo)
                        Logging?.LogWarning("Cannot stop monitoring for all watcher write files as monitoring didn't start");
                    else
                        Logging?.LogWarning("Cannot stop monitoring for all watcher read files as monitoring didn't start");

                    return false;
                }
            }
        }

        public bool EnterSafeMode(bool whatIf)
        {
            using (Logging?.BeginScope("EnterSafeMode"))
            {
                if (Status == PollerStatus.Armed)
                {
                    if (!whatIf)
                    {
                        if (Options.WriteTo)
                        {
                            Logging?.LogInformation("Entering safe mode for all watcher write files");
                            Logging?.LogWarning("Safe mode is an unlikely scenario for watcher write files");
                        }
                        else
                        {
                            Logging?.LogInformation("Entering safe mode for all watcher read files");
                        }
                        Options.Files.ForEach(itm => itm?.EnterSafeMode());
                        Status = PollerStatus.SafeMode;
                    }
                    else
                    {
                        if (Options.WriteTo)
                            Logging?.LogTrace("Only simulated entering safe mode for all watcher write files");
                        else
                            Logging?.LogTrace("Only simulated entering safe mode for all watcher read files");
                    }

                    return true;
                }
                else
                {
                    if (Status == PollerStatus.SafeMode)
                    {
                        if (Options.WriteTo)
                            Logging?.LogDebug("Already in safe mode for all watcher write files, ignoring new safe mode");
                        else
                            Logging?.LogDebug("Already in safe mode for all watcher read files, ignoring new safe mode");

                        return true;
                    }
                    else
                    {
                        if (Options.WriteTo)
                            Logging?.LogWarning("Cannot enter safe mode for all watcher write files, must be in armed mode");
                        else
                            Logging?.LogWarning("Cannot enter safe mode for all watcher read files, must be in armed mode");

                        return false;
                    }
                }
            }
        }

        public bool EnterHoldBackMode(bool whatIf)
        {
            using (Logging?.BeginScope("EnterHoldBackMode"))
            {
                if (Status == PollerStatus.Monitoring)
                {
                    if (!whatIf)
                    {
                        Logging?.LogDebug("Resetting any panics and poller counts before entering holdback mode for watcher files");
                        ResetPanic();
                        if (Options.WriteTo)
                            Logging?.LogInformation("Entering holdback mode for all watcher write files");
                        else
                            Logging?.LogInformation("Entering holdback mode for all watcher read files");
                        Options.Files.ForEach(itm => itm?.EnterHoldBackMode());
                        Status = PollerStatus.Holdback;
                    }
                    else
                    {
                        if (Options.WriteTo)
                            Logging?.LogTrace("Only simulated entering holdback mode for all watcher write files");
                        else
                            Logging?.LogTrace("Only simulated entering holdback mode for all watcher read files");
                    }

                    return true;
                }
                else
                {
                    if (Options.WriteTo)
                        Logging?.LogWarning("Cannot enter holdback mode for all watcher write files, must have started monitoring");
                    else
                        Logging?.LogWarning("Cannot enter holdback mode for all watcher read files, must have started monitoring");

                    return false;
                }
            }
        }

        public bool ResetPanic()
        {
            using (Logging?.BeginScope("ResetPanic"))
            {
                if (Options.WriteTo)
                    Logging?.LogDebug("Resetting panics and poller count for all watcher write files");
                else
                    Logging?.LogDebug("Resetting panics and poller count for all watcher read files");

                bool blPanicReset = false;
                if (HasPanicked)
                {
                    blPanicReset = true;
                    HasPanicked = false;
                }
                if (HasLoopPanic)
                {
                    blPanicReset = true;
                    HasLoopPanic = false;
                }
                Options?.Files?.ForEach(itm => blPanicReset = (itm?.ResetPanic() ?? false) || blPanicReset);
                PollerCount = 0;
                return blPanicReset;
            }
        }

        #endregion
    }
}
