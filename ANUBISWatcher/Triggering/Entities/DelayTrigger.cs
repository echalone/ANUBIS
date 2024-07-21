using ANUBISWatcher.Helpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Triggering.Entities
{
    public class DelayTrigger : TriggerEntity
    {
        #region Private fields

        private readonly ulong _delayInMilliseconds;

        #endregion

        #region Properties

        public override string Name { get { return $"delay {_delayInMilliseconds}ms"; } }

        public ulong DelayInMilliseconds { get { return _delayInMilliseconds; } }

        public long DelayInSeconds { get { return (long)Math.Ceiling(_delayInMilliseconds / 1000.0); } }

        #endregion

        #region Constructors

        public DelayTrigger(ulong delayInMilliseconds, CancellationToken? cancellationToken)
            : base(false, cancellationToken)
        {
            _delayInMilliseconds = delayInMilliseconds;
        }

        #endregion

        #region Helper methods

        #endregion

        #region Trigger implementation

        public override bool? Trigger(object? api, ILogger? logger)
        {
            using (logger?.BeginScope("DelayTrigger.Trigger"))
            {
                try
                {
                    logger?.LogInformation("Delaying next trigger by {shutdownType}ms in thread {threadId}...", _delayInMilliseconds, Environment.CurrentManagedThreadId);
                    CancellationUtils.WaitMilliseconds(_cancellationToken, _delayInMilliseconds);
                    logger?.LogDebug("Done delaying next trigger by {shutdownType}ms in thread {threadId}", _delayInMilliseconds, Environment.CurrentManagedThreadId);

                    return true;
                }
                catch (OperationCanceledException)
                {
                    logger?.LogTrace("Recieved thread cancellation during trigger delay");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    logger?.LogTrace("Recieved thread interruption during trigger delay");
                    throw;
                }
                catch (Exception ex)
                {
                    AddToTriggerHistory(TriggerOutcome.Failed);
                    logger?.LogCritical(ex, "While trying to delay next trigger: {message}", ex.Message);
                    return false;
                }
            }
        }

        #endregion
    }
}
