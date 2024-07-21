using ANUBISWatcher.Configuration.Serialization;
using System.Text.Json.Serialization;

namespace ANUBISWatcher.Configuration.ConfigFileData
{
#pragma warning disable IDE1006
    public class ClewareAPIConfigSettings
    {
        /// <summary>
        /// The path to the USB switch command line tool to use
        /// Default is "USBswitchCmd"
        /// </summary>
        public string usbSwitchCommand_Path { get; set; } = "USBswitchCmd";

        /// <summary>
        /// The arguments to use for getting USB switch list
        /// Default is "-l"
        /// </summary>
        public string usbSwitchCommand_Arguments_List { get; set; } = "-l";

        /// <summary>
        /// The command to use for getting switch state.
        /// {switch} will be replace with switch ID.
        /// Default is "-n {switch} -R"
        /// </summary>
        public string usbSwitchCommand_Arguments_Get { get; set; } = "-n {switch} -R";

        /// <summary>
        /// The command to use for setting switch on.
        /// {switch} will be replace with switch ID.
        /// Default is "-n {switch} 1" 
        /// </summary>
        public string usbSwitchCommand_Arguments_SetOn { get; set; } = "-n {switch} 1";

        /// <summary>
        /// The command to use for setting switch off.
        /// {switch} will be replace with switch ID.
        /// Default is "-n {switch} 0"
        /// </summary>
        public string usbSwitchCommand_Arguments_SetOff { get; set; } = "-n {switch} 0";

        /// <summary>
        /// The command to use for setting switch on securely.
        /// {switch} will be replace with switch ID.
        /// Default is "-n {switch} 1 -s"
        /// </summary>
        public string usbSwitchCommand_Arguments_SetOnSecure { get; set; } = "-n {switch} 1 -s";

        /// <summary>
        /// The command to use for setting switch off securely.
        /// {switch} will be replace with switch ID.
        /// Default is "-n {switch} 0 -s"
        /// </summary>
        public string usbSwitchCommand_Arguments_SetOffSecure { get; set; } = "-n {switch} 0 -s";

        /// <summary>
        /// What is the timeout for command calls in seconds?
        /// Default is 5 seconds.
        /// </summary>
        public ushort commandTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Should commands be resent on errors according to AutoRetry settings?
        /// Default is true.
        /// </summary>
        public bool autoRetryOnErrors { get; set; } = true;

        /// <summary>
        /// How often should commands be resent on errors if AutoRetryOnErrors is set to true?
        /// Default is 2.
        /// </summary>
        public byte autoRetryCount { get; set; } = 2;

        /// <summary>
        /// How long (in milliseconds) should the api wait at least before resending a command after an error if AutoRetryOnErrors is set to true?
        /// Default is 500 milliseconds
        /// </summary>
        public ushort autoRetryMinWaitMilliseconds { get; set; } = 500;

        /// <summary>
        /// How long (in milliseconds) should the timespan be, starting from the AutoRetryMinWaitMilliseconds time after an error,
        /// in which a command could be resent? The actual waiting time before resending a command after an error will be randomly
        /// chosen between the AutoRetryMinWaitMilliseconds value and the AutoRetryMinWaitMilliseconds+AutoRetryWaitSpanMilliseconds value
        /// each time a command is resent.
        /// Default is 500 milliseconds.
        /// </summary>
        public ushort autoRetryWaitSpanMilliseconds { get; set; } = 500;
    }

    public class FritzAPIConfigSettings
    {
        /// <summary>
        /// Base URL of FritzBox.
        /// Default is http://fritz.box
        /// </summary>
        public string baseUrl { get; set; } = "http://fritz.box";

        /// <summary>
        /// User to use for FritzBox login.
        /// To provide this property is mandatory.
        /// </summary>
        public string? user { get; set; }

        /// <summary>
        /// Password to use for FritzBox login.
        /// To provide this property is mandatory.
        /// </summary>
        public string? password { get; set; }

