using ANUBISSwitchBotAPI;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Entities
{
    public enum SwitchBotPanicReason
    {
        [JsonEnumErrorValue]
        Unknown = 0,
        [JsonEnumName(true, "all", "every", "fallback", "", "*")]
        [JsonEnumNullValue]
        All,
        [JsonEnumName(true, "none", "NoPanic")]
        NoPanic,
        NameNotFound,
        SwitchNotFound,
        SwitchOff,
        BatteryTooLow,
        ErrorResponse,
        GeneralError,
        InvalidState,
        InvalidBattery,
        UnknownBattery,
        UnknownState,
        HttpTimeout,
        NetworkError,
    }

    public class SwitchBotPollerSwitch
    {
        #region Fields

        #region Constants

        private readonly object lock_TimestampLastUpdate = new();
        private readonly object lock_CurrentBattery = new();
        private readonly object lock_CurrentState = new();
        private readonly object lock_Panic = new();
        private readonly object lock_NewPanic = new();

        #endregion

        #region Unchangable fields

        private readonly TimeSpan _lockTimeout = TimeSpan.Zero;
        private CancellationToken? _cancellationToken = null;

        #endregion

        #region Changable fields

        private volatile SwitchBotPowerState _state = SwitchBotPowerState.Unknown;
        private volatile short _battery = 0;
        private volatile bool _nullBattery = true;
        private DateTime _timestampLastUpdate;
        private volatile SwitchBotPanicReason _panic = SwitchBotPanicReason.NoPanic;
        private volatile SwitchBotPanicReason _panicNew = SwitchBotPanicReason.NoPanic;
        private volatile bool _hasNewPanic = false;

        #endregion

        #endregion

        #region Properties

        public SwitchBotPollerSwitchOptions Options { get; init; }

        private volatile bool _inSafeMode;
        public bool InSafeMode { get { return _inSafeMode; } private set { _inSafeMode = value; } }

        /// <summary>
        /// Timestamp of last time a turn switch off signal was sent due to too low battery
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

        public bool CanBeArmed { get { return DoMonitor && (!HoldBack || Panic == SwitchBotPanicReason.NoPanic); } }

        /// <summary>
        /// Is the system shut down according to this switch?
        /// </summary>
        public bool HasShutDown { get { return Options.MarkShutDownIfOff && CurrentState == SwitchBotPowerState.Off; } }

        public SwitchBotPowerState CurrentState
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

        public short? CurrentBattery
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_CurrentBattery, _lockTimeout);
                    if (lockTaken)
                    {
                        if (_nullBattery)
                            return null;
                        else
                            return _battery;
                    }
                    else
                        throw new LockTimeoutException(nameof(CurrentBattery), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CurrentBattery);
                    }
                }
            }
            private set
            {
                lock (lock_CurrentBattery)
                {
                    if (value.HasValue)
                    {
                        _nullBattery = false;
                        _battery = value.Value;
                    }
                    else
                    {
                        _nullBattery = true;
                    }
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

        public SwitchBotPanicReason Panic
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
                        if (value != SwitchBotPanicReason.NoPanic)
                        {
                            _panicNew = value;
                            _hasNewPanic = true;
                        }
                        else
                        {
                            _panicNew = SwitchBotPanicReason.NoPanic;
                            _hasNewPanic = false;
                        }
                    }
                }
            }
        }

        public bool DoCheckBattery { get { return Options.MinBattery.HasValue; } }
        public bool DoCheckState { get { return Options.StateCheck; } }
        public bool DoCheckAny { get { return DoCheckBattery || DoCheckState; } }

        public ILogger? Logging { get { return Options.Logger; } }

        #endregion

        #region Constructors

        public SwitchBotPollerSwitch(SwitchBotPollerSwitchOptions options)
        {
            Options = options;
            DoMonitor = false;
            CheckForPanic = false;
            LastAutoPowerOff = null;
            _lockTimeout = TimeSpan.FromMilliseconds(options.LockTimeoutInMilliseconds);
            UpdateTimestamp();
        }

        #endregion

        #region Helper methods

        private bool IsSafeModePanicReason(SwitchBotPanicReason reason)
        {
            return Options.SafeModeSensitive &&
                        (reason == SwitchBotPanicReason.ErrorResponse ||
                        reason == SwitchBotPanicReason.HttpTimeout || reason == SwitchBotPanicReason.NetworkError ||
                        reason == SwitchBotPanicReason.SwitchNotFound || reason == SwitchBotPanicReason.NameNotFound);
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

                    Logging?.LogTrace("Updated LastUpdateTimestamp for SwitchBot switch \"{name}\" to {timestamp}", Options.SwitchBotName, now);
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Switch polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Switch polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to set last update timestamp: {message}", ex.Message);
                }
            }
        }

        private bool SetPanic(SwitchBotPanicReason reason)
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
                            Logging?.LogTrace("Examining switch bot \"{name}\" for panic condition", Options.SwitchBotName);

                            if (reason != SwitchBotPanicReason.NoPanic)
                            {
                                if (Panic == SwitchBotPanicReason.NoPanic)
                                {
                                    Logging?.LogCritical("PANIC!!! Panic because of switch bot \"{name}\", panic reason is: {reason}", Options.SwitchBotName, reason);
                                    Panic = reason;

                                    hasPanic = true;
                                }
                                else
                                {
                                    Logging?.LogTrace("Would panic because of switch bot \"{name}\" and reason {newReason}, but this switch already panicked due to the following reason: {existingReason}", Options.SwitchBotName, reason, Panic);

                                    hasPanic = false;
                                }
                            }
                            else
                            {
                                Logging?.LogTrace("Examined switch bot \"{name}\" for panic condition but there was no reason to panic", Options.SwitchBotName);

                                hasPanic = false;
                            }
                        }
                        else
                        {
                            if (Panic == SwitchBotPanicReason.NoPanic && reason != SwitchBotPanicReason.NoPanic)
                            {
                                Logging?.LogTrace("Would panic because of switch bot \"{name}\" and reason {reason}, but switch is in safe mode", Options.SwitchBotName, reason);
                            }

                            hasPanic = false;
                        }
                    }
                    else
                    {
                        if (Panic == SwitchBotPanicReason.NoPanic && reason != SwitchBotPanicReason.NoPanic)
                        {
                            Logging?.LogTrace("Would panic because of switch bot \"{name}\" and reason {reason}, but panicing is turned off for this switch", Options.SwitchBotName, reason);
                        }

                        hasPanic = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Switch bot polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Switch bot polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogCritical(ex, "Fatal error while trying to set panic state: {message}", ex.Message);

                    try
                    {
                        Panic = SwitchBotPanicReason.GeneralError;
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
                    Logging?.LogDebug("Switch bot \"{name}\" entered panic mode, checking if this switch should be turned off by poller", Options.SwitchBotName);
                    if (Options.TurnOffOnPanic)
                    {
                        if (!HoldBack)
                        {
                            Logging?.LogWarning("Switch bot \"{name}\" entered panic mode due to reason {reason}, turning switch off by poller since TurnOffOnPanic was set to true", Options.SwitchBotName, Panic);

                            try
                            {
                                var switchStateNew = Options.Api.TurnSwitchBotOffByName(Options.SwitchBotName);

                                CheckThreadCancellation();

                                if (switchStateNew != null)
                                {
                                    if (switchStateNew.Power == SwitchBotPowerState.Off)
                                    {
                                        Logging?.LogInformation("Switch bot \"{name}\" was turned off successfully by poller due to panic reason {reason}", Options.SwitchBotName, Panic);
                                        LastAutoPowerOff = DateTime.UtcNow;
                                    }
                                    else
                                    {
                                        throw new SwitchBotPollerException($"Unexpected switch state {switchStateNew.Power}\" recieved while trying to turn off switch by poller due to panic reason {Panic}");
                                    }
                                }
                                else
                                {
                                    throw new SwitchBotPollerException($"Switch bot state object was null for switch bot \"{Options.SwitchBotName}\" when trying to turn off switch bot by poller due to panic reason {Panic}");
                                }
                            }
                            catch (SwitchBotAPITimeoutException)
                            {
                                Logging?.LogCritical("Recieved http timeout trying to turn off switch bot \"{name}\" when trying to turn off switch bot by poller due to panic reason {reason}", Options.SwitchBotName, Panic);
                            }
                            catch (SwitchBotAPINameNotFoundException)
                            {
                                Logging?.LogCritical("Could not find switch bot with name \"{name}\" when trying to turn off switch bot by poller due to panic reason {reason}", Options.SwitchBotName, Panic);
                            }
                            catch (OperationCanceledException)
                            {
                                Logging?.LogTrace("Switch polling thread was canceled");
                                throw;
                            }
                            catch (ThreadInterruptedException)
                            {
                                Logging?.LogTrace("Switch polling thread was interrupted");
                                throw;
                            }
                            catch (Exception exTurnOff)
                            {
                                Logging?.LogCritical(exTurnOff, "While trying to turn off switch bot \"{name}\" by poller due to panic reason {reason}: {message}", Options.SwitchBotName, Panic, exTurnOff.Message);
                            }
                        }
                        else
                        {
                            Logging?.LogDebug("Not turning off switch bot \"{name}\" by poller due to panic reason {reason} because we are in holdback mode", Options.SwitchBotName, Panic);
                        }
                    }
                    else
                    {
                        Logging?.LogTrace("Switch bot \"{name}\" will not be turned off by poller (because TurnOffOnPanic was set to false) even though it entered panic mode, letting main thread decide what to do", Options.SwitchBotName);
                    }
                }

                return hasPanic;
            }
        }

        private bool SetState(SwitchBotPowerState switchState)
        {
            using (Logging?.BeginScope("SetState"))
            {
                CheckThreadCancellation();

                CurrentState = switchState;

                if (DoCheckState)
                {
                    Logging?.LogTrace("Verifying state for switch bot \"{name}\": {switchState}", Options.SwitchBotName, switchState);

                    switch (switchState)
                    {
                        case SwitchBotPowerState.On:
                            Logging?.LogTrace("Switch bot \"{name}\" is turned on", Options.SwitchBotName);
                            return false;
                        case SwitchBotPowerState.Off:
                            Logging?.LogTrace("Switch bot \"{name}\" is turned off", Options.SwitchBotName);
                            return SetPanic(SwitchBotPanicReason.SwitchOff);
                        case SwitchBotPowerState.Unknown:
                            if (Options.StrictStateCheck)
                            {
                                Logging?.LogWarning("Switch bot \"{name}\" has invalid state", Options.SwitchBotName);
                                return SetPanic(SwitchBotPanicReason.InvalidState);
                            }
                            else
                            {
                                Logging?.LogTrace("Switch bot \"{name}\" has invalid state, but StrictStateCheck is false, so not throwing any panic", Options.SwitchBotName);
                                return false;
                            }
                        default:
                            if (Options.StrictStateCheck)
                            {
                                Logging?.LogWarning("Switch bot \"{name}\" has unknown state {state}", Options.SwitchBotName, switchState);
                                return SetPanic(SwitchBotPanicReason.UnknownState);
                            }
                            else
                            {
                                Logging?.LogTrace("Switch bot \"{name}\" has unknown state {state}, but StrictStateCheck is false, so not throwing any panic", Options.SwitchBotName, switchState);
                                return false;
                            }
                    }
                }
                else
                {
                    Logging?.LogTrace("Not checking state for switch bot \"{name}\" as StateCheck setting was false", Options.SwitchBotName);

                    return false;
                }
            }
        }

        private bool SetBattery(short? battery)
        {
            using (Logging?.BeginScope("SetBattery"))
            {
                CheckThreadCancellation();

                CurrentBattery = battery;
                if (DoCheckBattery)
                {
                    Logging?.LogTrace("Verifying battery for switch bot \"{name}\": {battery} mW", Options.SwitchBotName, battery);
                    if (!battery.HasValue)
                    {
                        Logging?.LogTrace("Switch bot \"{name}\" not found or battery unknown", Options.SwitchBotName);

                        if (Options.StrictBatteryCheck)
                        {
                            return SetPanic(SwitchBotPanicReason.UnknownBattery);
                        }
                        else
                        {
                            Logging?.LogTrace("Not throwing panic as StrictBatteryCheck is set to false for switch bot \"{name}\"", Options.SwitchBotName);
                            return false;
                        }
                    }
                    else if (battery < 0)
                    {
                        Logging?.LogTrace("Switch bot \"{name}\" has returned negative value of battery ({battery}%)", Options.SwitchBotName, battery);

                        if (Options.StrictBatteryCheck)
                        {
                            var panicValue = SetPanic(SwitchBotPanicReason.InvalidBattery);
                            if (panicValue)
                            {
                                Logging?.LogWarning("Switch bot \"{name}\" panicked because it has returned a negative value of battery ({battery}%).", Options.SwitchBotName, battery);
                            }
                            return panicValue;
                        }
                        else
                        {
                            Logging?.LogTrace("Not throwing panic as StrictBatteryCheck is set to false for switch bot \"{name}\"", Options.SwitchBotName);
                            return false;
                        }
                    }
                    else if (battery == 0 && Options.MinBattery.HasValue && battery < Options.MinBattery.Value)
                    {
                        if (Options.ZeroBatteryIsValidPanic)
                        {
                            Logging?.LogTrace("Switch bot \"{name}\" has no battery. Switch bot has currently {currentBattery}% of battery, but should at least be having {minBattery}% battery", Options.SwitchBotName, battery, Options.MinBattery.Value);
                            var panicValue = SetPanic(SwitchBotPanicReason.BatteryTooLow);
                            if (panicValue)
                            {
                                Logging?.LogWarning("Switch bot \"{name}\" panicked because it has no battery. Switch bot has currently {currentBattery}% of battery, but should at least be having {minBattery}% battery", Options.SwitchBotName, battery, Options.MinBattery.Value);
                            }
                            return panicValue;
                        }
                        else
                        {
                            Logging?.LogTrace("Switch bot \"{name}\" has no battery. Switch bot has currently {currentBattery}% of battery, but should at least be having {minBattery}% battery. Since 0% battery is not a valid value for panic we will ignore this state and not throw panic.", Options.SwitchBotName, battery, Options.MinBattery.Value);
                            return false;
                        }
                    }
                    else if (Options.MinBattery.HasValue && battery < Options.MinBattery.Value)
                    {
                        Logging?.LogTrace("Switch bot \"{name}\" has no or too low battery. Switch bot has currently {currentBattery}% of battery, but should at least be having {minBattery}% battery", Options.SwitchBotName, battery, Options.MinBattery.Value);
                        var panicValue = SetPanic(SwitchBotPanicReason.BatteryTooLow);
                        if (panicValue)
                        {
                            Logging?.LogWarning("Switch bot \"{name}\" panicked because it has no or too low battery. Switch bot has currently {currentBattery}% of battery, but should at least be having {minBattery}% battery", Options.SwitchBotName, battery, Options.MinBattery.Value);
                        }
                        return panicValue;
                    }
                    else
                    {
                        Logging?.LogTrace("Switch bot \"{name}\" has returned valid battery of {battery}%", Options.SwitchBotName, battery);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogTrace("Not checking battery for switch bot \"{name}\" as LowBatteryCutOff setting was not provided (current battery is: {battery}%)", Options.SwitchBotName, battery);

                    return false;
                }
            }
        }

        #endregion

        #region Check methods

        public SwitchBotPanicReason? GetNewPanic(bool remove = true)
        {
            bool lockTaken = false;
            try
            {
                lockTaken = Monitor.TryEnter(lock_NewPanic, _lockTimeout);
                if (lockTaken)
                {
                    var retVal = (SwitchBotPanicReason?)(_hasNewPanic ? _panicNew : null);
                    if (remove)
                    {
                        _panicNew = SwitchBotPanicReason.NoPanic;
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

        public bool CheckSwitch()
        {
            using (Logging?.BeginScope("CheckSwitch"))
            {
                try
                {
                    CheckThreadCancellation();

                    bool hasPanicked_General = false;
                    bool hasPanicked_State = false;
                    bool hasPanicked_Battery = false;
                    SwitchBotStateContent? switchBotState = null;

                    if (DoMonitor)
                    {
                        Logging?.LogDebug("Checking properties of switch bot \"{name}\"", Options.SwitchBotName);
                        try
                        {
                            switchBotState = Options.Api.GetSwitchBotStateByName(Options.SwitchBotName, true);

                            CheckThreadCancellation();
                        }
                        catch (SwitchBotAPINameNotFoundException nnf)
                        {
                            Logging?.LogWarning(nnf, "Switch bot with name \"{name}\" not found", Options.SwitchBotName);

                            hasPanicked_General = SetPanic(SwitchBotPanicReason.NameNotFound);
                        }
                        catch (SwitchBotAPIResponseException rex)
                        {
                            Logging?.LogWarning(rex, "Switch bot \"{name}\" had error in response when checking properties", Options.SwitchBotName);

                            hasPanicked_General = SetPanic(SwitchBotPanicReason.ErrorResponse);
                        }
                        catch (SwitchBotAPITimeoutException)
                        {
                            Logging?.LogWarning("Recieved http timeout while checking state");

                            hasPanicked_General = SetPanic(SwitchBotPanicReason.HttpTimeout);
                        }
                        catch (SwitchBotAPINetworkException ex)
                        {
                            Logging?.LogWarning(ex, "Network error while checking state: {message}", ex.Message);

                            hasPanicked_General = SetPanic(SwitchBotPanicReason.NetworkError);
                        }
                        catch (OperationCanceledException)
                        {
                            Logging?.LogTrace("Switch bot polling thread was canceled");
                            throw;
                        }
                        catch (ThreadInterruptedException)
                        {
                            Logging?.LogTrace("Switch bot polling thread was interrupted");
                            throw;
                        }
                        catch (SwitchBotAPIException sbex)
                        {
                            Logging?.LogError(sbex, "Switch bot exception while trying to check for properties of switch bot \"{name}\"", Options.SwitchBotName);

                            hasPanicked_General = SetPanic(SwitchBotPanicReason.GeneralError);
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogError(ex, "While trying to check for properties of switch bot \"{name}\": {message}", Options.SwitchBotName, ex.Message);

                            hasPanicked_General = SetPanic(SwitchBotPanicReason.GeneralError);
                        }

                        if (!hasPanicked_General)
                        {
                            if (DoCheckAny)
                            {
                                if (switchBotState != null)
                                {
                                    Logging?.LogDebug("State of switch bot \"{name}\" is: {state}", Options.SwitchBotName, switchBotState.Power);
                                    Logging?.LogDebug("Battery of switch bot \"{name}\" is: {battery}%", Options.SwitchBotName, switchBotState.Battery);

                                    hasPanicked_State = SetState(switchBotState.Power);
                                    hasPanicked_Battery = SetBattery(switchBotState.Battery);
                                }
                                else
                                {
                                    Logging?.LogWarning("Switch bot state object was null for switch bot \"{name}\"", Options.SwitchBotName);
                                }
                            }
                            else
                            {
                                Logging?.LogTrace("Not checking any property for switch bot \"{name}\"", Options.SwitchBotName);
                            }
                        }

                        CheckThreadCancellation();

                        var hasPanicked = hasPanicked_General || hasPanicked_State || hasPanicked_Battery;

                        if (hasPanicked)
                            Logging?.LogWarning("Switch bot \"{name}\" reached panic condition {panicReason} and switched to panic state!", Options.SwitchBotName, Panic);
                        else if (Panic != SwitchBotPanicReason.NoPanic)
                            Logging?.LogDebug("Switch bot \"{name}\" remains in panic state {panicReason}", Options.SwitchBotName, Panic);
                        else
                            Logging?.LogDebug("Switch bot \"{name}\" remains in normal/non-panic state", Options.SwitchBotName);

                        return hasPanicked;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Switch bot polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Switch bot polling thread was interrupted");
                    throw;
                }
                catch (Exception exFatal)
                {
                    Logging?.LogCritical(exFatal, "Fatal error while checking switch bot properties: {message}", exFatal.Message);

                    return SetPanic(SwitchBotPanicReason.GeneralError);
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

                if (Panic != SwitchBotPanicReason.NoPanic)
                {
                    Logging?.LogDebug("Resetting panic {reason} for switch bot \"{name}\"", Panic, Options.SwitchBotName);
                    Panic = SwitchBotPanicReason.NoPanic;
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Switch bot \"{name}\" had no panic to reset", Options.SwitchBotName);
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
                                Logging?.LogInformation("Switch bot \"{name}\" entered safe mode", Options.SwitchBotName);
                                return true;
                            }
                            else
                            {
                                Logging?.LogDebug("Switch bot \"{name}\" already in safe mode", Options.SwitchBotName);
                                return false;
                            }
                        }
                        else
                        {
                            Logging?.LogWarning("Not entering safe mode for switch bot \"{name}\" as we are in holdback mode", Options.SwitchBotName);
                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogDebug("Skip switching to safe mode for switch bot \"{name}\" as EnterSafeMode option was set to false", Options.SwitchBotName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip switching to safe mode for switch bot \"{name}\" as ArmPanicMode option was set to false", Options.SwitchBotName);
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
                        Logging?.LogInformation("Switch bot \"{name}\" entered holdback mode", Options.SwitchBotName);
                        return true;
                    }
                    else
                    {
                        Logging?.LogDebug("Switch bot \"{name}\" already entered holdback mode", Options.SwitchBotName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip entering holdback mode for switch bot \"{name}\" as ArmPanicMode option was set to false", Options.SwitchBotName);
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
                            Logging?.LogInformation("Switch bot \"{name}\" panic mode armed", Options.SwitchBotName);
                            return true;
                        }
                        else
                        {
                            Logging?.LogWarning("Cannot arm panic mode for switch bot \"{name}\", must be monitoring or holdback mode without panic", Options.SwitchBotName);
                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogDebug("Switch bot \"{name}\" already armed panic mode", Options.SwitchBotName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip arming panic mode for switch bot \"{name}\" as ArmPanicMode option was set to false", Options.SwitchBotName);
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
                    Logging?.LogInformation("Switch bot \"{name}\" left holdback mode", Options.SwitchBotName);
                    return true;
                }
                else if (CheckForPanic)
                {
                    CheckForPanic = false;
                    Logging?.LogInformation("Switch bot \"{name}\" panic mode disarmed", Options.SwitchBotName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Switch bot \"{name}\" has not armed panic mode", Options.SwitchBotName);
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
                    Logging?.LogInformation("Switch bot \"{name}\" started monitoring", Options.SwitchBotName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Switch bot \"{name}\" already monitoring", Options.SwitchBotName);
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
                    Logging?.LogInformation("Switch bot \"{name}\" stopped monitoring", Options.SwitchBotName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Switch bot \"{name}\" isn't monitoring", Options.SwitchBotName);
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
