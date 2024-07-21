namespace ANUBISWatcher.Configuration.ConfigFileData
{
#pragma warning disable IDE1006
    public class ConfigFile
    {
        public ClewareUSBConfigMapping[] clewareUSBMappings { get; set; } = [];
        public FritzAPIConfigSettings? fritzApiSettings { get; set; } = null;
        public SwitchBotAPIConfigSettings? switchApiSettings { get; set; } = null;
        public ClewareAPIConfigSettings? clewareApiSettings { get; set; } = null;
        public TurnOnEntity[] turnOn { get; set; } = [];
        public PollerAndTriggerConfig pollersAndTriggers { get; set; } = new PollerAndTriggerConfig();
    }
#pragma warning restore IDE1006
}
