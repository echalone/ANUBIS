using ANUBISWatcher.Shared;
using ANUBISWatcher.Triggering.Entities;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Triggering
{
    public class NextStartInfo
    {
        public TimeSpan? MaxWaitingTime { get; set; }
        public CancellationToken? WaitingToken { get; set; }
        public CancellationTokenSource? CancelTokenForNextStart { get; set; }
    }

    public class TriggerController
    {
        #region constants

        private const int c_maxOverallWaitingTimeInMinutes = 5;

        #endregion

        #region Static fields

        private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(3);

        private static object lock_StartInfo = new();
        private static DateTime? _maxNextStartUtc = null;
        private static CancellationToken? _nextCancellationToken = null;

        #endregion

        #region Static properties

        #endregion

        #region Object properties

        private TriggerConfiguration TriggerConfiguration { get; init; }
        private ILogger? Logging { get; init; }
        private NextStartInfo? StartInfo { get; set; }

        public bool IsPanic { get; private init; }

        #endregion

        #region Constructor

        public TriggerController(TriggerConfiguration triggerConfiguration, bool isPanic, ILogger? logger)
        {
            TriggerConfiguration = triggerConfiguration;
            Logging = logger;
            IsPanic = isPanic;
        }

        #endregion

        #region Object methods

        /// <summary>
        /// Call if the previous trigger thread didn't end in time for some reason, so we throw a panic just for that
        /// not yet implemented
        /// </summary>
        private void ThrowPreviousTriggerPanic()
        {
            using (Logging?.BeginScope($"ThrowPreviousTriggerPanic(TID:{Environment.CurrentManagedThreadId})"))
            {
                Logging?.LogWarning("There was an error waiting for the previous trigger thread, it might have got stuck, will try triggering system shutdown accordingly");
                Generator.TriggerShutDown(UniversalPanicType.General, ConstantTriggerIDs.ID_PreviousTriggerPanic, UniversalPanicReason.Unresponsive);
            }
        }

        private NextStartInfo GetNextStartInfo(uint maxDurationInSeconds)
        {
            using (Logging?.BeginScope($"GetNextStartInfo(TID:{Environment.CurrentManagedThreadId})"))
            {
                bool lockTaken = false;
                try
                {
                    Generator.CheckThreadCancellation();
                    lockTaken = Monitor.TryEnter(lock_StartInfo, _lockTimeout);

                    // Ensure that the lock didn't run into a timeout and if so create a new lock object
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_StartInfo = newLock;
                        lockTaken = true;
                        Logging?.LogWarning("Couldn't acquire trigger lock in time, generated new lock");
                        ThrowPreviousTriggerPanic();
                    }

                    Generator.CheckThreadCancellation();
                    NextStartInfo nsi = new()
                    {
                        WaitingToken = _nextCancellationToken,
                        CancelTokenForNextStart = Generator.GetLinkedCancellationTokenSource(),
                    };

                    if (nsi.WaitingToken != null)
                        Logging?.LogTrace("Trigger thread found waiting token of previous trigger thread, will use that");
                    else
                        Logging?.LogTrace("Trigger thread found no waiting token of a previous trigger thread");

                    _nextCancellationToken = nsi.CancelTokenForNextStart?.Token;
                    // add our maximum duration to the waiting time and return the current waiting timestamp
                    if (_maxNextStartUtc != null && _maxNextStartUtc <= DateTime.UtcNow.AddMinutes(c_maxOverallWaitingTimeInMinutes))
                    {
                        nsi.MaxWaitingTime = _maxNextStartUtc.Value - DateTime.UtcNow;
                        if (nsi.MaxWaitingTime.Value.TotalMilliseconds <= 0)
                        {
                            Logging?.LogTrace("Max next trigger thread start time of {maxnextstartutc} utc has been surpassed, will reset the max next start time to now", _maxNextStartUtc);
                            nsi.MaxWaitingTime = null;
                            _maxNextStartUtc = DateTime.UtcNow;
                        }
                        Logging?.LogTrace("Calculated a max waiting time of {waitingtime} for this trigger thread", nsi.MaxWaitingTime);
                        Logging?.LogTrace("Current next max start UTC is {maxnextstartutc}, will add to that our {maxduration} seconds of maximum duration to calculate next max start utc", _maxNextStartUtc, maxDurationInSeconds);
                        _maxNextStartUtc = _maxNextStartUtc.Value.AddSeconds(maxDurationInSeconds);
                        Logging?.LogTrace("Calculated a new max waiting timestamp of {waitingtimestamp} for next trigger thread", _maxNextStartUtc);
                    }
                    else
                    {
                        if (_maxNextStartUtc != null)
                        {
                            Logging?.LogWarning("Previous trigger threads are already waiting too long, will start this trigger thread immediately even though it may now run in parallel with other trigger threads, will reset the max next start time to now as well.");
                            ThrowPreviousTriggerPanic();
                        }
                        else
                        {
                            Logging?.LogTrace("It seems we are the first trigger thread so far, will start this trigger thread immediately");
                        }

                        nsi.WaitingToken = null;
                        _maxNextStartUtc = DateTime.UtcNow.AddSeconds(maxDurationInSeconds);
                        Logging?.LogTrace("Calculated a new max waiting timestamp of {waitingtimestamp} for next trigger thread", _maxNextStartUtc);
                    }

                    return nsi;
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Thread canceled during retrieval of next start info");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Thread interrupted during retrieval of next start info");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to retrieve next start info: {message}", ex.Message);
                    ThrowCurrentTriggerPanic(Logging);

                    return new NextStartInfo()
                    {
                        CancelTokenForNextStart = null,
                        MaxWaitingTime = null,
                        WaitingToken = null,
                    };
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                        Monitor.Exit(lock_StartInfo);
                }
            }
        }

        private bool WaitForStart()
        {
            using (Logging?.BeginScope($"WaitForStart(TID:{Environment.CurrentManagedThreadId})"))
            {
                bool blPreviousStuck = false;
                if (StartInfo?.MaxWaitingTime != null)
                {
                    try
                    {
                        Logging?.LogInformation("Trigger thread {triggerId} for configuration {triggerconfiguration} is waiting for start...", Environment.CurrentManagedThreadId, TriggerConfiguration.Name);
                        if (Helpers.CancellationUtils.Wait(StartInfo.WaitingToken, StartInfo.MaxWaitingTime))
                        {
                            Logging?.LogTrace("No previous trigger thread recorded, ready to start this trigger thread");
                        }
                        else
                        {
                            Logging?.LogWarning("Previous trigger threads did not finish in their maximum waiting time of {maxwaittime}, starting next trigger thread anyways", StartInfo.MaxWaitingTime);
                            blPreviousStuck = true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (Generator.IsThreadCancelled())
                        {
                            Logging?.LogTrace("General thread cancellation during wait for trigger start in thread {triggerId} for configuration {triggerconfiguration}", Environment.CurrentManagedThreadId, TriggerConfiguration.Name);
                            throw;
                        }
                        else
                        {
                            Logging?.LogTrace("Previous trigger thread has finished, ready to start this trigger thread");
                        }
                    }
                }
                else
                {
                    Logging?.LogTrace("Calculated no waiting time for trigger thread, ready to start it immediately");
                }
                Logging?.LogInformation("Trigger thread {triggerId} for configuration {triggerconfiguration} has finished waiting, starts triggering now...", Environment.CurrentManagedThreadId, TriggerConfiguration.Name);

                return !blPreviousStuck;
            }
        }

        private void ReleaseCancelTokenForNextStart()
        {
            StartInfo?.CancelTokenForNextStart?.Cancel();
        }

        private void TriggerThreadMain()
        {
            using (Logging?.BeginScope($"TriggerThreadMain(TID:{Environment.CurrentManagedThreadId})"))
            {
                using (Logging?.BeginScope($"TriggerConfig:{TriggerConfiguration.Name}"))
                {
                    try
                    {
                        Logging?.LogInformation("New trigger thread with id {threadId} for configuration \"{name}\" started up", Environment.CurrentManagedThreadId, TriggerConfiguration.Name);
                        Generator.CheckThreadCancellation();

                        StartInfo = GetNextStartInfo(TriggerConfiguration.MaxTimeInSeconds);

                        Generator.CheckThreadCancellation();

                        if (StartInfo.WaitingToken != null || StartInfo.MaxWaitingTime != null)
                            Logging?.LogDebug("Registering trigger configuration {name} to start after previous thread", TriggerConfiguration.Name);
                        else
                            Logging?.LogDebug("Registering trigger configuration {name} to start immediately", TriggerConfiguration.Name);

                        if (!WaitForStart())
                        {
                            Logging?.LogWarning("It seems the previous trigger threads did not finish in time");
                            ThrowPreviousTriggerPanic();
                        }

                        Generator.CheckThreadCancellation();

                        bool blFritzApiInitialized = false;
                        bool blSwitchBotApiInitialized = false;
                        bool blClewareApiInitialized = false;
                        ANUBISFritzAPI.FritzAPI? fritzAPI = null;
                        ANUBISSwitchBotAPI.SwitchBotAPI? switchBotAPI = null;
                        ANUBISClewareAPI.ClewareAPI? clewareAPI = null;

                        foreach (var trigger in TriggerConfiguration.Triggers)
                        {
                            try
                            {
                                Logging?.LogInformation("Next up for trigger configuration \"{configname}\" in thread {threadId}: switch/command \"{switchname}\" of type {triggerType}", TriggerConfiguration.Name, Environment.CurrentManagedThreadId, trigger?.Name ?? "<trigger is null>", trigger?.GetType()?.Name ?? "<trigger is null>");
                                if (trigger != null)
                                {
                                    if (trigger is FritzTrigger)
                                    {
                                        if (!blFritzApiInitialized)
                                        {
                                            // initialize fritz api if a fritz trigger is present and api wasn't yet initialized, only try once per trigger
                                            Logging?.LogTrace("Initializing Fritz API for Trigger configuration \"{name}\" in thread {threadId}", TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                            fritzAPI = Generator.GetFritzAPI();
                                            blFritzApiInitialized = true;
                                        }
                                        if (fritzAPI != null)
                                        {
                                            bool? result = trigger.Trigger(fritzAPI, Logging);
                                            if (result.HasValue)
                                            {
                                                if (result.Value)
                                                {
                                                    Logging?.LogInformation("Successfully turned off fritz switch \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId}", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                                }
                                                else
                                                {
                                                    Logging?.LogCritical("Failed to turn off fritz switch \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId}", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                                }
                                            }
                                            else
                                            {
                                                Logging?.LogDebug("Skipped turning off fritz switch \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId} because switch is marked for one-time-use and has already been triggered", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                            }
                                        }
                                        else
                                        {
                                            trigger.AddToTriggerHistory(TriggerOutcome.Failed);
                                            Logging?.LogCritical("Fritz API wasn't generated for trigger thread of configuration {name} even though a fritz trigger was present, skipping fritz trigger", TriggerConfiguration.Name);
                                        }
                                    }
                                    else if (trigger is SwitchBotTrigger)
                                    {
                                        if (!blSwitchBotApiInitialized)
                                        {
                                            // initialize switch bot api if a switch bot trigger is present and api wasn't yet initialized, only try once per trigger
                                            Logging?.LogTrace("Initializing SwitchBot API for Trigger configuration \"{name}\" in thread {threadId}", TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                            switchBotAPI = Generator.GetSwitchBotAPI();
                                            blSwitchBotApiInitialized = true;
                                        }
                                        if (switchBotAPI != null)
                                        {
                                            bool? result = trigger.Trigger(switchBotAPI, Logging);
                                            if (result.HasValue)
                                            {
                                                if (result.Value)
                                                {
                                                    Logging?.LogInformation("Successfully turned off switch bot \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId}", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                                }
                                                else
                                                {
                                                    Logging?.LogCritical("Failed to turn off switch bot \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId}", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                                }
                                            }
                                            else
                                            {
                                                Logging?.LogDebug("Skipped turning off switch bot \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId} because switch is marked for one-time-use and has already been triggered", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                            }
                                        }
                                        else
                                        {
                                            trigger.AddToTriggerHistory(TriggerOutcome.Failed);
                                            Logging?.LogCritical("SwitchBot API wasn't generated for trigger thread of configuration {name} even though a switch bot trigger was present, skipping switch bot trigger", TriggerConfiguration.Name);
                                        }
                                    }
                                    else if (trigger is ClewareTrigger)
                                    {
                                        if (!blClewareApiInitialized)
                                        {
                                            // initialize cleware api if a cleware trigger is present and api wasn't yet initialized, only try once per trigger
                                            Logging?.LogTrace("Initializing Cleware USB API for Trigger configuration \"{name}\" in thread {threadId}", TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                            clewareAPI = Generator.GetClewareAPI();
                                            blClewareApiInitialized = true;
                                        }
                                        if (clewareAPI != null)
                                        {
                                            bool? result = trigger.Trigger(clewareAPI, Logging);
                                            if (result.HasValue)
                                            {
                                                if (result.Value)
                                                {
                                                    Logging?.LogInformation("Successfully turned off cleare usb switch \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId}", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                                }
                                                else
                                                {
                                                    Logging?.LogCritical("Failed to turn off cleware usb switch \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId}", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                                }
                                            }
                                            else
                                            {
                                                Logging?.LogDebug("Skipped turning off cleware usb switch \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId} because switch is marked for one-time-use and has already been triggered", trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                            }
                                        }
                                        else
                                        {
                                            trigger.AddToTriggerHistory(TriggerOutcome.Failed);
                                            Logging?.LogCritical("Cleware API wasn't generated for trigger thread of configuration {name} even though a usb cleware trigger was present, skipping usb cleware trigger", TriggerConfiguration.Name);
                                        }
                                    }
                                    else
                                    {
                                        bool? result = trigger.Trigger(null, Logging);
                                        if (result.HasValue)
                                        {
                                            if (result.Value)
                                            {
                                                Logging?.LogInformation("Successfully sent generic {triggertype} trigger \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId}", trigger.GetType(), trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                            }
                                            else
                                            {
                                                Logging?.LogCritical("Failed to send generic {triggertype} trigger \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId}", trigger.GetType(), trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                            }
                                        }
                                        else
                                        {
                                            Logging?.LogDebug("Skipped turning off generic {triggertype} trigger \"{switchname}\" for trigger configuration \"{configname}\" in thread {threadId} because switch is marked for one-time-use and has already been triggered", trigger.GetType(), trigger.Name, TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                        }
                                    }
                                }
                                else
                                {
                                    Logging?.LogCritical("Trigger in trigger list was null for trigger configuration \"{name}\" in thread {threadId}", TriggerConfiguration.Name, Environment.CurrentManagedThreadId);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                Logging?.LogTrace("Thread was canceled during trigger");
                                throw;
                            }
                            catch (ThreadInterruptedException)
                            {
                                Logging?.LogTrace("Thread was interrupted during trigger");
                                throw;
                            }
                            catch (Exception ex)
                            {
                                Logging?.LogCritical(ex, "While trying to trigger for configuration  \"{name}\" in thread {threadId}: {message}", TriggerConfiguration.Name, Environment.CurrentManagedThreadId, ex.Message);
                                ThrowCurrentTriggerPanic(Logging);
                                trigger?.AddToTriggerHistory(TriggerOutcome.Failed);
                            }
                        }

                        Generator.CheckThreadCancellation();

                        ReleaseCancelTokenForNextStart();

                        Generator.CheckThreadCancellation();
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Trigger thread {threadId} for configuration \"{name}\" was canceled, ending now", Environment.CurrentManagedThreadId, TriggerConfiguration.Name);
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Trigger thread {threadId} for configuration \"{name}\" was interrupted, ending now", Environment.CurrentManagedThreadId, TriggerConfiguration.Name);
                    }
                    catch (Exception ex)
                    {
                        Logging?.LogCritical(ex, "General error during trigger thread: {message}", ex.Message);
                        ThrowCurrentTriggerPanic(Logging);
                    }
                    finally
                    {
                        Logging?.LogInformation("Trigger thread with id {threadId} for configuration \"{name}\" stopped", Environment.CurrentManagedThreadId, TriggerConfiguration.Name);
                        SharedData.DecreaseTriggerCount();
                    }
                }
            }
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Call if the current trigger thread has some problem, so we throw a panic just for that
        /// not yet implemented
        /// </summary>
        private static void ThrowCurrentTriggerPanic(ILogger? logger)
        {
            using (logger?.BeginScope($"ThrowCurrentTriggerPanic(TID:{Environment.CurrentManagedThreadId})"))
            {
                logger?.LogWarning("There was an error with the current trigger thread, it might have got stuck, will try triggering system shutdown accordingly");
                Generator.TriggerShutDown(UniversalPanicType.General, ConstantTriggerIDs.ID_CurrentTriggerPanic, UniversalPanicReason.Unresponsive);
            }
        }

        public static bool StartTriggerThread(TriggerConfiguration triggerConfiguration, bool isPanic)
        {
            ILogger? loggerStart = Generator.GetLogger("TriggerThreadStart");
            ILogger? loggerMain = Generator.GetLogger("TriggerThread");

            using (loggerStart?.BeginScope("StartTriggerThread"))
            {
                try
                {
                    if (triggerConfiguration.Triggers.Count > 0)
                    {
                        TriggerController tc = new(triggerConfiguration, isPanic, loggerMain);

                        if (SharedData.IncreaseTriggerCount())
                        {
                            bool canEnter = true;
                            if (!triggerConfiguration.Repeatable)
                            {
                                canEnter = SharedData.RequestTriggerEntry(TriggeredType.Config, triggerConfiguration.Name);
                                if (canEnter)
                                {
                                    loggerStart?.LogDebug("This is the first time configuration \"{name}\" is triggered, therefore executing this configuration even though it's marked as not repeatable", triggerConfiguration.Name);
                                }
                                else
                                {
                                    loggerStart?.LogDebug("This would NOT be the first time configuration \"{name}\" is triggered, therefore skipping renewed execution of this configuration because it is marked as not repeatable", triggerConfiguration.Name);
                                }
                            }
                            else
                            {
                                loggerStart?.LogDebug("The configuration \"{name}\" is marked as repeatable, therefore not checking if this configuratin has already been executed", triggerConfiguration.Name);
                            }

                            if (canEnter)
                            {
                                var triggerThread = new Thread(new ThreadStart(tc.TriggerThreadMain));
                                triggerThread.Start();
                                loggerStart?.LogTrace("Thread for trigger configuration {name} has been started", triggerConfiguration.Name);

                                return true;
                            }
                            else
                            {
                                SharedData.DecreaseTriggerCount();
                                return false;
                            }
                        }
                        else
                        {
                            loggerStart?.LogCritical("Skipping start of thread for trigger configuration {name} as too many trigger threads are already running. We are throwing away this trigger now.", triggerConfiguration.Name);

                            return false;
                        }
                    }
                    else
                    {
                        loggerStart?.LogDebug("No triggers defined for configuration {name}, not starting trigger thread", triggerConfiguration.Name);

                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    loggerStart?.LogTrace("Start trigger thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    loggerStart?.LogTrace("Start trigger thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    loggerStart?.LogCritical(ex, "While trying to start trigger thread for configuration \"{name}\": {message}", triggerConfiguration?.Name ?? "<null>", ex.Message);
                    ThrowCurrentTriggerPanic(loggerStart);
                    return false;
                }
            }
        }

        #endregion
    }
}
