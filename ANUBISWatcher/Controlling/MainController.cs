using ANUBISWatcher.Entities;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using ANUBISWatcher.Pollers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Controlling
{
    public class MainController
    {
        #region Fields

        #region Constants

        private readonly object lock_TimestampLastUpdate = new();

        #endregion

        #region Unchangable fields

        private readonly TimeSpan _sleepTimespan = TimeSpan.Zero;
        private readonly TimeSpan _lockTimeout = TimeSpan.Zero;

        #endregion

        #region Changable fields

        private Thread? _loopThread = null;
        private CancellationTokenSource? _cancellationTokenSource = null;
        private DateTime _timestampLastUpdate;
        private volatile bool _isAlive = false;
        private volatile bool _isInSafeMode = false;
        private volatile bool _hasShutDown = false;
        private volatile bool _hasShutDownVerified = false;
        private volatile bool _areRemoteFilesInSafeMode = false;
        private volatile bool _mailSendableAccordingToController = false;
        private volatile bool _mailSendingDisabledManually = false;

        #endregion

        #endregion

        #region Properties

        public MainControllerOptions Options { get; init; }

        public bool IsAlive { get { return _isAlive; } private set { _isAlive = value; } }
        public bool IsInSafeMode { get { return _isInSafeMode; } private set { _isInSafeMode = value; } }
        public bool AreRemoteFilesInSafeMode { get { return _areRemoteFilesInSafeMode; } private set { _areRemoteFilesInSafeMode = value; } }

        private ILogger? Logging { get { return Options?.Logger; } }

        public CountdownPoller? Poller_Countdown { get; private init; }
        public FritzPoller? Poller_Fritz { get; private init; }
        public SwitchBotPoller? Poller_SwitchBot { get; private init; }
        public ClewarePoller? Poller_ClewareUSB { get; private init; }
        public WatcherFilePoller? Poller_ReadFiles { get; private init; }
        public WatcherFilePoller? Poller_WriteFiles { get; private init; }

        private bool MailSendableAccordingToController { get { return _mailSendableAccordingToController; } set { _mailSendableAccordingToController = value; } }
        public bool MailSendingDisabledManually { get { return _mailSendingDisabledManually; } set { _mailSendingDisabledManually = value; } }
        public bool HasShutDown { get { return _hasShutDown; } private set { _hasShutDown = value; } }
        public bool HasShutDownVerified { get { return _hasShutDownVerified; } private set { _hasShutDownVerified = value; } }

        /// <summary>
        /// Is mail sending possible according to state, time of system shutdown, manually being disabled and other responsive machines
        /// </summary>
        public bool MailSendingPossible
        {
            get
            {
                return !MailSendingDisabledManually &&
                        (Poller_ReadFiles?.HasCurrentMailPriority ?? true) &&
                        MailSendableAccordingToController;
            }
        }

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

        public long AlertTimeInMilliseconds
        {
            get
            {
                return Options.AlertTimeInMilliseconds ?? (Options.SleepTimeInMilliseconds * 30);
            }
        }

        public long MillisecondsSinceLastUpdate
        {
            get
            {
                return (long)Math.Floor((DateTime.UtcNow - LastUpdateTimestamp).TotalMilliseconds);
            }
        }

        public bool IsControllerUnresponsive
        {
            get
            {
                try
                {
                    return MillisecondsSinceLastUpdate > AlertTimeInMilliseconds;
                }
                catch (Exception ex)
                {
                    Logging?.LogCritical(ex, "While trying to check for unresponsive main controller: {message}", ex.Message);
                    Logging?.LogWarning("Mimicking unresponsive behaviour so we catch this problem");
                    return true;
                }
            }
        }

        public static bool TriggerShutDownOnError
        {
            get
            {
                ControllerStatus statusController = SharedData.CurrentControllerStatus;
                return (statusController == ControllerStatus.Armed || statusController == ControllerStatus.SafeMode ||
                        statusController == ControllerStatus.ShutDown || statusController == ControllerStatus.Triggered); // make sure controller is in correct mode
            }
        }

        public static bool CanChangeShutDown
        {
            get
            {
                ControllerStatus statusController = SharedData.CurrentControllerStatus;
                return statusController == ControllerStatus.Armed ||
                        statusController == ControllerStatus.SafeMode ||
                        statusController == ControllerStatus.ShutDown;

            }
        }

        public bool HasPanicked
        {
            get
            {
                return (Poller_Fritz?.HasPanicked ?? false) ||
                         (Poller_ClewareUSB?.HasPanicked ?? false) ||
                         (Poller_Countdown?.HasPanicked ?? false) ||
                         (Poller_ReadFiles?.HasPanicked ?? false) ||
                         (Poller_SwitchBot?.HasPanicked ?? false) ||
                         (Poller_WriteFiles?.HasPanicked ?? false);
            }
        }

        public bool? CanBeArmed
        {
            get
            {
                ControllerStatus statusController = SharedData.CurrentControllerStatus;
                return (statusController == ControllerStatus.Monitoring ||
                        statusController == ControllerStatus.Holdback) ?
                        ((Poller_Fritz?.CanBeArmed ?? true) &&
                         (Poller_ClewareUSB?.CanBeArmed ?? true) &&
                         (Poller_Countdown?.CanBeArmed ?? true) &&
                         (Poller_ReadFiles?.CanBeArmed ?? true) &&
                         (Poller_SwitchBot?.CanBeArmed ?? true) &&
                         (Poller_WriteFiles?.CanBeArmed ?? true)) :
                        null;

            }
        }

        public List<FritzPollerSwitch>? Switches_Fritz
        {
            get
            {
                return Poller_Fritz?.Options?.Switches;
            }
        }

        public List<ClewarePollerSwitch>? Switches_ClewareUSB
        {
            get
            {
                return Poller_ClewareUSB?.Options?.Switches;
            }
        }

        public List<SwitchBotPollerSwitch>? Switches_SwitchBot
        {
            get
            {
                return Poller_SwitchBot?.Options?.Switches;
            }
        }

        public List<WatcherPollerFile>? Files_ReadRemote
        {
            get
            {
                return Poller_ReadFiles?.Options?.Files;
            }
        }

        public List<WatcherPollerFile>? Files_WriteLocal
        {
            get
            {
                return Poller_WriteFiles?.Options?.Files;
            }
        }

        #endregion

        #region Constructors

        public MainController(MainControllerOptions options)
        {
            Options = options;
            _lockTimeout = TimeSpan.FromMilliseconds(Options.LockTimeoutInMilliseconds);
            _sleepTimespan = TimeSpan.FromMilliseconds(Options.SleepTimeInMilliseconds);

            Poller_Countdown = Generator.GetCountdownPoller();
            Poller_ClewareUSB = Generator.GetClewarePoller();
            Poller_Fritz = Generator.GetFritzPoller();
            Poller_ReadFiles = Generator.GetReadFilePoller();
            Poller_WriteFiles = Generator.GetWriteFilePoller();
            Poller_SwitchBot = Generator.GetSwitchBotPoller();

            SharedData.Reset();
        }

        #endregion

        #region Helper methods

        private void ResetFlags()
        {
            MailSendableAccordingToController = false;
            IsInSafeMode = false;
            AreRemoteFilesInSafeMode = false;
            HasShutDown = false;
            HasShutDownVerified = false;
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
                    LastUpdateTimestamp = DateTime.UtcNow;
                    Poller_WriteFiles?.UpdateStateTimestamp();
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Main controller thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Main controller thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to set last update timestamp: {message}", ex.Message);
                }
            }
        }

        private void MainControllerLoop()
        {
            using (Logging?.BeginScope("MainControllerLoop"))
            {
                Logging?.LogInformation("Starting main controller loop");
                bool hadError = false;

                try
                {
                    IsAlive = true;
                    CancellationToken? token = _cancellationTokenSource?.Token;
                    ResetFlags();
                    SharedData.Reset();
                    SharedData.InitHistories();
                    bool blIsStartable;
                    ControllerStatus _stateBeforeShutDown = SharedData.CurrentControllerStatus;

                    try
                    {
                        blIsStartable = (Poller_Countdown?.StartPollingThread() ?? true) &&
                                        (Poller_ClewareUSB?.StartPollingThread() ?? true) &&
                                        (Poller_Fritz?.StartPollingThread() ?? true) &&
                                        (Poller_ReadFiles?.StartPollingThread() ?? true) &&
                                        (Poller_WriteFiles?.StartPollingThread() ?? true) &&
                                        (Poller_SwitchBot?.StartPollingThread() ?? true);
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogDebug("Recieved main controller loop cancelation");
                        blIsStartable = false;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogDebug("Recieved main controller loop interruption");
                        blIsStartable = false;
                    }
                    catch (Exception ex)
                    {
                        Logging?.LogCritical(ex, "While trying to start main controller loop, will not start main controller loop because of this error: {message}", ex.Message);
                        blIsStartable = false;
                        // if in armed or safe mode or something above: trigger general error trigger

                    }

                    if (blIsStartable)
                    {
                        SharedData.AddControllerStatusHistory(ControllerStatus.Monitoring);
                        Logging?.LogInformation("Main controller is reporting: Now monitoring");

                        while (!(token?.IsCancellationRequested ?? false))
                        {
                            try
                            {
                                CheckThreadCancellation();
                                UpdateTimestamp();

                                bool _newShutDownValue = false;
                                bool _newShutDownVerifiedValue = false;

                                if (Poller_Countdown?.IsPollerUnresponsive ?? false)
                                {
                                    if (TriggerShutDownOnError)
                                    {
                                        Logging?.LogWarning("Triggering system shutdown because of unresponsive countdown poller");
                                        Generator.TriggerShutDown(UniversalPanicType.ControllerPollers, ConstantTriggerIDs.ID_Poller_Countdown, UniversalPanicReason.Unresponsive);
                                    }
                                    Logging?.LogCritical("Countdown poller is unresponsive!");
                                }

                                CheckThreadCancellation();
                                UpdateTimestamp();

                                if (Poller_ClewareUSB?.IsPollerUnresponsive ?? false)
                                {
                                    if (TriggerShutDownOnError)
                                    {
                                        Logging?.LogWarning("Triggering system shutdown because of unresponsive Cleware USB poller");
                                        Generator.TriggerShutDown(UniversalPanicType.ControllerPollers, ConstantTriggerIDs.ID_Poller_ClewareUSB, UniversalPanicReason.Unresponsive);
                                    }
                                    Logging?.LogCritical("Cleware USB poller is unresponsive!");
                                }

                                _newShutDownValue = _newShutDownValue || (Poller_ClewareUSB?.HasShutDown ?? false);
                                _newShutDownVerifiedValue = _newShutDownVerifiedValue ||
                                                                ((Poller_ClewareUSB?.HasShutDown ?? false) &&
                                                                    ((Poller_ClewareUSB?.Status ?? PollerStatus.Stopped) == PollerStatus.SafeMode));

                                CheckThreadCancellation();
                                UpdateTimestamp();

                                if (Poller_Fritz?.IsPollerUnresponsive ?? false)
                                {
                                    if (TriggerShutDownOnError)
                                    {
                                        Logging?.LogWarning("Triggering system shutdown because of unresponsive fritz poller");
                                        Generator.TriggerShutDown(UniversalPanicType.ControllerPollers, ConstantTriggerIDs.ID_Poller_Fritz, UniversalPanicReason.Unresponsive);
                                    }
                                    Logging?.LogCritical("Fritz poller is unresponsive!");
                                }

                                _newShutDownValue = _newShutDownValue || (Poller_Fritz?.HasShutDown ?? false);
                                _newShutDownVerifiedValue = _newShutDownVerifiedValue ||
                                                                ((Poller_Fritz?.HasShutDown ?? false) &&
                                                                    ((Poller_Fritz?.Status ?? PollerStatus.Stopped) == PollerStatus.SafeMode));

                                CheckThreadCancellation();
                                UpdateTimestamp();

                                if (Poller_SwitchBot?.IsPollerUnresponsive ?? false)
                                {
                                    if (TriggerShutDownOnError)
                                    {
                                        Logging?.LogWarning("Triggering system shutdown because of unresponsive switch bot poller");
                                        Generator.TriggerShutDown(UniversalPanicType.ControllerPollers, ConstantTriggerIDs.ID_Poller_SwitchBot, UniversalPanicReason.Unresponsive);
                                    }
                                    Logging?.LogCritical("Switch bot poller is unresponsive!");
                                }

                                _newShutDownValue = _newShutDownValue || (Poller_SwitchBot?.HasShutDown ?? false);
                                _newShutDownVerifiedValue = _newShutDownVerifiedValue ||
                                                                ((Poller_SwitchBot?.HasShutDown ?? false) &&
                                                                    ((Poller_SwitchBot?.Status ?? PollerStatus.Stopped) == PollerStatus.SafeMode));

                                CheckThreadCancellation();
                                UpdateTimestamp();

                                if (Poller_ReadFiles?.IsPollerUnresponsive ?? false)
                                {
                                    if (TriggerShutDownOnError)
                                    {
                                        Logging?.LogWarning("Triggering system shutdown because of unresponsive WatchReadFiles poller");
                                        Generator.TriggerShutDown(UniversalPanicType.ControllerPollers, ConstantTriggerIDs.ID_Poller_ReadFile, UniversalPanicReason.Unresponsive);
                                    }
                                    Logging?.LogCritical("WatchReadFiles poller is unresponsive!");
                                }

                                CheckThreadCancellation();
                                UpdateTimestamp();

                                if (Poller_WriteFiles?.IsPollerUnresponsive ?? false)
                                {
                                    if (TriggerShutDownOnError)
                                    {
                                        Logging?.LogWarning("Triggering system shutdown because of unresponsive WatchWriteFiles poller");
                                        Generator.TriggerShutDown(UniversalPanicType.ControllerPollers, ConstantTriggerIDs.ID_Poller_WriteFile, UniversalPanicReason.Unresponsive);
                                    }
                                    Logging?.LogCritical("WatchWriteFiles poller is unresponsive!");
                                }

                                CheckThreadCancellation();
                                UpdateTimestamp();

                                if (CanChangeShutDown)
                                {
                                    if (_newShutDownValue != (SharedData.CurrentControllerStatus == ControllerStatus.ShutDown))
                                    {
                                        if (_newShutDownValue)
                                        {
                                            Logging?.LogInformation("Changing into ShutDown state for system shutdown due to one of the switches changing its off/on state");
                                            _stateBeforeShutDown = SharedData.CurrentControllerStatus;
                                            SharedData.AddControllerStatusHistory(ControllerStatus.ShutDown);
                                        }
                                        else
                                        {
                                            // change back to old armed/safe mode state if we weren't in triggered state before
                                            Logging?.LogInformation("Changing from ShutDown state for system shutdown back to old state {stateBeforeShutDown} due to one of the switches changing its off/on state", _stateBeforeShutDown);
                                            SharedData.AddControllerStatusHistory(_stateBeforeShutDown);
                                        }
                                    }
                                }

                                CheckThreadCancellation();
                                UpdateTimestamp();

                                if (HasShutDown)
                                {
                                    ControllerStatus controllerStatus = SharedData.CurrentControllerStatus;
                                    if (controllerStatus != ControllerStatus.ShutDown &&
                                            controllerStatus != ControllerStatus.Triggered)
                                    {
                                        HasShutDown = false;
                                    }
                                }
                                else
                                {
                                    ControllerStatus controllerStatus = SharedData.CurrentControllerStatus;
                                    if (controllerStatus == ControllerStatus.ShutDown ||
                                            controllerStatus == ControllerStatus.Triggered)
                                    {
                                        HasShutDown = true;
                                    }
                                }

                                if (!HasShutDownVerified && (Poller_Countdown?.CountdownTriggered ?? false))
                                {

                                    ControllerStatus controllerStatus = SharedData.CurrentControllerStatus;
                                    if (controllerStatus == ControllerStatus.ShutDown ||
                                            controllerStatus == ControllerStatus.Triggered)
                                    {
                                        if (_newShutDownVerifiedValue)
                                        {
                                            HasShutDownVerified = true;
                                        }
                                    }
                                }

                                if (MailSendableAccordingToController)
                                {
                                    ControllerStatus controllerStatus = SharedData.CurrentControllerStatus;
                                    if (Options.SendMailEarliestAfterMinutes <= 0 ||
                                            (controllerStatus != ControllerStatus.ShutDown &&
                                                controllerStatus != ControllerStatus.Triggered))
                                    {
                                        MailSendableAccordingToController = false;
                                        Logging?.LogInformation("Disabled mail sending in main controller again due to state or change in options");
                                    }
                                    else
                                    {
                                        DateTime? tsShutDown = SharedData.ShutDownTimestamp;

                                        if (!tsShutDown.HasValue || tsShutDown.Value.AddMinutes(Options.SendMailEarliestAfterMinutes) >= DateTime.UtcNow)
                                        {
                                            MailSendableAccordingToController = false;
                                            Logging?.LogInformation("Disabled mail sending in main controller again due to system shutdown timestamp");
                                        }
                                    }
                                }
                                else
                                {
                                    if (Options.SendMailEarliestAfterMinutes > 0)
                                    {
                                        ControllerStatus controllerStatus = SharedData.CurrentControllerStatus;
                                        if (controllerStatus == ControllerStatus.ShutDown || controllerStatus == ControllerStatus.Triggered)
                                        {
                                            DateTime? tsShutDown = SharedData.ShutDownTimestamp;
                                            if (tsShutDown.HasValue && tsShutDown.Value.AddMinutes(Options.SendMailEarliestAfterMinutes) < DateTime.UtcNow)
                                            {
                                                MailSendableAccordingToController = true;
                                                Logging?.LogInformation("Mail sending enabled in main controller");
                                            }
                                        }
                                    }
                                }

                                CheckThreadCancellation();
                                UpdateTimestamp();
                            }
                            catch (OperationCanceledException)
                            {
                                Logging?.LogDebug("Recieved main controller loop cancelation");
                            }
                            catch (ThreadInterruptedException)
                            {
                                Logging?.LogDebug("Recieved main controller loop interruption");
                            }
                            catch (Exception ex)
                            {
                                // if in armed or safe mode or something above: trigger general error trigger
                                Logging?.LogCritical(ex, "While running main controller loop, may trigger system shutdown because of this error: {message}", ex.Message);
                                if (TriggerShutDownOnError)
                                {
                                    Generator.TriggerShutDown(UniversalPanicType.Controller, ConstantTriggerIDs.ID_GeneralError, UniversalPanicReason.Error);
                                    Logging?.LogWarning("Triggering system shutdown due to error starting main controller while in armed mode or above");
                                }
                            }

                            if (!(token?.IsCancellationRequested ?? false))
                            {
                                if (token.HasValue)
                                {
                                    if (token.Value.WaitHandle.WaitOne(_sleepTimespan))
                                        Logging?.LogTrace("Main controller loop recieved thread cancellation request during sleep");
                                }
                                else
                                    Thread.Sleep(_sleepTimespan);
                            }
                        }
                    }
                    else
                    {
                        Logging?.LogError("Cannot start main controller loop because of an error trying to start up one of the pollers");
                        if (TriggerShutDownOnError)
                        {
                            Generator.TriggerShutDown(UniversalPanicType.ControllerPollers, ConstantTriggerIDs.ID_PollerStartup, UniversalPanicReason.GeneralError);
                            Logging?.LogWarning("Triggering system shutdown due to error starting main controller while in armed mode or above");
                        }
                        else
                        {
                            SharedData.AddControllerStatusHistory(ControllerStatus.Stopped);
                            Logging?.LogInformation("Main controller is reporting: Now stopped");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Main controller thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Main controller thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    hadError = true; // in case of an error, do not set write file status to stopped but leave as is and 
                    Logging?.LogError(ex, "Unexpected error in main controller loop that ended the loop, may trigger system shutdown because of this error: {message}", ex.Message);
                    if (TriggerShutDownOnError)
                    {
                        Generator.TriggerShutDown(UniversalPanicType.Controller, ConstantTriggerIDs.ID_GeneralError, UniversalPanicReason.GeneralError);
                        Logging?.LogWarning("Triggering system shutdown due to error in main controller while in armed mode or above");
                    }
                    else
                    {
                        SharedData.AddControllerStatusHistory(ControllerStatus.Stopped);
                        Logging?.LogInformation("Main controller is reporting: Now stopped");
                    }
                    throw;
                }
                finally
                {
                    try
                    {
                        Logging?.LogDebug("Stopping all poller threads");

                        bool hasStopped = false;
                        bool blStoppedAll = true;
                        bool blStoppedAny = false;
                        try
                        {
                            hasStopped = Poller_Countdown?.StopPollingThread() ?? true;
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to stop countdown poller: {message}", ex.Message);
                            hasStopped = false;
                        }
                        blStoppedAll = blStoppedAll && hasStopped;
                        blStoppedAny = blStoppedAny || hasStopped;
                        try
                        {
                            hasStopped = Poller_Fritz?.StopPollingThread() ?? true;
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to stop Fritz poller: {message}", ex.Message);
                            hasStopped = false;
                        }
                        blStoppedAll = blStoppedAll && hasStopped;
                        blStoppedAny = blStoppedAny || hasStopped;
                        try
                        {
                            hasStopped = Poller_ClewareUSB?.StopPollingThread() ?? true;
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to stop ClewareUSB poller: {message}", ex.Message);
                            hasStopped = false;
                        }
                        blStoppedAll = blStoppedAll && hasStopped;
                        blStoppedAny = blStoppedAny || hasStopped;
                        try
                        {
                            hasStopped = Poller_SwitchBot?.StopPollingThread() ?? true;
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to stop SwitchBot poller: {message}", ex.Message);
                            hasStopped = false;
                        }
                        blStoppedAll = blStoppedAll && hasStopped;
                        blStoppedAny = blStoppedAny || hasStopped;
                        try
                        {
                            hasStopped = Poller_ReadFiles?.StopPollingThread(true) ?? true;
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to stop remote read file poller: {message}", ex.Message);
                            hasStopped = false;
                        }
                        blStoppedAll = blStoppedAll && hasStopped;
                        blStoppedAny = blStoppedAny || hasStopped;
                        try
                        {
                            hasStopped = Poller_WriteFiles?.StopPollingThread(!hadError) ?? true;
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to stop local write file poller: {message}", ex.Message);
                            hasStopped = false;
                        }
                        blStoppedAll = blStoppedAll && hasStopped;
                        blStoppedAny = blStoppedAny || hasStopped;

                        if (!blStoppedAll)
                        {
                            Logging?.LogWarning("Not all pollers could stop monitoring");
                        }

                        try
                        {
                            ResetFlags();

                            SharedData.AddControllerStatusHistory(ControllerStatus.Stopped);
                            Logging?.LogInformation("Main controller is reporting: Stopped monitoring");
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to reset after poller stops: {message}", ex.Message);
                        }

                        IsAlive = hadError;
                    }
                    catch (Exception ex)
                    {
                        Logging?.LogError(ex, "While trying to stop controller: {message}", ex.Message);
                    }
                }

                Logging?.LogInformation("Ending main controller loop");
            }
        }

        #endregion

        #region Control methods

        public bool StopControllerThread()
        {
            using (Logging?.BeginScope("StopControllerThread"))
            {
                if (_loopThread != null)
                {
                    bool blStopMonitoring = StopMonitoring(true);
                    if (_loopThread.IsAlive)
                        _cancellationTokenSource?.Cancel();
                    _loopThread.Join(_sleepTimespan * 2);

                    if (_loopThread.IsAlive)
                        throw new ControllerException("Main controller loop is still running after shutdown command, this was unexpected");

                    Logging?.LogDebug("Main controller loop ended gracefully");

                    _cancellationTokenSource?.Dispose();
                    _loopThread = null;
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

        public bool StartControllerThread()
        {
            using (Logging?.BeginScope("StartControllerThread"))
            {
                if (_loopThread == null)
                {
                    UpdateTimestamp();
                    _cancellationTokenSource = Generator.GetLinkedCancellationTokenSource();
                    _loopThread = new Thread(new ThreadStart(MainControllerLoop));
                    _loopThread.Start();
                    return true;
                }
                else
                {
                    throw new ControllerException("Main controller thread already started, please stop thread before starting anew");
                }
            }
        }

        public bool ArmPanicMode()
        {
            using (Logging?.BeginScope("ArmPanicMode"))
            {
                try
                {
                    bool blIsArmed = true;
                    bool blCanBeArmed = true;

                    ControllerStatus statusController = SharedData.CurrentControllerStatus;
                    if (statusController == ControllerStatus.Monitoring || statusController == ControllerStatus.Holdback)
                    {
                        if (!(Poller_Countdown?.ArmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter panic mode for countdown poller");
                            blCanBeArmed = false;
                        }
                        if (!(Poller_ClewareUSB?.ArmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter panic mode for cleware usb poller");
                            blCanBeArmed = false;
                        }
                        if (!(Poller_Fritz?.ArmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter panic mode for fritz poller");
                            blCanBeArmed = false;
                        }
                        if (!(Poller_ReadFiles?.ArmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter panic mode for read files poller");
                            blCanBeArmed = false;
                        }
                        if (!(Poller_WriteFiles?.ArmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter panic mode for write files poller");
                            blCanBeArmed = false;
                        }
                        if (!(Poller_SwitchBot?.ArmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter panic mode for switch bot poller");
                            blCanBeArmed = false;
                        }

                        if (blCanBeArmed)
                        {
                            blIsArmed = (Poller_Countdown?.ArmPanicMode(false) ?? true) &&
                                            (Poller_ClewareUSB?.ArmPanicMode(false) ?? true) &&
                                            (Poller_Fritz?.ArmPanicMode(false) ?? true) &&
                                            (Poller_ReadFiles?.ArmPanicMode(false) ?? true) &&
                                            (Poller_WriteFiles?.ArmPanicMode(false) ?? true) &&
                                            (Poller_SwitchBot?.ArmPanicMode(false) ?? true);

                            if (blIsArmed)
                            {
                                ResetFlags();

                                SharedData.AddControllerStatusHistory(ControllerStatus.Armed);
                                Logging?.LogInformation("Main controller is reporting: Panic mode armed");
                            }
                            else
                            {
                                Logging?.LogWarning("Failed to arm panic mode in main controller, disarming pollers again");
                                Poller_Countdown?.DisarmPanicMode(false);
                                Poller_ClewareUSB?.DisarmPanicMode(false);
                                Poller_Fritz?.DisarmPanicMode(false);
                                Poller_ReadFiles?.DisarmPanicMode(false);
                                Poller_WriteFiles?.DisarmPanicMode(false);
                                Poller_SwitchBot?.DisarmPanicMode(false);
                            }

                            return blIsArmed;
                        }
                        else
                        {
                            Logging?.LogWarning("Cannot arm panic mode in controller due to problem with at least one of the pollers");

                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot arm panic mode in controller, must be in monitoring or holdback mode");

                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Thread was canceled while trying to arm panic mode");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Thread was interrupted while trying to arm panic mode");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to arm panic mode in main controller: {message}", ex.Message);

                    return false;
                }
            }
        }

        public bool DisarmPanicMode()
        {
            using (Logging?.BeginScope("DisarmPanicMode"))
            {
                try
                {
                    bool blIsDisarmed = true;
                    bool blCanBeDisarmed = true;

                    ControllerStatus statusController = SharedData.CurrentControllerStatus;
                    if (statusController == ControllerStatus.Armed ||
                            statusController == ControllerStatus.Holdback ||
                            statusController == ControllerStatus.SafeMode ||
                            statusController == ControllerStatus.ShutDown ||
                            statusController == ControllerStatus.Triggered)
                    {
                        if (!(Poller_Countdown?.DisarmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not disarm panic mode for countdown poller");
                            blCanBeDisarmed = false;
                        }
                        if (!(Poller_ClewareUSB?.DisarmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not disarm panic or safe mode for cleware usb poller");
                            blCanBeDisarmed = false;
                        }
                        if (!(Poller_Fritz?.DisarmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not disarm panic or safe mode for fritz poller");
                            blCanBeDisarmed = false;
                        }
                        if (!(Poller_ReadFiles?.DisarmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not disarm panic or safe mode for read files poller");
                            blCanBeDisarmed = false;
                        }
                        if (!(Poller_WriteFiles?.DisarmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not disarm panic or safe mode for write files poller");
                            blCanBeDisarmed = false;
                        }
                        if (!(Poller_SwitchBot?.DisarmPanicMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not disarm panic or safe mode for switch bot poller");
                            blCanBeDisarmed = false;
                        }

                        if (blCanBeDisarmed)
                        {
                            bool hasDisarmed;
                            bool allDisarmed = true;
                            bool anyDisarmed = false;

                            hasDisarmed = (Poller_Countdown?.DisarmPanicMode(false) ?? true);
                            allDisarmed = allDisarmed && hasDisarmed;
                            anyDisarmed = hasDisarmed || anyDisarmed;
                            hasDisarmed = (Poller_ClewareUSB?.DisarmPanicMode(false) ?? true);
                            allDisarmed = allDisarmed && hasDisarmed;
                            anyDisarmed = hasDisarmed || anyDisarmed;
                            hasDisarmed = (Poller_Fritz?.DisarmPanicMode(false) ?? true);
                            allDisarmed = allDisarmed && hasDisarmed;
                            anyDisarmed = hasDisarmed || anyDisarmed;
                            hasDisarmed = (Poller_ReadFiles?.DisarmPanicMode(false) ?? true);
                            allDisarmed = allDisarmed && hasDisarmed;
                            anyDisarmed = hasDisarmed || anyDisarmed;
                            hasDisarmed = (Poller_WriteFiles?.DisarmPanicMode(false) ?? true);
                            allDisarmed = allDisarmed && hasDisarmed;
                            anyDisarmed = hasDisarmed || anyDisarmed;
                            hasDisarmed = (Poller_SwitchBot?.DisarmPanicMode(false) ?? true);
                            allDisarmed = allDisarmed && hasDisarmed;
                            anyDisarmed = hasDisarmed || anyDisarmed;

                            blIsDisarmed = anyDisarmed;

                            if (!allDisarmed)
                            {
                                Logging?.LogWarning("Not all pollers could be disarmed");
                            }

                            if (blIsDisarmed)
                            {
                                ResetFlags();

                                SharedData.AddControllerStatusHistory(ControllerStatus.Monitoring);
                                Logging?.LogInformation("Main controller is reporting: Panic mode or safe mode disarmed, back to monitoring");
                            }
                            else
                            {
                                Logging?.LogWarning("Failed to disarm panic mode in main controller since none of the controllers got disarmed");
                            }

                            return blIsDisarmed;
                        }
                        else
                        {
                            Logging?.LogWarning("Cannot disarm panic mode in controller due to problem with at least one of the pollers");

                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot disarm panic mode in controller, must be in armed, holdback or safe mode");

                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Thread was canceled while trying to disarm panic mode");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Thread was interrupted while trying to disarm panic mode");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to disarm panic mode in main controller: {message}", ex.Message);

                    return false;
                }
            }
        }

        public bool StartMonitoring()
        {
            using (Logging?.BeginScope("StartMonitoring"))
            {
                try
                {
                    bool blIsMonitoring = true;
                    bool blCanMonitor = true;

                    if (SharedData.CurrentControllerStatus == ControllerStatus.Stopped)
                    {
                        if (!(Poller_Countdown?.StartMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not start monitoring for countdown poller");
                            blCanMonitor = false;
                        }
                        if (!(Poller_ClewareUSB?.StartMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not start monitoring for cleware usb poller");
                            blCanMonitor = false;
                        }
                        if (!(Poller_Fritz?.StartMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not start monitoring for fritz poller");
                            blCanMonitor = false;
                        }
                        if (!(Poller_ReadFiles?.StartMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not start monitoring for read files poller");
                            blCanMonitor = false;
                        }
                        if (!(Poller_WriteFiles?.StartMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not start monitoring for write files poller");
                            blCanMonitor = false;
                        }
                        if (!(Poller_SwitchBot?.StartMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not start monitoring for switch bot poller");
                            blCanMonitor = false;
                        }

                        if (blCanMonitor)
                        {
                            ResetPanic();
                            blIsMonitoring = (Poller_Countdown?.StartMonitoring(false) ?? true) &&
                                                (Poller_ClewareUSB?.StartMonitoring(false) ?? true) &&
                                                (Poller_Fritz?.StartMonitoring(false) ?? true) &&
                                                (Poller_ReadFiles?.StartMonitoring(false) ?? true) &&
                                                (Poller_WriteFiles?.StartMonitoring(false) ?? true) &&
                                                (Poller_SwitchBot?.StartMonitoring(false) ?? true);

                            if (blIsMonitoring)
                            {
                                ResetFlags();

                                SharedData.AddControllerStatusHistory(ControllerStatus.Monitoring);
                                Logging?.LogInformation("Main controller is reporting: Now monitoring");
                            }
                            else
                            {
                                Logging?.LogWarning("Failed to start monitoring in main controller, stop monitoring for all pollers again");
                                Poller_Countdown?.StopMonitoring(false);
                                Poller_ClewareUSB?.StopMonitoring(false);
                                Poller_Fritz?.StopMonitoring(false);
                                Poller_ReadFiles?.StopMonitoring(false, true);
                                Poller_WriteFiles?.StopMonitoring(false, true);
                                Poller_SwitchBot?.StopMonitoring(false);
                            }

                            return blIsMonitoring;
                        }
                        else
                        {
                            Logging?.LogWarning("Cannot start monitoring in controller due to problem with at least one of the pollers");

                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot start monitoring in controller, must be in stopped mode");

                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Thread was canceled while trying to start monitoring");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Thread was interrupted while trying to start monitoring");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to start monitoring in main controller: {message}", ex.Message);

                    return false;
                }
            }
        }

        public bool StopMonitoring(bool whatIf)
        {
            using (Logging?.BeginScope("StopMonitoring"))
            {
                try
                {
                    bool blStoppedMonitoring = true;
                    bool blCanStopMonitoring = true;

                    if (SharedData.CurrentControllerStatus != ControllerStatus.Stopped)
                    {
                        if (!(Poller_Countdown?.StopMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not stop monitoring for countdown poller");
                            blCanStopMonitoring = false;
                        }
                        if (!(Poller_ClewareUSB?.StopMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not stop monitoring for cleware usb poller");
                            blCanStopMonitoring = false;
                        }
                        if (!(Poller_Fritz?.StopMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not stop monitoring for fritz poller");
                            blCanStopMonitoring = false;
                        }
                        if (!(Poller_ReadFiles?.StopMonitoring(true, true) ?? true))
                        {
                            Logging?.LogWarning("Could not stop monitoring for read files poller");
                            blCanStopMonitoring = false;
                        }
                        if (!(Poller_WriteFiles?.StopMonitoring(true, true) ?? true))
                        {
                            Logging?.LogWarning("Could not stop monitoring for write files poller");
                            blCanStopMonitoring = false;
                        }
                        if (!(Poller_SwitchBot?.StopMonitoring(true) ?? true))
                        {
                            Logging?.LogWarning("Could not stop monitoring for switch bot poller");
                            blCanStopMonitoring = false;
                        }

                        if (!whatIf)
                        {
                            if (blCanStopMonitoring)
                            {
                                bool hasStopped;
                                bool allStopped = true;
                                bool anyStopped = false;

                                hasStopped = (Poller_Countdown?.StopMonitoring(false) ?? true);
                                allStopped = allStopped && hasStopped;
                                anyStopped = hasStopped || anyStopped;
                                hasStopped = (Poller_ClewareUSB?.StopMonitoring(false) ?? true);
                                allStopped = allStopped && hasStopped;
                                anyStopped = hasStopped || anyStopped;
                                hasStopped = (Poller_Fritz?.StopMonitoring(false) ?? true);
                                allStopped = allStopped && hasStopped;
                                anyStopped = hasStopped || anyStopped;
                                hasStopped = (Poller_ReadFiles?.StopMonitoring(false, true) ?? true);
                                allStopped = allStopped && hasStopped;
                                anyStopped = hasStopped || anyStopped;
                                hasStopped = (Poller_WriteFiles?.StopMonitoring(false, true) ?? true);
                                allStopped = allStopped && hasStopped;
                                anyStopped = hasStopped || anyStopped;
                                hasStopped = (Poller_SwitchBot?.StopMonitoring(false) ?? true);
                                allStopped = allStopped && hasStopped;
                                anyStopped = hasStopped || anyStopped;

                                blStoppedMonitoring = anyStopped;

                                if (!allStopped)
                                {
                                    Logging?.LogWarning("Not all pollers stopped monitoring");
                                }

                                if (blStoppedMonitoring)
                                {
                                    ResetFlags();

                                    SharedData.AddControllerStatusHistory(ControllerStatus.Stopped);
                                    Logging?.LogInformation("Main controller is reporting: Stopped monitoring");
                                }
                                else
                                {
                                    Logging?.LogWarning("Failed to stop monitoring in main controller since none of the controllers stopped monitoring");
                                }

                                return blStoppedMonitoring;
                            }
                            else
                            {
                                Logging?.LogWarning("Cannot stop monitoring in controller due to problem with at least one of the pollers");

                                return false;
                            }
                        }
                        else
                        {
                            return blCanStopMonitoring;
                        }
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot stop monitoring in controller, as monitoring didn't start");

                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Thread was canceled while trying to stop monitoring");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Thread was interrupted while trying to stop monitoring");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to stop monitoring in main controller: {message}", ex.Message);

                    return false;
                }
            }
        }

        public bool EnterSafeMode(bool includeRemoteFiles)
        {
            using (Logging?.BeginScope("EnterSafeMode"))
            {
                try
                {
                    bool blIsSaved = true;
                    bool blCanBeSaved = true;

                    ControllerStatus statusController = SharedData.CurrentControllerStatus;
                    if ((!IsInSafeMode || (includeRemoteFiles && !AreRemoteFilesInSafeMode)) &&
                            (statusController == ControllerStatus.SafeMode ||
                                statusController == ControllerStatus.Armed ||
                                statusController == ControllerStatus.ShutDown ||
                                statusController == ControllerStatus.Triggered))
                    {
                        if (!(Poller_ClewareUSB?.EnterSafeMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter safe mode for cleware usb poller");
                            blCanBeSaved = false;
                        }
                        if (!(Poller_Fritz?.EnterSafeMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter safe mode for fritz poller");
                            blCanBeSaved = false;
                        }
                        if (includeRemoteFiles)
                        {
                            if (!(Poller_ReadFiles?.EnterSafeMode(true) ?? true))
                            {
                                Logging?.LogWarning("Could not enter safe mode for read files poller");
                                blCanBeSaved = false;
                            }
                        }
                        if (!(Poller_SwitchBot?.EnterSafeMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter safe mode for switch bot poller");
                            blCanBeSaved = false;
                        }

                        if (blCanBeSaved)
                        {
                            bool isSaved;
                            bool allSaved = true;
                            bool anySaved = false;

                            isSaved = (Poller_ClewareUSB?.EnterSafeMode(false) ?? true);
                            allSaved = allSaved && isSaved;
                            anySaved = anySaved || isSaved;
                            isSaved = (Poller_Fritz?.EnterSafeMode(false) ?? true);
                            allSaved = allSaved && isSaved;
                            anySaved = anySaved || isSaved;
                            isSaved = (Poller_SwitchBot?.EnterSafeMode(false) ?? true);
                            allSaved = allSaved && isSaved;
                            anySaved = anySaved || isSaved;

                            if (includeRemoteFiles)
                            {
                                isSaved = (Poller_ReadFiles?.EnterSafeMode(false) ?? true);
                                allSaved = allSaved && isSaved;
                                anySaved = anySaved || isSaved;
                            }

                            blIsSaved = anySaved;

                            if (!allSaved)
                            {
                                Logging?.LogWarning("Not all pollers could be saved");
                            }

                            if (blIsSaved)
                            {
                                SharedData.AddControllerStatusHistory(ControllerStatus.SafeMode, includeRemoteFiles);
                                IsInSafeMode = true;
                                AreRemoteFilesInSafeMode = AreRemoteFilesInSafeMode || includeRemoteFiles;
                                if (includeRemoteFiles)
                                    Logging?.LogInformation("Main controller is reporting: Entered safe mode (including remote files)");
                                else
                                    Logging?.LogInformation("Main controller is reporting: Entered safe mode");
                            }
                            else
                            {
                                Logging?.LogWarning("Failed to enter safe mode in main controller since none of the controllers entered safe mode");
                            }

                            return blIsSaved;
                        }
                        else
                        {
                            Logging?.LogWarning("Cannot enter safe mode in controller due to problem with at least one of the pollers");

                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot enter safe mode in controller, must be in armed mode or shutdown mode without having entered safe mode before");

                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Thread was canceled while trying to enter safe mode");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Thread was interrupted while trying to enter safe mode");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to enter safe mode in main controller: {message}", ex.Message);

                    return false;
                }
            }
        }

        public bool EnterHoldBackMode()
        {
            using (Logging?.BeginScope("EnterHoldBackMode"))
            {
                try
                {
                    bool blIsHeldBack = true;
                    bool blCanBeHeldBack = true;

                    if (SharedData.CurrentControllerStatus == ControllerStatus.Monitoring)
                    {
                        if (!(Poller_Countdown?.EnterHoldBackMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter holdback mode for countdown poller");
                            blCanBeHeldBack = false;
                        }
                        if (!(Poller_ClewareUSB?.EnterHoldBackMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter holdback mode for cleware usb poller");
                            blCanBeHeldBack = false;
                        }
                        if (!(Poller_Fritz?.EnterHoldBackMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter holdback mode for fritz poller");
                            blCanBeHeldBack = false;
                        }
                        if (!(Poller_ReadFiles?.EnterHoldBackMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter holdback mode for read files poller");
                            blCanBeHeldBack = false;
                        }
                        if (!(Poller_WriteFiles?.EnterHoldBackMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter holdback mode for write files poller");
                            blCanBeHeldBack = false;
                        }
                        if (!(Poller_SwitchBot?.EnterHoldBackMode(true) ?? true))
                        {
                            Logging?.LogWarning("Could not enter holdback mode in main controller");
                            blCanBeHeldBack = false;
                        }

                        if (blCanBeHeldBack)
                        {
                            blIsHeldBack = (Poller_Countdown?.EnterHoldBackMode(false) ?? true) &&
                                            (Poller_ClewareUSB?.EnterHoldBackMode(false) ?? true) &&
                                            (Poller_Fritz?.EnterHoldBackMode(false) ?? true) &&
                                            (Poller_ReadFiles?.EnterHoldBackMode(false) ?? true) &&
                                            (Poller_WriteFiles?.EnterHoldBackMode(false) ?? true) &&
                                            (Poller_SwitchBot?.EnterHoldBackMode(false) ?? true);

                            if (blIsHeldBack)
                            {
                                SharedData.AddControllerStatusHistory(ControllerStatus.Holdback);
                                Logging?.LogInformation("Main controller is reporting: Entered holdback mode");
                            }
                            else
                            {
                                Logging?.LogWarning("Failed to enter holdback mode in main controller, disarming pollers again");
                                Poller_Countdown?.DisarmPanicMode(false);
                                Poller_ClewareUSB?.DisarmPanicMode(false);
                                Poller_Fritz?.DisarmPanicMode(false);
                                Poller_ReadFiles?.DisarmPanicMode(false);
                                Poller_WriteFiles?.DisarmPanicMode(false);
                                Poller_SwitchBot?.DisarmPanicMode(false);
                            }

                            return blIsHeldBack;
                        }
                        else
                        {
                            Logging?.LogWarning("Cannot enter holdback mode in controller due to problem with at least one of the pollers");

                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot enter holdback mode in controller, must be in monitoring mode");

                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Thread was canceled while trying to enter holdback mode");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Thread was interrupted while trying to enter holdback mode");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to enter holdback mode in main controller: {message}", ex.Message);

                    return false;
                }
            }
        }

        public void UpdateWriteFileState(WatcherFileState state)
        {
            using (Logging?.BeginScope("UpdateWriteFileState"))
            {
                Logging?.LogDebug("Updating state in write files to: {newstate}", state);
                Poller_WriteFiles?.UpdateState(state);
            }
        }

        public bool ResetPanic()
        {
            using (Logging?.BeginScope("ResetPanic"))
            {
                Logging?.LogDebug("Resetting panics and poller counts in main controller");

                try
                {
                    SharedData.ResetPanicHistory();

                    bool anyReset = false;

                    anyReset = (Poller_Countdown?.ResetPanic() ?? false) || anyReset;
                    anyReset = (Poller_ClewareUSB?.ResetPanic() ?? false) || anyReset;
                    anyReset = (Poller_Fritz?.ResetPanic() ?? false) || anyReset;
                    anyReset = (Poller_ReadFiles?.ResetPanic() ?? false) || anyReset;
                    anyReset = (Poller_WriteFiles?.ResetPanic() ?? false) || anyReset;
                    anyReset = (Poller_SwitchBot?.ResetPanic() ?? false) || anyReset;

                    return anyReset;
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Thread was canceled while trying to reset panic in main controller");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Thread was interrupted while trying to reset panic in main controller");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to reset panic in main controller: {message}", ex.Message);

                    return false;
                }
            }
        }

        #endregion
    }
}