        /// <summary>
        /// Should the api auto-login in case you're currently not logged in?
        /// If this and the CheckLoginBeforeCommands options are activated the api will automatically
        /// log you in before a command if you're logged out (even due to inactity).
        /// Default is true (will perform login if you're currently not logged in).
        /// </summary>
        public bool autoLogin { get; set; } = true;

        /// <summary>
        /// Should the api check your login status against the server before each command?
        /// If this and the AutoLogin options are activated the api will automatically
        /// log you in before a command if you're logged out (even due to inactity).
        /// Default is true (will check if you're logged in before a command).
        /// </summary>
        public bool checkLoginBeforeCommands { get; set; } = true;

        /// <summary>
        /// Should the api reload all device names if a device was not found by the name you provided during a command?
        /// Default is flase (do not reload all device names if device name was not found).
        /// </summary>
        public bool reloadNamesIfNotFound { get; set; } = false;

        /// <summary>
        /// What is the timeout for login calls in seconds?
        /// Default is 8 seconds.
        /// </summary>
        public ushort loginTimeoutSeconds { get; set; } = 8;

        /// <summary>
        /// What is the timeout for command calls in seconds?
        /// Default is 5 seconds.
        /// </summary>
        public ushort commandTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Should any SSL validation error be ignored?
        /// Default is false.
        /// </summary>
        public bool ignoreSSLError { get; set; } = false;

        /// <summary>
        /// Should commands be resent on errors according to AutoRetry settings?
        /// Default is true.
        /// </summary>
        public bool autoRetryOnErrors { get; set; } = true;

        /// <summary>
        /// How often should commands be resent on errors if AutoRetryOnErrors is set to true?
        /// Default is 2.
        /// </summary>
        public byte autoRetryCount { get; set; } = 2;

        /// <summary>
        /// How long (in milliseconds) should the api wait at least before resending a command after an error if AutoRetryOnErrors is set to true?
        /// Default is 700 milliseconds
        /// </summary>
        public ushort autoRetryMinWaitMilliseconds { get; set; } = 700;

        /// <summary>
        /// How long (in milliseconds) should the timespan be, starting from the AutoRetryMinWaitMilliseconds time after an error,
        /// in which a command could be resent? The actual waiting time before resending a command after an error will be randomly
        /// chosen between the AutoRetryMinWaitMilliseconds value and the AutoRetryMinWaitMilliseconds+AutoRetryWaitSpanMilliseconds value
        /// each time a command is resent.
        /// Default is 800 milliseconds.
        /// </summary>
        public ushort autoRetryWaitSpanMilliseconds { get; set; } = 800;

    }

    public class SwitchBotAPIConfigSettings
    {
        /// <summary>
        /// Base URL of SwitchBot API.
        /// Default is https://api.switch-bot.com/
        /// </summary>
        public string baseUrl { get; set; } = "https://api.switch-bot.com/";

        /// <summary>
        /// Token to use for SwitchBot login.
        /// To provide this property is mandatory.
        /// </summary>
        public string? token { get; set; }

        /// <summary>
        /// Secret to use for SwitchBot login.
        /// To provide this property is mandatory.
        /// </summary>
        public string? secret { get; set; }

        /// <summary>
        /// Should the api reload all device names if a device was not found by the name you provided during a command?
        /// Default is flase (do not reload all device names if device name was not found).
        /// </summary>
        public bool reloadNamesIfNotFound { get; set; } = false;

        /// <summary>
        /// What is the timeout for command calls in seconds?
        /// Default is 20 seconds.
        /// </summary>
        public ushort commandTimeoutSeconds { get; set; } = 20;

        /// <summary>
        /// Should any SSL validation error be ignored?
        /// Default is false.
        /// </summary>
        public bool ignoreSSLError { get; set; } = false;

        /// <summary>
        /// Should commands be resent on errors according to AutoRetry settings?
        /// Default is true.
        /// </summary>
        public bool autoRetryOnErrors { get; set; } = true;

