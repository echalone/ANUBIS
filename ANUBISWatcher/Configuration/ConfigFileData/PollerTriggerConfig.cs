using ANUBISWatcher.Configuration.Serialization;
using ANUBISWatcher.Entities;
using ANUBISWatcher.Shared;
using System.Text.Json.Serialization;

namespace ANUBISWatcher.Configuration.ConfigFileData
{
#pragma warning disable IDE1006
    public class PollerAndTriggerConfig
    {
        public BasePollerData[] pollers { get; set; } = [];

        public TriggerConfig[] triggerConfigs { get; set; } = [];
    }

    [JsonDerivedType(typeof(SwitchBotPollerData), typeDiscriminator: "switchBot")]
    [JsonDerivedType(typeof(FritzPollerData), typeDiscriminator: "fritz")]
    [JsonDerivedType(typeof(ClewareUSBPollerData), typeDiscriminator: "usb")]
    [JsonDerivedType(typeof(RemoteFilePollerData), typeDiscriminator: "remoteFile")]
    [JsonDerivedType(typeof(LocalFilePollerData), typeDiscriminator: "localFile")]
    [JsonDerivedType(typeof(CountdownPollerData), typeDiscriminator: "countdown")]
    [JsonDerivedType(typeof(ControllerData), typeDiscriminator: "controller")]
    [JsonDerivedType(typeof(GeneralData), typeDiscriminator: "general")]
    public abstract class BasePollerData
    {
        /// <summary>
        /// Has the sanity check failed for this config entity?
        /// </summary>
        [JsonIgnore]
        public bool sanityCheckFailed { get; set; } = false;

        /// <summary>
        /// Why has sanit check failed for this config entity, if it has failed?
        /// </summary>
        [JsonIgnore]
        public List<string> sanityCheckError { get; set; } = [];

        public bool enabled { get; set; } = true;
    }

    public class SwitchBotPollerData : BasePollerData
    {
        public SwitchBotPollerConfigOptions options { get; set; } = new SwitchBotPollerConfigOptions();

        public SwitchBotSwitchConfigOptions[] switches { get; set; } = [];

        public PollerTriggerConfig_SwitchBot[] generalTriggers { get; set; } = [];

        public PollerTriggerConfigWithId_SwitchBot[] switchTriggers { get; set; } = [];
    }

    public class FritzPollerData : BasePollerData
    {
        public FritzPollerConfigOptions options { get; set; } = new FritzPollerConfigOptions();

        public FritzSwitchConfigOptions[] switches { get; set; } = [];

        public PollerTriggerConfig_Fritz[] generalTriggers { get; set; } = [];

        public PollerTriggerConfigWithId_Fritz[] switchTriggers { get; set; } = [];
    }

    public class ClewareUSBPollerData : BasePollerData
    {
        public ClewareUSBPollerConfigOptions options { get; set; } = new ClewareUSBPollerConfigOptions();

        public ClewareUSBSwitchConfigOptions[] switches { get; set; } = [];

        public PollerTriggerConfig_ClewareUSB[] generalTriggers { get; set; } = [];

        public PollerTriggerConfigWithId_ClewareUSB[] switchTriggers { get; set; } = [];
    }

    public class RemoteFilePollerData : BasePollerData
    {
        public FilePollerConfigOptions options { get; set; } = new FilePollerConfigOptions();

        public FileConfigOptions[] files { get; set; } = [];

        public PollerTriggerConfig_File[] generalTriggers { get; set; } = [];

        public PollerTriggerConfigWithId_File[] fileTriggers { get; set; } = [];
    }

    public class LocalFilePollerData : BasePollerData
    {
        public FilePollerConfigOptions options { get; set; } = new FilePollerConfigOptions();

        public FileConfigOptions[] files { get; set; } = [];

        public PollerTriggerConfig_File[] generalTriggers { get; set; } = [];

        public PollerTriggerConfigWithId_File[] fileTriggers { get; set; } = [];
    }

    public class CountdownPollerData : BasePollerData
    {
        public CountdownConfigOptions options { get; set; } = new CountdownConfigOptions();
        public MailingConfigOptions mailingOptions { get; set; } = new MailingConfigOptions();

        public PollerTriggerConfig_Universal[] generalTriggers { get; set; } = [];

        public PollerTriggerConfigWithId_Universal[] countdownTriggers { get; set; } = [];
    }

    public class ControllerData : BasePollerData
    {
        public ControllerConfigOptions options { get; set; } = new ControllerConfigOptions();

