using ANUBISWatcher.Helpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ANUBISWatcher.Triggering.Entities
{
    #region Option class

    public class OSShutdownTriggerOptions
    {
        /// <summary>
        /// What is the timeout for command calls in seconds?
        /// Default is 5 seconds.
        /// </summary>
        public ushort CommandTimeoutSeconds { get; init; }

        /// <summary>
        /// Should commands be resent on errors according to AutoRetry settings?
        /// Default is true.
        /// </summary>
        public bool AutoRetryOnErrors { get; init; }

        /// <summary>
        /// How often should commands be resent on errors if AutoRetryOnErrors is set to true?
        /// Default is 3.
        /// </summary>
        public byte AutoRetryCount { get; init; }

        /// <summary>
        /// How long (in milliseconds) should the api wait at least before resending a command after an error if AutoRetryOnErrors is set to true?
        /// Default is 3000 milliseconds
        /// </summary>
        public int AutoRetryMinWaitMilliseconds { get; init; }

        /// <summary>
        /// How long (in milliseconds) should the timespan be, starting from the AutoRetryMinWaitMilliseconds time after an error,
        /// in which a command could be resent? The actual waiting time before resending a command after an error will be randomly
        /// chosen between the AutoRetryMinWaitMilliseconds value and the AutoRetryMinWaitMilliseconds+AutoRetryWaitSpanMilliseconds value
        /// each time a command is resent.
        /// Default is 1500 milliseconds.
        /// </summary>
        public int AutoRetryWaitSpanMilliseconds { get; init; }

        /// <summary>
        /// Logging object to implement logging.
        /// If no logger is set then nothing will be logged.
        /// </summary>
        public ILogger? Logger { get; init; }

        public OSShutdownTriggerOptions()
        {
            CommandTimeoutSeconds = 5;
            AutoRetryOnErrors = true;
            AutoRetryCount = 3;
            AutoRetryMinWaitMilliseconds = 1500;
            AutoRetryWaitSpanMilliseconds = 3000;
            Logger = null;
        }
    }

    #endregion

    #region Shutdown Trigger IDs
    public class OSShutdownTriggerIDs
    {
        public const string SHUTDOWNID_LinuxPowerOff = "linuxpoweroff";
        public const string SHUTDOWNID_LinuxShutDown = "linuxshutdown";
        public const string SHUTDOWNID_LinuxReboot = "linuxreboot";
        public const string SHUTDOWNID_WindowsPowerOff = "windowspoweroff";
        public const string SHUTDOWNID_WindowsShutDown = "windowsshutdown";
        public const string SHUTDOWNID_WindowsReboot = "windowsreboot";
    }
    #endregion

    public class OSShutdownTrigger : TriggerEntity
    {
        #region Private fields

        private readonly string _osShutdownType;

        #endregion

        #region Properties

        public override string Name { get { return _osShutdownType; } }

        public OSShutdownTriggerOptions Options { get; init; }

        #endregion

        #region Constructors

        public OSShutdownTrigger(string osShutdownType, bool oneTimeUseOnly, CancellationToken? cancellationToken)
            : this(osShutdownType, oneTimeUseOnly, cancellationToken, new OSShutdownTriggerOptions())
        {

        }

        public OSShutdownTrigger(string osShutdownType, bool oneTimeUseOnly, CancellationToken? cancellationToken, OSShutdownTriggerOptions options)
            : base(oneTimeUseOnly, cancellationToken)
        {
            Options = options;
            _osShutdownType = osShutdownType;
        }

        #endregion

        #region Helper methods

        private int GetWaitingTimeBeforeRetry()
        {
            CheckThreadCancellation();

            return Random.Shared.Next(Options.AutoRetryMinWaitMilliseconds,
                                        Options.AutoRetryMinWaitMilliseconds + Options.AutoRetryWaitSpanMilliseconds);
        }

        /// <summary>
        /// Waits a random waiting time on errors.
        /// Returns true if it waited and command should be retried.
        /// Returns false if it didn't wait and command should be retried.
        /// </summary>
        /// <param name="retry">Retry count for current command</param>
        private bool WaitBeforeRetry(ref byte retry, ILogger? logging)
        {
            using (logging?.BeginScope("WaitBeforeRetry"))
            {
                CheckThreadCancellation();

                if (Options.AutoRetryOnErrors && retry < Options.AutoRetryCount)
                {
                    // waiting before retrying command
                    retry++;
                    int retryInMilliseconds = GetWaitingTimeBeforeRetry();
                    logging?.LogWarning("OSShutdown request failed or timed out, retriggering in {retryInMilliseconds} milliseconds (retry {currentRetry}/{maxRetries})", retryInMilliseconds, retry, Options.AutoRetryCount);

                    if (_cancellationToken.HasValue)
                    {
                        if (_cancellationToken.Value.WaitHandle.WaitOne(retryInMilliseconds))
                            logging?.LogTrace("OSShutdown request recieved thread cancellation request during command-retriggering sleep");
                    }
                    else
                        Thread.Sleep(retryInMilliseconds);

                    logging?.LogTrace("Done waiting before retriggering OS shutdown request");

                    CheckThreadCancellation();

                    return true;
                }
                else
                {
                    if (retry > 0)
                    {
                        logging?.LogWarning("OSShutdown trigger failed, all {maxRetries} retries exhausted", retry);
                    }
                    else
                    {
                        logging?.LogWarning($"OSShutdown trigger failed, no retrying configured");
                    }
                    return false;
                }
            }
        }

        private static void KillProcess(Process prc, ILogger? logger)
        {
            if (!(prc?.HasExited ?? true))
            {
                try
                {
                    prc.Kill(true);
                }
                catch (Exception ex1)
                {
                    logger?.LogError(ex1, "While trying to kill OS shutdown command process tree: {message}", ex1.Message);

                    try
                    {
                        prc.Kill();
                    }
                    catch (Exception ex2)
                    {
                        logger?.LogError(ex2, "While trying to kill OS shutdown command process: {message}", ex2.Message);
                    }
                }
            }
        }

        #endregion

        #region Trigger implementation

        public override bool? Trigger(object? api, ILogger? logger)
        {
            using (logger?.BeginScope("OSShutdownTrigger.Trigger"))
            {
                try
                {
                    if (Triggerable(TriggeredType.OSShutdown, logger))
                    {
                        logger?.LogInformation("Sending OS OS shutdown command \"{osShutdownType}\" in thread {threadId}...", _osShutdownType, Environment.CurrentManagedThreadId);
                        ProcessStartInfo psi;
                        Process? prc = null;
                        int[] allowedExitCodes = [0];
                        CheckThreadCancellation();
                        if (!string.IsNullOrWhiteSpace(_osShutdownType))
                        {
                            switch (_osShutdownType.ToLower())
                            {
                                case OSShutdownTriggerIDs.SHUTDOWNID_LinuxPowerOff:
                                    psi = new ProcessStartInfo("/usr/sbin/poweroff")
                                    {
                                        UseShellExecute = true,
                                        CreateNoWindow = true,
                                        WindowStyle = ProcessWindowStyle.Hidden
                                    };
                                    allowedExitCodes = [0];
                                    break;
                                case OSShutdownTriggerIDs.SHUTDOWNID_LinuxShutDown:
                                    psi = new ProcessStartInfo("shutdown")
                                    {
                                        Arguments = "-h now",
                                        UseShellExecute = true,
                                        CreateNoWindow = true,
                                        WindowStyle = ProcessWindowStyle.Hidden
                                    };
                                    allowedExitCodes = [0];
                                    break;
                                case OSShutdownTriggerIDs.SHUTDOWNID_LinuxReboot:
                                    psi = new ProcessStartInfo("reboot")
                                    {
                                        UseShellExecute = true,
                                        CreateNoWindow = true,
                                        WindowStyle = ProcessWindowStyle.Hidden
                                    };
                                    allowedExitCodes = [0];
                                    break;
                                case OSShutdownTriggerIDs.SHUTDOWNID_WindowsPowerOff:
                                case OSShutdownTriggerIDs.SHUTDOWNID_WindowsShutDown:
                                    psi = new ProcessStartInfo("shutdown")
                                    {
                                        Arguments = "/s /f /t 0",
                                        UseShellExecute = true,
                                        CreateNoWindow = true,
                                        WindowStyle = ProcessWindowStyle.Hidden
                                    };
                                    allowedExitCodes = [0];
                                    break;
                                case OSShutdownTriggerIDs.SHUTDOWNID_WindowsReboot:
                                    psi = new ProcessStartInfo("shutdown")
                                    {
                                        Arguments = "/r /f /t 0",
                                        UseShellExecute = true,
                                        CreateNoWindow = true,
                                        WindowStyle = ProcessWindowStyle.Hidden
                                    };
                                    allowedExitCodes = [0];
                                    break;
                                default:
                                    throw new TriggerException($"Unknown OS shutdown type \"{_osShutdownType}\", allowed values are: LinuxPoweroff, LinuxShutdown, LinuxReboot, WindowsPoweroff, WindowsShutdown, WindowsReboot");
                            }

                            Exception? exCaught = null;
                            byte retry = 0;
                            int exitCode = 0;
                            bool hasExited = false;
                            bool isLegalExitCode = false;

                            do
                            {
                                exCaught = null;

                                try
                                {
                                    CheckThreadCancellation();
                                    prc = Process.Start(psi);
                                    CheckThreadCancellation();

                                    if (prc != null)
                                    {
                                        hasExited = true;
                                        exitCode = -1;
                                        isLegalExitCode = false;
                                        CheckThreadCancellation();
                                        if (_cancellationToken.HasValue)
                                        {
                                            try
                                            {
                                                Task tsk = prc.WaitForExitAsync();
                                                CheckThreadCancellation();
                                                if (tsk != null)
                                                {
                                                    hasExited = tsk.Wait(Options.CommandTimeoutSeconds * 1000, _cancellationToken.Value) && prc.HasExited;
                                                    CheckThreadCancellation();
                                                }
                                                else
                                                {
                                                    if (Options.CommandTimeoutSeconds > 0)
                                                    {
                                                        DateTime dtKillAfter = DateTime.UtcNow.AddSeconds(Options.CommandTimeoutSeconds);
                                                        while (!prc.HasExited)
                                                        {
                                                            CheckThreadCancellation();
                                                            if (DateTime.UtcNow > dtKillAfter)
                                                                break;
                                                            Thread.Sleep(10);
                                                        }
                                                        hasExited = prc.HasExited;
                                                    }
                                                    else
                                                    {
                                                        while (!prc.HasExited)
                                                            CheckThreadCancellation();
                                                    }
                                                    CheckThreadCancellation();
                                                }
                                            }
                                            catch (OperationCanceledException)
                                            {
                                                hasExited = false;
                                                logger?.LogTrace("OSShutdown trigger was canceled during command call");
                                                if (!prc.HasExited)
                                                    KillProcess(prc, logger);
                                                throw;
                                            }
                                            catch (ThreadInterruptedException)
                                            {
                                                hasExited = false;
                                                logger?.LogTrace("OSShutdown trigger was interrupted during command call");
                                                if (!prc.HasExited)
                                                    KillProcess(prc, logger);
                                                throw;
                                            }
                                        }
                                        else
                                        {
                                            if (Options.CommandTimeoutSeconds > 0)
                                            {
                                                hasExited = prc.WaitForExit(Options.CommandTimeoutSeconds * 1000) && prc.HasExited;
                                            }
                                            else
                                            {
                                                prc.WaitForExit();
                                            }
                                        }

                                        if (hasExited)
                                        {
                                            exitCode = prc.ExitCode;
                                            isLegalExitCode = allowedExitCodes.Any(itm => itm == exitCode);

                                            if (!isLegalExitCode)
                                            {
                                                logger?.LogWarning("OS shutdown command exited with illegal exit code {exitCode}", exitCode);
                                            }
                                        }
                                        else
                                        {
                                            logger?.LogWarning("OS shutdown command did not exit in time");
                                        }
                                    }
                                    else
                                    {
                                        throw new TriggerException($"Could not create process for OS shutdown type {_osShutdownType}");
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    logger?.LogTrace("OSShutdown thread was canceled");
                                    throw;
                                }
                                catch (ThreadInterruptedException)
                                {
                                    logger?.LogTrace("OSShutdown thread was interrupted");
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    exCaught = ex;
                                    logger?.LogWarning(ex, "Error \"{errortype}\" during OS shutdown trigger, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                                }
                            }
                            while ((exCaught != null || !hasExited || !isLegalExitCode) && WaitBeforeRetry(ref retry, logger));
                            // retry if exception was caught or if response was empty

                            CheckThreadCancellation();

                            if (exCaught != null)
                            {
                                logger?.LogCritical(exCaught, "Failed to send OS shutdown command: {message}", exCaught.Message);
                                throw exCaught;
                            }

                            if (hasExited)
                            {
                                if (isLegalExitCode)
                                {
                                    AddToTriggerHistory(TriggerOutcome.Success);
                                    logger?.LogInformation("Successfully sent OS shutdown command \"{osShutdownType}\"", _osShutdownType);
                                    return true;
                                }
                                else
                                {
                                    AddToTriggerHistory(TriggerOutcome.Failed);
                                    logger?.LogCritical("Sent OS shutdown command \"{osShutdownType}\" but command exited with illegal exit code {exitCode}", _osShutdownType, exitCode);
                                    return false;
                                }
                            }
                            else
                            {
                                AddToTriggerHistory(TriggerOutcome.Failed);
                                logger?.LogCritical("OS shutdown command \"{osShutdownType}\" timed out", _osShutdownType);
                                return false;
                            }
                        }
                        else
                        {
                            AddToTriggerHistory(TriggerOutcome.Failed);
                            throw new TriggerException($"OS shutdown type was empty");
                        }
                    }
                    else
                    {
                        AddToTriggerHistory(TriggerOutcome.Skipped);
                        return null;
                    }
                }
                catch (OperationCanceledException)
                {
                    logger?.LogTrace("Recieved thread cancellation during OS shutdown trigger");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    logger?.LogTrace("Recieved thread interruption during OS shutdown trigger");
                    throw;
                }
                catch (Exception ex)
                {
                    AddToTriggerHistory(TriggerOutcome.Failed);
                    logger?.LogCritical(ex, "While trying to send OS shutdown command \"{osShutdownType}\": {message}", _osShutdownType, ex.Message);
                    return false;
                }
            }
        }

        #endregion
    }
}
