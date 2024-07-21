using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Options
{
    public class MainControllerOptions
    {
        /// <summary>
        /// The time to sleep between loops.
        /// Default is 100 milliseconds.
        /// </summary>
        public long SleepTimeInMilliseconds { get; init; }

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 30 times the SleeTimeInMilliseconds value (3 seconds for sleep time default value).
        /// </summary>
        public long? AlertTimeInMilliseconds { get; init; }

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long LockTimeoutInMilliseconds { get; init; }

        /// <summary>
        /// How long (in minutes) after the system shutdown (state shutdown or triggered) should we send the email?
        /// Everything below 1 will be ignored and no mail will be sent.
        /// Default is 55 minutes
        /// </summary>
        public int SendMailEarliestAfterMinutes { get; init; }

        /// <summary>
        /// The logger to log logging messages with.
        /// Default is null and no logger.
        /// </summary>
        public ILogger? Logger { get; init; }

        public MainControllerOptions()
        {
            SendMailEarliestAfterMinutes = 55;
            LockTimeoutInMilliseconds = 500;
            SleepTimeInMilliseconds = 5000;
            Logger = null;
        }
    }
}