        public PollerTriggerConfig_Universal[] generalTriggers { get; set; } = [];

        public PollerTriggerConfigWithId_Universal[] pollerTriggers { get; set; } = [];
    }

    public class GeneralData : BasePollerData
    {
        public PollerTriggerConfigWithId_Universal[] generalTriggers { get; set; } = [];

        /// <summary>
        /// Fallback Triggers will be searched for an enabled machting id/panic if no
        /// enabled matching id/panic in the triggers of the corresponding poller was found.
        /// Any trigger config defined under this class will on a panic
        /// first be searched for an exactly matching id and panic,
        /// then for an exactly matching id with any panic ("all"),
        /// then for any id ("*") with an exactly matching panic,
        /// and then for an entry with any id ("*") and any panic ("all") defined.
        /// These triggers will NOT be executed just because a configuration is not found,
        /// that's what the "isFallback" flag is on a trigger configuration.
        /// </summary>
        public PollerTriggerConfigWithId_Universal[] fallbackTriggers { get; set; } = [];
    }

    /// <summary>
    /// Any trigger config defined under this class will on a panic
    /// first be searched for an exactly matching id and panic,
    /// then for an exactly matching id with any panic ("all"),
    /// then for any id ("*") with an exactly matching panic,
    /// and then for an entry with any id ("*") and any panic ("all") defined.
    /// For whatever one or more configurations are found this will be executed.
    /// If no matching enabled configuration is found it will search the fallbackTriggers
    /// in the GeneralData for a matching enabled configuration.
    /// If a matching configuration is found but an enabled trigger configuration was not found with this name
    /// in the trigger configuration definitions then the first enabled trigger configuration with
    /// the "isFallback" flag set to true will be executed.
    /// </summary>
    public class PollerTriggerConfigBase
    {
        private const int c_DefaultLockTimeoutInMilliseconds = 1000;

        private object lock_RepeatCount = new();

        /// <summary>
        /// how often has this configuration been repeated?
        /// </summary>
        private volatile uint _repeatCount = 0;

        /// <summary>
        /// Has the sanity check failed for this config entity?
        /// </summary>
        [JsonIgnore]
        public bool sanityCheckFailed { get; set; } = false;

        /// <summary>
        /// Why has sanit check failed for this config entity, if it has failed?
        /// </summary>
        [JsonIgnore]
        public List<string> sanityCheckError { get; set; } = [];

        /// <summary>
        /// The trigger configuration to execute if id and panic are matching
        /// </summary>
        public string? config { get; set; }
        public bool enabled { get; set; } = true;

        /// <summary>
        /// How often should we repeat this configuration at most?
        /// Default is 0 and means there are no restrictions.
        /// </summary>
        public uint maxRepeats { get; set; } = 0;

