using ANUBISWatcher.Configuration.ConfigFileData;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ANUBISConsole.ConfigHelpers
{
    public enum EditValueType
    {
        String,
        Long,
        DateTime,
        Bool,
    }

    public class EditValuesConstIDs
    {
        public const string STR_True = "true";
        public const string STR_False = "false";
        public const string STR_Null = "<null>";

        public const string DUM_Back = "<back>";

        public const string VAL_FritzAPI_BaseUrl = "fritzApi.baseUrl";
        public const string VAL_FritzAPI_User = "fritzApi.user";
        public const string VAL_FritzAPI_Password = "fritzApi.password";
        public const string VAL_SwitchBotAPI_BaseUrl = "switchBotApi.baseUrl";
        public const string VAL_SwitchBotAPI_Token = "switchBotApi.token";
        public const string VAL_SwitchBotAPI_Secret = "switchBotApi.secret";
        public const string VAL_Countdown_ShutDownOnT0 = "countdown.shutDownOnT0";
        public const string VAL_Countdown_SafeModeBeforeT0InMinutes = "countdown.safeModeBeforeT0InMinutes";
        public const string VAL_Countdown_CheckShutDownAfterMinutes = "countdown.checkShutDownAfterMinutes";
        public const string VAL_Countdown_T0Time = "countdown.T0Time";
        public const string VAL_Controller_SendMailsEarliestAfterMinutes = "controller.sendMailsEarliestAfterMinutes";
        public const string VAL_Mailing_Enabled = "mailing.enabled";
        public const string VAL_Mailing_CheckForShutDown = "mailing.checkForShutDown";
        public const string VAL_Mailing_CheckForShutDownVerified = "mailing.checkForShutDownVerified";
        public const string VAL_Mailing_SendInfoMails = "mailing.sendInfoMails";
        public const string VAL_Mailing_SendEmergencyMails = "mailing.sendEmergencyMails";
        public const string VAL_Mailing_SimulateToMailAddress = "mailing.simulateToMailAddress";
        public const string VAL_Mailing_SmtpServer = "mailing.smtpServer";
        public const string VAL_Mailing_SmtpPort = "mailing.smtpPort";
        public const string VAL_Mailing_UseSslForSmtp = "mailing.useSslForSmtp";
        public const string VAL_Mailing_FromAddress = "mailing.fromAddress";
        public const string VAL_Mailing_SmtpUser = "mailing.smtpUser";
        public const string VAL_Mailing_SmtpPassword = "mailing.smtpPassword";
        public const string VAL_Mailing_SimulateSending = "mailing.simulateSending";
        public const string VAL_Mailing_SendMailsAfterT0InMinutes = "mailing.sendMailsAfterT0InMinutes";
    }

    public class EditValuesHelper
    {
        public bool IsDummy { get; set; } = false;
        public string ValueName { get; set; }
        public string? ValuePostfix { get; set; } = null;
        public bool NullableValue { get; set; }
        public EditValueType ValueType { get; set; }
        public string? Value_String { get; set; }
        public long? Value_Long { get; set; }
        public DateTime? Value_DateTime { get; set; }
        public bool? Value_Bool { get; set; }
        public string? Value_Bool_TrueString { get; set; } = null;
        public string? Value_Bool_FalseString { get; set; } = null;
        public string Value_Bool_TrueStringNonNull { get { return Value_Bool_TrueString ?? EditValuesConstIDs.STR_True; } }
        public string Value_Bool_FalseStringNonNull { get { return Value_Bool_FalseString ?? EditValuesConstIDs.STR_False; } }
        public long Value_Long_Min { get; set; }
        public long Value_Long_Max { get; set; }
        public bool NoPastDate { get; set; }
        public TimeSpan? MaxFutureTS { get; set; }
        public string HelpText_Input { get; set; } = "";


        public FritzAPIConfigSettings? ConfigObject_FritzApi { get; set; }
        public SwitchBotAPIConfigSettings? ConfigObject_SwitchBotApi { get; set; }
        public CountdownConfigOptions? ConfigObject_CountdownConfig { get; set; }
        public ControllerConfigOptions? ConfigObject_ControllerConfig { get; set; }
        public MailingConfigOptions? ConfigObject_MailingConfig { get; set; }
        public FileConfigOptions? ConfigObject_FileConfig { get; set; }

        public EditValuesHelper(string dummyName)
        {
            IsDummy = true;
            ValueName = dummyName;
        }

        public EditValuesHelper(FritzAPIConfigSettings fritzApiConfigSettings, string valueName)
        {
            ConfigObject_FritzApi = fritzApiConfigSettings;
            ValueName = valueName;
            switch (valueName)
            {
                case EditValuesConstIDs.VAL_FritzAPI_BaseUrl:
                    InitStringValue(false, fritzApiConfigSettings.baseUrl,
                                    "Base URL of FritzBox API (and of own FritzBox). (example: \"http://fritz.box\")");
                    break;
                case EditValuesConstIDs.VAL_FritzAPI_User:
                    InitStringValue(true, fritzApiConfigSettings.user,
                                    "User to use for FritzBox login.");
                    break;
                case EditValuesConstIDs.VAL_FritzAPI_Password:
                    InitStringValue(true, fritzApiConfigSettings.password,
                                    "Password to use for FritzBox login.");
                    break;
                default:
                    throw new ArgumentException($"Unknown FritzAPI setting: {valueName}");
            }
        }

        public EditValuesHelper(SwitchBotAPIConfigSettings switchBotApiConfigSettings, string valueName)
        {
            ConfigObject_SwitchBotApi = switchBotApiConfigSettings;
            ValueName = valueName;
            switch (valueName)
            {
                case EditValuesConstIDs.VAL_SwitchBotAPI_BaseUrl:
                    InitStringValue(false, switchBotApiConfigSettings.baseUrl,
                                    "Base URL of SwitchBot API. (example: \"https://api.switch-bot.com/\")");
                    break;
                case EditValuesConstIDs.VAL_SwitchBotAPI_Token:
                    InitStringValue(false, switchBotApiConfigSettings.token,
                                    "Token to use for SwitchBot login. In App under Profil Tab > \"Settings\" > Click 10 times fast on the \"App Version\" text to activate developer mode, then click on options for developers to find Token und Secret and copy them from there");
                    break;
                case EditValuesConstIDs.VAL_SwitchBotAPI_Secret:
                    InitStringValue(false, switchBotApiConfigSettings.secret,
                                    "Secret to use for SwitchBot login. In App under Profil Tab > \"Settings\" > Click 10 times fast on the \"App Version\" text to activate developer mode, then click on options for developers to find Token und Secret and copy them from there");
                    break;
                default:
                    throw new ArgumentException($"Unknown SwitchBotAPI setting: {valueName}");
            }
        }

        public EditValuesHelper(CountdownConfigOptions countdownConfigOptions, string valueName)
        {
            ConfigObject_CountdownConfig = countdownConfigOptions;
            ValueName = valueName;
            switch (valueName)
            {
                case EditValuesConstIDs.VAL_Countdown_ShutDownOnT0:
                    InitBooleanValue(false, countdownConfigOptions.shutDownOnT0,
                                        $"[{AnubisOptions.Options.defaultColor_Activated}]yes[/]", $"[{AnubisOptions.Options.defaultColor_Warning}]no[/]",
                                        "Should we shut down the system on T0 (yes)? Or is this a manual triggering (no)?");
                    break;
                case EditValuesConstIDs.VAL_Countdown_SafeModeBeforeT0InMinutes:
                    InitIntValue(false, countdownConfigOptions.countdownAutoSafeModeMinutes, " minutes", 0, 1440,
                                    "How long (in minutes) before the Countdown T-0 should we put everything into SafeMode? A value of 0 means that we will only go into safe mode on triggering the shutdown.");
                    break;
                case EditValuesConstIDs.VAL_Countdown_CheckShutDownAfterMinutes:
                    InitIntValue(false, countdownConfigOptions.checkShutDownAfterMinutes, " minutes", 0, 1440,
                                    "How long (in minutes) after the Countdown T0 should we check for shutdown and throw a panic if shutdown didn't happen? A value of 0 means that this will not be checked.");
                    break;
                case EditValuesConstIDs.VAL_Countdown_T0Time:
                    InitDateTimeValue(false, countdownConfigOptions.countdownT0TimestampUTC, true, TimeSpan.FromDays(1),
                                        "When exactly should the countdown reach T0 (and system shutdown started if shutDownOnT0 is true)?");
                    break;
                default:
                    throw new ArgumentException($"Unknown Countdown setting: {valueName}");
            }
        }

        public EditValuesHelper(ControllerConfigOptions controllerConfigOptions, string valueName)
        {
            ConfigObject_ControllerConfig = controllerConfigOptions;
            ValueName = valueName;
            switch (valueName)
            {
                case EditValuesConstIDs.VAL_Controller_SendMailsEarliestAfterMinutes:
                    InitIntValue(false, controllerConfigOptions.sendMailEarliestAfterMinutes, " minutes", 0, 1440,
                                    "How long(in minutes) after the system shutdown(state shutdown or triggered) should we send the email? A value of 0 will be ignored and no mail will be sent.");
                    break;
                default:
                    throw new ArgumentException($"Unknown Controller setting: {valueName}");
            }
        }

        public EditValuesHelper(MailingConfigOptions mailingConfigOptions, string valueName)
        {
            ConfigObject_MailingConfig = mailingConfigOptions;
            ValueName = valueName;
            switch (valueName)
            {
                case EditValuesConstIDs.VAL_Mailing_Enabled:
                    InitBooleanValue(false, mailingConfigOptions.enabled,
                                        $"[{AnubisOptions.Options.defaultColor_Activated}]yes[/]", $"[{AnubisOptions.Options.defaultColor_Warning}]no[/]",
                                        "Is mail sending generally enabled?");
                    break;
                case EditValuesConstIDs.VAL_Mailing_CheckForShutDown:
                    InitBooleanValue(false, mailingConfigOptions.checkForShutDown,
                                        $"[{AnubisOptions.Options.defaultColor_Ok}]yes[/]", $"[{AnubisOptions.Options.defaultColor_Warning}]no[/]",
                                        "Should we check if the system has shut down before sending Emails?");
                    break;
                case EditValuesConstIDs.VAL_Mailing_CheckForShutDownVerified:
                    InitBooleanValue(false, mailingConfigOptions.checkForShutDownVerified,
                                        $"[{AnubisOptions.Options.defaultColor_Ok}]yes[/]", $"[{AnubisOptions.Options.defaultColor_Warning}]no[/]",
                                        "Should we check (verified) if the system has shut down before sending Emails?");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SendInfoMails:
                    InitBooleanValue(false, mailingConfigOptions.sendInfoMails,
                                        $"[{AnubisOptions.Options.defaultColor_Activated}]yes[/]", $"[{AnubisOptions.Options.defaultColor_Warning}]no[/]", "Should we send information mails?");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SendEmergencyMails:
                    InitBooleanValue(false, mailingConfigOptions.sendEmergencyMails,
                                        $"[{AnubisOptions.Options.defaultColor_Activated}]yes[/]", $"[{AnubisOptions.Options.defaultColor_Warning}]no[/]", "Should we send emergency mails?");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SimulateToMailAddress:
                    InitStringValue(true, mailingConfigOptions.mailAddress_Simulate,
                                    "What is the mail address we should send simulated mails to? If this is null/empty simulated mails will not be sent to a real smtp server but only internally simulated.");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SmtpServer:
                    InitStringValue(false, mailingConfigOptions.mailSettings_SmtpServer, "SMTP Server");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SmtpPort:
                    InitIntValue(true, mailingConfigOptions.mailSettings_Port, 0, ushort.MaxValue,
                                    "Port for SMTP Server. Set to null/empty for default (default is 587 for ssl and 25 for non-ssl)");
                    break;
                case EditValuesConstIDs.VAL_Mailing_UseSslForSmtp:
                    InitBooleanValue(false, mailingConfigOptions.mailSettings_UseSsl,
                                        $"[{AnubisOptions.Options.defaultColor_Ok}]yes[/]", $"[{AnubisOptions.Options.defaultColor_Warning}]no[/]", "Use SSL for SMTP Server?");
                    break;
                case EditValuesConstIDs.VAL_Mailing_FromAddress:
                    InitStringValue(false, mailingConfigOptions.mailSettings_FromAddress,
                                    "Mail address to use for From-Address. Should be mail address of the mailing account we send the mails from.");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SmtpUser:
                    InitStringValue(true, mailingConfigOptions.mailSettings_User,
                                    "User for mail account. Set to empty if no user is needed.");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SmtpPassword:
                    InitStringValue(true, mailingConfigOptions.mailSettings_Password,
                                    "Password for mail account");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SimulateSending:
                    InitBooleanValue(false, mailingConfigOptions.simulateMails, $"[{AnubisOptions.Options.defaultColor_Warning}]yes[/]", $"[{AnubisOptions.Options.defaultColor_Activated}]no[/]",
                                        "Should we only simulate sending mails?");
                    break;
                case EditValuesConstIDs.VAL_Mailing_SendMailsAfterT0InMinutes:
                    InitIntValue(false, mailingConfigOptions.countdownSendMailMinutes, " minutes", 0, 1440,
                                    "How long (in minutes) after the Countdown T-0 should we send the email? A value of 0 will be ignored and no mail will be sent.");
                    break;
                default:
                    throw new ArgumentException($"Unknown Mailing setting: {valueName}");
            }
        }

        private void InitStringValue(bool nullable, string? value, string helpText)
        {
            InitStringValue(nullable, value, null, helpText);
        }

        private void InitStringValue(bool nullable, string? value, string? postfix, string helpText)
        {
            InitBasics(nullable, postfix, helpText);
            ValueType = EditValueType.String;
            Value_String = value;
        }

#pragma warning disable IDE0051 // Nicht verwendete private Member entfernen
        private void InitIntValue(bool nullable, long? value, string helpText)
        {
            InitIntValue(nullable, value, int.MinValue, int.MaxValue, helpText);
        }

        private void InitIntValue(bool nullable, long? value, long minValue, long maxValue, string helpText)
        {
            InitNumericValue(nullable, value, minValue, maxValue, helpText);
        }

        private void InitNumericValue(bool nullable, long? value, string helpText)
        {
            InitNumericValue(nullable, value, long.MinValue, long.MaxValue, helpText);
        }

        private void InitNumericValue(bool nullable, long? value, long minValue, long maxValue, string helpText)
        {
            InitNumericValue(nullable, value, null, minValue, maxValue, helpText);
        }

        private void InitIntValue(bool nullable, long? value, string? postfix, string helpText)
        {
            InitIntValue(nullable, value, postfix, int.MinValue, int.MaxValue, helpText);
        }

        private void InitIntValue(bool nullable, long? value, string? postfix, long minValue, long maxValue, string helpText)
        {
            InitNumericValue(nullable, value, postfix, minValue, maxValue, helpText);
        }

        private void InitNumericValue(bool nullable, long? value, string? postfix, string helpText)
        {
            InitNumericValue(nullable, value, postfix, long.MinValue, long.MaxValue, helpText);
        }

        private void InitNumericValue(bool nullable, long? value, string? postfix, long minValue, long maxValue, string helpText)
        {
            InitBasics(nullable, postfix, helpText);
            Value_Long_Min = minValue;
            Value_Long_Max = maxValue;
            ValueType = EditValueType.Long;
            Value_Long = value;
        }

        private void InitDateTimeValue(bool nullable, DateTime? value, bool noPastDate, TimeSpan? maxTimeSpanInFuture, string helpText)
        {
            InitBasics(nullable, null, helpText);
            ValueType = EditValueType.DateTime;
            Value_DateTime = value;
            NoPastDate = noPastDate;
            MaxFutureTS = maxTimeSpanInFuture;
        }

        private void InitBooleanValue(bool nullable, bool? value, string trueString, string falseString, string helpText)
        {
            InitBasics(nullable, null, helpText);
            ValueType = EditValueType.Bool;
            Value_Bool = value;
            Value_Bool_TrueString = trueString;
            Value_Bool_FalseString = falseString;
        }

        private void InitBooleanValue(bool nullable, bool? value, string helpText)
        {
            InitBooleanValue(nullable, value, EditValuesConstIDs.STR_True, EditValuesConstIDs.STR_False, helpText);
        }

        private void InitBasics(bool nullable, string? postfix, string helpText)
        {
            HelpText_Input = helpText;
            NullableValue = nullable;
            ValuePostfix = postfix;
        }
#pragma warning restore IDE0051 // Nicht verwendete private Member entfernen

        public SelectionPrompt<EditValuesHelper> AddToSelection(SelectionPrompt<EditValuesHelper> selection)
        {
            selection.AddChoice(this);

            return selection;
        }

        public static SelectionPrompt<EditValuesHelper> AddToSelection(SelectionPrompt<EditValuesHelper> selection, bool asGroup, bool showEmpty, FritzAPIConfigSettings? settings)
        {
            var lstEditValues = GetEditValues(settings);

            if (lstEditValues != null)
            {
                if (asGroup)
                {
                    selection.AddChoiceGroup(new EditValuesHelper("Fritz-Api settings"), lstEditValues);
                }
                else
                {
                    selection.AddChoices(lstEditValues);
                }
            }
            else if (showEmpty)
            {
                selection.AddChoiceGroup(new EditValuesHelper("[dim]Fritz-Api settings (empty)[/]"), []);
            }

            return selection;
        }

        public static SelectionPrompt<EditValuesHelper> AddToSelection(SelectionPrompt<EditValuesHelper> selection, bool asGroup, bool showEmpty, SwitchBotAPIConfigSettings? settings)
        {
            var lstEditValues = GetEditValues(settings);

            if (lstEditValues != null)
            {
                if (asGroup)
                {
                    selection.AddChoiceGroup(new EditValuesHelper("SwitchBot-Api settings"), lstEditValues);
                }
                else
                {
                    selection.AddChoices(lstEditValues);
                }
            }
            else if (showEmpty)
            {
                selection.AddChoiceGroup(new EditValuesHelper("[dim]SwitchBot-Api settings (empty)[/]"), []);
            }

            return selection;
        }

        public static SelectionPrompt<EditValuesHelper> AddToSelection(SelectionPrompt<EditValuesHelper> selection, bool asGroup, bool showEmpty, CountdownConfigOptions? settings)
        {
            var lstEditValues = GetEditValues(settings);

            if (lstEditValues != null)
            {
                if (asGroup)
                {
                    selection.AddChoiceGroup(new EditValuesHelper("Countdown settings"), lstEditValues);
                }
                else
                {
                    selection.AddChoices(lstEditValues);
                }
            }
            else if (showEmpty)
            {
                selection.AddChoiceGroup(new EditValuesHelper("[dim]Countdown settings (empty)[/]"), []);
            }

            return selection;
        }

        public static SelectionPrompt<EditValuesHelper> AddToSelection(SelectionPrompt<EditValuesHelper> selection, bool asGroup, bool showEmpty, ControllerConfigOptions? settings)
        {
            var lstEditValues = GetEditValues(settings);

            if (lstEditValues != null)
            {
                if (asGroup)
                {
                    selection.AddChoiceGroup(new EditValuesHelper("Controller settings"), lstEditValues);
                }
                else
                {
                    selection.AddChoices(lstEditValues);
                }
            }
            else if (showEmpty)
            {
                selection.AddChoiceGroup(new EditValuesHelper("[dim]Controller settings (empty)[/]"), []);
            }

            return selection;
        }

        public static SelectionPrompt<EditValuesHelper> AddToSelection(SelectionPrompt<EditValuesHelper> selection, bool asGroup, bool showEmpty, MailingConfigOptions? settings)
        {
            var lstEditValues = GetEditValues(settings);

            if (lstEditValues != null)
            {
                if (asGroup)
                {
                    selection.AddChoiceGroup(new EditValuesHelper("Mailing settings"), lstEditValues);
                }
                else
                {
                    selection.AddChoices(lstEditValues);
                }
            }
            else if (showEmpty)
            {
                selection.AddChoiceGroup(new EditValuesHelper("[dim]Mailing settings (empty)[/]"), []);
            }

            return selection;
        }


        public static List<EditValuesHelper>? GetEditValues(FritzAPIConfigSettings? settings)
        {
            if (settings != null)
            {
                return
                    [
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_FritzAPI_BaseUrl),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_FritzAPI_User),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_FritzAPI_Password),
                    ];
            }
            else
            {
                return null;
            }
        }

        public static List<EditValuesHelper>? GetEditValues(SwitchBotAPIConfigSettings? settings)
        {
            if (settings != null)
            {
                return
                    [
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_SwitchBotAPI_BaseUrl),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_SwitchBotAPI_Token),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_SwitchBotAPI_Secret),
                    ];
            }
            else
            {
                return null;
            }
        }

        public static List<EditValuesHelper>? GetEditValues(CountdownConfigOptions? settings)
        {
            if (settings != null)
            {
                return
                    [
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Countdown_T0Time),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Countdown_ShutDownOnT0),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Countdown_SafeModeBeforeT0InMinutes),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Countdown_CheckShutDownAfterMinutes),
                    ];
            }
            else
            {
                return null;
            }
        }

        public static List<EditValuesHelper>? GetEditValues(ControllerConfigOptions? settings)
        {
            if (settings != null)
            {
                return
                    [
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Controller_SendMailsEarliestAfterMinutes),
                    ];
            }
            else
            {
                return null;
            }
        }

        public static List<EditValuesHelper>? GetEditValues(MailingConfigOptions? settings)
        {
            if (settings != null)
            {
                return
                    [
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_Enabled),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SendEmergencyMails),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SendInfoMails),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SmtpUser),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SmtpPassword),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SimulateSending),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SimulateToMailAddress),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_CheckForShutDown),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_CheckForShutDownVerified),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_FromAddress),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SmtpServer),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SmtpPort),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_UseSslForSmtp),
                        new EditValuesHelper(settings, EditValuesConstIDs.VAL_Mailing_SendMailsAfterT0InMinutes),
                    ];
            }
            else
            {
                return null;
            }
        }

        public static EditValuesHelper GetBackDummy()
        {
            return new EditValuesHelper(EditValuesConstIDs.DUM_Back);
        }

        public bool IsBackDummy() { return IsDummy && ValueName == EditValuesConstIDs.DUM_Back; }

        public string ValueToString()
        {
            string strRetVal = "<null>";

            switch (ValueType)
            {
                case EditValueType.String:
                    if (Value_String == null)
                        strRetVal = "<null>";
                    else if (string.IsNullOrEmpty(Value_String))
                        strRetVal = "<empty>";
                    else
                        strRetVal = "\"" + Value_String + "\"";
                    break;
                case EditValueType.Long:
                    strRetVal = Value_Long?.ToString() ?? "<null>";
                    break;
                case EditValueType.DateTime:
                    strRetVal = Value_DateTime.HasValue ? (Value_DateTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") + " (" + Value_DateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") + " UTC)") : "<null>";
                    break;
                case EditValueType.Bool:
                    if (!Value_Bool.HasValue)
                    {
                        strRetVal = "<null>";
                    }
                    else if (Value_Bool.Value)
                    {
                        strRetVal = Value_Bool_TrueStringNonNull;
                    }
                    else
                    {
                        strRetVal = Value_Bool_FalseStringNonNull;
                    }
                    break;
            }

            if (!string.IsNullOrWhiteSpace(ValuePostfix))
            {
                strRetVal += ValuePostfix;
            }

            if (ValueType == EditValueType.String)
            {
                return strRetVal.EscapeMarkup();
            }
            else
            {
                return strRetVal;
            }
        }

        public bool SetValue(string? value)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("SetValue"))
            {
                if (ValueType == EditValueType.String)
                {
                    if (NullableValue || !string.IsNullOrWhiteSpace(value))
                    {
                        Value_String = value;
                        logging?.LogInformation(@"Changed string config value ""{name}"" to ""{value}""", ValueName, Value_String);
                        return true;
                    }
                    else
                    {
                        logging?.LogWarning("Cannot set null for non-nullable value {valuename}", ValueName);
                        return false;
                    }
                }
                else
                {
                    logging?.LogWarning("Cannot set string value as value {valuename} is of type {valuetype}", ValueName, ValueType);
                    return false;
                }
            }
        }

        public bool SetValue(long? value)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("SetValue"))
            {
                if (ValueType == EditValueType.Long)
                {
                    if (NullableValue || value.HasValue)
                    {
                        if (!value.HasValue || value >= Value_Long_Min)
                        {
                            if (!value.HasValue || value <= Value_Long_Max)
                            {
                                Value_Long = value;
                                logging?.LogInformation(@"Changed numeric config value ""{name}"" to ""{value}""", ValueName, Value_Long);
                                return true;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set {number} for value {valuename} as this would exceed the allowed maximum of {maxallowed}", value, ValueName, Value_Long_Max);
                                return false;
                            }
                        }
                        else
                        {
                            logging?.LogWarning("Cannot set {number} for value {valuename} as this would subceed the allowed minimum of {minallowed}", value, ValueName, Value_Long_Min);
                            return false;
                        }
                    }
                    else
                    {
                        logging?.LogWarning("Cannot set null for non-nullable value {valuename}", ValueName);
                        return false;
                    }
                }
                else
                {
                    logging?.LogWarning("Cannot set numeric value as value {valuename} is of type {valuetype}", ValueName, ValueType);
                    return false;
                }
            }
        }

        public bool SetValue(DateTime? value, DateTime? maxUtc)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("SetValue"))
            {
                if (ValueType == EditValueType.DateTime)
                {
                    if (NullableValue || value.HasValue)
                    {
                        if (!value.HasValue)
                        {
                            Value_DateTime = null;
                            return true;
                        }
                        else
                        {
                            if (!NoPastDate || value.Value > DateTime.UtcNow)
                            {
                                if (!maxUtc.HasValue || value.Value <= maxUtc.Value)
                                {
                                    Value_DateTime = value;
                                    logging?.LogInformation(@"Changed datetime config value ""{name}"" to ""{value}""", ValueName, Value_DateTime);
                                    return true;
                                }
                                else
                                {
                                    logging?.LogWarning("Cannot set date beyond {maxfuturedate} UTC for value {valuename}", maxUtc, ValueName);
                                    return false;
                                }
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set past date for value {valuename}", ValueName);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        logging?.LogWarning("Cannot set null for non-nullable value {valuename}", ValueName);
                        return false;
                    }
                }
                else
                {
                    logging?.LogWarning("Cannot set DateTime value as value {valuename} is of type {valuetype}", ValueName, ValueType);
                    return false;
                }
            }
        }

        public bool SetValue(bool? value)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("SetValue"))
            {
                if (ValueType == EditValueType.Bool)
                {
                    if (NullableValue || value.HasValue)
                    {
                        Value_Bool = value;
                        logging?.LogInformation(@"Changed boolean config value ""{name}"" to ""{value}""", ValueName, Value_Bool);
                        return true;
                    }
                    else
                    {
                        logging?.LogWarning("Cannot set null for non-nullable value {valuename}", ValueName);
                        return false;
                    }
                }
                else
                {
                    logging?.LogWarning("Cannot set boolean value as value {valuename} is of type {valuetype}", ValueName, ValueType);
                    return false;
                }
            }
        }


        public void SyncValue()
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("SyncValue"))
            {
                if (ConfigObject_ControllerConfig != null)
                {
                    switch (ValueName)
                    {
                        case EditValuesConstIDs.VAL_Controller_SendMailsEarliestAfterMinutes:
                            if (Value_Long.HasValue)
                            {
                                ConfigObject_ControllerConfig.sendMailEarliestAfterMinutes = (int)Value_Long;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set controller value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        default:
                            logging?.LogWarning("Cannot update value for unknown controller value \"{valuename}\"", ValueName);
                            break;
                    }
                }
                else if (ConfigObject_CountdownConfig != null)
                {
                    switch (ValueName)
                    {
                        case EditValuesConstIDs.VAL_Countdown_ShutDownOnT0:
                            if (Value_Bool.HasValue)
                            {
                                ConfigObject_CountdownConfig.shutDownOnT0 = Value_Bool.Value;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set Countdown settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Countdown_SafeModeBeforeT0InMinutes:
                            if (Value_Long.HasValue)
                            {
                                ConfigObject_CountdownConfig.countdownAutoSafeModeMinutes = (int)Value_Long;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set Countdown settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Countdown_CheckShutDownAfterMinutes:
                            if (Value_Long.HasValue)
                            {
                                ConfigObject_CountdownConfig.checkShutDownAfterMinutes = (int)Value_Long;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set Countdown settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Countdown_T0Time:
                            if (Value_DateTime.HasValue)
                            {
                                ConfigObject_CountdownConfig.countdownT0TimestampUTC = Value_DateTime;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set Countdown settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        default:
                            logging?.LogWarning("Cannot update value for unknown Countdown settings value \"{valuename}\"", ValueName);
                            break;
                    }
                }
                else if (ConfigObject_FritzApi != null)
                {
                    switch (ValueName)
                    {
                        case EditValuesConstIDs.VAL_FritzAPI_BaseUrl:
                            if (!string.IsNullOrWhiteSpace(Value_String))
                            {
                                ConfigObject_FritzApi.baseUrl = Value_String.Trim();
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set Fritz API value \"{valuename}\" to empty", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_FritzAPI_User:
                            if (!string.IsNullOrEmpty(Value_String))
                            {
                                ConfigObject_FritzApi.user = Value_String;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set Fritz API value \"{valuename}\" to empty", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_FritzAPI_Password:
                            if (!string.IsNullOrEmpty(Value_String))
                            {
                                ConfigObject_FritzApi.password = Value_String;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set Fritz API value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        default:
                            logging?.LogWarning("Cannot update value for unknown Fritz API value \"{valuename}\"", ValueName);
                            break;
                    }
                }
                else if (ConfigObject_MailingConfig != null)
                {
                    switch (ValueName)
                    {
                        case EditValuesConstIDs.VAL_Mailing_Enabled:
                            if (Value_Bool.HasValue)
                            {
                                ConfigObject_MailingConfig.enabled = Value_Bool.Value;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_CheckForShutDown:
                            if (Value_Bool.HasValue)
                            {
                                ConfigObject_MailingConfig.checkForShutDown = Value_Bool.Value;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_CheckForShutDownVerified:
                            if (Value_Bool.HasValue)
                            {
                                ConfigObject_MailingConfig.checkForShutDownVerified = Value_Bool.Value;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SendInfoMails:
                            if (Value_Bool.HasValue)
                            {
                                ConfigObject_MailingConfig.sendInfoMails = Value_Bool.Value;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SendEmergencyMails:
                            if (Value_Bool.HasValue)
                            {
                                ConfigObject_MailingConfig.sendEmergencyMails = Value_Bool.Value;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SimulateToMailAddress:
                            ConfigObject_MailingConfig.mailAddress_Simulate = Value_String;
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SmtpServer:
                            if (!string.IsNullOrWhiteSpace(Value_String))
                            {
                                ConfigObject_MailingConfig.mailSettings_SmtpServer = Value_String.Trim();
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SmtpPort:
                            ConfigObject_MailingConfig.mailSettings_Port = (int?)Value_Long;
                            break;
                        case EditValuesConstIDs.VAL_Mailing_UseSslForSmtp:
                            if (Value_Bool.HasValue)
                            {
                                ConfigObject_MailingConfig.mailSettings_UseSsl = Value_Bool.Value;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_FromAddress:
                            if (!string.IsNullOrWhiteSpace(Value_String))
                            {
                                ConfigObject_MailingConfig.mailSettings_FromAddress = Value_String.Trim();
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SmtpUser:
                            ConfigObject_MailingConfig.mailSettings_User = Value_String;
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SmtpPassword:
                            ConfigObject_MailingConfig.mailSettings_Password = Value_String;
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SimulateSending:
                            if (Value_Bool.HasValue)
                            {
                                ConfigObject_MailingConfig.simulateMails = Value_Bool.Value;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_Mailing_SendMailsAfterT0InMinutes:
                            if (Value_Long.HasValue)
                            {
                                ConfigObject_MailingConfig.countdownSendMailMinutes = (int)Value_Long;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set mailing settings value \"{valuename}\" to null", ValueName);
                            }
                            break;
                        default:
                            logging?.LogWarning("Cannot update value for unknown mailing settings value \"{valuename}\"", ValueName);
                            break;
                    }
                }
                else if (ConfigObject_SwitchBotApi != null)
                {
                    switch (ValueName)
                    {
                        case EditValuesConstIDs.VAL_SwitchBotAPI_BaseUrl:
                            if (!string.IsNullOrWhiteSpace(Value_String))
                            {
                                ConfigObject_SwitchBotApi.baseUrl = Value_String.Trim();
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set SwitchBot API value \"{valuename}\" to empty", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_SwitchBotAPI_Token:
                            if (!string.IsNullOrWhiteSpace(Value_String))
                            {
                                ConfigObject_SwitchBotApi.token = Value_String;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set SwitchBot API value \"{valuename}\" to empty", ValueName);
                            }
                            break;
                        case EditValuesConstIDs.VAL_SwitchBotAPI_Secret:
                            if (!string.IsNullOrWhiteSpace(Value_String))
                            {
                                ConfigObject_SwitchBotApi.secret = Value_String;
                            }
                            else
                            {
                                logging?.LogWarning("Cannot set SwitchBot API value \"{valuename}\" to empty", ValueName);
                            }
                            break;
                        default:
                            logging?.LogWarning("Cannot update value for unknown SwitchBot API value \"{valuename}\"", ValueName);
                            break;
                    }
                }
                else
                {
                    logging?.LogWarning("Cannot update value for an unknow type");
                }
            }
        }

        public void UpdateValue()
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("UpdateValue"))
            {
                if (ValueType == EditValueType.Bool)
                {
                    if (!NullableValue)
                    {
                        UpdateBool();
                    }
                    else
                    {
                        UpdateNullableBool();
                    }
                }
                else if (ValueType == EditValueType.Long)
                {
                    UpdateNumeric();
                }
                else if (ValueType == EditValueType.String)
                {
                    UpdateString();
                }
                else if (ValueType == EditValueType.DateTime)
                {
                    UpdateDateTime();
                }
                else
                {
                    logging?.LogWarning("Cannot update value, unknown value type {}", ValueType);
                }
                SyncValue();
            }
        }

        public void UpdateNullableBool()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine(HelpText_Input);
            AnsiConsole.MarkupLine($"Current value: [bold]{(Value_Bool.HasValue ?
                                                                (Value_Bool.Value ?
                                                                    Value_Bool_TrueStringNonNull :
                                                                    Value_Bool_FalseStringNonNull) :
                                                                "<null>")}[/]");
            var spEdit = new SelectionPrompt<string>()
                                .Title($"New value for {ValueName}:")
                                .HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));

            //spEdit.DisabledStyle = new Style(AnubisOptions.Options.defaultColor_Normal);

            if (!Value_Bool.HasValue || !Value_Bool.Value)
            {
                spEdit.AddChoices(Value_Bool_TrueStringNonNull, Value_Bool_FalseStringNonNull, "<null>");
            }
            else if (Value_Bool.Value)
            {
                spEdit.AddChoices(Value_Bool_FalseStringNonNull, Value_Bool_TrueStringNonNull, "<null>");
            }

            var valNew = AnsiConsole.Prompt(spEdit);

            if (valNew == "<null>")
            {
                SetValue((bool?)null);
            }
            else if (valNew == Value_Bool_TrueStringNonNull)
            {
                SetValue(true);
            }
            else
            {
                SetValue(false);
            }
        }

        public void UpdateBool()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine(HelpText_Input);
            AnsiConsole.MarkupLine($"Current value: [bold]{(Value_Bool.HasValue ?
                                                                (Value_Bool.Value ?
                                                                    Value_Bool_TrueStringNonNull :
                                                                    Value_Bool_FalseStringNonNull) :
                                                                "<null>")}[/]");
            var spEdit = new SelectionPrompt<string>()
                                .Title($"New value for {ValueName}:")
                                .HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));

            //spEdit.DisabledStyle = new Style(AnubisOptions.Options.defaultColor_Normal);

            if (!Value_Bool.HasValue || !Value_Bool.Value)
            {
                spEdit.AddChoices(Value_Bool_TrueStringNonNull, Value_Bool_FalseStringNonNull);
            }
            else if (Value_Bool.Value)
            {
                spEdit.AddChoices(Value_Bool_FalseStringNonNull, Value_Bool_TrueStringNonNull);
            }

            var valNew = AnsiConsole.Prompt(spEdit);

            if (valNew == Value_Bool_TrueStringNonNull)
            {
                SetValue(true);
            }
            else
            {
                SetValue(false);
            }
        }

        public void UpdateNumeric()
        {
            long? valNew = null;
            bool firstRun = true;
            bool exit = false;
            string strWarnNotAllowed = $"[{AnubisOptions.Options.defaultColor_Warning}]Value not allowed[/]";
            string strRestrictions = "";
            List<string> lstRestrictions = [];

            if (!NullableValue)
            {
                lstRestrictions.Add("empty/null not allowed");
            }
            if (Value_Long_Min != long.MinValue && Value_Long_Min != int.MinValue)
            {
                lstRestrictions.Add($"value must be greater or equal to {Value_Long_Min}");
            }
            if (Value_Long_Max != long.MinValue && Value_Long_Max != int.MinValue)
            {
                lstRestrictions.Add($"value must be lower or equal to {Value_Long_Max}");
            }

            if (lstRestrictions.Count > 0)
            {
                strRestrictions += "Restrictions: ";
                strRestrictions += string.Join(", ", lstRestrictions);
            }

            if (!string.IsNullOrWhiteSpace(strRestrictions))
            {
                strWarnNotAllowed += ", " + strRestrictions;
            }

            do
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine(HelpText_Input);
                if (!firstRun)
                    AnsiConsole.MarkupLine(strWarnNotAllowed);
                else
                    AnsiConsole.MarkupLine(strRestrictions);

                AnsiConsole.MarkupLine($"Current value: [bold]{Value_Long?.ToString() ?? "<null>"}[/]");
                if (NullableValue)
                {
                    valNew = AnsiConsole.Ask<long?>($"Enter new value for {ValueName}:", null);

                    if (!valNew.HasValue && Value_Long.HasValue)
                    {
                        var confirmed = AnsiConsole.Confirm($"Do you really want to set the value for {ValueName} to <null>?", false);
                        if (!confirmed)
                        {
                            exit = true;
                        }
                    }
                }
                else
                {
                    valNew = AnsiConsole.Ask<long>($"Enter new value for {ValueName}:", Value_Long ?? Value_Long_Min);
                }
                firstRun = false;
            }
            while (!exit && !SetValue(valNew));
        }

        public void UpdateString()
        {
            string? valNew = null;
            bool firstRun = true;
            bool exit = false;
            string strWarnNotAllowed = $"[{AnubisOptions.Options.defaultColor_Warning}]Value not allowed[/]";
            string strRestrictions = "";
            List<string> lstRestrictions = [];

            if (!NullableValue)
            {
                lstRestrictions.Add("empty/null not allowed");
            }

            if (lstRestrictions.Count > 0)
            {
                strRestrictions += "Restrictions: ";
                strRestrictions += string.Join(", ", lstRestrictions);
            }

            if (!string.IsNullOrWhiteSpace(strRestrictions))
            {
                strWarnNotAllowed += ", " + strRestrictions;
            }

            do
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine(HelpText_Input);
                if (!firstRun)
                    AnsiConsole.MarkupLine(strWarnNotAllowed);
                else
                    AnsiConsole.MarkupLine(strRestrictions);

                AnsiConsole.MarkupLine($"Current value: [bold]{(string.IsNullOrWhiteSpace(Value_String) ? "<null/empty>" : Value_String)}[/]");
                if (NullableValue)
                {
                    valNew = AnsiConsole.Ask<string?>($"Enter new value for {ValueName}:", null);

                    if (string.IsNullOrWhiteSpace(valNew) && !string.IsNullOrWhiteSpace(Value_String))
                    {
                        var confirmed = AnsiConsole.Confirm($"Do you really want to set the value for {ValueName} to <null/empty>?", false);
                        if (!confirmed)
                        {
                            exit = true;
                        }
                    }
                }
                else
                {
                    valNew = AnsiConsole.Ask<string>($"Enter new value for {ValueName}:", Value_String ?? "");
                }
                firstRun = false;
            }
            while (!exit && !SetValue(valNew));
        }

        public void UpdateDateTime()
        {
            DateTime? valNew = null;
            string? strValNew = null;
            bool firstRun = true;
            bool exit = false;
            bool blUseLocalTime = true;
            bool blParseOk = true;
            string strWarnNotAllowed = $"[{AnubisOptions.Options.defaultColor_Warning}]Value not allowed[/]";
            string strRestrictions = "";
            string strCurrentValue = !Value_DateTime.HasValue ? "<null>" : Value_DateTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") + " (" + Value_DateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") + " UTC)";
            string strDefaultValue = "";
            string strAppendQuestion = " in local time";
            string strExactFormat = "dd.MM.yyyy HH:mm:ss";
            string strExactAlternateFormat = "dd.MM.yyyy HH:mm";
            List<string> lstRestrictions = [];
            DateTime dtCurrentUtc = DateTime.UtcNow;
            DateTime dtCurrentRoundedUtc = new(dtCurrentUtc.Year, dtCurrentUtc.Month, dtCurrentUtc.Day,
                                                dtCurrentUtc.Hour, dtCurrentUtc.Minute, 0);
            DateTime? dtMaxUtc = null;

            if (!NullableValue)
            {
                lstRestrictions.Add("empty/null not allowed");
            }
            if (NoPastDate)
            {
                lstRestrictions.Add("no past dates allowed");
            }
            if (MaxFutureTS.HasValue)
            {
                dtMaxUtc = dtCurrentRoundedUtc + MaxFutureTS.Value;
                DateTime dtMaxLocal = dtMaxUtc.Value.ToLocalTime();
                lstRestrictions.Add("date must not be past " + dtMaxLocal.ToString("dd.MM.yyyy HH:mm") + " (" + dtMaxUtc.Value.ToString("yyyy-MM-dd HH:mm") + " UTC)");
            }

            if (lstRestrictions.Count > 0)
            {
                strRestrictions += "Restrictions: ";
                strRestrictions += string.Join(", ", lstRestrictions);
            }

            if (!string.IsNullOrWhiteSpace(strRestrictions))
            {
                strWarnNotAllowed += ", " + strRestrictions;
            }

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine(HelpText_Input);

            var selPrompt = new SelectionPrompt<string>()
                        .Title($"Do you want to enter the timestamp in [bold]local time[/] or [bold]UTC time[/]?:")
                        .AddChoices("Local time", "UTC time");

            selPrompt.HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));
            //selPrompt.DisabledStyle = new Style(AnubisOptions.Options.defaultColor_Normal);

            var spTime =
                AnsiConsole.Prompt(selPrompt);

            blUseLocalTime = spTime == "Local time";

            if (!NullableValue && Value_DateTime.HasValue)
            {
                if (blUseLocalTime)
                {
                    strDefaultValue = Value_DateTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
                }
                else
                {
                    strDefaultValue = Value_DateTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }

            if (blUseLocalTime)
            {
                strExactFormat = "dd.MM.yyyy HH:mm:ss";
                strExactAlternateFormat = "dd.MM.yyyy HH:mm";
                strAppendQuestion = $"in [b]local[/] time (format: {strExactFormat})";
            }
            else
            {
                strExactFormat = "yyyy-MM-dd HH:mm:ss";
                strExactAlternateFormat = "yyyy-MM-dd HH:mm";
                strAppendQuestion = $"in [b]UTC[/] time (format: {strExactFormat})";
            }

            do
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine(HelpText_Input);
                if (!firstRun)
                    AnsiConsole.MarkupLine(strWarnNotAllowed);
                else
                    AnsiConsole.MarkupLine(strRestrictions);

                if (!blParseOk)
                {
                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Could not parse timestamp value[/], use this exact format: [bold]{strExactFormat}[/]");
                }
                blParseOk = true;

                AnsiConsole.MarkupLine($"Current time: {UI.InitConfig.GetCurrentTime(AnubisOptions.Options.defaultColor_Info)}");
                AnsiConsole.MarkupLine($"Current value: [bold]{strCurrentValue}[/]");
                if (NullableValue)
                {
                    strValNew = AnsiConsole.Ask<string?>($"Enter new value for {ValueName} {strAppendQuestion}:", null);

                    if (string.IsNullOrWhiteSpace(strValNew) && Value_DateTime.HasValue)
                    {
                        var confirmed = AnsiConsole.Confirm($"Do you really want to set the value for {ValueName} to <null>?", false);
                        if (!confirmed)
                        {
                            exit = true;
                        }
                    }
                }
                else
                {
                    strValNew = AnsiConsole.Ask<string>($"Enter new value for {ValueName} {strAppendQuestion}:", strDefaultValue);
                }

                if (string.IsNullOrWhiteSpace(strValNew))
                {
                    valNew = null;
                    blParseOk = true;
                }
                else
                {
                    if (blUseLocalTime)
                    {
                        blParseOk = DateTime.TryParseExact(strValNew, [strExactFormat, strExactAlternateFormat], System.Globalization.DateTimeFormatInfo.CurrentInfo, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out DateTime result);
                        if (blParseOk)
                        {
                            valNew = result.ToUniversalTime();
                        }
                    }
                    else
                    {
                        blParseOk = DateTime.TryParseExact(strValNew, [strExactFormat, strExactAlternateFormat], System.Globalization.DateTimeFormatInfo.CurrentInfo, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out DateTime result);
                        if (blParseOk)
                        {
                            valNew = result;
                        }
                    }
                }

                firstRun = false;
            }
            while (!exit && (!blParseOk || !SetValue(valNew, dtMaxUtc)));
        }

        public string GetHelpText()
        {
            return HelpText_Input;
        }

        public override string ToString()
        {
            if (IsDummy)
            {
                return ValueName;
            }
            else
            {
                return $"[underline]{ValueName.EscapeMarkup()}[/]: {ValueToString()}";
            }
        }
    }
}
