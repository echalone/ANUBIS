using ANUBISFritzAPI;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Options
{
    public class FritzPollerSwitchOptions
    {
        /// <summary>
        /// Reference to the fritz api object to use.
        /// This option is mandatory.
        /// </summary>
        public FritzAPI Api { get; init; }

        /// <summary>
        /// Name of the switch.
        /// This option is mandatory.
        /// </summary>
        public string SwitchName { get; init; }

        /// <summary>
        /// Sets if panic mode for this switch should be armed if ArmPanicMode method is called
        /// Default is true/yes.
        /// </summary>
        public bool ArmPanicMode { get; init; }

        /// <summary>
        /// Sets if safe mode for this switch should be entered if EnterSafeMode method is called
        /// Default is true/yes.
        /// </summary>
        public bool EnterSafeMode { get; init; }

        /// <summary>
        /// Sets if switch should mark the system as shut down if it has been turned off
        /// Default is false/no.
        /// </summary>
        public bool MarkShutDownIfOff { get; init; }

        /// <summary>
        /// Sets if switch should be turned off in poller as well if encountering any panic
        /// Default is false/no.
        /// </summary>
        public bool TurnOffOnPanic { get; init; }

        /// <summary>
        /// Below or equal to what mW should the switch be turned off if it is still turned on?
        /// This will only take effect if switch is in panic of safe mode
        /// Default is 0
        /// Example: 
        ///     0 will turn off switch if power usage is fully cut
        ///     200 will turn off switch if power usage fell to 200 mW or lower
        ///     null will never turn off switch due to power usage.
        ///     Switch will only be turned off if TurnOffOnLowPower is true,
        ///     which is its default value. Otherwise this value will
        ///     only be used to determine if the switch has been powered
        ///     down before a possible SafeModePowerUp alarm. If this value
        ///     is null the switch needs to lose all power for the switch
        ///     to be considered powered down for a SafeModePowerUpAlarm to
        ///     be possible.
        /// </summary>
        public long? LowPowerCutOff { get; init; }

        /// <summary>
        /// Defines if power will be cut if lowPowerCutOff level
        /// was reached (or below). This will not change the behaviour
        /// if lowPowerCutOff was set to null since this will always
        /// disable this feature. If a lowPowerCutOff value was
        /// set and this value is set to false then the lowPowerCutOff value
        /// will only be used to determine if a safeModePowerUp alert
        /// can be triggered.
        /// The default is true and will cut the power if the
        /// lowPowerCutOff value was reached.
        /// </summary>
        public bool TurnOffOnLowPower { get; set; }

        /// <summary>
        /// Above what mW should the switch throw panic if it was turned back on during safe mode?
        /// Default is 200
        /// Example: 
        ///     0 will put switch into panic mode if any power usage is back on after the power fell below or to LowPowerCutOff
        ///     200 will put switch into panic mode if power usage is back above 200 mW after the power fell below or to LowPowerCutOff
        ///     null will never throw panic due to power up during safe mode
        /// </summary>
        public long? SafeModePowerUpAlarm { get; init; }

        /// <summary>
        /// Below what power (in mW)  should the switch display the power in the graph warning colors? (has no actual functioning effect)
        /// Default is null and will be minPower + ((maxPower - minPower) * 0.2)
        /// </summary>
        public long? MinPowerWarn { get; set; } = null;

        /// <summary>
        /// Above what power (in mW) should the switch display the power in the graph warning colors? (has no actual functioning effect)
        /// Default is null and will be maxPower - ((maxPower - minPower) * 0.2)
        /// </summary>
        public long? MaxPowerWarn { get; set; } = null;

        /// <summary>
        /// Below what power (in mW) should the switch throw panic outside of safe mode?
        /// Default is null.
        /// </summary>
        public long? MinPower { get; init; }

        /// <summary>
        /// Above what power (in mW) should the switch throw panic outside of safe mode?
        /// If it was turned off before is determined by the value of LowPoweCutOff (and not MinPower!). 
        /// If the power shouldn't be cut by that value (for which it is used too) just turn that 
        /// feature off by setting TurnOffOnLowPower to false.
        /// Default is null.
        /// </summary>
        public long? MaxPower { get; init; }

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long LockTimeoutInMilliseconds { get; init; }

        /// <summary>
        /// Will we still be sensitive to certain errors in safe mode?
        /// Default is true.
        /// </summary>
        public bool SafeModeSensitive { get; init; }

        /// <summary>
        /// The logger to log logging messages with.
        /// Default is null and no logger.
        /// </summary>
        public ILogger? Logger { get; init; }

#pragma warning disable 8618
        public FritzPollerSwitchOptions()
#pragma warning restore 8618
        {
            LockTimeoutInMilliseconds = 500;
            SafeModePowerUpAlarm = 200;
            LowPowerCutOff = 0;
            SwitchName = "";
            ArmPanicMode = true;
            EnterSafeMode = true;
            TurnOffOnPanic = false;
            MarkShutDownIfOff = false;
            TurnOffOnLowPower = true;
            SafeModeSensitive = true;
        }
    }
}
