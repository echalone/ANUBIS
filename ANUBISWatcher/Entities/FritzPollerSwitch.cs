using ANUBISFritzAPI;
using ANUBISWatcher.Configuration.Serialization;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Entities
{
    public enum FritzPanicReason
    {
        [JsonEnumErrorValue]
        Unknown = 0,
        [JsonEnumName(true, "all", "every", "fallback", "", "*")]
        [JsonEnumNullValue]
        All,
        [JsonEnumName(true, "none", "NoPanic")]
        NoPanic,
        SafeModePowerUp,
        NameNotFound,
        InvalidState,
        InvalidPower,
        SwitchNotFound,
        SwitchOff,
        PowerTooLow,
        PowerTooHigh,
        ErrorPresence,
        ErrorState,
        ErrorPower,
        GeneralError,
        UnknownPresence,
        UnknownState,
        UnknownPower,
        HttpTimeout,
        NetworkError,
    }

    public class FritzPollerSwitch
    {
        #region Fields

        #region Constants

        private readonly object lock_TimestampLastUpdate = new();
        private readonly object lock_CurrentState = new();
        private readonly object lock_CurrentPresence = new();
        private readonly object lock_CurrentPower = new();
        private readonly object lock_Panic = new();
        private readonly object lock_NewPanic = new();

        #endregion

        #region Unchangable fields

        private readonly TimeSpan _lockTimeout = TimeSpan.Zero;
        private CancellationToken? _cancellationToken = null;

        #endregion

        #region Changable fields

        private volatile SwitchState _state = SwitchState.Unknown;
        private volatile SwitchPresence _presence = SwitchPresence.Missing;
        private long _power = 0;
        private bool _nullPower = true;
        private DateTime _timestampLastUpdate;
        private long? _previousPower = null;
        private volatile bool _previousIsDrawingPower = false;
        private volatile FritzPanicReason _panic = FritzPanicReason.NoPanic;
        private volatile FritzPanicReason _panicNew = FritzPanicReason.NoPanic;
        private volatile bool _hasNewPanic = false;

        #endregion

        #endregion

        #region Properties

        public FritzPollerSwitchOptions Options { get; init; }

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

        public bool CanBeArmed { get { return DoMonitor && (!HoldBack || Panic == FritzPanicReason.NoPanic); } }

        /// <summary>
        /// Is the system shut down according to this switch?
        /// </summary>
        public bool HasShutDown
        {
            get
            {
                return Options.MarkShutDownIfOff &&
                        CurrentPresence == SwitchPresence.Present &&
                        (CurrentState == SwitchState.Off ||
                            (CurrentPower.HasValue &&
                                CurrentPower <= (Options.LowPowerCutOff ?? 0)
                            )
                        );
            }
        }

        public SwitchState CurrentState
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

        public SwitchPresence CurrentPresence
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_CurrentPresence, _lockTimeout);
                    if (lockTaken)
                        return _presence;
                    else
                        throw new LockTimeoutException(nameof(CurrentPresence), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CurrentPresence);
                    }
                }
            }
            private set
            {
                lock (lock_CurrentPresence)
                {
                    _presence = value;
                }
            }
        }

        public long? CurrentPower
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_CurrentPower, _lockTimeout);
                    if (lockTaken)
                    {
                        if (_nullPower)
                            return null;
                        else
                            return Volatile.Read(ref _power);
                    }
                    else
                        throw new LockTimeoutException(nameof(CurrentPower), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CurrentPower);
                    }
                }
            }
            private set
            {
                lock (lock_CurrentPower)
                {
                    if (value.HasValue)
                    {
                        _nullPower = false;
                        Volatile.Write(ref _power, value.Value);
                    }
                    else
                    {
                        _nullPower = true;
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

        /// <summary>
        /// Is switch drawing power?
        /// </summary>
        public bool IsDrawingPower
        {
            get
            {
                return CurrentPresence == SwitchPresence.Present &&
                        CurrentState == SwitchState.On &&
                        CurrentPower.HasValue &&
                        (
                            CurrentPower > (Options.LowPowerCutOff ?? 0) ||
                            CurrentPower > ((Options.SafeModePowerUpAlarm ?? Options.LowPowerCutOff) ?? 0)
                        );
            }
        }

        /// <summary>
        /// Is switch drawing enough power to cause safe mode alarm?
        /// </summary>
        public bool IsDrawingSafeModePower
        {
            get
            {
                return Options.SafeModePowerUpAlarm.HasValue &&
                        CurrentPresence == SwitchPresence.Present &&
                        CurrentState == SwitchState.On &&
                        CurrentPower.HasValue &&
                        CurrentPower.Value > Options.SafeModePowerUpAlarm.Value;
            }
        }


        public FritzPanicReason Panic
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
                        if (value != FritzPanicReason.NoPanic)
                        {
                            _panicNew = value;
                            _hasNewPanic = true;
                        }
                        else
                        {
                            _panicNew = FritzPanicReason.NoPanic;
                            _hasNewPanic = false;
                        }
                    }
                }
            }
        }
        public bool DoCheckPower { get { return Options.MinPower.HasValue || Options.MaxPower.HasValue; } }

        public ILogger? Logging { get { return Options.Logger; } }

        #endregion

        #region Constructors

        public FritzPollerSwitch(FritzPollerSwitchOptions options)
        {
            Options = options;
            DoMonitor = false;
            CheckForPanic = false;
            LastAutoPowerOff = null;
            _previousIsDrawingPower = false;
            _previousPower = null;
            _lockTimeout = TimeSpan.FromMilliseconds(options.LockTimeoutInMilliseconds);
            UpdateTimestamp();
        }

        #endregion

        #region Helper methods

        private bool IsSafeModePanicReason(FritzPanicReason reason)
        {
            return reason == FritzPanicReason.SafeModePowerUp ||
                        (Options.SafeModeSensitive &&
                            (reason == FritzPanicReason.HttpTimeout || reason == FritzPanicReason.NetworkError ||
                            reason == FritzPanicReason.SwitchNotFound || reason == FritzPanicReason.NameNotFound));
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

                    Logging?.LogTrace("Updated LastUpdateTimestamp for Fritz switch \"{name}\" to {timestamp}", Options.SwitchName, now);
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

        private bool SetPanic(FritzPanicReason reason)
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
                            Logging?.LogTrace("Examining switch \"{name}\" for panic condition", Options.SwitchName);

                            if (reason != FritzPanicReason.NoPanic)
                            {
                                if (Panic == FritzPanicReason.NoPanic || reason == FritzPanicReason.SafeModePowerUp)
                                {
                                    Logging?.LogCritical("PANIC!!! Panic because of switch \"{name}\", panic reason is: {reason}", Options.SwitchName, reason);
                                    Panic = reason;

                                    hasPanic = true;
                                }
                                else
                                {
                                    Logging?.LogTrace("Would panic because of switch \"{name}\" and reason {newReason}, but this switch already panicked due to the following reason: {existingReason}", Options.SwitchName, reason, Panic);

                                    hasPanic = false;
                                }
                            }
                            else
                            {
                                Logging?.LogTrace("Examined switch \"{name}\" for panic condition but there was no reason to panic", Options.SwitchName);

                                hasPanic = false;
                            }
                        }
                        else
                        {
                            if (Panic == FritzPanicReason.NoPanic && reason != FritzPanicReason.NoPanic)
                            {
                                Logging?.LogTrace("Would panic because of switch \"{name}\" and reason {reason}, but switch is in safe mode", Options.SwitchName, reason);
                            }

                            hasPanic = false;
                        }
                    }
                    else
                    {
                        if (Panic == FritzPanicReason.NoPanic && reason != FritzPanicReason.NoPanic)
                        {
                            Logging?.LogTrace("Would panic because of switch \"{name}\" and reason {reason}, but panicing is turned off for this switch", Options.SwitchName, reason);
                        }

                        hasPanic = false;
                    }
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
                    Logging?.LogCritical(ex, "Fatal error while trying to set panic state: {message}", ex.Message);

                    try
                    {
                        Panic = FritzPanicReason.GeneralError;
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
                    Logging?.LogDebug("Switch \"{name}\" entered panic mode, checking if this switch should be turned off by poller", Options.SwitchName);
                    if (Options.TurnOffOnPanic)
                    {
                        if (!HoldBack)
                        {
                            Logging?.LogWarning("Switch \"{name}\" entered panic mode due to reason {reason}, turning switch off by poller since TurnOffOnPanic was set to true", Options.SwitchName, Panic);

                            try
                            {
                                var switchStateNew = Options.Api.TurnSwitchOffByName(Options.SwitchName);

                                CheckThreadCancellation();

                                if (switchStateNew == SwitchState.Off)
                                {
                                    Logging?.LogInformation("Switch \"{name}\" was turned off successfully by poller due to panic reason {reason}", Options.SwitchName, Panic);
                                    LastAutoPowerOff = DateTime.UtcNow;
                                }
                                else
                                {
                                    throw new FritzPollerException($"Unexpected switch state {switchStateNew}\" recieved while trying to turn off switch by poller due to panic reason {Panic}");
                                }
                            }
                            catch (FritzAPITimeoutException)
                            {
                                Logging?.LogCritical("Recieved http timeout trying to turn off switch \"{name}\" by poller due to panic reason {reason}", Options.SwitchName, Panic);
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
                                Logging?.LogCritical(exTurnOff, "While trying to turn off switch \"{name}\" by poller due to panic reason {reason}: {message}", Options.SwitchName, Panic, exTurnOff.Message);
                            }
                        }
                        else
                        {
                            Logging?.LogDebug("Not turning off switch \"{name}\" by poller due to panic reason {reason} because we are in holdback mode", Options.SwitchName, Panic);
                        }
                    }
                    else
                    {
                        Logging?.LogTrace("Switch \"{name}\" will not be turned off by poller (because TurnOffOnPanic was set to false) even though it entered panic mode, letting main thread decide what to do", Options.SwitchName);
                    }
                }

                return hasPanic;
            }
        }

        private bool SetPresence(SwitchPresence switchPresence)
        {
            using (Logging?.BeginScope("SetPresence"))
            {
                CheckThreadCancellation();

                CurrentPresence = switchPresence;
                Logging?.LogTrace("Verifying presence for switch \"{name}\": {switchPresence}", Options.SwitchName, switchPresence);

                switch (switchPresence)
                {
                    case SwitchPresence.Present:
                        Logging?.LogTrace("Switch \"{name}\" is present", Options.SwitchName);
                        return false;
                    case SwitchPresence.NameNotFound:
                        Logging?.LogTrace("Switch with name \"{name}\" not found", Options.SwitchName);
                        return SetPanic(FritzPanicReason.NameNotFound);
                    case SwitchPresence.Missing:
                        Logging?.LogTrace("Switch \"{name}\" is not present", Options.SwitchName);
                        return SetPanic(FritzPanicReason.SwitchNotFound);
                    case SwitchPresence.Error:
                        Logging?.LogTrace("Switch \"{name}\" had error checking presence", Options.SwitchName);
                        return SetPanic(FritzPanicReason.ErrorPresence);
                    default:
                        Logging?.LogWarning("Switch \"{name}\" has unknown presence", Options.SwitchName);
                        return SetPanic(FritzPanicReason.UnknownPresence);
                }
            }
        }

        private bool SetState(SwitchState switchState)
        {
            using (Logging?.BeginScope("SetState"))
            {
                CheckThreadCancellation();

                CurrentState = switchState;
                Logging?.LogTrace("Verifying state for switch \"{name}\": {switchState}", Options.SwitchName, switchState);

                switch (switchState)
                {
                    case SwitchState.On:
                        Logging?.LogTrace("Switch \"{name}\" is turned on", Options.SwitchName);
                        return false;
                    case SwitchState.NameNotFound:
                        Logging?.LogTrace("Switch with name \"{name}\" not found", Options.SwitchName);
                        return SetPanic(FritzPanicReason.NameNotFound);
                    case SwitchState.Off:
                        Logging?.LogTrace("Switch \"{name}\" is turned off", Options.SwitchName);
                        return SetPanic(FritzPanicReason.SwitchOff);
                    case SwitchState.Unknown:
                        Logging?.LogTrace("Switch \"{name}\" has invalid state", Options.SwitchName);
                        return SetPanic(FritzPanicReason.InvalidState);
                    case SwitchState.Error:
                        Logging?.LogTrace("Switch \"{name}\" had error checking state", Options.SwitchName);
                        return SetPanic(FritzPanicReason.ErrorState);
                    default:
                        Logging?.LogWarning("Switch \"{name}\" has unknown state {state}", Options.SwitchName, switchState);
                        return SetPanic(FritzPanicReason.UnknownState);
                }
            }
        }

        private bool SetPower(long? power)
        {
            using (Logging?.BeginScope("SetPower"))
            {
                CheckThreadCancellation();

                CurrentPower = power;
                if (DoCheckPower)
                {
                    Logging?.LogTrace("Verifying power for switch \"{name}\": {power} mW", Options.SwitchName, power);
                    if (!power.HasValue)
                    {
                        Logging?.LogTrace("Switch \"{name}\" not found or power unknown", Options.SwitchName);
                        return SetPanic(FritzPanicReason.UnknownPower);
                    }
                    else if (power < 0)
                    {
                        Logging?.LogTrace("Switch \"{name}\" has returned negative value for power ({power} mW)", Options.SwitchName, power);
                        var panicValue = SetPanic(FritzPanicReason.InvalidPower);
                        if (panicValue)
                        {
                            Logging?.LogWarning("Switch \"{name}\" paniced because of negative value for power ({power} mW)", Options.SwitchName, power);
                        }
                        return panicValue;
                    }
                    else if (Options.MinPower.HasValue && power < Options.MinPower.Value)
                    {
                        Logging?.LogTrace("Switch \"{name}\" is using no or too low power. Switch is currently using {currentPower} mW of power, but should at least be using {minPower} mW of power", Options.SwitchName, power, Options.MinPower.Value);
                        var panicValue = SetPanic(FritzPanicReason.PowerTooLow);
                        if (panicValue)
                        {
                            Logging?.LogWarning("Switch \"{name}\" paniced because of no or too low power. Switch is currently using {currentPower} mW of power, but should at least be using {minPower} mW of power", Options.SwitchName, power, Options.MinPower.Value);
                        }
                        return panicValue;
                    }
                    else if (Options.MaxPower.HasValue && power > Options.MaxPower.Value)
                    {
                        Logging?.LogTrace("Switch \"{name}\" is using too high power. Switch is currently using {currentPower} mW of power, but should be using no more than {maxPower} mW of power", Options.SwitchName, power, Options.MaxPower.Value);
                        var panicValue = SetPanic(FritzPanicReason.PowerTooHigh);
                        if (panicValue)
                        {
                            Logging?.LogWarning("Switch \"{name}\" paniced because of too high power. Switch is currently using {currentPower} mW of power, but should be using no more than {minPower} mW of power", Options.SwitchName, power, Options.MaxPower.Value);
                        }
                        return panicValue;
                    }
                    else
                    {
                        Logging?.LogTrace("Switch \"{name}\" has returned valid power of {power} mW", Options.SwitchName, power);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogTrace("Not checking power for switch \"{name}\" as min/max power settings were not provided (current power is: {power} mW)", Options.SwitchName, power);

                    return false;
                }
            }
        }

        #endregion

        #region Check methods

        public FritzPanicReason? GetNewPanic(bool remove = true)
        {
            bool lockTaken = false;
            try
            {
                lockTaken = Monitor.TryEnter(lock_NewPanic, _lockTimeout);
                if (lockTaken)
                {
                    var retVal = (FritzPanicReason?)(_hasNewPanic ? _panicNew : null);
                    if (remove)
                    {
                        _panicNew = FritzPanicReason.NoPanic;
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

        public bool CheckPresence()
        {
            using (Logging?.BeginScope("CheckPresence"))
            {
                try
                {
                    CheckThreadCancellation();

                    if (DoMonitor)
                    {
                        SwitchPresence presence = SwitchPresence.Error;
                        Logging?.LogDebug("Checking presence of switch \"{name}\"", Options.SwitchName);
                        try
                        {
                            presence = Options.Api.GetSwitchPresenceByName(Options.SwitchName, true);
                            Logging?.LogDebug("Presence of switch \"{name}\" is: {presence}", Options.SwitchName, presence);
                        }
                        catch (FritzAPITimeoutException)
                        {
                            Logging?.LogWarning("Recieved http timeout while checking presence");

                            return SetPanic(FritzPanicReason.HttpTimeout);
                        }
                        catch (FritzAPINetworkException ex)
                        {
                            Logging?.LogWarning(ex, "Network error while checking presence: {message}", ex.Message);

                            return SetPanic(FritzPanicReason.NetworkError);
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
                            Logging?.LogError(ex, "While trying to check for presence of switch \"{name}\": {message}", Options.SwitchName, ex.Message);
                        }

                        CheckThreadCancellation();

                        return SetPresence(presence);
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

        public bool CheckState()
        {
            using (Logging?.BeginScope("CheckState"))
            {
                try
                {
                    CheckThreadCancellation();

                    if (DoMonitor)
                    {
                        SwitchState state = SwitchState.Error;
                        Logging?.LogDebug("Checking state of switch \"{name}\"", Options.SwitchName);
                        try
                        {
                            state = Options.Api.GetSwitchStateByName(Options.SwitchName, true);

                            CheckThreadCancellation();

                            Logging?.LogDebug("State of switch \"{name}\" is: {state}", Options.SwitchName, state);
                        }
                        catch (FritzAPITimeoutException)
                        {
                            Logging?.LogWarning("Recieved http timeout while checking state");

                            return SetPanic(FritzPanicReason.HttpTimeout);
                        }
                        catch (FritzAPINetworkException ex)
                        {
                            Logging?.LogWarning(ex, "Network error while checking state: {message}", ex.Message);

                            return SetPanic(FritzPanicReason.NetworkError);
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
                            Logging?.LogError(ex, "While trying to check for state of switch \"{name}\": {message}", Options.SwitchName, ex.Message);
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

        public bool CheckPower()
        {
            using (Logging?.BeginScope("CheckPower"))
            {
                try
                {
                    CheckThreadCancellation();

                    if (DoMonitor && DoCheckPower)
                    {
                        long? power = null;
                        bool errorPanic = false;
                        Logging?.LogDebug("Checking power of switch \"{name}\"", Options.SwitchName);
                        try
                        {
                            power = Options.Api.GetSwitchPowerByName(Options.SwitchName, true);

                            CheckThreadCancellation();

                            if (power != null)
                            {
                                Logging?.LogDebug("Power of switch \"{name}\" is: {presence} mW", Options.SwitchName, power);
                            }
                            else
                            {
                                Logging?.LogDebug("Power of switch \"{name}\" is unknown", Options.SwitchName);
                            }
                        }
                        catch (FritzAPITimeoutException)
                        {
                            Logging?.LogWarning("Recieved http timeout while checking power");

                            return SetPanic(FritzPanicReason.HttpTimeout);
                        }
                        catch (FritzAPINetworkException ex)
                        {
                            Logging?.LogWarning(ex, "Network error while checking power: {message}", ex.Message);

                            return SetPanic(FritzPanicReason.NetworkError);
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
                            Logging?.LogTrace(ex, "Switch \"{name}\" had error checking power", Options.SwitchName);

                            errorPanic = SetPanic(FritzPanicReason.ErrorPower);

                            CheckThreadCancellation();

                            Logging?.LogError(ex, "While trying to check for power of switch \"{name}\": {message}", Options.SwitchName, ex.Message);
                        }

                        return SetPower(power) || errorPanic;
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
                    bool hasPanicked_Presence = false;
                    bool hasPanicked_State = false;
                    bool hasPanicked_Power = false;

                    try
                    {
                        Logging?.LogDebug("Begin checking switch \"{name}\"", Options.SwitchName);
                        hasPanicked_Presence = CheckPresence();
                        hasPanicked_State = CheckState();
                        hasPanicked_Power = CheckPower();

                        if (InSafeMode && !_previousIsDrawingPower && IsDrawingSafeModePower)
                        {
                            Logging?.LogCritical("Switch \"{name}\" is in safe mode and was previously powered down but seems to have powered up again by drawing {power} mW power which is above safe mode threshold of {safeModeThreshold} mW", Options.SwitchName, CurrentPower, Options.SafeModePowerUpAlarm);
                            hasPanicked_General = SetPanic(FritzPanicReason.SafeModePowerUp) || hasPanicked_General;
                            _previousIsDrawingPower = true;
                        }
                        else
                        {
                            // once we fell below the normal threshold make the safe mode our new threshold
                            if (!_previousIsDrawingPower)
                                _previousIsDrawingPower = IsDrawingSafeModePower;
                            else
                                _previousIsDrawingPower = IsDrawingPower;
                        }

                        CheckThreadCancellation();

                        if ((CheckForPanic || InSafeMode) &&
                            Options.TurnOffOnLowPower &&
                            Options.LowPowerCutOff.HasValue &&
                            CurrentState == SwitchState.On &&
                            _previousPower.HasValue &&
                            _previousPower > Options.LowPowerCutOff.Value)
                        {
                            Logging?.LogTrace("Checking turned on and used switch \"{name}\" for power usage", Options.SwitchName);
                            if (CurrentPower.HasValue && CurrentPower <= Options.LowPowerCutOff.Value)
                            {
                                if (!HoldBack)
                                {
                                    Logging?.LogWarning("Switch \"{name}\" lost power usage (current power {currentPower} mW is below {lowPowerCutOff} mW), turning switch off by poller now", Options.SwitchName, CurrentPower, Options.LowPowerCutOff.Value);

                                    try
                                    {
                                        var switchStateNew = Options.Api.TurnSwitchOffByName(Options.SwitchName);

                                        CheckThreadCancellation();

                                        if (switchStateNew == SwitchState.Off)
                                        {
                                            Logging?.LogInformation("Switch \"{name}\" was turned off successfully by poller due to lost power usage", Options.SwitchName);
                                            LastAutoPowerOff = DateTime.UtcNow;
                                        }
                                        else
                                        {
                                            throw new FritzPollerException($"Unexpected switch state \"{switchStateNew}\" recieved while trying to turn off switch due to lost power usage by poller");
                                        }
                                    }
                                    catch (FritzAPITimeoutException)
                                    {
                                        Logging?.LogWarning("Recieved http timeout while trying to turn off switch \"{name}\" by poller because of lost power usage", Options.SwitchName);

                                        hasPanicked_General = SetPanic(FritzPanicReason.HttpTimeout) || hasPanicked_General;
                                    }
                                    catch (FritzAPINetworkException ex)
                                    {
                                        Logging?.LogWarning(ex, "Network error while trying to turn off switch \"{name}\" by poller because of lost power usage: {message}", Options.SwitchName, ex.Message);

                                        hasPanicked_General = SetPanic(FritzPanicReason.NetworkError) || hasPanicked_General;
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
                                        Logging?.LogCritical(exTurnOff, "While trying to turn off switch \"{name}\" by poller because of lost power usage: {message}", Options.SwitchName, exTurnOff.Message);
                                        hasPanicked_General = SetPanic(FritzPanicReason.GeneralError) || hasPanicked_General;
                                    }
                                }
                                else
                                {
                                    Logging?.LogDebug("Switch \"{name}\" lost power usage (current power {currentPower} mW is below {lowPowerCutOff} mW), but not turning switch off by poller because we are in holdback mode", Options.SwitchName, CurrentPower, Options.LowPowerCutOff.Value);
                                }
                            }
                            else
                            {
                                Logging?.LogTrace("Switch \"{name}\" is still using power, not taking any special action", Options.SwitchName);
                            }
                        }
                        _previousPower = CurrentPower;

                        Logging?.LogDebug("Finished checking switch \"{name}\"", Options.SwitchName);
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
                        Logging?.LogError(ex, "General error while checking switch properties: {message}", ex.Message);
                        hasPanicked_General = SetPanic(FritzPanicReason.GeneralError) || hasPanicked_General;
                    }

                    CheckThreadCancellation();

                    var hasPanicked = hasPanicked_General || hasPanicked_Presence || hasPanicked_State || hasPanicked_Power;

                    if (hasPanicked)
                        Logging?.LogWarning("Switch \"{name}\" reached panic condition {panicReason} and switched to panic state!", Options.SwitchName, Panic);
                    else if (Panic != FritzPanicReason.NoPanic)
                        Logging?.LogDebug("Switch \"{name}\" remains in panic state {panicReason}", Options.SwitchName, Panic);
                    else
                        Logging?.LogDebug("Switch \"{name}\" remains in normal/non-panic state", Options.SwitchName);

                    return hasPanicked;
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
                catch (Exception exFatal)
                {
                    Logging?.LogCritical(exFatal, "Fatal error while checking switch properties: {message}", exFatal.Message);

                    return SetPanic(FritzPanicReason.GeneralError);
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

                if (Panic != FritzPanicReason.NoPanic)
                {
                    Logging?.LogDebug("Resetting panic {reason} for switch \"{name}\"", Panic, Options.SwitchName);
                    Panic = FritzPanicReason.NoPanic;
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Switch \"{name}\" had no panic to reset", Options.SwitchName);
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
                                Logging?.LogInformation("Switch \"{name}\" entered safe mode", Options.SwitchName);
                                return true;
                            }
                            else
                            {
                                Logging?.LogDebug("Switch \"{name}\" already in safe mode", Options.SwitchName);
                                return false;
                            }
                        }
                        else
                        {
                            Logging?.LogWarning("Not entering safe mode for switch \"{name}\" as we are in holdback mode", Options.SwitchName);
                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogDebug("Skip switching to safe mode for switch \"{name}\" as EnterSafeMode option was set to false", Options.SwitchName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip switching to safe mode for switch \"{name}\" as ArmPanicMode option was set to false", Options.SwitchName);
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
                        Logging?.LogInformation("Switch \"{name}\" entered holdback mode", Options.SwitchName);
                        return true;
                    }
                    else
                    {
                        Logging?.LogDebug("Switch \"{name}\" already entered holdback mode", Options.SwitchName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip entering holdback mode for switch \"{name}\" as ArmPanicMode option was set to false", Options.SwitchName);
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
                            Logging?.LogInformation("Switch \"{name}\" panic mode armed", Options.SwitchName);
                            return true;
                        }
                        else
                        {
                            Logging?.LogWarning("Cannot arm panic mode for switch \"{name}\", must be monitoring or holdback mode without panic", Options.SwitchName);
                            return false;
                        }
                    }
                    else
                    {
                        Logging?.LogDebug("Switch \"{name}\" already armed panic mode", Options.SwitchName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Skip arming panic mode for switch \"{name}\" as ArmPanicMode option was set to false", Options.SwitchName);
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
                    Logging?.LogInformation("Switch \"{name}\" left holdback mode", Options.SwitchName);
                    return true;
                }
                else if (CheckForPanic)
                {
                    CheckForPanic = false;
                    Logging?.LogInformation("Switch \"{name}\" panic mode disarmed", Options.SwitchName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Switch \"{name}\" has not armed panic mode", Options.SwitchName);
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
                    Logging?.LogInformation("Switch \"{name}\" started monitoring", Options.SwitchName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Switch \"{name}\" already monitoring", Options.SwitchName);
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
                    Logging?.LogInformation("Switch \"{name}\" stopped monitoring", Options.SwitchName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Switch \"{name}\" isn't monitoring", Options.SwitchName);
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
