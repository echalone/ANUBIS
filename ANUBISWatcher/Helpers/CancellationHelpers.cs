namespace ANUBISWatcher.Helpers
{
    public static class CancellationUtils
    {

        public static bool WaitMilliseconds(CancellationToken? cancellationToken, int? milliseconds)
        {
            if (milliseconds.HasValue && milliseconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMilliseconds(milliseconds.Value));
            else
                return true;
        }

        public static bool WaitMilliseconds(CancellationToken? cancellationToken, uint? milliseconds)
        {
            if (milliseconds.HasValue && milliseconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMilliseconds(milliseconds.Value));
            else
                return true;
        }

        public static bool WaitMilliseconds(CancellationToken? cancellationToken, long? milliseconds)
        {
            if (milliseconds.HasValue && milliseconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMilliseconds(milliseconds.Value));
            else
                return true;
        }

        public static bool WaitMilliseconds(CancellationToken? cancellationToken, ulong? milliseconds)
        {
            if (milliseconds.HasValue && milliseconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMilliseconds(milliseconds.Value));
            else
                return true;
        }

        public static bool WaitMilliseconds(CancellationToken? cancellationToken, double? milliseconds)
        {
            if (milliseconds.HasValue && milliseconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMilliseconds(milliseconds.Value));
            else
                return true;
        }

        public static bool WaitSeconds(CancellationToken? cancellationToken, int? seconds)
        {
            if (seconds.HasValue && seconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromSeconds(seconds.Value));
            else
                return true;
        }

        public static bool WaitSeconds(CancellationToken? cancellationToken, uint? seconds)
        {
            if (seconds.HasValue && seconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromSeconds(seconds.Value));
            else
                return true;
        }

        public static bool WaitSeconds(CancellationToken? cancellationToken, long? seconds)
        {
            if (seconds.HasValue && seconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromSeconds(seconds.Value));
            else
                return true;
        }

        public static bool WaitSeconds(CancellationToken? cancellationToken, ulong? seconds)
        {
            if (seconds.HasValue && seconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromSeconds(seconds.Value));
            else
                return true;
        }

        public static bool WaitSeconds(CancellationToken? cancellationToken, double? seconds)
        {
            if (seconds.HasValue && seconds.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromSeconds(seconds.Value));
            else
                return true;
        }

        public static bool WaitMinutes(CancellationToken? cancellationToken, int? minutes)
        {
            if (minutes.HasValue && minutes.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMinutes(minutes.Value));
            else
                return true;
        }

        public static bool WaitMinutes(CancellationToken? cancellationToken, uint? minutes)
        {
            if (minutes.HasValue && minutes.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMinutes(minutes.Value));
            else
                return true;
        }

        public static bool WaitMinutes(CancellationToken? cancellationToken, long? minutes)
        {
            if (minutes.HasValue && minutes.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMinutes(minutes.Value));
            else
                return true;
        }

        public static bool WaitMinutes(CancellationToken? cancellationToken, ulong? minutes)
        {
            if (minutes.HasValue && minutes.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMinutes(minutes.Value));
            else
                return true;
        }

        public static bool WaitMinutes(CancellationToken? cancellationToken, double? minutes)
        {
            if (minutes.HasValue && minutes.Value > 0)
                return Wait(cancellationToken, TimeSpan.FromMinutes(minutes.Value));
            else
                return true;
        }

        public static bool WaitLocal(CancellationToken? cancellationToken, DateTime? timestamp)
        {
            if (timestamp.HasValue)
            {
                TimeSpan timespan = timestamp.Value - DateTime.Now;
                return Wait(cancellationToken, timespan);
            }
            else
            {
                return true;
            }
        }

        public static bool WaitUtc(CancellationToken? cancellationToken, DateTime? timestamp)
        {
            if (timestamp.HasValue)
            {
                TimeSpan timespan = timestamp.Value - DateTime.UtcNow;
                return Wait(cancellationToken, timespan);
            }
            else
            {
                return true;
            }
        }

        public static bool Wait(CancellationToken? cancellationToken, TimeSpan? timespan)
        {
            if (timespan.HasValue && timespan.Value.TotalMilliseconds > 0)
            {
                if (cancellationToken != null)
                {
                    cancellationToken.Value.ThrowIfCancellationRequested();
                    if (cancellationToken.Value.WaitHandle.WaitOne(timespan.Value))
                        throw new OperationCanceledException();
                    cancellationToken.Value.ThrowIfCancellationRequested();

                    return false;
                }
                else
                {
                    Thread.Sleep(timespan.Value);
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
