namespace ANUBISWatcher.Options
{
    public class MailingOptions
    {
        /// <summary>
        /// Should we check if the system has shut down?
        /// Default is true
        /// </summary>
        public bool CheckForShutDown { get; init; }

        /// <summary>
        /// Should we check if the system has shut down verified?
        /// Default is true
        /// </summary>
        public bool CheckForShutDownVerified { get; init; }

        /// <summary>
        /// Should we send information mails
        /// Default is false
        /// </summary>
        public bool SendInfoMails { get; init; }

        /// <summary>
        /// Should we send emergency mails
        /// Default is false
        /// </summary>
        public bool SendEmergencyMails { get; init; }

        /// <summary>
        /// Should we only simulate sending mails
        /// Default is true
        /// </summary>
        public bool SimulateMails { get; init; }

        /// <summary>
        /// Mail setting: SMTP Server
        /// Default is smtp-mail.outlook.com
        /// </summary>
        public string MailSettings_SmtpServer { get; init; }

        /// <summary>
        /// Mail setting: Port for SMTP Server
        /// Default is null and will be 587 for ssl and 25 for non-ssl
        /// </summary>
        public int? MailSettings_Port { get; init; }

        /// <summary>
        /// Mail setting: Use SSL for SMTP Server?
        /// Default is true
        /// </summary>
        public bool MailSettings_UseSsl { get; init; }

        /// <summary>
        /// Mail setting: From address
        /// Default is echalone@hotmail.com
        /// </summary>
        public string MailSettings_FromAddress { get; init; }

        /// <summary>
        /// Mail setting: User for mail account
        /// Default is echalone@hotmail.com
        /// </summary>
        public string? MailSettings_User { get; init; }

        /// <summary>
        /// Mail setting: Password for mail account
        /// Default is none
        /// </summary>
        public string? MailSettings_Password { get; init; }

        /// <summary>
        /// What is the mail address to simulate mail sending to?
        /// Default is none and will not send for real during simulating
        /// </summary>
        public string? MailAddress_Simulate { get; init; }

        /// <summary>
        /// What are the info mail files?
        /// Default are none.
        /// </summary>
        public List<string> MailConfig_Info { get; init; }

        /// <summary>
        /// What are the emergency mail files?
        /// Default are none.
        /// </summary>
        public List<string> MailConfig_Emergency { get; init; }

        /// <summary>
        /// How long (in minutes) after the Countdown T-0 should we send the email?
        /// Everything below 1 will be ignored and no mail will be sent.
        /// Default is 180 minutes (3 hours)
        /// </summary>
        public int CountdownSendMailMinutes { get; init; }

        public MailingOptions()
        {
            SendInfoMails = false;
            SendEmergencyMails = false;
            SimulateMails = true;
            CheckForShutDown = true;
            CheckForShutDownVerified = true;
            CountdownSendMailMinutes = 180;
            MailSettings_SmtpServer = "smtp-mail.outlook.com";
            MailSettings_Port = null;
            MailSettings_UseSsl = true;
            MailSettings_FromAddress = "echalone@hotmail.com";
            MailSettings_User = "echalone@hotmail.com";
            MailSettings_Password = null;
            MailAddress_Simulate = null;
            MailConfig_Info = [];
            MailConfig_Emergency = [];
        }
    }
}
