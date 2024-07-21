using ANUBISWatcher.Configuration.Serialization;

namespace ANUBISWatcher.Shared
{
    public enum UniversalPanicType : byte
    {
        General = 0,
        Controller,
        ControllerPollers,
        ClewareUSBSwitch,
        FritzSwitch,
        SwitchBotSwitch,
        WriterFile,
        ReaderFile,
        Countdown,
        ClewareUSBPoller,
        FritzPoller,
        SwitchBotPoller,
        WriterFilePoller,
        ReaderFilePoller,
        CountdownPoller
    }

    public enum UniversalPanicReason : byte
    {
        [JsonEnumErrorValue]
        Unknown = 0,
        [JsonEnumName(true, "all", "any", "every", "fallback", "", "*")]
        [JsonEnumNullValue]
        All,
        [JsonEnumName(true, "none", "NoPanic")]
        NoPanic,
        SafeModeTurnOn,
        NameNotFound,
        InvalidState,
        SwitchNotFound,
        SwitchOff,
        ErrorState,
        [JsonEnumName(true, "GeneralError", "ErrorGeneral")]
        GeneralError,
        UnknownState,
        CommandTimeout,
        CommandError,
        SafeModePowerUp,
        InvalidPower,
        PowerTooLow,
        PowerTooHigh,
        ErrorPresence,
        ErrorPower,
        UnknownPresence,
        UnknownPower,
        HttpTimeout,
        NetworkError,
        BatteryTooLow,
        ErrorResponse,
        InvalidBattery,
        UnknownBattery,
        Panic,
        InvalidTimestamp,
        Error,
        DeSynced,
        CheckConditionViolation,
        NoResponse,
        Unreachable,
        Unresponsive,
    }
}