        /// <summary>
        /// How often should commands be resent on errors if AutoRetryOnErrors is set to true?
        /// Default is 3.
        /// </summary>
        public byte autoRetryCount { get; set; } = 3;

        /// <summary>
        /// How long (in milliseconds) should the api wait at least before resending a command after an error if AutoRetryOnErrors is set to true?
        /// Default is 1500 milliseconds
        /// </summary>
        public ushort autoRetryMinWaitMilliseconds { get; set; } = 1500;

        /// <summary>
        /// How long (in milliseconds) should the timespan be, starting from the AutoRetryMinWaitMilliseconds time after an error,
        /// in which a command could be resent? The actual waiting time before resending a command after an error will be randomly
        /// chosen between the AutoRetryMinWaitMilliseconds value and the AutoRetryMinWaitMilliseconds+AutoRetryWaitSpanMilliseconds value
        /// each time a command is resent.
        /// Default is 3000 milliseconds.
        /// </summary>
        public ushort autoRetryWaitSpanMilliseconds { get; set; } = 3000;
    }

    public class ClewareUSBPollerConfigOptions
    {
        /// <summary>
        /// The time to sleep between switch property polling in milliseconds.
        /// Default is 5000 milliseconds.
        /// </summary>
        public long sleepTimeInMilliseconds { get; set; } = 5000;

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 3 times the SleeTimeInMilliseconds value.
        /// </summary>
        public long? alertTimeInMilliseconds { get; set; } = null;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// The minimum poller count to be able to leave holdback mode and arm panic mode
        /// Default is 3.
        /// </summary>
        public ushort minPollerCountToArm { get; set; } = 3;

        /// <summary>
        /// Should we put all switches immediately into safe mode if one panics?
        /// Default is true and will do so.
        /// </summary>
        public bool autoSafeMode { get; set; } = true;
    }

    public class FritzPollerConfigOptions
    {
        /// <summary>
        /// The time to sleep between switch property polling in milliseconds.
        /// Default is 5000 milliseconds.
        /// </summary>
        public long sleepTimeInMilliseconds { get; set; } = 5000;

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 3 times the SleeTimeInMilliseconds value.
        /// </summary>
        public long? alertTimeInMilliseconds { get; set; } = null;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// The minimum poller count to be able to leave holdback mode and arm panic mode
        /// Default is 3.
        /// </summary>
        public ushort minPollerCountToArm { get; set; } = 3;

        /// <summary>
        /// Should we put all switches immediately into safe mode if one panics?
        /// Default is true and will do so.
        /// </summary>
        public bool autoSafeMode { get; set; } = true;
    }

    public class SwitchBotPollerConfigOptions
    {
        /// <summary>
        /// The time to sleep between switch property polling in milliseconds.
        /// Default is 5000 milliseconds.
        /// </summary>
        public long sleepTimeInMilliseconds { get; set; } = 5000;

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 3 times the SleeTimeInMilliseconds value.
        /// </summary>
        public long? alertTimeInMilliseconds { get; set; } = null;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// The minimum poller count to be able to leave holdback mode and arm panic mode
        /// Default is 3.
        /// </summary>
        public ushort minPollerCountToArm { get; set; } = 3;

        /// <summary>
        /// Should we put all switches immediately into safe mode if one panics?
        /// Default is true and will do so.
        /// </summary>
        public bool autoSafeMode { get; set; } = true;
    }

    public class FilePollerConfigOptions
    {
        /// <summary>
        /// The time to sleep between switch property polling in milliseconds.
        /// Default is 5000 milliseconds.
        /// </summary>
        public long sleepTimeInMilliseconds { get; set; } = 5000;

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 3 times the SleeTimeInMilliseconds value plus the maximum failure read/write time on all files.
        /// </summary>
        public long? alertTimeInMilliseconds { get; set; } = null;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// The minimum poller count to be able to leave holdback mode and arm panic mode
        /// Default is 3.
        /// </summary>
        public ushort minPollerCountToArm { get; set; } = 3;

