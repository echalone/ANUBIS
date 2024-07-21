using ANUBISWatcher.Entities;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Options
{
    public class ClewarePollerOptions
    {
        /// <summary>
        /// The switches to monitor
        /// </summary>
        public List<ClewarePollerSwitch> Switches { get; init; }

        /// <summary>
        /// The time to sleep between switch property polling in milliseconds.
        /// Default is 5000 milliseconds.
        /// </summary>
        public long SleepTimeInMilliseconds { get; init; }

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 3 times the SleeTimeInMilliseconds value.
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
        /// Should we put all switches immediately into safe mode if one panics?
        /// Default is true and will do so.
        /// </summary>
        public bool AutoSafeMode { get; init; }

        /// <summary>
        /// The logger to log logging messages with.
        /// Default is null and no logger.
        /// </summary>
        public ILogger? Logger { get; init; }

        public ClewarePollerOptions()
        {
            LockTimeoutInMilliseconds = 500;
            SleepTimeInMilliseconds = 5000;
            AlertTimeInMilliseconds = null;
            Logger = null;
            AutoSafeMode = true;
            MinPollerCountToArm = 3;
            Switches = [];
        }
    }
}
