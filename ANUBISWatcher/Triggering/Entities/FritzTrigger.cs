using ANUBISWatcher.Helpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Triggering.Entities
{
    public class FritzTrigger : TriggerEntity
    {
        private readonly string _switchName;

        public override string Name { get { return _switchName; } }

        public FritzTrigger(string switchName, bool oneTimeUseOnly, CancellationToken? cancellationToken)
            : base(oneTimeUseOnly, cancellationToken)
        {
            _switchName = switchName;
        }

        public override bool? Trigger(object? api, ILogger? logger)
        {
            using (logger?.BeginScope("FritzTrigger.Trigger"))
            {
                try
                {
                    if (Triggerable(TriggeredType.FritzSwitch, logger))
                    {
                        logger?.LogInformation("Triggering fritz switch \"{name}\" in thread {threadId}...", _switchName, Environment.CurrentManagedThreadId);
                        CheckThreadCancellation();
                        if (api is ANUBISFritzAPI.FritzAPI apiFritz)
                        {
                            var switchState = apiFritz.TurnSwitchOffByName(_switchName, true);
                            CheckThreadCancellation();

                            if (switchState == ANUBISFritzAPI.SwitchState.Off)
                            {
                                AddToTriggerHistory(TriggerOutcome.Success);
                                logger?.LogInformation("Successfully turned off fritz switch \"{name}\"", _switchName);
                            }
                            else
                            {
                                AddToTriggerHistory(TriggerOutcome.Failed);
                                logger?.LogCritical("Could not turn off fritz switch \"{name}\"", _switchName);
                            }

                            return switchState == ANUBISFritzAPI.SwitchState.Off;
                        }
                        else
                        {
                            throw new TriggerException($"Api was null or of wrong type");
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
                    logger?.LogTrace("Recieved thread cancellation during fritz trigger");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    logger?.LogTrace("Recieved thread interruption during fritz trigger");
                    throw;
                }
                catch (Exception ex)
                {
                    AddToTriggerHistory(TriggerOutcome.Failed);
                    logger?.LogCritical(ex, "While trying to turn off fritz switch \"{name}\": {message}", _switchName, ex.Message);
                    return false;
                }
            }
        }
    }
}