        /// <summary>
        /// Should we put all files immediately into safe mode if one panics?
        /// Default is null and will depend on if this is a write or read file.
        /// Default for write file: false, will not enter safe mode on panic
        /// Default for read file: true, will enter safe mode on panic
        /// </summary>
        public bool? autoSafeMode { get; set; } = null;
    }

    public class CountdownConfigOptions
    {
        /// <summary>
        /// The time to sleep between switch property polling in milliseconds.
        /// Default is 100 milliseconds.
        /// </summary>
        public long sleepTimeInMilliseconds { get; set; } = 100;

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 50 times the SleeTimeInMilliseconds value (5 seconds for sleep time default value).
        /// </summary>
        public long? alertTimeInMilliseconds { get; set; } = null;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// The minimum poller count to be able to leave holdback mode and arm panic mode
        /// Default is 10.
        /// </summary>
        public ushort minPollerCountToArm { get; set; } = 10;

        /// <summary>
        /// Should we shut down the system on T0? Or is this a manual triggering?
        /// Default is true and will shut down the system on T0
        /// </summary>
        public bool shutDownOnT0 { get; set; } = true;

        /// <summary>
        /// Should we put everything immediately into safe mode if Countdown panics?
        /// Default is true
        /// </summary>
        public bool autoSafeMode { get; set; } = true;

        /// <summary>
        /// How long (in minutes) before the Countdown T-0 should we put everything into SafeMode?
        /// Default is 10 minutes.
        /// A value of 0 means that we will only go into safe mode on triggering the shutdown.
        /// </summary>
        public int countdownAutoSafeModeMinutes { get; set; } = 10;

        /// <summary>
        /// How long (in minutes) after the Countdown T0 should we check for shutdown?
        /// Default is 10 minutes.
        /// A value of 0 means that this will not be checked.
        /// </summary>
        public int checkShutDownAfterMinutes { get; set; } = 10;

        /// <summary>
        /// When exactly, in minutes, should the countdown reach T0 (and system shutdown started if ShutDownOnT0 is true),
        /// starting from now in the future.
        /// Default is in 240 minutes (4 hours) in the future
        /// </summary>
        public long countdownT0MinutesInFuture { get; set; } = 240;

        /// <summary>
        /// Should the calculated countdown T0 time be rounded to the next full hour?
        /// Default is true.
        /// </summary>
        public bool countdownT0RoundToNextHour { get; set; } = true;

        /// <summary>
        /// When exactly should the countdown reach T0 (and system shutdown started if ShutDownOnT0 is true)?
        /// </summary>
        [JsonIgnore]
        public DateTime? countdownT0TimestampUTC { get; set; } = null;

        /// <summary>
        /// When exactly should the countdown reach T0 (and system shutdown started if ShutDownOnT0 is true)
        /// in local time?
        /// </summary>
        [JsonIgnore]
        public DateTime? countdownT0TimestampLocal
        {
            get
            {
                return countdownT0TimestampUTC?.ToLocalTime();
            }
        }


        public void CalculateCountdownT0()
        {
            CalculateCountdownT0(countdownT0RoundToNextHour);
        }

        public void CalculateCountdownT0(bool roundToNextHour)
        {
            countdownT0TimestampUTC = DateTime.UtcNow.AddMinutes(countdownT0MinutesInFuture);
            if (roundToNextHour)
            {
                RoundT0ToNextHour();
            }
        }

        public void RoundT0ToNextHour()
        {
            if (countdownT0TimestampUTC.HasValue)
            {
                DateTime dtCountdown = countdownT0TimestampUTC.Value;
                bool addOneHour = false;
                if (dtCountdown.Minute > 0)
                {
                    addOneHour = true;
                }
                countdownT0TimestampUTC = new DateTime(dtCountdown.Year, dtCountdown.Month, dtCountdown.Day, dtCountdown.Hour, 0, 0);
                if (addOneHour)
                    countdownT0TimestampUTC = countdownT0TimestampUTC.Value.AddHours(1);
            }
        }

