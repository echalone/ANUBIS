using ANUBISWatcher.Helpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Triggering.Entities
{
    public class ClewareTrigger : TriggerEntity
    {
        private readonly string _switchName;

        public override string Name { get { return _switchName; } }

        public ClewareTrigger(string switchName, bool oneTimeUseOnly, CancellationToken? cancellationToken)
            : base(oneTimeUseOnly, cancellationToken)
        {
            _switchName = switchName;
        }

        public override bool? Trigger(object? api, ILogger? logger)
        {
            using (logger?.BeginScope("ClewareTrigger.Trigger"))
            {
                try
                {
                    if (Triggerable(TriggeredType.ClewareUSBSwitch, logger))
                    {
                        logger?.LogInformation("Triggering cleware usb switch \"{name}\" in thread {threadId}...", _switchName, Environment.CurrentManagedThreadId);
                        CheckThreadCancellation();
                        if (api is ANUBISClewareAPI.ClewareAPI apiUsbSwitch)
                        {
                            var switchState = apiUsbSwitch.TurnUSBSwitchOffByName(_switchName, true);
                            CheckThreadCancellation();

                            if (switchState == ANUBISClewareAPI.USBSwitchState.Off)
                            {
                                AddToTriggerHistory(TriggerOutcome.Success);
                                logger?.LogInformation("Successfully turned off cleware usb switch \"{name}\"", _switchName);
                            }
                            else
                            {
                                AddToTriggerHistory(TriggerOutcome.Failed);
                                logger?.LogCritical("Could not turn off cleware usb switch \"{name}\"", _switchName);
                            }

                            return switchState == ANUBISClewareAPI.USBSwitchState.Off;
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
                    logger?.LogTrace("Recieved thread cancellation during cleware trigger");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    logger?.LogTrace("Recieved thread interruption during cleware trigger");
                    throw;
                }
                catch (Exception ex)
                {
                    AddToTriggerHistory(TriggerOutcome.Failed);
                    logger?.LogCritical(ex, "While trying to turn off usb switch \"{name}\": {message}", _switchName, ex.Message);
                    return false;
                }
            }
        }
    }
}
