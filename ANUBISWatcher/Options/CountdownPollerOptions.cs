using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Options
{
    public class CountdownPollerOptions
    {
        /// <summary>
        /// The time to sleep between switch property polling in milliseconds.
        /// Default is 100 milliseconds.
        /// </summary>
        public long SleepTimeInMilliseconds { get; init; }

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 50 times the SleeTimeInMilliseconds value (5 seconds for sleep time default value).
        /// </summary>
        public long? AlertTimeInMilliseconds { get; init; }

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long LockTimeoutInMilliseconds { get; init; }

        /// <summary>
        /// The minimum poller count to be able to leave holdback mode and arm panic mode
        /// Default is 10.
        /// </summary>
        public ushort MinPollerCountToArm { get; init; }

        /// <summary>
        /// Should we shut down the system on T0? Or is this a manual triggering?
        /// Default is true and will shut down the system on T0
        /// </summary>
        public bool ShutDownOnT0 { get; init; }

        /// <summary>
        /// Should we put everything immediately into safe mode if Countdown panics?
        /// Default is true
        /// </summary>
        public bool AutoSafeMode { get; init; }

        /// <summary>
        /// How long (in minutes) before the Countdown T-0 should we put everything into SafeMode?
        /// Default is 10 minutes.
        /// </summary>
        public int CountdownAutoSafeModeMinutes { get; init; }

        /// <summary>
        /// How long (in minutes) after the Countdown T0 should we check for shutdown?
        /// Default is 10 minutes.
        /// A value of 0 means that this will not be checked.
        /// </summary>
        public int CheckShutDownAfterMinutes { get; init; }

        /// <summary>
        /// When exactly should the countdown reach T0 (and system shutdown started if ShutDownOnT0 is true)
        /// Default is in 3 hours in the future
        /// </summary>
        public DateTime CountdownT0UTC { get; set; }

        /// <summary>
        /// The logger to log logging messages with.
        /// Default is null and no logger.
        /// </summary>
        public ILogger? Logger { get; init; }

        /// <summary>
        /// The mailing settings/options
        /// </summary>
        public MailingOptions MailSettings { get; init; }

        public CountdownPollerOptions()
        {
            LockTimeoutInMilliseconds = 500;
            SleepTimeInMilliseconds = 100;
            AlertTimeInMilliseconds = null;
            Logger = null;
            AutoSafeMode = true;
            ShutDownOnT0 = true;
            MinPollerCountToArm = 10;
            CountdownAutoSafeModeMinutes = 10;
            CheckShutDownAfterMinutes = 10;
            CountdownT0UTC = DateTime.UtcNow.AddHours(3);
            MailSettings = new MailingOptions();
        }
    }
}