        public void RoundT0ToPreviousHour()
        {
            if (countdownT0TimestampUTC.HasValue)
            {
                DateTime dtCountdown = countdownT0TimestampUTC.Value;
                countdownT0TimestampUTC = new DateTime(dtCountdown.Year, dtCountdown.Month, dtCountdown.Day, dtCountdown.Hour, 0, 0);
            }
        }
    }

    public class ControllerConfigOptions
    {
        /// <summary>
        /// The time to sleep between loops.
        /// Default is 100 milliseconds.
        /// </summary>
        public long sleepTimeInMilliseconds { get; set; } = 100;

        /// <summary>
        /// The maximum time in milliseconds without update
        /// Default is null which equals 30 times the SleeTimeInMilliseconds value (3 seconds for sleep time default value).
        /// </summary>
        public long? alertTimeInMilliseconds { get; set; } = null;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// How long (in minutes) after the system shutdown (state shutdown or triggered) should we send the email?
        /// Everything below 1 will be ignored and no mail will be sent.
        /// Default is 55 minutes
        /// </summary>
        public int sendMailEarliestAfterMinutes { get; set; } = 55;

    }

    public class MailingConfigOptions
    {
        /// <summary>
        /// Is mail sending generally enabled? Default is true.
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// Should we check if the system has shut down?
        /// Default is true
        /// </summary>
        public bool checkForShutDown { get; set; } = true;

        /// <summary>
        /// Should we check if the system has shut down verified?
        /// Default is true
        /// </summary>
        public bool checkForShutDownVerified { get; set; } = true;

        /// <summary>
        /// Should we send information mails
        /// Default is false
        /// </summary>
        public bool sendInfoMails { get; set; } = false;

        /// <summary>
        /// Should we send emergency mails
        /// Default is false
        /// </summary>
        public bool sendEmergencyMails { get; set; } = false;

        /// <summary>
        /// Should we only simulate sending mails
        /// Default is true
        /// </summary>
        public bool simulateMails { get; set; } = true;

        /// <summary>
        /// Mail setting: SMTP Server
        /// Default is smtp-mail.outlook.com
        /// </summary>
        public string mailSettings_SmtpServer { get; set; } = "smtp-mail.outlook.com";

        /// <summary>
        /// Mail setting: Port for SMTP Server
        /// Default is null and will be 587 for ssl and 25 for non-ssl
        /// </summary>
        public int? mailSettings_Port { get; set; } = null;

        /// <summary>
        /// Mail setting: Use SSL for SMTP Server?
        /// Default is true
        /// </summary>
        public bool mailSettings_UseSsl { get; set; } = true;

        /// <summary>
        /// Mail setting: From address
        /// Default is echalone@hotmail.com
        /// </summary>
        public string mailSettings_FromAddress { get; set; } = "echalone@hotmail.com";

        /// <summary>
        /// Mail setting: User for mail account
        /// Default is echalone@hotmail.com
        /// </summary>
        public string? mailSettings_User { get; set; } = "echalone@hotmail.com";

        /// <summary>
        /// Mail setting: Password for mail account
        /// Default is none
        /// </summary>
        public string? mailSettings_Password { get; set; } = null;

        /// <summary>
        /// What is the mail address to simulate mail sending to?
        /// Default is none and will not send for real during simulating
        /// </summary>
        public string? mailAddress_Simulate { get; set; } = null;

        /// <summary>
        /// What are the info mail files?
        /// Default are none.
        /// </summary>
        public string[] mailConfig_Info { get; set; } = [];

        /// <summary>
        /// What are the emergency mail files?
        /// Default are none.
        /// </summary>
        public string[] mailConfig_Emergency { get; set; } = [];

        /// <summary>
        /// How long (in minutes) after the Countdown T-0 should we send the email?
        /// Everything below 1 will be ignored and no mail will be sent.
        /// Default is 180 minutes (3 hours)
        /// </summary>
        public int countdownSendMailMinutes { get; set; } = 180;
    }

    public class ClewareUSBSwitchConfigOptions
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

