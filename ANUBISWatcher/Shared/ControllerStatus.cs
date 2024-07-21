namespace ANUBISWatcher.Shared
{
    public enum ControllerStatus
    {
        /// <summary>
        /// ANUBIS Watcher is stopped.
        /// This status is used when the program has not started monitoring the sensors yet or when it has been stopped doing so.
        /// </summary>
        Stopped,

        /// <summary>
        /// ANUBIS Watcher is monitoring the sensors, but not acting on them.
        /// This status is used for setup of program and before arming the program, to manually check that the connection to the sensors is working and the values are in order before arming the program.
        /// </summary>
        Monitoring,

        /// <summary>
        /// ANUBIS Watcher will report any panic scenario in any of the sensors that would be armed,
        /// but it will not act upon those panics.
        /// This status is used to see if we can enter the armed mode without causing immediat panic.
        /// </summary>
        Holdback,

        /// <summary>
        /// ANUBIS Watcher will report and act on any panic scenario of any sensors that are armed.
        /// This status is the default status while the system is waiting to be triggered by the program.
        /// </summary>
        Armed,

        /// <summary>
        /// ANUBIS Watcher will not throw a panic if any of the sensors that are configured for safe mode are in a panic scenario.
        /// This may or may not include the remote files. In case of a panic that has been thrown by the local program, it will
        /// also set the remote files into safe mode since these will then also throw a panic, so not to throw a panic again
        /// because of these remote systems now also going into panic mode. If the local system is in safe mode, but has not
        /// thrown a panic itself, it should still react to remote systems throwing a panic and throw a panic themselves.
        /// If a remote system becomes unresponsive they will still throw a panic, even in safe mode.
        /// SharedData.Controller.IsInSafeMode will be set to true if this mode has been reached. Even if only after ShutDown or Triggered mode have already
        /// been reached, so to always know if this mode is activated.  SharedData.Controller.AreRemoteFilesInSafeMode will be set to true if this mode
        /// has been reached for remote systems too, even if the SafeMode has already been activated without remote systems/files.
        /// This mode is used so not to throw a panic if the system is expected to shut down (be triggered, start system shutdown)
        /// </summary>
        SafeMode,

        /// <summary>
        /// ANUBIS Watcher has registered a shutdown of the system.
        /// This mode is used for information only, except for mail sending enabling, and may come after Armed or SafeMode.
        /// </summary>
        ShutDown,

        /// <summary>
        /// ANUBIS Watcher has triggered the shutdown of the system.
        /// This mode is used for information only, except for mail sending enabling, and may come after Armed, SafeMode or ShutDown
        /// </summary>
        Triggered,
    }
}
