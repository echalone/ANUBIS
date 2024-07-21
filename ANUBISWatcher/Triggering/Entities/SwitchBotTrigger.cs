using ANUBISWatcher.Helpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Triggering.Entities
{
    public class SwitchBotTrigger : TriggerEntity
    {
        private readonly string _switchName;

        public override string Name { get { return _switchName; } }

        public SwitchBotTrigger(string switchName, bool oneTimeUseOnly, CancellationToken? cancellationToken)
            : base(oneTimeUseOnly, cancellationToken)
        {
            _switchName = switchName;
        }

        public override bool? Trigger(object? api, ILogger? logger)
        {
            using (logger?.BeginScope("SwitchBotTrigger.Trigger"))
            {
                try
                {
                    if (Triggerable(TriggeredType.SwitchBot, logger))
                    {
                        logger?.LogInformation("Triggering switch bot \"{name}\" in thread {threadId}...", _switchName, Environment.CurrentManagedThreadId);
                        CheckThreadCancellation();
                        if (api is ANUBISSwitchBotAPI.SwitchBotAPI apiSwitchBot)
                        {
                            var switchState = apiSwitchBot.TurnSwitchBotOffByName(_switchName, true, true);
                            CheckThreadCancellation();

                            if (switchState.Power == ANUBISSwitchBotAPI.SwitchBotPowerState.Off)
                            {
                                AddToTriggerHistory(TriggerOutcome.Success);
                                logger?.LogInformation("Successfully turned off switch bot \"{name}\"", _switchName);
                            }
                            else
                            {
                                AddToTriggerHistory(TriggerOutcome.Failed);
                                logger?.LogCritical("Could not turn off switch bot \"{name}\"", _switchName);
                            }

                            return switchState.Power == ANUBISSwitchBotAPI.SwitchBotPowerState.Off;
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
                    logger?.LogTrace("Recieved thread cancellation during switch bot trigger");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    logger?.LogTrace("Recieved thread interruption during switch bot trigger");
                    throw;
                }
                catch (Exception ex)
                {
                    AddToTriggerHistory(TriggerOutcome.Failed);
                    logger?.LogCritical(ex, "While trying to turn off switch bot \"{name}\": {message}", _switchName, ex.Message);
                    return false;
                }
            }
        }
    }
}
