using ANUBISWatcher.Entities;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Options
{
    public class WatcherFilePollerOptions
    {
        /// <summary>
        /// The files to monitor or write to
        /// </summary>
        public List<WatcherPollerFile> Files { get; init; }

        /// <summary>
        /// The time to sleep between switch property polling in milliseconds.
        /// Default is 5000 milliseconds.
        /// </summary>
        public long SleepTimeInMilliseconds { get; init; }

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 3 times the SleeTimeInMilliseconds value plus the maximum failure read/write time on all files.
        /// </summary>
        public long? AlertTimeInMilliseconds { get; init; }

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long LockTimeoutInMilliseconds { get; init; }

        /// <summary>
        /// The minimum poller count to be able to leave holdback mode and arm panic mode
        /// Default is 3.
        /// </summary>
        public ushort MinPollerCountToArm { get; init; }

        /// <summary>
        /// Are these files we write our status to (true) or files we read the status from others from (false)
        /// Default is false (reading status).
        /// </summary>
        public bool WriteTo { get; init; }

        /// <summary>
        /// Should we put all files immediately into safe mode if one panics?
        /// Default is null and will depend on if this is a write or read file.
        /// Default for write file: false, will not enter safe mode on panic
        /// Default for read file: true, will enter safe mode on panic
        /// </summary>
        public bool? AutoSafeMode { get; init; }

        /// <summary>
        /// Should we put all files immediately into safe mode if one panics?
        /// Default is null and will depend on if this is a write or read file.
        /// Default for write file: false, will not enter safe mode on panic
        /// Default for read file: true, will enter safe mode on panic
        /// </summary>
        public bool CalculatedAutoSafeMode { get { return AutoSafeMode ?? (!WriteTo); } }

        /// <summary>
        /// The logger to log logging messages with.
        /// Default is null and no logger.
        /// </summary>
        public ILogger? Logger { get; init; }

        public WatcherFilePollerOptions()
        {
            LockTimeoutInMilliseconds = 500;
            SleepTimeInMilliseconds = 5000;
            AlertTimeInMilliseconds = null;
            Logger = null;
            WriteTo = false;
            AutoSafeMode = null;
            MinPollerCountToArm = 3;
            Files = [];
        }
    }
}
