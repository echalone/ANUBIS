using ANUBISWatcher.Configuration.Serialization;

namespace ANUBISWatcher.Configuration.ConfigFileData
{
    public enum TriggerType
    {
        [JsonEnumErrorValue]
        [JsonEnumNullValue]
        Unknown = 0,
        [JsonEnumName(true, "SwitchBot", "SwitchBotSwitch", "BotSwitch")]
        SwitchBot,
        [JsonEnumName(true, "Fritz", "FritzSwitch")]
        Fritz,
        [JsonEnumName(true, "ClewareUSB", "USB", "Cleware", "USBCleware", "ClewareUSBSwitch", "USBSwitch", "ClewareSwitch", "USBClewareSwitch")]
        ClewareUSB,
        [JsonEnumName(true, "OSShutdown", "System", "OS", "OperatingSystem", "Shutdown", "Reboot", "PowerOff", "PowerDown", "ShutOff", "Off", "TurnOff", "OffSwitch")]
        OSShutdown,
    }
}
