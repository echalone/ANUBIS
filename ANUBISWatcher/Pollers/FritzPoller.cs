using ANUBISFritzAPI;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Pollers
{
    public class FritzPoller
    {
        #region Fields

        #region Constants

        private readonly object lock_TimestampLastUpdate = new();
        private readonly object lock_PollerCount = new();
        private readonly object lock_PollerStatus = new();
        private const long c_maxSwitchAdditionalTimeInMilliseconds = 1000;
        private const long c_maxCommandsWithLoginPerSwitch = 3;
        private const long c_maxNormalCommandsPerSwitch = 0;
        private const long c_maxTurnOffCommandsPerSwitch = 1;
        private const long c_maxCommandsPerLogin = 3;

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

        public FritzPollerOptions Options { get; init; }


        private volatile bool _hasPanicked = false;
        public bool HasPanicked { get { return _hasPanicked; } private set { _hasPanicked = value; } }

        private volatile bool _hasLoopPanic = false;
        public bool HasLoopPanic { get { return _hasLoopPanic; } private set { _hasLoopPanic = value; } }

        /// <summary>
        /// Is the system shut down according to this poller?
        /// </summary>
        private volatile bool _hasShutDown = false;
        public bool HasShutDown { get { return _hasShutDown; } private set { _hasShutDown = value; } }

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
                         (!(Options?.Switches?.Any(itm => !itm.CanBeArmed) ?? false)); // make sure switches don't have panic and are in correct mode
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
                    Logging?.LogCritical(ex, "While trying to check for unresponsive Fritz poller: {message}", ex.Message);
                    Logging?.LogDebug("Mimicking unresponsive behaviour so we catch this problem");
                    return true;
                }
            }
        }

        #endregion

        #region Constructors

        public FritzPoller(FritzPollerOptions options)
        {
            Options = options;
            _lockTimeout = TimeSpan.FromMilliseconds(Options.LockTimeoutInMilliseconds);
            _sleepTimespan = TimeSpan.FromMilliseconds(Options.SleepTimeInMilliseconds);
            InitDefaultAlertTimeInMilliseconds();
        }

        #endregion

        #region Helper methods

        private static long OneCommandMaxMilliseconds(FritzAPIOptions apiOptions, int switchCount, bool withReloadNameCommand = true)
        {
            var commandTime = c_maxSwitchAdditionalTimeInMilliseconds + (apiOptions.CommandTimeoutSeconds * 1000);
            return (apiOptions.CheckLoginBeforeCommands ?
                        OneLoginCommandMaxMilliseconds(apiOptions, 1) // per command there is one check login command possible
                        : 0
                    ) + (((apiOptions.AutoRetryMinWaitMilliseconds +
                        apiOptions.AutoRetryWaitSpanMilliseconds +
                        commandTime) * apiOptions.AutoRetryCount) +
                            commandTime) +
                            (withReloadNameCommand && apiOptions.ReloadNamesIfNotFound ?
                                ((1 + switchCount) * OneCommandMaxMilliseconds(apiOptions, switchCount, false)) : 0); // name reload, plus one name lookup per found switch
        }

        private static long OneCommandWithLoginsMaxMilliseconds(FritzAPIOptions apiOptions, int switchCount, bool withReloadNameCommand = true)
        {

            return (apiOptions.AutoLogin ?
                        OneLoginCommandMaxMilliseconds(apiOptions, c_maxCommandsPerLogin) // per command there are two login command tries possible
                        : 0
                    ) +
                    OneCommandMaxMilliseconds(apiOptions, switchCount, withReloadNameCommand); // and then there's the command itself
        }

        private static long OneLoginCommandMaxMilliseconds(FritzAPIOptions? apiOptions, long loginCommandCount)
        {
            if (apiOptions != null)
            {
                var commandTime = c_maxSwitchAdditionalTimeInMilliseconds + (apiOptions.LoginTimeoutSeconds * 1000);
                return (loginCommandCount * (((apiOptions.AutoRetryMinWaitMilliseconds +
                                                    apiOptions.AutoRetryWaitSpanMilliseconds +
                                                    commandTime) * apiOptions.AutoRetryCount) +
                                                        commandTime));
            }
            else
            {
                return 0;
            }
        }

        private void InitDefaultAlertTimeInMilliseconds()
        {
            using (Logging?.BeginScope("InitDefaultAlertTimeInMilliseconds"))
            {
                var switchOptions = Options.Switches.FirstOrDefault()?.Options;
                var anyTurnOffPossibility = Options.Switches.Any(itm => itm.Options.TurnOffOnPanic || itm.Options.TurnOffOnLowPower);
                var apiOptions = switchOptions?.Api.Options;
                _defaultAlertTimeInMilliseconds = ((Options.SleepTimeInMilliseconds * 3) +
                                                        (apiOptions != null && switchOptions != null ?
                                                            (
                                                                (c_maxCommandsWithLoginPerSwitch * OneCommandWithLoginsMaxMilliseconds(apiOptions, Options.Switches.Count)) +
                                                                (c_maxNormalCommandsPerSwitch * OneCommandMaxMilliseconds(apiOptions, Options.Switches.Count)) +
                                                                (anyTurnOffPossibility ?
                                                                    (c_maxTurnOffCommandsPerSwitch * OneCommandWithLoginsMaxMilliseconds(apiOptions, Options.Switches.Count)) : 0)
                                                            ) : 0
                                                        )
                                                    );
                //+ (c_maxLoginCommandsPerCycle * OneLoginCommandMaxMilliseconds(Options.Switches.FirstOrDefault()?.Options.Api.Options));

                if (Options.AlertTimeInMilliseconds.HasValue)
                {
                    Logging?.LogInformation("Using the set alert time of {alerttime} milliseconds for Fritz poller. One poller cycle must not exceed this time or it will be considered stuck.", Options.AlertTimeInMilliseconds.Value);
                }
                else
                {
                    Logging?.LogInformation("Calculated a default alert time of {defaultalerttime} milliseconds for Fritz poller. One poller cycle must not exceed this time or it will be considered stuck.", _defaultAlertTimeInMilliseconds);
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

                    Logging?.LogTrace("Updated LastUpdateTimestamp for Fritz poller to {timestamp}", now);
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Fritz switch polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Fritz switch polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to set last update timestamp: {message}", ex.Message);
                }
            }
        }

        private void FritzPollerLoop()
        {
            using (Logging?.BeginScope("FritzPollerLoop"))
            {
                HasPanicked = false;
                HasLoopPanic = false;
                CancellationToken? token = _cancellationTokenSource?.Token;
                if (token.HasValue)
                {
                    Options.Switches.ForEach(itm => itm?.SetCancellationToken(token.Value));
                }
                Logging?.LogInformation("Starting Fritz polling loop");
                try
                {
                    while (!HasLoopPanic && !(token?.IsCancellationRequested ?? false))
                    {
                        try
                        {
                            bool switchesPanic = false;
                            bool hasAnyShutDown = false;
                            UpdateTimestamp();
                            foreach (var singleSwitch in Options.Switches)
                            {
                                if (singleSwitch?.CheckSwitch() ?? false)
                                {
                                    UpdateTimestamp();
                                    CheckThreadCancellation();

                                    switchesPanic = true;
                                    if (!(singleSwitch?.HoldBack ?? false))
                                    {
                                        UniversalPanicReason reason = Generator.GetUniversalPanicReason(singleSwitch?.Panic);
                                        Generator.TriggerShutDown(UniversalPanicType.FritzSwitch, singleSwitch?.Options.SwitchName, reason);

                                        if (Options.AutoSafeMode)
                                        {
                                            Logging?.LogInformation("Automatically entering safe mode in poller after panic of Fritz switches because AutoSafeMode was set to true");
                                            EnterSafeMode(false);
                                            SharedData.EnterSafeMode(true);
                                        }
                                    }
                                    else
                                    {
                                        if (Options.AutoSafeMode)
                                        {
                                            Logging?.LogDebug("Not entering safe mode in poller after panic of Fritz switch because we are in holdback mode");
                                        }
                                    }
                                }
                                hasAnyShutDown = hasAnyShutDown || (singleSwitch?.HasShutDown ?? false);
                                if ((singleSwitch?.HasShutDown ?? false) && !HasShutDown)
                                {
                                    Logging?.LogDebug("Changing fritz poller shutdown state from not-shutdown to shutdown according to fritz switch {name}", singleSwitch?.Options.SwitchName);
                                    HasShutDown = true;
                                }
                                UpdateTimestamp();
                                CheckThreadCancellation();
                            }
                            UpdateTimestamp();

                            if (!hasAnyShutDown && HasShutDown)
                            {
                                Logging?.LogDebug("Changing fritz poller shutdown state from shutdown to not-shutdown because non of the switches is reporting a shutdown state any more");
                                HasShutDown = false;
                            }

                            UpdateTimestamp();
                            CheckThreadCancellation();

                            HasPanicked = HasPanicked || switchesPanic;

                            if (switchesPanic)
                                Logging?.LogWarning("One of the Fritz switches panicked");
                        }
                        catch (OperationCanceledException)
                        {
                            Logging?.LogDebug("Recieved Fritz polling loop cancelation");
                        }
                        catch (ThreadInterruptedException)
                        {
                            Logging?.LogDebug("Recieved Fritz polling loop interruption");
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogCritical(ex, "While running Fritz polling loop, will end Fritz polling loop due to this error: {message}", ex.Message);
                            HasLoopPanic = HasPanicked = true;
                            if (TriggerShutDownOnError)
                                Generator.TriggerShutDown(UniversalPanicType.FritzPoller, ConstantTriggerIDs.ID_GeneralError, UniversalPanicReason.GeneralError);
                        }
                        PollerCount++;
                        Logging?.LogDebug("Fritz poller count is now at {pollercount}", PollerCount);
                        if (!(token?.IsCancellationRequested ?? false))
                        {
                            Logging?.LogDebug("Sleeping for {sleepTime} before requesting new information on all Fritz switches", _sleepTimespan);
                            if (token.HasValue)
                            {
                                if (token.Value.WaitHandle.WaitOne(_sleepTimespan))
                                    Logging?.LogTrace("Fritz poller recieved thread cancellation request during sleep");
                            }
                            else
                                Thread.Sleep(_sleepTimespan);
                        }
                    }
                }
                finally
                {
                    Options.Switches.ForEach(itm => itm?.RemoveCancellationToken());
                }
                Logging?.LogInformation("Ending Fritz polling loop");
            }
        }

        #endregion

        #region Control methods

        public bool StopPollingThread()
        {
            using (Logging?.BeginScope("StopPollingThread"))
            {
                if (_pollingThread != null)
                {
                    bool blStopMonitoring = StopMonitoring(false);
                    if (_pollingThread.IsAlive)
                        _cancellationTokenSource?.Cancel();
                    _pollingThread.Join(_sleepTimespan * 2);

                    if (_pollingThread.IsAlive)
                        throw new FritzPollerException("Fritz polling loop is still running after shutdown command, this was unexpected");

                    Logging?.LogDebug("Fritz polling loop ended gracefully");

                    _cancellationTokenSource?.Dispose();
                    _pollingThread = null;
                    _cancellationTokenSource = null;
                    UpdateTimestamp();

                    return blStopMonitoring;
                }
                else
                {
                    return StopMonitoring(false);
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
                        _pollingThread = new Thread(new ThreadStart(FritzPollerLoop));
                        _pollingThread.Start();
                        return true;
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot start Fritz switch polling due to wrong poller mode");
                        return false;
                    }
                }
                else
                {
                    throw new FritzPollerException("Polling thread already started, please stop thread before starting anew");
                }
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
                            Logging?.LogInformation("Arming panic mode for Fritz switches");
                            Options?.Switches?.ForEach(itm => itm?.ArmPanicMode());
                            Status = PollerStatus.Armed;
                        }
                        else
                        {
                            Logging?.LogTrace("Only simulated arming panic mode for Fritz switches");
                        }

                        return true;
                    }
                    else
                    {
                        Logging?.LogWarning("Unable to arm Fritz switches");

                        if (PollerCount < (Options?.MinPollerCountToArm ?? 0))
                            Logging?.LogWarning("Cannot arm Fritz switches because only {currentcount} of a minimum of {mincount} poller loops have been ran through", PollerCount, (Options?.MinPollerCountToArm ?? 0));
                        if (HasLoopPanic)
                            Logging?.LogWarning("Cannot arm Fritz switches because of loop panic");
                        if (HasPanicked)
                            Logging?.LogWarning("Cannot arm Fritz switches because one of the switches panicked");

                        return false;
                    }
                }
                else
                {
                    Logging?.LogWarning("Cannot arm panic mode for Fritz switches, must be in monitoring or holdback mode");

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
                        Logging?.LogInformation("Disarming panic mode for Fritz switches");
                        Options.Switches.ForEach(itm => itm?.DisarmPanicMode());
                        Status = PollerStatus.Monitoring;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated disarming panic mode for Fritz switches");
                    }

                    return true;
                }
                else
                {
                    Logging?.LogWarning("Cannot disarm panic mode for Fritz switches, must be in armed, holdback or safe mode");

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
                        Logging?.LogInformation("Start monitoring all Fritz switches");
                        Options.Switches.ForEach(itm => itm?.StartMonitoring());
                        Status = PollerStatus.Monitoring;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated start monitoring of all Fritz switches");
                    }

                    return true;
                }
                else
                {
                    Logging?.LogWarning("Cannot start monitoring for all Fritz switches, must be in stopped mode");

                    return false;
                }
            }
        }

        public bool StopMonitoring(bool whatIf)
        {
            using (Logging?.BeginScope("StopMonitoring"))
            {
                if (Status != PollerStatus.Stopped)
                {
                    if (!whatIf)
                    {
                        Logging?.LogInformation("Stop monitoring all Fritz switches");
                        Options.Switches.ForEach(itm => itm?.StopMonitoring());
                        Status = PollerStatus.Stopped;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated stop monitoring for all Fritz switches");
                    }

                    return true;
                }
                else
                {
                    Logging?.LogWarning("Cannot stop monitoring for all Fritz switches as monitoring didn't start");

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
                        Logging?.LogInformation("Entering safe mode for Fritz switches");
                        Options.Switches.ForEach(itm => itm?.EnterSafeMode());
                        Status = PollerStatus.SafeMode;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated entering safe mode for Fritz switches");
                    }

                    return true;
                }
                else
                {
                    if (Status == PollerStatus.SafeMode)
                    {
                        Logging?.LogDebug("Already in safe mode for Fritz switches, ignoring new safe mode");

                        return true;
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot enter safe mode for Fritz switches, must be in armed mode");

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
                        Logging?.LogDebug("Resetting any panics and poller counts before entering holdback mode for Fritz switches");
                        ResetPanic();
                        Logging?.LogInformation("Entering holdback mode for Fritz switches");
                        Options.Switches.ForEach(itm => itm?.EnterHoldBackMode());
                        Status = PollerStatus.Holdback;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated entering holdback mode for Fritz switches");
                    }

                    return true;
                }
                else
                {
                    Logging?.LogWarning("Cannot enter holdback mode for Fritz switch, must have started monitoring");

                    return false;
                }
            }
        }

        public bool ResetPanic()
        {
            using (Logging?.BeginScope("ResetPanic"))
            {
                Logging?.LogDebug("Resetting panics and poller count for Fritz switches");
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
                Options?.Switches?.ForEach(itm => blPanicReset = (itm?.ResetPanic() ?? false) || blPanicReset);
                PollerCount = 0;
                return blPanicReset;
            }
        }

        #endregion
    }
}
