using ANUBISWatcher.Controlling;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace ANUBISWatcher.Pollers
{
    public class CountdownPoller
    {
        #region Fields

        #region Constants

        private readonly object lock_TimestampLastUpdate = new();
        private object lock_T0Reset = new();
        private readonly object lock_PollerCount = new();
        private readonly object lock_PollerStatus = new();

        /// <summary>
        /// by how much should the internal countdown be pushed back (in seconds)
        /// so it will not fire before the displayed countdown but
        /// maybe a few moments afterwards.
        /// </summary>
        private const int c_pushInternalCountdownBySeconds = 1;

        /// <summary>
        /// how many milliseconds should we wait between mail sending checks?
        /// </summary>
        private const int c_sleepBetweenMailChecksInMilliseconds = 10;

        #endregion

        #region Unchangable fields

        private readonly TimeSpan _sleepTimespan = TimeSpan.Zero;
        private readonly TimeSpan _lockTimeout = TimeSpan.Zero;

        #endregion

        #region Changable fields

        private Thread? _pollingThread = null;
        private CancellationTokenSource? _cancellationTokenSource = null;
        private DateTime _timestampLastUpdate;
        private ulong _pollerCount = 0;
        private volatile PollerStatus _pollerStatus = PollerStatus.Stopped;

        #endregion

        #endregion

        #region Properties

        public CountdownPollerOptions Options { get; init; }

        private volatile bool _hasPanicked = false;
        public bool HasPanicked { get { return _hasPanicked; } private set { _hasPanicked = value; } }

        private volatile bool _hasLoopPanic = false;
        public bool HasLoopPanic { get { return _hasLoopPanic; } private set { _hasLoopPanic = value; } }

        private volatile bool _sendingMail = false;
        public bool SendingMail { get { return _sendingMail; } private set { _sendingMail = value; } }

        private volatile bool _mailSent = false;
        public bool MailSent { get { return _mailSent; } private set { _mailSent = value; } }

        private volatile bool _countdownTriggered = false;
        public bool CountdownTriggered { get { return _countdownTriggered; } private set { _countdownTriggered = value; } }

        private volatile bool _hasVerifiedSystemShutDown = false;
        public bool HasVerifiedSystemShutdown { get { return _hasVerifiedSystemShutDown; } private set { _hasVerifiedSystemShutDown = value; } }

        private volatile bool _hasNewT0 = false;
        private DateTime? _resetT0ToUTC = null;

        private DateTime? ResetT0ToUTC
        {
            get
            {
                if (_hasNewT0)
                {
                    bool lockTaken = false;
                    try
                    {
                        lockTaken = Monitor.TryEnter(lock_T0Reset, _lockTimeout);

                        if (!lockTaken)
                        {
                            object newLock = new();
                            Monitor.Enter(newLock);
                            lock_T0Reset = newLock;
                            lockTaken = true;
                        }

                        _hasNewT0 = false;
                        if (_resetT0ToUTC.HasValue)
                        {
                            DateTime? retVal = _resetT0ToUTC.Value;
                            _resetT0ToUTC = null;

                            return retVal;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    finally
                    {
                        // Ensure that the lock is released.
                        if (lockTaken)
                        {
                            Monitor.Exit(lock_T0Reset);
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            set
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_T0Reset, _lockTimeout);

                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_T0Reset = newLock;
                        lockTaken = true;
                    }

                    _resetT0ToUTC = value;
                    _hasNewT0 = true;
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_T0Reset);
                    }
                }
            }
        }


        public bool TriggerShutDownOnError
        {
            get
            {
                return (Status == PollerStatus.Armed || Status == PollerStatus.SafeMode); // make sure poller is in correct mode
            }
        }

        public bool CanBeArmed
        {
            get
            {
                return (Status == PollerStatus.Monitoring || Status == PollerStatus.Holdback) && // make sure poller is in correct mode
                        (PollerCount >= (Options?.MinPollerCountToArm ?? 0)) && // make sure we ran through enough poller loops
                        !HasLoopPanic && !HasPanicked; // make sure poller has no panics
            }
        }

        private ILogger? Logging { get { return Options?.Logger; } }

        public DateTime LastUpdateTimestamp
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_TimestampLastUpdate, _lockTimeout);
                    if (lockTaken)
                        return _timestampLastUpdate;
                    else
                        throw new LockTimeoutException(nameof(LastUpdateTimestamp), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_TimestampLastUpdate);
                    }
                }
            }
            private set
            {
                lock (lock_TimestampLastUpdate)
                {
                    _timestampLastUpdate = value;
                }
            }
        }

        public ulong PollerCount
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_PollerCount, _lockTimeout);
                    if (lockTaken)
                        return _pollerCount;
                    else
                        throw new LockTimeoutException(nameof(PollerCount), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_PollerCount);
                    }
                }
            }
            private set
            {
                lock (lock_PollerCount)
                {
                    _pollerCount = value;
                }
            }
        }

        public PollerStatus Status
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_PollerStatus, _lockTimeout);
                    if (lockTaken)
                        return _pollerStatus;
                    else
                        throw new LockTimeoutException(nameof(Status), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_PollerStatus);
                    }
                }
            }
            private set
            {
                lock (lock_PollerStatus)
                {
                    _pollerStatus = value;
                }
            }
        }

        public long AlertTimeInMilliseconds
        {
            get
            {
                return Options.AlertTimeInMilliseconds ?? (Options.SleepTimeInMilliseconds * 50);
            }
        }

        public long MillisecondsSinceLastUpdate
        {
            get
            {
                return (long)Math.Floor((DateTime.UtcNow - LastUpdateTimestamp).TotalMilliseconds);
            }
        }

        public bool IsPollerUnresponsive
        {
            get
            {
                try
                {
                    // do not check while sending mail since this might take longer and it's not important then any more anyways
                    return !SendingMail && MillisecondsSinceLastUpdate > AlertTimeInMilliseconds;
                }
                catch (Exception ex)
                {
                    Logging?.LogCritical(ex, "While trying to check for unresponsive countdown poller: {message}", ex.Message);
                    Logging?.LogDebug("Mimicking unresponsive behaviour so we catch this problem");
                    return true;
                }
            }
        }

        #endregion

        #region Constructors

        public CountdownPoller(CountdownPollerOptions options)
        {
            Options = options;
            _lockTimeout = TimeSpan.FromMilliseconds(Options.LockTimeoutInMilliseconds);
            _sleepTimespan = TimeSpan.FromMilliseconds(Options.SleepTimeInMilliseconds);
            ResetFlags();
        }

        #endregion

        #region Helper methods

        private void ResetFlags()
        {
            CountdownTriggered = false;
            MailSent = false;
            HasVerifiedSystemShutdown = false;
        }

        private void CheckThreadCancellation()
        {
            (_cancellationTokenSource?.Token)?.ThrowIfCancellationRequested();
        }

        private void UpdateTimestamp()
        {
            using (Logging?.BeginScope("UpdateTimestamp"))
            {
                try
                {
                    LastUpdateTimestamp = DateTime.UtcNow;
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Countdown polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Countdown polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to set last update timestamp: {message}", ex.Message);
                }
            }
        }

        private CountdownData GetCountdownInfo(DateTime T0timestampUTC)
        {
            Options.CountdownT0UTC = T0timestampUTC;
            DateTime dtCorrectedT0 = Options.CountdownT0UTC.AddSeconds(c_pushInternalCountdownBySeconds); // add 1 second just to be sure it's not ahead of displayed countdown
                                                                                                          // calculate safe mode time or set to null if not setting to safe mode beforehand
            DateTime? dtSafeMode = Options.CountdownAutoSafeModeMinutes > 0 ? dtCorrectedT0.AddMinutes(-1 * Options.CountdownAutoSafeModeMinutes) : null;
            // calculate check verified system shutdown time or set to null if not checking for system shutdown
            DateTime? dtCheckShutDown = Options.CheckShutDownAfterMinutes > 0 ? dtCorrectedT0.AddMinutes(Options.CheckShutDownAfterMinutes) : null;
            // calculate mail send time or set to null if not sending mails
            DateTime? dtMailSend = Options.MailSettings.CountdownSendMailMinutes > 0 &&
                                    (Options.MailSettings.SendEmergencyMails || Options.MailSettings.SendInfoMails) ?
                                        dtCorrectedT0.AddMinutes(Options.MailSettings.CountdownSendMailMinutes) : null;

            Logging?.LogTrace("Countdown in poller now at {countdown} UTC", dtCorrectedT0);
            if (dtSafeMode.HasValue)
                Logging?.LogTrace("SafeMode in countdown poller will be activated at {safemode} UTC", dtSafeMode.Value);
            else
                Logging?.LogTrace("SafeMode in countdown poller will be activated on panic or trigger");
            if (dtCheckShutDown.HasValue)
                Logging?.LogTrace("ShutDown in countdown poller will be checked at {shutdowncheck} UTC", dtCheckShutDown.Value);
            else
                Logging?.LogTrace("ShutDown in countdown poller will NOT be checked");
            if (dtMailSend.HasValue)
                Logging?.LogTrace("Mails in countdown poller will be sent at {sendmail} UTC", dtMailSend.Value);
            else
                Logging?.LogTrace("Mails in countdown poller will NOT be sent");


            return
                new CountdownData()
                {
                    Timestamp_T0_UTC = dtCorrectedT0,
                    Timestamp_SafeMode_UTC = dtSafeMode,
                    Timestamp_CheckShutDown_UTC = dtCheckShutDown,
                    Timestamp_Emails_UTC = dtMailSend,

                    Countdown_T0 = TimeSpan.Zero,
                    Countdown_SafeMode = null,
                    Countdown_CheckShutDown = null,
                    Countdown_Emails = null,

                    Reached_T0 = false,
                    Reached_SafeMode = false,
                    Reached_Emails = false,

                    Triggered_T0 = false,
                    Triggered_SafeMode = false,
                    Triggered_Emails = false,

                    Emails_Sending = false,
                    Emails_Sent = false,

                    HasVerifiedSystemShutdown = false,
                };
        }

        private void CountdownPollerLoop()
        {
            using (Logging?.BeginScope("CountdownPollerLoop"))
            {
                HasPanicked = false;
                HasLoopPanic = false;
                ResetFlags();
                CancellationToken? token = _cancellationTokenSource?.Token;

                CountdownData countdownInfo = GetCountdownInfo(Options.CountdownT0UTC);

                Logging?.LogInformation("Starting countdown polling loop");
                while (!HasLoopPanic && !(token?.IsCancellationRequested ?? false))
                {
                    try
                    {
                        if (_hasNewT0)
                        {
                            DateTime? newT0UTC = ResetT0ToUTC;
                            if (newT0UTC.HasValue)
                            {
                                Logging?.LogInformation("Resetting countdown to {countdownT0} UTC", newT0UTC.Value);
                                countdownInfo = GetCountdownInfo(newT0UTC.Value);
                                ResetFlags();
                            }
                        }

                        UpdateTimestamp();
                        CheckThreadCancellation();

                        MainController? controller = SharedData.Controller;

                        if (controller != null)
                        {
                            bool mainControllerUnresponsive = controller.IsAlive && controller.IsControllerUnresponsive;

                            if (mainControllerUnresponsive && TriggerShutDownOnError)
                            {
                                Logging?.LogCritical("The main controller got stuck according to the countdown poller, throwing unresponsive panic and triggering system shutdown!");
                                HasPanicked = true;
                                if (Options.AutoSafeMode)
                                {
                                    Logging?.LogInformation("Automatically entering safe mode in poller after panic of CountdownPoller because AutoSafeMode was set to true");
                                    SharedData.EnterSafeMode(true);
                                }
                                Generator.TriggerShutDown(UniversalPanicType.Countdown, ConstantTriggerIDs.ID_MainController, UniversalPanicReason.Unresponsive);
                            }

                            UpdateTimestamp();
                            CheckThreadCancellation();

                            // calculate the duration until T-0 time
                            TimeSpan tsCountdown = DateTime.UtcNow - countdownInfo.Timestamp_T0_UTC;

                            countdownInfo.Countdown_T0 = tsCountdown;
                            SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds);

                            // here we enter safe mode according to our countdown, if we are in armed mode or shutdown mode or triggered mode
                            // and if we have not entered safe mode before
                            if (!(SharedData.Controller?.IsInSafeMode ?? true) &&
                                    (SharedData.CurrentControllerStatus == ControllerStatus.Armed ||
                                        SharedData.CurrentControllerStatus == ControllerStatus.ShutDown ||
                                        SharedData.CurrentControllerStatus == ControllerStatus.Triggered))
                            {
                                if (countdownInfo.Timestamp_SafeMode_UTC.HasValue)
                                {
                                    TimeSpan tsSafeMode = DateTime.UtcNow - countdownInfo.Timestamp_SafeMode_UTC.Value;

                                    countdownInfo.Countdown_SafeMode = tsSafeMode;
                                    SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds);

                                    if (tsCountdown.TotalMilliseconds > 0 || tsSafeMode.TotalMilliseconds > 0)
                                    {
                                        Logging?.LogInformation("Entering SafeMode according to countdown");
                                        SharedData.EnterSafeMode(false);
                                        countdownInfo.Reached_SafeMode = true;
                                        countdownInfo.Triggered_SafeMode = true;
                                        SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                                    }
                                }
                                else
                                {
                                    if (tsCountdown.TotalMilliseconds > 0)
                                    {
                                        Logging?.LogInformation("Entering SafeMode before triggering due to countdown");
                                        SharedData.EnterSafeMode(false);
                                        countdownInfo.Triggered_SafeMode = true;
                                        SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                                    }
                                }
                            }
                            else
                            {
                                if (countdownInfo.Timestamp_SafeMode_UTC.HasValue)
                                {
                                    TimeSpan tsSafeMode = DateTime.UtcNow - countdownInfo.Timestamp_SafeMode_UTC.Value;

                                    countdownInfo.Countdown_SafeMode = tsSafeMode;
                                    SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds);
                                }
                            }

                            UpdateTimestamp();
                            CheckThreadCancellation();

                            // here we trigger the system shutdown according to our countdown T-0, if we are in the correct modes and haven't triggered yet
                            if (!CountdownTriggered &&
                                    (SharedData.CurrentControllerStatus == ControllerStatus.SafeMode ||
                                        SharedData.CurrentControllerStatus == ControllerStatus.Armed ||
                                        SharedData.CurrentControllerStatus == ControllerStatus.ShutDown ||
                                        SharedData.CurrentControllerStatus == ControllerStatus.Triggered))
                            {
                                // check if the countdown has reached T-0
                                if (tsCountdown.TotalMilliseconds > 0)
                                {
                                    Logging?.LogInformation("!!!Countdown reached T-0!!!");
                                    countdownInfo.Reached_T0 = true;
                                    SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                                    if (Options.ShutDownOnT0)
                                    {
                                        Logging?.LogInformation("Triggering system shutdown according to countdown");
                                        Generator.TriggerShutDown(UniversalPanicType.Countdown, ConstantTriggerIDs.ID_Countdown_T0, UniversalPanicReason.NoPanic);
                                        countdownInfo.Triggered_T0 = true;
                                        SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                                    }
                                    else
                                    {
                                        Logging?.LogInformation("Not triggering system shutdown since ShutDownOnT0 was set to false");
                                    }
                                    CountdownTriggered = true;
                                }
                            }

                            UpdateTimestamp();
                            CheckThreadCancellation();

                            // System shutdown is checked here
                            if (countdownInfo.Timestamp_CheckShutDown_UTC.HasValue)
                            {
                                TimeSpan tsCheckSystemShutdown = DateTime.UtcNow - countdownInfo.Timestamp_CheckShutDown_UTC.Value;

                                countdownInfo.Countdown_CheckShutDown = tsCheckSystemShutdown;
                                SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds);

                                if (!HasVerifiedSystemShutdown &&
                                    (SharedData.CurrentControllerStatus == ControllerStatus.Armed ||
                                        SharedData.CurrentControllerStatus == ControllerStatus.SafeMode ||
                                        SharedData.CurrentControllerStatus == ControllerStatus.ShutDown ||
                                        SharedData.CurrentControllerStatus == ControllerStatus.Triggered))
                                {
                                    if (tsCountdown.TotalMilliseconds > 0 && tsCheckSystemShutdown.TotalMilliseconds > 0)
                                    {
                                        bool blVerifiedShutdown = false;
                                        if (SharedData.CurrentControllerStatus == ControllerStatus.ShutDown ||
                                            SharedData.CurrentControllerStatus == ControllerStatus.Triggered)
                                        {
                                            if (SharedData.Controller?.HasShutDownVerified ?? false)
                                            {
                                                Logging?.LogInformation("CountdownPoller is reporting verified system shutdown");
                                                HasVerifiedSystemShutdown = true;
                                                countdownInfo.HasVerifiedSystemShutdown = HasVerifiedSystemShutdown;
                                                SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds);
                                                blVerifiedShutdown = true;
                                            }
                                            else
                                            {
                                                Logging?.LogWarning("CountdownPoller reporting: Controller has not verified shutdown state of system, this will trigger a panic and requeue the shutdown verification");
                                            }
                                        }
                                        else
                                        {
                                            Logging?.LogWarning("CountdownPoller reporting: ANUBIS System NOT in shutdown or trigger state during verification check, this will trigger a panic and requeue the shutdown verification, current state is: {state}", SharedData.CurrentControllerStatus);
                                        }

                                        if (!blVerifiedShutdown)
                                        {
                                            countdownInfo.Timestamp_CheckShutDown_UTC = DateTime.UtcNow.AddMinutes(Options.CheckShutDownAfterMinutes);
                                            Logging?.LogCritical("CountdownPoller could not verify system shutdown, throwing panic, triggering system shutdown and requeuing check for system shutdown to {newsystemshutdowncheck}", countdownInfo.Timestamp_CheckShutDown_UTC);
                                            HasPanicked = true;
                                            if (Options.AutoSafeMode)
                                            {
                                                Logging?.LogInformation("Automatically entering safe mode in poller after panic of CountdownPoller because AutoSafeMode was set to true");
                                                SharedData.EnterSafeMode(true);
                                            }
                                            Generator.TriggerShutDown(UniversalPanicType.Countdown, ConstantTriggerIDs.ID_SystemShutdownUnverified, UniversalPanicReason.CheckConditionViolation);
                                            HasVerifiedSystemShutdown = false;
                                            countdownInfo.HasVerifiedSystemShutdown = false;
                                            tsCheckSystemShutdown = DateTime.UtcNow - countdownInfo.Timestamp_CheckShutDown_UTC.Value;
                                            countdownInfo.Countdown_CheckShutDown = tsCheckSystemShutdown;
                                            SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds);
                                        }
                                    }
                                }
                            }

                            UpdateTimestamp();
                            CheckThreadCancellation();

                            // Mails are sent here
                            if (countdownInfo.Timestamp_Emails_UTC.HasValue)
                            {
                                TimeSpan tsEmail = DateTime.UtcNow - countdownInfo.Timestamp_Emails_UTC.Value;

                                countdownInfo.Countdown_Emails = tsEmail;
                                SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds);

                                // check if the time for mails have arrived and if we haven't sent the mails yet
                                if (!MailSent && tsEmail.TotalMilliseconds > 0)
                                {
                                    CheckMailSending(countdownInfo, token);
                                }
                            }
                        }
                        else
                        {
                            throw new CountdownPollerException("Main controller not found in shared data, this was unexpected");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogDebug("Recieved countdown polling loop cancelation");
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogDebug("Recieved countdown polling loop interruption");
                    }
                    catch (Exception ex)
                    {
                        Logging?.LogCritical(ex, "While running countdown polling loop, will end countdown polling loop due to this error: {message}", ex.Message);
                        HasLoopPanic = HasPanicked = true;
                        if (TriggerShutDownOnError)
                            Generator.TriggerShutDown(UniversalPanicType.CountdownPoller, ConstantTriggerIDs.ID_GeneralError, UniversalPanicReason.GeneralError);
                    }

                    PollerCount++;
                    if (PollerCount > (ulong.MaxValue - 10))
                    {
                        PollerCount = 1000; // just start back at 1000 if we would loop around
                    }
                    if (PollerCount % 1000 == 0)
                    {
                        Logging?.LogDebug("Countdown poller count is now at {pollercount}", PollerCount);
                        Logging?.LogDebug("Countdown to T0 is now at {countdownT0}", countdownInfo.Countdown_T0);
                        if (countdownInfo.Countdown_SafeMode.HasValue)
                        {
                            Logging?.LogDebug("Countdown to safe mode is now at {countdownSafeMode}", countdownInfo.Countdown_SafeMode.Value);
                        }
                        if (countdownInfo.Countdown_CheckShutDown.HasValue)
                        {
                            Logging?.LogDebug("Countdown to shutdown check is now at {countdownCheckShutDown}", countdownInfo.Countdown_CheckShutDown.Value);
                        }
                        if (countdownInfo.Countdown_Emails.HasValue)
                        {
                            Logging?.LogDebug("Countdown to mail sending is now at {countdownSendMails}", countdownInfo.Countdown_Emails.Value);
                        }
                    }
                    if (!(token?.IsCancellationRequested ?? false))
                    {
                        if (PollerCount % 1000 == 0)
                            Logging?.LogDebug("Sleeping for {sleepTime} milliseconds before relooping in countdown", _sleepTimespan);
                        if (token.HasValue)
                        {
                            if (token.Value.WaitHandle.WaitOne(_sleepTimespan))
                                Logging?.LogTrace("Countdown poller recieved thread cancellation request during sleep");
                        }
                        else
                            Thread.Sleep(_sleepTimespan);
                    }
                }
                Logging?.LogInformation("Ending countdown polling loop");
            }
        }

        private void WaitShortTime(CancellationToken? token)
        {
            if (!(token?.IsCancellationRequested ?? false))
            {
                if (token.HasValue)
                {
                    if (token.Value.WaitHandle.WaitOne(c_sleepBetweenMailChecksInMilliseconds))
                        Logging?.LogTrace("Countdown poller recieved thread cancellation request during short sleep");
                }
                else
                    Thread.Sleep(c_sleepBetweenMailChecksInMilliseconds);
            }

        }

        // Make sure this method isn't optimized to make sure our multiple tests in if statements for mail sending
        // aren't combined to say, one bit in the memory, which might then be affected by things like a freak cosmic
        // rays that flip the bit and then send the mails earlier than they should
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void CheckMailSending(CountdownData countdownInfo, CancellationToken? token)
        {
            WaitShortTime(token); // this is to make sure there's no optimization and some time between multiple mailing checks

            // check if mail sending is now possible according to controller
            if (SharedData.Controller?.MailSendingPossible ?? false)
            {
                WaitShortTime(token); // this is to make sure there's no optimization and some time between multiple mailing checks

                // check if system has shut down or even verified shut down if these options are set
                if ((!Options.MailSettings.CheckForShutDown || (SharedData.Controller?.HasShutDown ?? false)) &&
                        (!Options.MailSettings.CheckForShutDownVerified ||
                            ((SharedData.Controller?.HasShutDownVerified ?? false) &&
                                countdownInfo.HasVerifiedSystemShutdown)))
                {
                    Logging?.LogInformation("Reached mail sending time according to countdown and controller and we have mail priority...");
                    countdownInfo.Reached_Emails = true;
                    SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);

                    try
                    {
                        if (string.IsNullOrWhiteSpace(Options.MailSettings.MailSettings_FromAddress))
                        {
                            throw new SendMailException("From address was empty");
                        }

                        if (Options.MailSettings.MailSettings_Port.HasValue && Options.MailSettings.MailSettings_Port <= 0)
                        {
                            throw new SendMailException("Port was 0 or less");
                        }

                        if (string.IsNullOrWhiteSpace(Options.MailSettings.MailSettings_SmtpServer))
                        {
                            throw new SendMailException("Smtp server was empty");
                        }

                        if (!Options.MailSettings.SimulateMails || !string.IsNullOrWhiteSpace(Options.MailSettings.MailAddress_Simulate))
                        {
                            if (!string.IsNullOrEmpty(Options.MailSettings.MailSettings_User))
                            {
                                if (string.IsNullOrEmpty(Options.MailSettings.MailSettings_Password))
                                {
                                    throw new SendMailException("Password was empty while user was provided");
                                }
                            }
                        }

                        if (Options.MailSettings.SimulateMails)
                        {
                            Logging?.LogInformation("!SENDING SIMULATED MAILS NOW!");
                        }
                        else
                        {
                            Logging?.LogInformation("!!!SENDING MAILS FOR REAL NOW!!!");
                        }
                        if (Options.MailSettings.SendEmergencyMails)
                        {
                            SendingMail = true;
                            countdownInfo.Emails_Sending = true;
                            countdownInfo.Triggered_Emails = true;
                            SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                            Logging?.LogInformation("Sending emergency mails...");
                            SendEmergencyMails(Options.MailSettings.SimulateMails);
                            UpdateTimestamp();
                            CheckThreadCancellation();
                            SendingMail = false;
                            countdownInfo.Emails_Sending = false;
                            SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                        }
                        if (Options.MailSettings.SendInfoMails)
                        {
                            SendingMail = true;
                            countdownInfo.Emails_Sending = true;
                            countdownInfo.Triggered_Emails = true;
                            SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                            Logging?.LogInformation("Sending information mails...");
                            SendInfoMails(Options.MailSettings.SimulateMails);
                            UpdateTimestamp();
                            CheckThreadCancellation();
                            SendingMail = false;
                            countdownInfo.Emails_Sending = false;
                            SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logging?.LogError("Error while trying to send email, will not try again, error message was: {message}", ex.Message);
                    }
                    finally
                    {
                        SendingMail = false;
                        countdownInfo.Emails_Sending = false;
                        SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                    }
                    // mark that we have sent the mails, or at least tried
                    MailSent = true;
                    countdownInfo.Emails_Sent = true;
                    SharedData.SetCountdownInfo(countdownInfo, c_pushInternalCountdownBySeconds, true);
                }
            }
        }

        private void SendInfoMails(bool simulate)
        {
            foreach (string configFile in Options.MailSettings.MailConfig_Info)
            {
                UpdateTimestamp();
                CheckThreadCancellation();

                SendMailOptions smo = new()
                {
                    Logging = Logging,
                    File = configFile,
                    From = Options.MailSettings.MailSettings_FromAddress,
                    SmtpPassword = Options.MailSettings.MailSettings_Password,
                    SmtpUser = Options.MailSettings.MailSettings_User,
                    SmtpPort = Options.MailSettings.MailSettings_Port,
                    SmtpServer = Options.MailSettings.MailSettings_SmtpServer,
                    UseSsl = Options.MailSettings.MailSettings_UseSsl,
                };

                MailSending.ReadAndSendMailConfig(smo, Options.MailSettings.MailAddress_Simulate, simulate);
            }
        }

        private void SendEmergencyMails(bool simulate)
        {
            foreach (string configFile in Options.MailSettings.MailConfig_Emergency)
            {
                UpdateTimestamp();
                CheckThreadCancellation();

                SendMailOptions smo = new()
                {
                    Logging = Logging,
                    File = configFile,
                    From = Options.MailSettings.MailSettings_FromAddress,
                    SmtpPassword = Options.MailSettings.MailSettings_Password,
                    SmtpUser = Options.MailSettings.MailSettings_User,
                    SmtpPort = Options.MailSettings.MailSettings_Port,
                    SmtpServer = Options.MailSettings.MailSettings_SmtpServer,
                    UseSsl = Options.MailSettings.MailSettings_UseSsl,
                };

                MailSending.ReadAndSendMailConfig(smo, Options.MailSettings.MailAddress_Simulate, simulate);
            }
        }

        #endregion

        #region Control methods

        public bool StopPollingThread()
        {
            using (Logging?.BeginScope("StopPollingThread"))
            {
                if (_pollingThread != null)
                {
                    bool blStopMonitoring = StopMonitoring(false);
                    if (_pollingThread.IsAlive)
                        _cancellationTokenSource?.Cancel();
                    _pollingThread.Join(_sleepTimespan * 2);

                    if (_pollingThread.IsAlive)
                        throw new WatcherPollerException("Countdown polling loop is still running after shutdown command, this was unexpected");

                    Logging?.LogDebug("Countdown polling loop ended gracefully");

                    SharedData.RemoveCountdownInfo();
                    _cancellationTokenSource?.Dispose();
                    _pollingThread = null;
                    _cancellationTokenSource = null;
                    UpdateTimestamp();

                    return blStopMonitoring;
                }
                else
                {
                    return StopMonitoring(false);
                }
            }
        }

        public bool StartPollingThread()
        {
            using (Logging?.BeginScope("StartPollingThread"))
            {
                if (_pollingThread == null)
                {
                    UpdateTimestamp();
                    PollerCount = 0;
                    _cancellationTokenSource = Generator.GetLinkedCancellationTokenSource();

                    bool blStartMonitoring = StartMonitoring(false);
                    if (blStartMonitoring)
                    {
                        _pollingThread = new Thread(new ThreadStart(CountdownPollerLoop));
                        _pollingThread.Start();
                        return true;
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot start countdown polling due to wrong poller mode");
                        return false;
                    }
                }
                else
                {
                    throw new FritzPollerException("Polling thread already started, please stop thread before starting anew");
                }
            }
        }

        public bool ArmPanicMode(bool whatIf)
        {
            using (Logging?.BeginScope("ArmPanicMode"))
            {
                if (Status == PollerStatus.Monitoring || Status == PollerStatus.Holdback)
                {
                    if (CanBeArmed)
                    {
                        if (!whatIf)
                        {
                            Logging?.LogInformation("Arming panic mode for countdown poller");
                            ResetFlags();
                            Status = PollerStatus.Armed;
                        }
                        else
                        {
                            Logging?.LogTrace("Only simulated arming panic mode for countdown poller");
                        }

                        return true;
                    }
                    else
                    {
                        Logging?.LogWarning("Unable to arm countdown poller");

                        if (PollerCount < (Options?.MinPollerCountToArm ?? 0))
                            Logging?.LogWarning("Cannot arm countdown poller because only {currentcount} of a minimum of {mincount} poller loops have been ran through", PollerCount, (Options?.MinPollerCountToArm ?? 0));
                        if (HasLoopPanic)
                            Logging?.LogWarning("Cannot arm countdown poller because of loop panic");
                        if (HasPanicked)
                            Logging?.LogWarning("Cannot arm countdown poller because there was a panic");

                        return false;
                    }
                }
                else
                {
                    Logging?.LogWarning("Cannot arm panic mode for countdown poller, must be in monitoring or holdback mode");

                    return false;
                }
            }
        }

        public bool DisarmPanicMode(bool whatIf)
        {
            using (Logging?.BeginScope("DisarmPanicMode"))
            {
                if (Status == PollerStatus.Armed || Status == PollerStatus.Holdback)
                {
                    if (!whatIf)
                    {
                        Logging?.LogInformation("Disarming panic mode for countdown poller");
                        ResetFlags();
                        Status = PollerStatus.Monitoring;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated disarming panic mode for countdown poller");
                    }

                    return true;
                }
                else
                {
                    Logging?.LogWarning("Cannot disarm panic mode for countdown poller, must be in armed or holdback");

                    return false;
                }
            }
        }

        public bool StartMonitoring(bool whatIf)
        {
            using (Logging?.BeginScope("StartMonitoring"))
            {
                if (Status == PollerStatus.Stopped)
                {
                    if (!whatIf)
                    {
                        Logging?.LogInformation("Start monitoring in countdown poller");
                        ResetFlags();
                        Status = PollerStatus.Monitoring;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated start monitoring for countdown poller");
                    }

                    return true;
                }
                else
                {
                    Logging?.LogWarning("Cannot start monitoring in countdown poller, must be in stopped mode");

                    return false;
                }
            }
        }

        public bool StopMonitoring(bool whatIf)
        {
            using (Logging?.BeginScope("StopMonitoring"))
            {
                if (Status != PollerStatus.Stopped)
                {
                    if (!whatIf)
                    {
                        Logging?.LogInformation("Stop monitoring in countdown poller");
                        ResetFlags();
                        Status = PollerStatus.Stopped;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated stop monitoring in countdown poller");
                    }

                    return true;
                }
                else
                {
                    Logging?.LogWarning("Cannot stop monitoring in countdown poller as monitoring didn't start");

                    return false;
                }
            }
        }

        public bool EnterHoldBackMode(bool whatIf)
        {
            using (Logging?.BeginScope("EnterHoldBackMode"))
            {
                if (Status == PollerStatus.Monitoring)
                {
                    if (!whatIf)
                    {
                        Logging?.LogDebug("Resetting any panics and poller counts before entering holdback mode for countdown poller");
                        ResetPanic();
                        Logging?.LogInformation("Entering holdback mode for countdown poller");
                        Status = PollerStatus.Holdback;
                    }
                    else
                    {
                        Logging?.LogTrace("Only simulated entering holdback mode for countdown poller");
                    }

                    return true;
                }
                else
                {
                    Logging?.LogWarning("Cannot enter holdback mode for countdown poller, must have started monitoring");

                    return false;
                }
            }
        }

        public bool ResetPanic()
        {
            using (Logging?.BeginScope("ResetPanic"))
            {
                Logging?.LogDebug("Resetting panics and poller count for countdown poller");

                bool blPanicReset = false;
                if (HasPanicked)
                {
                    blPanicReset = true;
                    HasPanicked = false;
                }
                if (HasLoopPanic)
                {
                    blPanicReset = true;
                    HasLoopPanic = false;
                }
                PollerCount = 0;
                return blPanicReset;
            }
        }

        #endregion
    }
}
