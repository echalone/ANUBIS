using ANUBISWatcher.Helpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Triggering.Entities
{
    public abstract class TriggerEntity
    {
        protected CancellationToken? _cancellationToken = null;

        public abstract string Name { get; }

        public bool OneTimeUseOnly { get; private init; }

        public TriggerEntity(bool oneTimeUseOnly, CancellationToken? cancellationToken)
        {
            _cancellationToken = cancellationToken;
            OneTimeUseOnly = oneTimeUseOnly;
        }

        public abstract bool? Trigger(object? api, ILogger? logger);

        protected void CheckThreadCancellation()
        {
            _cancellationToken?.ThrowIfCancellationRequested();
        }

        protected bool Triggerable(TriggeredType type, ILogger? logger)
        {
            bool canEnter;
            if (OneTimeUseOnly)
            {
                canEnter = SharedData.RequestTriggerEntry(type, Name);
                if (canEnter)
                {
                    logger?.LogDebug("This is the first time {switchtype} switch/command \"{switchname}\" is triggered, therefore triggering this switch/command even though it's marked for one-time-use only", type, Name);
                    return true;
                }
                else
                {
                    logger?.LogDebug("This would NOT be the first time {switchtype} switch/command \"{name}\" is triggered, therefore skipping renewed triggering of this switch because it is marked for one-time-use only", type, Name);
                    return false;
                }
            }
            else
            {
                logger?.LogDebug("The {switchtype} switch/command \"{name}\" is not marked for one-time-use, therefore not checking if this switch/command has already been triggered", type, Name);
                return true;
            }
        }

        public void AddToTriggerHistory(TriggerOutcome outcome)
        {
            TriggeredType type;
            if (GetType() == typeof(FritzTrigger))
            {
                type = TriggeredType.FritzSwitch;
            }
            else if (GetType() == typeof(ClewareTrigger))
            {
                type = TriggeredType.ClewareUSBSwitch;
            }
            else if (GetType() == typeof(SwitchBotTrigger))
            {
                type = TriggeredType.SwitchBot;
            }
            else if (GetType() == typeof(OSShutdownTrigger))
            {
                type = TriggeredType.OSShutdown;
            }
            else
            {
                throw new TriggerException("Unknown trigger type to add to history: " + GetType().Name);
            }

            TriggerHistoryEntry the = new(type, Name);
            the.AddToHistory(outcome);
        }
    }
}
