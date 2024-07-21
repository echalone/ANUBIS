using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Options
{
    public class WatcherPollerFileOptions
    {
        /// <summary>
        /// Path to the file to watch or write to
        /// This option is mandatory.
        /// </summary>
        public string FilePath { get; init; }

        /// <summary>
        /// Global name of the watcher file (for later references).
        /// This option is mandatory.
        /// </summary>
        public string WatcherFileName { get; init; }

        /// <summary>
        /// Sets if we should write panic state to file immediatly if we encounter panic on a write file
        /// Default is false/no.
        /// </summary>
        public bool WriteStateOnPanic { get; init; }

        /// <summary>
        /// The maximum age of the last update to the status timestamp in seconds
        /// Default is 120 seconds.
        /// </summary>
        public int MaxUpdateAgeInSeconds { get; init; }

        /// <summary>
        /// The maximum age of the last update to the status timestamp in negative seconds (if time might be ahead on remote computer).
        /// Default is null and therefore same as age in positive seconds.
        /// </summary>
        public int? MaxUpdateAgeInNegativeSeconds { get; init; }

        public int MaxUpdateAgeNegativeSecondsCalculated { get { return (MaxUpdateAgeInNegativeSeconds ?? MaxUpdateAgeInSeconds) * -1; } }

        /// <summary>
        /// Should we check the file state?
        /// Default is yes (true)
        /// </summary>
        public bool StateCheck { get; init; }

        /// <summary>
        /// Should we check the file state timestamp?
        /// Default is yes (true)
        /// </summary>
        public bool StateTimestampCheck { get; init; }

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long LockTimeoutInMilliseconds { get; init; }

        /// <summary>
        /// The min timeout for file access retry in milliseconds.
        /// Default is 100 milliseconds.
        /// </summary>
        public int FileAccessRetryMillisecondsMin { get; init; }

        /// <summary>
        /// The max timeout for file access retry in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public int FileAccessRetryMillisecondsMax { get; init; }

        /// <summary>
        /// The amount of times we will retry to access the file.
        /// Default is 10 times.
        /// </summary>
        public int FileAccessRetryCountMax { get; init; }

        /// <summary>
        /// Our machine will only send the mails if all read files with MailPriority set to true
        /// are unreachable or unresponsive (panic does not count).
        /// Default is false.
        /// </summary>
        public bool MailPriority { get; init; }

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

        public WatcherPollerFileOptions()
        {
            StateCheck = true;
            StateTimestampCheck = true;
            LockTimeoutInMilliseconds = 500;
            FileAccessRetryCountMax = 10;
            FileAccessRetryMillisecondsMin = 100;
            FileAccessRetryMillisecondsMax = 500;
            FilePath = "";
            WatcherFileName = "";
            MaxUpdateAgeInSeconds = 120;
            MaxUpdateAgeInNegativeSeconds = null;
            WriteStateOnPanic = false;
            SafeModeSensitive = true;
            MailPriority = false;
        }
    }
}