        /// <summary>
        /// Is this switch enabled?
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// Name of the switch.
        /// This option is mandatory.
        /// </summary>
        public string? usbSwitchName { get; set; }

        /// <summary>
        /// Sets if panic mode for this switch should be armed if ArmPanicMode method is called
        /// Default is true/yes.
        /// </summary>
        public bool armPanicMode { get; set; } = true;

        /// <summary>
        /// Sets if safe mode for this switch should be entered if EnterSafeMode method is called
        /// Default is true/yes.
        /// </summary>
        public bool enterSafeMode { get; set; } = true;

        /// <summary>
        /// Sets if switch should be turned off in poller as well if encountering any panic
        /// Default is false/no.
        /// </summary>
        public bool turnOffOnPanic { get; set; } = false;

        /// <summary>
        /// Sets if switch should mark the system as shut down if it has been turned off
        /// Default is false/no.
        /// </summary>
        public bool markShutDownIfOff { get; set; } = false;

        /// <summary>
        /// Should the switch throw panic if it was turned back on during safe mode?
        /// Default is true.
        /// </summary>
        public bool safeModeTurnOnAlarm { get; set; } = true;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// Will we still be sensitive to certain errors in safe mode?
        /// Default is true.
        /// </summary>
        public bool safeModeSensitive { get; set; } = true;

    }

    public class FritzSwitchConfigOptions
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

        /// <summary>
        /// Is this switch enabled?
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// Name of the switch.
        /// This option is mandatory.
        /// </summary>
        public string? switchName { get; set; }

        /// <summary>
        /// Sets if panic mode for this switch should be armed if ArmPanicMode method is called
        /// Default is true/yes.
        /// </summary>
        public bool armPanicMode { get; set; } = true;

        /// <summary>
        /// Sets if safe mode for this switch should be entered if EnterSafeMode method is called
        /// Default is true/yes.
        /// </summary>
        public bool enterSafeMode { get; set; } = true;

        /// <summary>
        /// Sets if switch should mark the system as shut down if it has been turned off
        /// Default is false/no.
        /// </summary>
        public bool markShutDownIfOff { get; set; } = false;

        /// <summary>
        /// Sets if switch should be turned off in poller as well if encountering any panic
        /// Default is false/no.
        /// </summary>
        public bool turnOffOnPanic { get; set; } = false;

        /// <summary>
        /// Below or equal to what mW should the switch be turned off if it is still turned on?
        /// This will only take effect if switch is in panic of safe mode.
        ///     Switch will only be turned off if turnOffOnLowPower is true,
        ///     which is its default value. Otherwise this value will
        ///     only be used to determine if the switch has been powered
        ///     down before a possible SafeModePowerUp alarm. If this value
        ///     is null the switch needs to lose all power for the switch
        ///     to be considered powered down for a SafeModePowerUpAlarm to
        ///     be possible.
        /// Default is 0
        /// Example: 
        ///     0 will turn off switch if power usage is fully cut
        ///     200 will turn off switch if power usage fell to 200 mW or lower
        ///     null will never turn off switch due to power usage
        /// </summary>
        public long? lowPowerCutOff { get; set; } = 0;

        /// <summary>
        /// Defines if power will be cut if lowPowerCutOff level
        /// was reached (or below). This will not change the behaviour
        /// if lowPowerCutOff was set to null since this will always
        /// disable this feature. If a lowPowerCutOff value was
        /// set and this value is set to false then the lowPowerCutOff value
        /// will only be used to determine if a safeModePowerUp alert
        /// can be triggered.
        /// The default is true and will cut the power if the
        /// lowPowerCutOff value was reached.
        /// </summary>
        public bool turnOffOnLowPower { get; set; } = true;

        /// <summary>
        /// Above what mW should the switch throw panic if it was turned back on during safe mode?
        /// If it was turned off before is determined by the value of lowPoweCutOff (and not minPower!). 
        /// If the power shouldn't be cut by that value (for which it is used too) just turn that 
        /// feature off by setting turnOffOnLowPower to false.
        /// Default is 200
        /// Example: 
        ///     0 will put switch into panic mode if any power usage is back on after the power fell below or to LowPowerCutOff
        ///     200 will put switch into panic mode if power usage is back above 200 mW after the power fell below or to LowPowerCutOff
        ///     null will never throw panic due to power up during safe mode
        /// </summary>
        public long? safeModePowerUpAlarm { get; set; } = 200;

        /// <summary>
        /// Below what power (in mW)  should the switch display the power in the graph warning colors? (has no actual functioning effect)
        /// Default is null and will be minPower + ((maxPower - minPower) * 0.2)
        /// </summary>
        public long? minPowerWarn { get; set; } = null;

        /// <summary>
        /// Above what power (in mW) should the switch display the power in the graph warning colors? (has no actual functioning effect)
        /// Default is null and will be maxPower - ((maxPower - minPower) * 0.2)
        /// </summary>
        public long? maxPowerWarn { get; set; } = null;

        /// <summary>
        /// Below what power (in mW) should the switch throw panic outside of safe mode?
        /// Default is null.
        /// </summary>
        public long? minPower { get; set; } = null;

        /// <summary>
        /// Above what power (in mW) should the switch throw panic outside of safe mode?
        /// Default is null.
        /// </summary>
        public long? maxPower { get; set; } = null;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// Will we still be sensitive to certain errors in safe mode?
        /// Default is true.
        /// </summary>
        public bool safeModeSensitive { get; set; } = true;

    }

    public class SwitchBotSwitchConfigOptions
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

        /// <summary>
        /// Is this switch enabled?
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// Name of the switch bot.
        /// This option is mandatory.
        /// </summary>
        public string? switchName { get; set; }

        /// <summary>
        /// Sets if panic mode for this switch should be armed if ArmPanicMode method is called
        /// Default is true/yes.
        /// </summary>
        public bool armPanicMode { get; set; } = true;

        /// <summary>
        /// Sets if safe mode for this switch should be entered if EnterSafeMode method is called
        /// Default is true/yes.
        /// </summary>
        public bool enterSafeMode { get; set; } = true;

        /// <summary>
        /// Sets if switch should mark the system as shut down if it has been turned off
        /// Default is false/no.
        /// </summary>
        public bool markShutDownIfOff { get; set; } = false;

        /// <summary>
        /// Sets if switch should be turned off in poller as well if encountering any panic
        /// Default is false/no.
        /// </summary>
        public bool turnOffOnPanic { get; set; } = false;

        /// <summary>
        /// Below or equal to what percentage should the switch be turned off if it is still turned on?
        /// This will only take effect if switch is in panic of safe mode
        /// Default is 5
        /// Example: 
        ///     5 will turn off switch if battery is below 5 percent
        ///     10 will turn off switch if battery is below 10 percent
        ///     null will never turn off switch due to battery
        /// </summary>
        public long? minBattery { get; set; } = 5;

        /// <summary>
        /// Should we use a strict battery check, that means we will throw panic if 
        /// we have any problem checking the battery (true), otherwise (false) we will only
        /// throw panic if we were able to check the battery AND it is below the LowBatteryCutOff limit.
        /// Default is false.
        /// </summary>
        public bool strictBatteryCheck { get; set; } = false;

        /// <summary>
        /// Should we check the power state of the switch (on/off)?
        /// Default is true.
        /// </summary>
        public bool stateCheck { get; set; } = true;

        /// <summary>
        /// Should we use a strict state check, that means we will throw panic if 
        /// we have any problem checking the state (true), otherwise (false) we will only
        /// throw panic if we were able to check the state AND it is off or unknown.
        /// Default is false.
        /// </summary>
        public bool strictStateCheck { get; set; } = false;

        /// <summary>
        /// Should we throw panic if the battery is below battery cut off and
        /// the measured battery is 0? This could just mean there's no real
        /// data from the switch bot and a battery value of above 0 should have been
        /// registered previously. The default is false and will not throw panic if 
        /// the battery value is 0, even if that's below the battery cut off.
        /// Default is false.
        /// </summary>
        public bool zeroBatteryIsValidPanic { get; set; } = false;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// Will we still be sensitive to certain errors in safe mode?
        /// Default is true.
        /// </summary>
        public bool safeModeSensitive { get; set; } = true;

    }

    public class FileConfigOptions
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

        /// <summary>
        /// Is this switch enabled?
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// Global name of the watcher file (for later references).
        /// This option is mandatory.
        /// </summary>
        public string? name { get; set; }

        /// <summary>
        /// Path to the file to watch or write to
        /// This option is mandatory.
        /// </summary>
        public string? path { get; set; }

        /// <summary>
        /// Sets if we should write panic state to file immediatly if we encounter panic on a write file
        /// Default is false/no.
        /// </summary>
        public bool writeStateOnPanic { get; set; } = false;

        /// <summary>
        /// The maximum age of the last update to the status timestamp in seconds
        /// Default is 120 seconds.
        /// </summary>
        public int maxUpdateAgeInSeconds { get; set; } = 120;

        /// <summary>
        /// The maximum age of the last update to the status timestamp in negative seconds (if time might be ahead on remote computer).
        /// Default is null and therefore same as age in positive seconds.
        /// </summary>
        public int? maxUpdateAgeInNegativeSeconds { get; set; } = null;

        /// <summary>
        /// Should we check the file state?
        /// Default is yes (true)
        /// </summary>
        public bool stateCheck { get; set; } = true;

        /// <summary>
        /// Should we check the file state timestamp?
        /// Default is yes (true)
        /// </summary>
        public bool stateTimestampCheck { get; set; } = true;

        /// <summary>
        /// The timeout for property access locks in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public long lockTimeoutInMilliseconds { get; set; } = 500;

        /// <summary>
        /// The min timeout for file access retry in milliseconds.
        /// Default is 100 milliseconds.
        /// </summary>
        public int fileAccessRetryMillisecondsMin { get; set; } = 100;

        /// <summary>
        /// The max timeout for file access retry in milliseconds.
        /// Default is 500 milliseconds.
        /// </summary>
        public int fileAccessRetryMillisecondsMax { get; set; } = 500;

        /// <summary>
        /// The amount of times we will retry to access the file.
        /// Default is 10 times.
        /// </summary>
        public int fileAccessRetryCountMax { get; set; } = 10;

        /// <summary>
        /// Our machine will only send the mails if all read files with MailPriority set to true
        /// are unreachable or unresponsive (panic does not count).
        /// Default is false.
        /// </summary>
        public bool mailPriority { get; set; } = false;

        /// <summary>
        /// Will we still be sensitive to certain errors in safe mode?
        /// Default is true.
        /// </summary>
        public bool safeModeSensitive { get; set; } = true;

    }

    public class ClewareUSBConfigMapping
    {
        public string? name { get; set; }
        public long? id { get; set; }
    }

    [JsonDerivedType(typeof(MessageTurnOn), typeDiscriminator: "message")]
    [JsonDerivedType(typeof(DeviceTurnOn), typeDiscriminator: "device")]
    public abstract class TurnOnEntity
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

        /// <summary>
        /// should we disable this switch was not discovered?
        /// Default is true
        /// </summary>
        public bool disableIfNotDiscovered { get; set; } = true;

        public bool enabled { get; set; } = true;

        /// <summary>
        /// Whats the id of the device to turn on?
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// how many seconds should we wait after we turned on this device?
        /// default is 5
        /// </summary>
        public long waitSecondsAfterTurnOn { get; set; } = 5;
    }

    public class MessageTurnOn : TurnOnEntity
    {
        public string? message { get; set; }
    }

    public class DeviceTurnOn : TurnOnEntity
    {
        [JsonConverter(typeof(JsonEnumConverter<TriggerType>))]
        public TriggerType deviceType { get; set; }
    }

#pragma warning restore IDE1006
}
