using ANUBISSwitchBotAPI;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Options
{
    public class SwitchBotPollerSwitchOptions
    {
        /// <summary>
        /// Reference to the switchbot api object to use.
        /// This option is mandatory.
        /// </summary>
        public SwitchBotAPI Api { get; init; }

        /// <summary>
        /// Name of the switch bot.
        /// This option is mandatory.
        /// </summary>
        public string SwitchBotName { get; init; }

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
        /// Below or equal to what percentage should the switch be turned off if it is still turned on?
        /// This will only take effect if switch is in panic of safe mode
        /// Default is 5
        /// Example: 
        ///     5 will turn off switch if battery is below 5 percent
        ///     10 will turn off switch if battery is below 10 percent
        ///     null will never turn off switch due to battery
        /// </summary>
        public long? MinBattery { get; init; }

        /// <summary>
        /// Should we use a strict battery check, that means we will throw panic if 
        /// we have any problem checking the battery (true), otherwise (false) we will only
        /// throw panic if we were able to check the battery AND it is below the LowBatteryCutOff limit.
        /// Default is false.
        /// </summary>
        public bool StrictBatteryCheck { get; init; }

        /// <summary>
        /// Should we check the power state of the switch (on/off)?
        /// Default is true.
        /// </summary>
        public bool StateCheck { get; init; }

        /// <summary>
        /// Should we use a strict state check, that means we will throw panic if 
        /// we have any problem checking the state (true), otherwise (false) we will only
        /// throw panic if we were able to check the state AND it is off or unknown.
        /// Default is false.
        /// </summary>
        public bool StrictStateCheck { get; init; }

        /// <summary>
        /// Should we throw panic if the battery is below battery cut off and
        /// the measured battery is 0? This could just mean there's no real
        /// data from the switch bot and a battery value of above 0 should have been
        /// registered previously. The default is false and will not throw panic if 
        /// the battery value is 0, even if that's below the battery cut off.
        /// Default is false.
        /// </summary>
        public bool ZeroBatteryIsValidPanic { get; init; }

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
        public SwitchBotPollerSwitchOptions()
#pragma warning restore 8618
        {
            LockTimeoutInMilliseconds = 500;
            StrictBatteryCheck = false;
            StrictStateCheck = false;
            StateCheck = true;
            MinBattery = 5;
            SwitchBotName = "";
            ArmPanicMode = true;
            EnterSafeMode = true;
            TurnOffOnPanic = false;
            ZeroBatteryIsValidPanic = false;
            MarkShutDownIfOff = false;
            SafeModeSensitive = true;
        }
    }
}
