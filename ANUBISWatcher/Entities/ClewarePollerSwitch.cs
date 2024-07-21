using ANUBISClewareAPI;
using ANUBISWatcher.Configuration.Serialization;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Entities
{
    public enum ClewareUSBPanicReason
    {
        [JsonEnumErrorValue]
        Unknown = 0,
        [JsonEnumName(true, "all", "every", "fallback", "", "*")]
        [JsonEnumNullValue]
        All,
        [JsonEnumName(true, "none", "NoPanic")]
        NoPanic,
        SafeModeTurnOn,
        NameNotFound,
        InvalidState,
        SwitchNotFound,
        SwitchOff,
        ErrorState,
        GeneralError,
        UnknownState,
        CommandTimeout,
        CommandError,
    }

    public class ClewarePollerSwitch
    {
        #region Fields

        #region Constants

        private readonly object lock_TimestampLastUpdate = new();
        private readonly object lock_CurrentState = new();
        private readonly object lock_Panic = new();
        private readonly object lock_NewPanic = new();

        #endregion

        #region Unchangable fields

        private readonly TimeSpan _lockTimeout = TimeSpan.Zero;
        private CancellationToken? _cancellationToken = null;

        #endregion

        #region Changable fields

        private volatile USBSwitchState _state = USBSwitchState.Unknown;
        private DateTime _timestampLastUpdate;
        private volatile bool _previousIsTurnedOn = false;
        private volatile ClewareUSBPanicReason _panic = ClewareUSBPanicReason.NoPanic;
        private volatile ClewareUSBPanicReason _panicNew = ClewareUSBPanicReason.NoPanic;
        private volatile bool _hasNewPanic = false;

        #endregion

        #endregion

        #region Properties

        public ClewarePollerSwitchOptions Options { get; init; }

        private volatile bool _inSafeMode;
        public bool InSafeMode { get { return _inSafeMode; } private set { _inSafeMode = value; } }

        /// <summary>
        /// Timestamp of last time a turn switch off signal was sent due to too low power usage
        /// </summary>
        public DateTime? LastAutoPowerOff { get; private set; }

        private volatile bool _doMonitor;
        public bool DoMonitor { get { return _doMonitor; } private set { _doMonitor = value; } }

        private volatile bool _holdBack;
        /// <summary>
        /// Are we in holdback mode? Which means not to trigger any switches on panic and not to enter safe mode.
        /// </summary>
        public bool HoldBack { get { return _holdBack; } private set { _holdBack = value; } }

        private volatile bool _checkForPanic;
        /// <summary>
        /// Should we check for panic?
        /// </summary>
        public bool CheckForPanic { get { return _checkForPanic; } private set { _checkForPanic = value; } }

        public bool CanBeArmed { get { return DoMonitor && (!HoldBack || Panic == ClewareUSBPanicReason.NoPanic); } }

        /// <summary>
        /// Is the system shut down according to this switch?
        /// </summary>
        public bool HasShutDown { get { return Options.MarkShutDownIfOff && CurrentState == USBSwitchState.Off; } }

        public USBSwitchState CurrentState
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_CurrentState, _lockTimeout);
                    if (lockTaken)
                        return _state;
                    else
                        throw new LockTimeoutException(nameof(CurrentState), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CurrentState);
                    }
                }
            }
            private set
            {
                lock (lock_CurrentState)
                {
                    _state = value;
                }
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

        /// <summary>
        /// Is switch turned on?
        /// </summary>
        public bool IsTurnedOn
        {
            get
            {
                return CurrentState == USBSwitchState.On;
            }
        }

        /// <summary>
        /// Is switch turned on to trigger safe mode alarm in case it was turned off before?
        /// </summary>
        public bool IsSafeModeTurnedOn
        {
            get
            {
                return Options.SafeModeTurnOnAlarm &&
                        CurrentState == USBSwitchState.On;
            }
        }


        public ClewareUSBPanicReason Panic
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_Panic, _lockTimeout);
                    if (lockTaken)
                        return _panic;
                    else
                        throw new LockTimeoutException(nameof(Panic), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_Panic);
                    }
                }
            }
            private set
            {
                lock (lock_Panic)
                {
                    lock (lock_NewPanic)
                    {
                        _panic = value;
                        if (value != ClewareUSBPanicReason.NoPanic)
                        {
                            _panicNew = value;
                            _hasNewPanic = true;
                        }
                        else
                        {
                            _panicNew = ClewareUSBPanicReason.NoPanic;
                            _hasNewPanic = false;
                        }
                    }
                }
            }
        }

        public ILogger? Logging { get { return Options.Logger; } }

        #endregion

        #region Constructors

        public ClewarePollerSwitch(ClewarePollerSwitchOptions options)
        {
            Options = options;
            DoMonitor = false;
            CheckForPanic = false;
            LastAutoPowerOff = null;
            _previousIsTurnedOn = false;
            _lockTimeout = TimeSpan.FromMilliseconds(options.LockTimeoutInMilliseconds);
            UpdateTimestamp();
        }

        #endregion

        #region Helper methods

        private bool IsSafeModePanicReason(ClewareUSBPanicReason reason)
        {
            return reason == ClewareUSBPanicReason.SafeModeTurnOn ||
                        (Options.SafeModeSensitive &&
                            (reason == ClewareUSBPanicReason.SwitchNotFound ||
                            reason == ClewareUSBPanicReason.CommandError ||
                            reason == ClewareUSBPanicReason.CommandTimeout));
        }

        private void CheckThreadCancellation()
        {
            _cancellationToken?.ThrowIfCancellationRequested();
        }

        private void UpdateTimestamp()
        {
            using (Logging?.BeginScope("UpdateTimestamp"))
            {
                try
                {
                    CheckThreadCancellation();

                    DateTime now = DateTime.UtcNow;
                    LastUpdateTimestamp = now;

                    Logging?.LogTrace("Updated LastUpdateTimestamp for Cleware USB switch \"{name}\" to {timestamp}", Options.USBSwitchName, now);
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("USB Switch polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("USB Switch polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to set last update timestamp: {message}", ex.Message);
                }
            }
        }

        private bool SetPanic(ClewareUSBPanicReason reason)
        {
            using (Logging?.BeginScope("SetPanic"))
            {
                bool hasPanic = false;
                try
                {
                    CheckThreadCancellation();

                    if (CheckForPanic)
                    {
                        if (!InSafeMode || IsSafeModePanicReason(reason))
                        {
                            Logging?.LogTrace("Examining USB switch \"{name}\" for panic condition", Options.USBSwitchName);

                            if (reason != ClewareUSBPanicReason.NoPanic)
                            {
                                if (Panic == ClewareUSBPanicReason.NoPanic || reason == ClewareUSBPanicReason.SafeModeTurnOn)
                                {
                                    Logging?.LogCritical("PANIC!!! Panic because of USB switch \"{name}\", panic reason is: {reason}", Options.USBSwitchName, reason);
                                    Panic = reason;

                                    hasPanic = true;
                                }
                                else
                                {
                                    Logging?.LogTrace("Would panic because of USB switch \"{name}\" and reason {newReason}, but this USB switch already panicked due to the following reason: {existingReason}", Options.USBSwitchName, reason, Panic);

                                    hasPanic = false;
                                }
                            }
                            else
                            {
                                Logging?.LogTrace("Examined USB switch \"{name}\" for panic condition but there was no reason to panic", Options.USBSwitchName);

                                hasPanic = false;
                            }
                        }
                        else
                        {
                            if (Panic == ClewareUSBPanicReason.NoPanic && reason != ClewareUSBPanicReason.NoPanic)
                            {
                                Logging?.LogTrace("Would panic because of USB switch \"{name}\" and reason {reason}, but USB switch is in safe mode", Options.USBSwitchName, reason);
                            }

                            hasPanic = false;
                        }
                    }
                    else
                    {
                        if (Panic == ClewareUSBPanicReason.NoPanic && reason != ClewareUSBPanicReason.NoPanic)
                        {
                            Logging?.LogTrace("Would panic because of USB switch \"{name}\" and reason {reason}, but panicing is turned off for this USB switch", Options.USBSwitchName, reason);
                        }

                        hasPanic = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("USB Switch polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("USB Switch polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogCritical(ex, "Fatal error while trying to set panic state: {message}", ex.Message);

                    try
                    {
                        Panic = ClewareUSBPanicReason.GeneralError;
                    }
                    catch (Exception exInner)
                    {
                        Logging?.LogCritical(exInner, "Fatal inner error while trying to set panic state in fatal error: {message}", exInner.Message);
                    }

                    hasPanic = true;
                }

                CheckThreadCancellation();

                if (hasPanic)
                {
                    Logging?.LogDebug("USB Switch \"{name}\" entered panic mode, checking if this USB switch should be turned off by poller", Options.USBSwitchName);
                    if (Options.TurnOffOnPanic)
                    {
                        if (!HoldBack)
                        {
                            Logging?.LogWarning("USB Switch \"{name}\" entered panic mode due to reason {reason}, turning USB switch off by poller since TurnOffOnPanic was set to true", Options.USBSwitchName, Panic);

                            try
                            {
                                var switchStateNew = Options.Api.TurnUSBSwitchOffByName(Options.USBSwitchName);

                                CheckThreadCancellation();

                                if (switchStateNew == USBSwitchState.Off)
                                {
                                    Logging?.LogInformation("USB Switch \"{name}\" was turned off successfully by poller due to panic reason {reason}", Options.USBSwitchName, Panic);
                                    LastAutoPowerOff = DateTime.UtcNow;
                                }
                                else
                                {
                                    throw new ClewarePollerException($"Unexpected USB switch state {switchStateNew}\" recieved while trying to turn off USB switch by poller due to panic reason {Panic}");
                                }
                            }
                            catch (ClewareAPITimeoutException)
                            {
                                Logging?.LogCritical("Recieved command timeout trying to turn off USB switch \"{name}\" by poller due to panic reason {reason}", Options.USBSwitchName, Panic);
                            }
                            catch (OperationCanceledException)
                            {
                                Logging?.LogTrace("USB Switch polling thread was canceled");
                                throw;
                            }
                            catch (ThreadInterruptedException)
                            {
                                Logging?.LogTrace("USB Switch polling thread was interrupted");
                                throw;
                            }
                            catch (Exception exTurnOff)
                            {
                                Logging?.LogCritical(exTurnOff, "While trying to turn off USB switch \"{name}\" by poller due to panic reason {reason}: {message}", Options.USBSwitchName, Panic, exTurnOff.Message);
                            }
                        }
                        else
                        {
                            Logging?.LogDebug("Not turning off USB switch \"{name}\" by poller due to panic reason {reason} because we are in holdback mode", Options.USBSwitchName, Panic);
                        }
                    }
                    else
                    {
                        Logging?.LogTrace("USB Switch \"{name}\" will not be turned off by poller (because TurnOffOnPanic was set to false) even though it entered panic mode, letting main thread decide what to do", Options.USBSwitchName);
                    }
                }

                return hasPanic;
            }
        }

        private bool SetState(USBSwitchState switchState)
        {
            using (Logging?.BeginScope("SetState"))
            {
                CheckThreadCancellation();

                CurrentState = switchState;
                Logging?.LogTrace("Verifying state for USB switch \"{name}\": {switchState}", Options.USBSwitchName, switchState);

                switch (switchState)
                {
                    case USBSwitchState.On:
                        Logging?.LogTrace("USB Switch \"{name}\" is turned on", Options.USBSwitchName);
                        return false;
                    case USBSwitchState.NameNotFound:
                        Logging?.LogTrace("USB Switch with name \"{name}\" not found", Options.USBSwitchName);
                        return SetPanic(ClewareUSBPanicReason.NameNotFound);
                    case USBSwitchState.Off:
                        Logging?.LogTrace("USB Switch \"{name}\" is turned off", Options.USBSwitchName);
                        return SetPanic(ClewareUSBPanicReason.SwitchOff);
                    case USBSwitchState.Unknown:
                        Logging?.LogTrace("USB Switch \"{name}\" has invalid state", Options.USBSwitchName);
                        return SetPanic(ClewareUSBPanicReason.InvalidState);
                    case USBSwitchState.Error:
                        Logging?.LogTrace("USB Switch \"{name}\" had error checking state", Options.USBSwitchName);
                        return SetPanic(ClewareUSBPanicReason.ErrorState);
                    case USBSwitchState.SwitchNotFound:
                        Logging?.LogTrace("USB Switch \"{name}\" not found", Options.USBSwitchName);
                        return SetPanic(ClewareUSBPanicReason.SwitchNotFound);
                    default:
                        Logging?.LogWarning("USB Switch \"{name}\" has unknown state {state}", Options.USBSwitchName, switchState);
                        return SetPanic(ClewareUSBPanicReason.UnknownState);
                }
            }
        }

        #endregion

        #region Check methods

        public ClewareUSBPanicReason? GetNewPanic(bool remove = true)
        {
            bool lockTaken = false;
            try
            {
                lockTaken = Monitor.TryEnter(lock_NewPanic, _lockTimeout);
                if (lockTaken)
                {
                    var retVal = (ClewareUSBPanicReason?)(_hasNewPanic ? _panicNew : null);
                    if (remove)
                    {
                        _panicNew = ClewareUSBPanicReason.NoPanic;
                        _hasNewPanic = false;
                    }
                    return retVal;
                }
                else
                    throw new LockTimeoutException("NewPanic", _lockTimeout);
            }
            finally
            {
                // Ensure that the lock is released.
                if (lockTaken)
                {
                    Monitor.Exit(lock_NewPanic);
                }
            }
        }

        public bool CheckState()
        {
            using (Logging?.BeginScope("CheckState"))
            {
                try
                {
                    CheckThreadCancellation();

                    if (DoMonitor)
                    {
                        USBSwitchState state = USBSwitchState.Error;
                        Logging?.LogDebug("Checking state of USB switch \"{name}\"", Options.USBSwitchName);
                        try
                        {
                            state = Options.Api.GetUSBSwitchStateByName(Options.USBSwitchName);

                            CheckThreadCancellation();

                            Logging?.LogDebug("State of USB switch \"{name}\" is: {state}", Options.USBSwitchName, state);
                        }
                        catch (ClewareAPITimeoutException)
                        {
                            Logging?.LogWarning("Recieved command timeout while checking state");

                            return SetPanic(ClewareUSBPanicReason.CommandTimeout);
                        }
                        catch (OperationCanceledException)
                        {
                            Logging?.LogTrace("USB Switch polling thread was canceled");
                            throw;
                        }
                        catch (ThreadInterruptedException)
                        {
                            Logging?.LogTrace("USB Switch polling thread was interrupted");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to check for state of USB switch \"{name}\": {message}", Options.USBSwitchName, ex.Message);
                        }

                        return SetState(state);
                    }
                    else
                    {
                        return false;
                    }
                }
                finally
                {
                    UpdateTimestamp();
                }
            }
        }

        public bool CheckSwitch()
        {
            using (Logging?.BeginScope("CheckSwitch"))
            {
                try
                {
                    CheckThreadCancellation();

                    bool hasPanicked_General = false;
                    bool hasPanicked_State = false;

                    try
                    {
                        Logging?.LogDebug("Begin checking USB switch \"{name}\"", Options.USBSwitchName);
                        hasPanicked_State = CheckState();

                        if (InSafeMode && !_previousIsTurnedOn && IsSafeModeTurnedOn)
                        {
                            Logging?.LogCritical("USB Switch \"{name}\" is in safe mode and was previously turned off but seems to have turned on again", Options.USBSwitchName);
                            hasPanicked_General = SetPanic(ClewareUSBPanicReason.SafeModeTurnOn) || hasPanicked_General;
                            _previousIsTurnedOn = true;
                        }
                        else
                        {
                            _previousIsTurnedOn = IsTurnedOn;
                        }

                        CheckThreadCancellation();

                        Logging?.LogDebug("Finished checking USB switch \"{name}\"", Options.USBSwitchName);
                    }
                    catch (OperationCanceledException)
                    {

                        Logging?.LogTrace("USB Switch polling thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {

                        Logging?.LogTrace("USB Switch polling thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logging?.LogError(ex, "General error while checking USB switch properties: {message}", ex.Message);
                        hasPanicked_General = SetPanic(ClewareUSBPanicReason.GeneralError) || hasPanicked_General;
                    }

                    CheckThreadCancellation();

                    var hasPanicked = hasPanicked_General || hasPanicked_State;

                    if (hasPanicked)
                        Logging?.LogWarning("USB Switch \"{name}\" reached panic condition {panicReason} and switched to panic state!", Options.USBSwitchName, Panic);
                    else if (Panic != ClewareUSBPanicReason.NoPanic)
                        Logging?.LogDebug("USB Switch \"{name}\" remains in panic state {panicReason}", Options.USBSwitchName, Panic);
                    else
                        Logging?.LogDebug("USB Switch \"{name}\" remains in normal/non-panic state", Options.USBSwitchName);

                    return hasPanicked;
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("USB Switch polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("USB Switch polling thread was interrupted");
                    throw;
                }
                catch (Exception exFatal)
                {
                    Logging?.LogCritical(exFatal, "Fatal error while checking USB switch properties: {message}", exFatal.Message);

                    return SetPanic(ClewareUSBPanicReason.GeneralError);
                }
                finally
                {
                    UpdateTimestamp();
                }
            }
        }

        #endregion

        #region Control methods

        public bool ResetPanic()
        {
            using (Logging?.BeginScope("ResetPanic"))
            {
                CheckThreadCancellation();

                if (Panic != ClewareUSBPanicReason.NoPanic)
                {
                    Logging?.LogDebug("Resetting panic {reason} for USB switch \"{name}\"", Panic, Options.USBSwitchName);
                    Panic = ClewareUSBPanicReason.NoPanic;
                    return true;
                }
                else
                {
                    Logging?.LogDebug("USB switch \"{name}\" had no panic to reset", Options.USBSwitchName);
                    return false;
                }
            }
        }

        public bool EnterSafeMode()
        {
            using (Logging?.BeginScope("EnterSafeMode"))
            {
                CheckThreadCancellation();

                if (Options.ArmPanicMode)
                {
                    if (Options.EnterSafeMode)
                    {
                        if (!HoldBack)
                        {
                            if (!InSafeMode)
                            {
                                InSafeMode = true;
                                Logging?.LogInformation("USB Switch \"{name}\" entered safe mode", Options.USBSwitchName);
                                return true;
                            }
                            else
                            {
                                Logging?.LogDebug("USB Switch \"{name}\" already in safe mode", Options.USBSwitchName);
                                return false;
                            }
                        }
                        else
                        {
                            Logging?.LogWarning("Not entering safe mode for USB Switch \"{name}\" as we are in holdback mode", Options.USBSwitchName);
                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogDebug("Skip switching to safe mode for USB switch \"{name}\" as EnterSafeMode option was set to false", Options.USBSwitchName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip switching to safe mode for USB switch \"{name}\" as ArmPanicMode option was set to false", Options.USBSwitchName);
                    return false;
                }
            }
        }

        public bool EnterHoldBackMode()
        {
            using (Logging?.BeginScope("EnterHoldBackMode"))
            {
                CheckThreadCancellation();

                if (Options.ArmPanicMode)
                {
                    if (!HoldBack)
                    {
                        HoldBack = true;
                        CheckForPanic = true;
                        Logging?.LogInformation("USB Switch \"{name}\" entered holdback mode", Options.USBSwitchName);
                        return true;
                    }
                    else
                    {
                        Logging?.LogDebug("USB Switch \"{name}\" already entered holdback mode", Options.USBSwitchName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip entering holdback mode for USB switch \"{name}\" as ArmPanicMode option was set to false", Options.USBSwitchName);
                    return false;
                }
            }
        }

        public bool ArmPanicMode()
        {
            using (Logging?.BeginScope("ArmPanicMode"))
            {
                CheckThreadCancellation();

                if (Options.ArmPanicMode)
                {
                    if (!CheckForPanic || HoldBack)
                    {
                        if (CanBeArmed)
                        {
                            CheckForPanic = true;
                            HoldBack = false;
                            Logging?.LogInformation("USB Switch \"{name}\" panic mode armed", Options.USBSwitchName);
                            return true;
                        }
                        else
                        {
                            Logging?.LogWarning("Cannot arm panic mode for USB Switch \"{name}\", must be monitoring or holdback mode without panic", Options.USBSwitchName);
                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogDebug("USB Switch \"{name}\" already armed panic mode", Options.USBSwitchName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip arming panic mode for USB switch \"{name}\" as ArmPanicMode option was set to false", Options.USBSwitchName);
                    return false;
                }
            }
        }

        public bool DisarmPanicMode()
        {
            using (Logging?.BeginScope("DisarmPanicMode"))
            {
                CheckThreadCancellation();

                InSafeMode = false;
                if (HoldBack)
                {
                    HoldBack = false;
                    CheckForPanic = false;
                    Logging?.LogInformation("USB Switch \"{name}\" left holdback mode", Options.USBSwitchName);
                    return true;
                }
                else if (CheckForPanic)
                {
                    CheckForPanic = false;
                    Logging?.LogInformation("USB Switch \"{name}\" panic mode disarmed", Options.USBSwitchName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("USB Switch \"{name}\" has not armed panic mode", Options.USBSwitchName);
                    return false;
                }
            }
        }

        public bool StartMonitoring()
        {
            using (Logging?.BeginScope("StartMonitoring"))
            {
                CheckThreadCancellation();

                if (!DoMonitor)
                {
                    DoMonitor = true;
                    Logging?.LogInformation("USB Switch \"{name}\" started monitoring", Options.USBSwitchName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("USB Switch \"{name}\" already monitoring", Options.USBSwitchName);
                    return false;
                }
            }
        }

        public bool StopMonitoring()
        {
            using (Logging?.BeginScope("StopMonitoring"))
            {
                CheckThreadCancellation();

                CheckForPanic = false;
                HoldBack = false;
                InSafeMode = false;
                if (DoMonitor)
                {
                    DoMonitor = false;
                    Logging?.LogInformation("USB Switch \"{name}\" stopped monitoring", Options.USBSwitchName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("USB Switch \"{name}\" isn't monitoring", Options.USBSwitchName);
                    return false;
                }
            }
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            Options?.Api?.SetCancellationToken(cancellationToken);
        }

        public void RemoveCancellationToken()
        {
            _cancellationToken = null;
        }

        #endregion
    }
}
