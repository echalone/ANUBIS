using ANUBISClewareAPI;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Options
{
    public class ClewarePollerSwitchOptions
    {
        /// <summary>
        /// Reference to the Cleware api object to use.
        /// This option is mandatory.
        /// </summary>
        public ClewareAPI Api { get; init; }

        /// <summary>
        /// Name of the switch.
        /// This option is mandatory.
        /// </summary>
        public string USBSwitchName { get; init; }

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
        /// Sets if switch should be turned off in poller as well if encountering any panic
        /// Default is false/no.
        /// </summary>
        public bool TurnOffOnPanic { get; init; }

        /// <summary>
        /// Sets if switch should mark the system as shut down if it has been turned off
        /// Default is false/no.
        /// </summary>
        public bool MarkShutDownIfOff { get; init; }

        /// <summary>
        /// Should the switch throw panic if it was turned back on during safe mode?
        /// Default is true.
        /// </summary>
        public bool SafeModeTurnOnAlarm { get; init; }

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
        public ClewarePollerSwitchOptions()
#pragma warning restore 8618
        {
            LockTimeoutInMilliseconds = 500;
            SafeModeTurnOnAlarm = true;
            USBSwitchName = "";
            ArmPanicMode = true;
            EnterSafeMode = true;
            TurnOffOnPanic = false;
            MarkShutDownIfOff = false;
            SafeModeSensitive = true;
        }
    }
}
