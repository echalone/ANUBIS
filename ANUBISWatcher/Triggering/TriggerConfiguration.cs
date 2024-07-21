using ANUBISWatcher.Triggering.Entities;

namespace ANUBISWatcher.Triggering
{
    public class TriggerConfiguration
    {
        public string Name { get; init; }
        public List<TriggerEntity> Triggers { get; init; }
        public uint MaxTimeInSeconds { get; init; }
        public bool Repeatable { get; private init; }

        public TriggerConfiguration(string name, bool repeatable, List<TriggerEntity> triggers, uint maxTimeInSeconds)
        {
            Name = name;
            Repeatable = repeatable;
            Triggers = triggers;
            MaxTimeInSeconds = maxTimeInSeconds;
        }
    }
}