        public void ResetRepeatCount()
        {
            if (maxRepeats > 0)
            {
                bool lockTaken = Monitor.TryEnter(lock_RepeatCount, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_RepeatCount = newLock;
                        lockTaken = true;
                    }

                    _repeatCount = 0;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_RepeatCount);
                    }
                }
            }
        }

        /// <summary>
        /// Can we execute this trigger according to its trigger execution count?
        /// </summary>
        /// <returns></returns>
        public bool AskForPermission(bool increaseRepeatCount = true)
        {
            if (maxRepeats > 0)
            {
                bool lockTaken = Monitor.TryEnter(lock_RepeatCount, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_RepeatCount = newLock;
                        lockTaken = true;
                    }

                    if (_repeatCount < maxRepeats)
                    {
                        if (increaseRepeatCount)
                            _repeatCount++;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_RepeatCount);
                    }
                }
            }
            else
            {
                return true;
            }
        }
    }

    public class PollerTriggerConfigWithIdBase : PollerTriggerConfigBase
    {
        /// <summary>
        /// The ids for which to execute the trigger configuration if a panic matches too
        /// </summary>
        public string[]? id { get; set; }
    }

    public class PollerTriggerConfig_Universal : PollerTriggerConfigBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<UniversalPanicReason[], UniversalPanicReason>))]
        public UniversalPanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfigWithId_Universal : PollerTriggerConfigWithIdBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<UniversalPanicReason[], UniversalPanicReason>))]
        public UniversalPanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfig_SwitchBot : PollerTriggerConfigBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<SwitchBotPanicReason[], SwitchBotPanicReason>))]
        public SwitchBotPanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfigWithId_SwitchBot : PollerTriggerConfigWithIdBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<SwitchBotPanicReason[], SwitchBotPanicReason>))]
        public SwitchBotPanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfig_Fritz : PollerTriggerConfigBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<FritzPanicReason[], FritzPanicReason>))]
        public FritzPanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfigWithId_Fritz : PollerTriggerConfigWithIdBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<FritzPanicReason[], FritzPanicReason>))]
        public FritzPanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfig_ClewareUSB : PollerTriggerConfigBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<ClewareUSBPanicReason[], ClewareUSBPanicReason>))]
        public ClewareUSBPanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfigWithId_ClewareUSB : PollerTriggerConfigWithIdBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<ClewareUSBPanicReason[], ClewareUSBPanicReason>))]
        public ClewareUSBPanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfig_File : PollerTriggerConfigBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<WatcherFilePanicReason[], WatcherFilePanicReason>))]
        public WatcherFilePanicReason[]? panic { get; set; }
    }

    public class PollerTriggerConfigWithId_File : PollerTriggerConfigWithIdBase
    {
        /// <summary>
        /// The panics for which to execute the trigger configuration if an id matches too
        /// </summary>
        [JsonConverter(typeof(JsonEnumArrayConverter<WatcherFilePanicReason[], WatcherFilePanicReason>))]
        public WatcherFilePanicReason[]? panic { get; set; }
    }

    public class TriggerConfig
    {
        /// <summary>
        /// Has the sanity check failed for this config entity?
        /// </summary>
        [JsonIgnore]
        public bool sanityCheckFailed { get; set; } = false;

        /// <summary>
        /// Why has sanit check failed for this config entity, if it has failed?
        /// </summary>
        [JsonIgnore]
        public List<string> sanityCheckError { get; set; } = [];

        public string? id { get; set; }

        /// <summary>
        /// Is this a fallback configuration that will be used if a configuration name cannot be found?
        /// default is false.
        /// These triggers will be executed when a configuration with a specific name is not found,
        /// even though there was an enabled matching id/panic entry for it in the trigger list defined.
        /// This does not mean that this configuration will be used if no matching enabled id/panic definition
        /// was found, that's what the fallbackTriggers in the general data are for.
        /// </summary>
        public bool isFallback { get; set; } = false;

        /// <summary>
        /// is this configuration repeatable? default is true
        /// </summary>
        public bool repeatable { get; set; } = true;
        public bool enabled { get; set; } = true;

        /// <summary>
        /// Was this switch disabled by the discoverer (and can therefore be enabled if discovered)?
        /// </summary>
        [JsonIgnore]
        public bool disabledByDiscoverer { get; set; } = false;

        public SingleTrigger[] triggers { get; set; } = [];
    }

    [JsonDerivedType(typeof(SingleDeviceTrigger), typeDiscriminator: "device")]
    [JsonDerivedType(typeof(SingleDelayTrigger), typeDiscriminator: "delay")]
    public abstract class SingleTrigger
    {
        /// <summary>
        /// Has the sanity check failed for this config entity?
        /// </summary>
        [JsonIgnore]
        public bool sanityCheckFailed { get; set; } = false;

        /// <summary>
        /// Why has sanit check failed for this config entity, if it has failed?
        /// </summary>
        [JsonIgnore]
        public List<string> sanityCheckError { get; set; } = [];

        public bool enabled { get; set; } = true;
    }

    public class SingleDeviceTrigger : SingleTrigger
    {
        /// <summary>
        /// Was this switch discovered?
        /// </summary>
        [JsonIgnore]
        public bool discovered { get; set; } = false;

        /// <summary>
        /// Was this switch disabled by the discoverer (and can therefore be enabled if discovered)?
        /// </summary>
        [JsonIgnore]
        public bool disabledByDiscoverer { get; set; } = false;

        /// <summary>
        /// What was the problem when trying to discover the switches, if any?
        /// </summary>
        [JsonIgnore]
        public List<string> discoveryProblem { get; set; } = [];

        [JsonConverter(typeof(JsonEnumConverter<TriggerType>))]
        public TriggerType deviceType { get; set; }
        public string? id { get; set; }

        /// <summary>
        /// is this trigger repeatable? default is true.
        /// If not this means that it will not be executed if
        /// another trigger with the same id that was also marked
        /// as not repeatable has already been triggered, even if
        /// in another trigger configuration.
        /// </summary>
        public bool repeatable { get; set; } = true;
    }

    public class SingleDelayTrigger : SingleTrigger
    {
        public ulong milliseconds { get; set; } = 5000;
    }
#pragma warning restore IDE1006
}
