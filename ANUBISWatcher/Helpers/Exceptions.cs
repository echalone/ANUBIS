namespace ANUBISWatcher.Helpers
{
    public class LockTimeoutException : TimeoutException
    {
        public LockTimeoutException(string propertyName, TimeSpan lockTimeout)
            : base($"Lock for property \"{propertyName}\" could not be aquired in {lockTimeout}")
        {
        }
    }

    public class FritzPollerException : Exception
    {
        public FritzPollerException(string message)
            : base(message)
        { }
    }

    public class ClewarePollerException : Exception
    {
        public ClewarePollerException(string message)
            : base(message)
        { }
    }

    public class SwitchBotPollerException : Exception
    {
        public SwitchBotPollerException(string message)
            : base(message)
        { }
    }

    public class WatcherPollerException : Exception
    {
        public WatcherPollerException(string message)
            : base(message)
        { }
    }

    public class CountdownPollerException : Exception
    {
        public CountdownPollerException(string message)
            : base(message)
        { }
    }

    public class TriggerException : Exception
    {
        public TriggerException(string message)
            : base(message)
        {

        }
    }

    public class ControllerException : Exception
    {
        public ControllerException(string message)
            : base(message)
        {

        }
    }

    public class SharedDataException : Exception
    {
        public SharedDataException(string message)
            : base(message)
        {

        }
    }

    public class GeneratorException : Exception
    {
        public GeneratorException(string message)
            : base(message)
        {

        }
    }

    public class SendMailException : Exception
    {
        public SendMailException(string message)
            : base(message)
        {

        }
    }

    public class ConfigException : Exception
    {
        public ConfigException(string message)
            : base(message)
        {

        }
    }

    public class MissingFileException : Exception
    {
        public MissingFileException(string message)
            : base(message)
        {

        }
    }
}
