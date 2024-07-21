namespace ANUBISWatcher.Shared
{
    public class CountdownData
    {
        public DateTime Timestamp_T0_Local
        {
            get
            {
                return Timestamp_T0_UTC.ToLocalTime();
            }
        }

        public DateTime? Timestamp_SafeMode_Local
        {
            get
            {
                return Timestamp_SafeMode_UTC?.ToLocalTime();
            }
        }

        public DateTime? Timestamp_CheckShutDown_Local
        {
            get
            {
                return Timestamp_CheckShutDown_UTC?.ToLocalTime();
            }
        }

        public DateTime? Timestamp_Emails_Local
        {
            get
            {
                return Timestamp_Emails_UTC?.ToLocalTime();
            }
        }


        public DateTime Timestamp_T0_UTC { get; set; }
        public DateTime? Timestamp_SafeMode_UTC { get; set; }
        public DateTime? Timestamp_CheckShutDown_UTC { get; set; }
        public DateTime? Timestamp_Emails_UTC { get; set; }

        public TimeSpan Countdown_T0 { get; set; }
        public TimeSpan? Countdown_SafeMode { get; set; }
        public TimeSpan? Countdown_CheckShutDown { get; set; }
        public TimeSpan? Countdown_Emails { get; set; }

        public bool Reached_T0 { get; set; }
        public bool Reached_SafeMode { get; set; }
        public bool Reached_Emails { get; set; }

        public bool Triggered_T0 { get; set; }
        public bool Triggered_SafeMode { get; set; }
        public bool Triggered_Emails { get; set; }

        public bool Emails_Sending { get; set; }
        public bool Emails_Sent { get; set; }

        public bool HasVerifiedSystemShutdown { get; set; }

        public CountdownData GetCopy(int correctBySeconds)
        {
            TimeSpan tsCorrection = TimeSpan.FromSeconds(correctBySeconds);

            return new CountdownData()
            {
                Countdown_T0 = this.Countdown_T0 + tsCorrection,
                Countdown_SafeMode = this.Countdown_SafeMode.HasValue ?
                                            this.Countdown_SafeMode.Value + tsCorrection :
                                            null,
                Countdown_CheckShutDown = this.Countdown_CheckShutDown.HasValue ?
                                                    this.Countdown_CheckShutDown.Value + tsCorrection :
                                                    null,
                Countdown_Emails = this.Countdown_Emails.HasValue ?
                                                this.Countdown_Emails.Value + tsCorrection :
                                                null,

                Emails_Sending = this.Emails_Sending,
                Emails_Sent = this.Emails_Sent,

                Reached_T0 = this.Reached_T0,
                Reached_SafeMode = this.Reached_SafeMode,
                Reached_Emails = this.Reached_Emails,

                Triggered_T0 = this.Triggered_T0,
                Triggered_SafeMode = this.Triggered_SafeMode,
                Triggered_Emails = this.Triggered_Emails,

                Timestamp_T0_UTC = Timestamp_T0_UTC - tsCorrection,
                Timestamp_SafeMode_UTC = this.Timestamp_SafeMode_UTC.HasValue ?
                                            this.Timestamp_SafeMode_UTC.Value - tsCorrection :
                                            null,
                Timestamp_CheckShutDown_UTC = this.Timestamp_CheckShutDown_UTC.HasValue ?
                                            this.Timestamp_CheckShutDown_UTC.Value - tsCorrection :
                                            null,
                Timestamp_Emails_UTC = this.Timestamp_Emails_UTC.HasValue ?
                                            this.Timestamp_Emails_UTC.Value - tsCorrection :
                                            null,

                HasVerifiedSystemShutdown = this.HasVerifiedSystemShutdown,
            };
        }
    }
}
