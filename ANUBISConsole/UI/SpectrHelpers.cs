using ANUBISConsole.ConfigHelpers;

namespace ANUBISConsole.UI
{
    public static class InitConfigCommands
    {
        public const string CHOICE_ShowVersions = "Show Versions, License and Copyright";
        public const string CHOICE_LoadConfigFromFile = "Load config from file";
        public const string CHOICE_LoadExampleConfig = "Load example config";
        public const string CHOICE_ShowEnabledDevicesInLoadedConfig = "Show enabled devices in loaded config";
        public const string CHOICE_EnabledDisableDevices_TurnOn = "Enabled/disabled devices to turn on";
        public const string CHOICE_EnabledDisableDevices_Pollers = "Enabled/disabled devices in pollers";
        public const string CHOICE_EnabledDisableDevices_Triggers = "Enabled/disabled devices in triggers";
        public const string CHOICE_EditConfigValues = "Edit configuration values";
        public const string CHOICE_SaveConfigToFile = "Save config to file";
        public const string CHOICE_TurnOnDevices = "Turn on devices";
        public const string CHOICE_RediscoverDevicesTurnOnSwitchBot = "Rediscover devices (try switching SwitchBots)";
        public const string CHOICE_RediscoverDevices = "Rediscover devices";
        public const string CHOICE_ResetCountdown = "Reset Countdown to configuration default";
        public const string CHOICE_LaunchController = "Launch [bold]COUNTDOWN[/]";
        public const string CHOICE_Exit = "Exit";
    }

    public static class ControlCommandPrompt
    {
        public const string PROMPT_State_Stopped = "Currently stopped - Possible commands: (ESC) Exit, (t)oggle mail sending, Start (m)onitoring";
        public const string PROMPT_State_Monitoring = "Currently monitoring - Possible commands: (ESC) Exit, (t)oggle mail sending, (s)top monitoring, go into (h)oldback mode to prepare for arming";
        public const string PROMPT_State_Holdback = "Currently in holdback mode, wait for \"HOLDBACK\" mode to switch to \"armable\" before arming system - Possible commands: (ESC) Exit, (t)oggle mail sending, (s)top monitoring, (d)isarm holdback mode";
        public const string PROMPT_State_HoldbackArmable = "Currently in holdback mode, system is armable - Possible commands: (ESC) Exit, (t)oggle mail sending, (s)top monitoring, (d)isarm holdback mode, (a)rm system";
        public const string PROMPT_State_Armed = "System is armed - Possible commands: (ESC) Exit, (t)oggle mail sending, (s)top monitoring, (d)isarm system";
        public const string PROMPT_State_SafeMode = "System is saved - Possible commands: (ESC) Exit, (t)oggle mail sending, (s)top monitoring";
        public const string PROMPT_WaitForStateChange = "Currently changing state, please wait till finished...";

        public const string WARN_StateSwitchFailed = " - WARNING: Changing the state failed, see console logging output for details";
    }

    public static class UIHelpers
    {
        public static string GetPollerCount(ulong current, ushort minimum)
        {
            var colorMarkup = current >= minimum ? AnubisOptions.Options.defaultColor_Ok : AnubisOptions.Options.defaultColor_Warning;
            var maxDisplay = minimum * 10.0;

            string strCurrent;
            if (current > maxDisplay)
            {
                strCurrent = $">{maxDisplay}";
            }
            else
            {
                strCurrent = $"{current}";
            }

            return $"[{colorMarkup}]{strCurrent}[/]/[{AnubisOptions.Options.defaultColor_Info}]{minimum}[/]";
        }

        public static BooleanState GetBooleanState(bool? value)
        {
            if (value.HasValue)
            {
                return value.Value ? BooleanState.True : BooleanState.False;
            }
            else
            {
                return BooleanState.Unknown;
            }
        }

        public static string? GetTimestampString(DateTime? timestamp, bool isUtc = true, bool includeLocal = true, bool includeUtc = true)
        {
            if (timestamp.HasValue)
            {
                DateTime tsUtc = isUtc ? timestamp.Value : timestamp.Value.ToUniversalTime();
                DateTime tsLocal = isUtc ? timestamp.Value.ToLocalTime() : timestamp.Value;
                if (includeLocal || includeUtc)
                {
                    string strTimestamp = "";

                    if (includeLocal)
                    {
                        strTimestamp = tsLocal.ToString("dd.MM.yyyy HH:mm:ss");
                    }

                    if (includeUtc)
                    {
                        if (includeLocal)
                        {
                            strTimestamp += " (";
                        }

                        strTimestamp += tsUtc.ToString("yyyy-MM-dd HH:mm:ss UTC");

                        if (includeLocal)
                        {
                            strTimestamp += ")";
                        }
                    }

                    return strTimestamp;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static string? GetTimeSinceString(DateTime? timestamp, bool isUtc = true)
        {
            if (timestamp.HasValue)
            {
                DateTime tsUtc = isUtc ? timestamp.Value : timestamp.Value.ToUniversalTime();
                string strPrefix = "+";
                TimeSpan spnSince = DateTime.UtcNow - tsUtc;

                if (spnSince.TotalNanoseconds < 0)
                {
                    strPrefix = "-";
                }
                return strPrefix + spnSince.ToString(@"mm\:ss");
            }
            else
            {
                return null;
            }
        }
    }
}
