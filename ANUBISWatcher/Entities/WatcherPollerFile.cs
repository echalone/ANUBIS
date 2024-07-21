using ANUBISWatcher.Configuration.Serialization;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;

namespace ANUBISWatcher.Entities
{
    #region Enums

    public enum WatcherFileState
    {
        Unknown = 0, // Unknown state
        Stopped, // Not armed yet
        NoPanic, // As long as nothing happened while armed (not triggered, no panic, no error, etc.)
        Panic, // When watcher went into panic
        Error, // When any error occured that's not a panic
        NoResponse, // When own poller hasn't heard from main process in time
        Unreachable, // When remote file can't be reached
    }

    public enum WatcherFilePanicReason
    {
        [JsonEnumErrorValue]
        Unknown = 0,
        [JsonEnumName(true, "all", "every", "fallback", "", "*")]
        [JsonEnumNullValue]
        All,
        [JsonEnumName(true, "none", "NoPanic")]
        NoPanic, // As long as nothing happened while armed (not triggered, no panic, no error, etc.)
        Panic, // When watcher went into panic
        InvalidState, // When the state is invalid/unknown
        InvalidTimestamp, // When the state is invalid/unknown
        Error, // When any error occured that's not a panic
        DeSynced, // When time of remote poller is too much ahead
        NoResponse, // When poller hasn't heard from main process in time
        Unreachable, // When remote file can't be reached
    }

    #endregion

    public class WatcherPollerFile
    {
        #region Fields

        #region Constants

        private readonly object lock_TimestampLastUpdate = new();
        private readonly object lock_CurrentStateTimestamp = new();
        private readonly object lock_CurrentStateTimestampAge = new();
        private readonly object lock_CurrentState = new();
        private readonly object lock_Panic = new();
        private readonly object lock_NewPanic = new();

        #endregion

        #region Unchangable fields

        private readonly TimeSpan _lockTimeout = TimeSpan.Zero;
        private CancellationToken? _cancellationToken = null;
        private TimeSpan _offsetRemote = TimeSpan.Zero;

        #endregion

        #region Changable fields

        private volatile WatcherFileState _state = WatcherFileState.Unknown;
        private DateTime? _stateTimestamp = DateTime.MinValue;
        private long _stateTimestampAge = 0;
        private volatile bool _nullStateTimestampAge = true;
        private DateTime _timestampLastUpdate;
        private volatile WatcherFileState _statePrevious = WatcherFileState.Stopped;
        private volatile WatcherFilePanicReason _panic = WatcherFilePanicReason.NoPanic;
        private volatile WatcherFilePanicReason _panicNew = WatcherFilePanicReason.NoPanic;
        private volatile bool _hasNewPanic = false;

        #endregion

        #endregion

        #region Properties

        public WatcherPollerFileOptions Options { get; init; }

        private volatile bool _inSafeMode;
        public bool InSafeMode { get { return _inSafeMode; } private set { _inSafeMode = value; } }

        private volatile bool _doMonitor;
        public bool DoMonitor { get { return _doMonitor; } private set { _doMonitor = value; } }

        private volatile bool _holdBack;
        /// <summary>
        /// Are we in holdback mode? Which means not to trigger any switches on panic and not to enter safe mode.
        /// </summary>
        public bool HoldBack { get { return _holdBack; } private set { _holdBack = value; } }

        private volatile bool _checkForPanic;
        /// <summary>
        /// Should we check for panic?
        /// </summary>
        public bool CheckForPanic { get { return _checkForPanic; } private set { _checkForPanic = value; } }

        public bool CanBeArmed { get { return DoMonitor && (!HoldBack || Panic == WatcherFilePanicReason.NoPanic); } }

        public WatcherFileState CurrentState
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_CurrentState, _lockTimeout);
                    if (lockTaken)
                        return _state;
                    else
                        throw new LockTimeoutException(nameof(CurrentState), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CurrentState);
                    }
                }
            }
            private set
            {
                lock (lock_CurrentState)
                {
                    _state = value;
                }
            }
        }

        public DateTime? CorrectedStateTimestamp
        {
            get
            {
                if (CurrentStateTimestamp.HasValue)
                {
                    return CurrentStateTimestamp.Value + _offsetRemote;
                }
                else
                {
                    return null;
                }
            }
        }

        public DateTime? CurrentStateTimestamp
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_CurrentStateTimestamp, _lockTimeout);
                    if (lockTaken)
                        return _stateTimestamp;
                    else
                        throw new LockTimeoutException(nameof(CurrentStateTimestamp), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CurrentStateTimestamp);
                    }
                }
            }
            private set
            {
                lock (lock_CurrentStateTimestamp)
                {
                    _stateTimestamp = value;
                }
            }
        }

        public long? CurrentStateTimestampAge
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_CurrentStateTimestampAge, _lockTimeout);
                    if (lockTaken)
                    {
                        if (_nullStateTimestampAge)
                            return null;
                        else
                            return Volatile.Read(ref _stateTimestampAge);
                    }
                    else
                        throw new LockTimeoutException(nameof(CurrentStateTimestamp), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CurrentStateTimestampAge);
                    }
                }
            }
            private set
            {
                lock (lock_CurrentStateTimestampAge)
                {
                    if (value.HasValue)
                    {
                        _nullStateTimestampAge = false;
                        Volatile.Write(ref _stateTimestampAge, value.Value);
                    }
                    else
                    {
                        _nullStateTimestampAge = true;
                    }
                }
            }
        }

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

        public TimeSpan OffsetRemote { get { return _offsetRemote; } }


        public WatcherFilePanicReason Panic
        {
            get
            {
                bool lockTaken = false;
                try
                {
                    lockTaken = Monitor.TryEnter(lock_Panic, _lockTimeout);
                    if (lockTaken)
                        return _panic;
                    else
                        throw new LockTimeoutException(nameof(Panic), _lockTimeout);
                }
                finally
                {
                    // Ensure that the lock is released.
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_Panic);
                    }
                }
            }
            private set
            {
                lock (lock_Panic)
                {
                    lock (lock_NewPanic)
                    {
                        _panic = value;
                        if (value != WatcherFilePanicReason.NoPanic)
                        {
                            _panicNew = value;
                            _hasNewPanic = true;
                        }
                        else
                        {
                            _panicNew = WatcherFilePanicReason.NoPanic;
                            _hasNewPanic = false;
                        }
                    }
                }
            }
        }
        public bool DoCheckStateTimestamp { get { return Options.StateTimestampCheck; } }
        public bool DoCheckState { get { return Options.StateCheck; } }
        public bool DoCheckAny { get { return DoCheckState || DoCheckStateTimestamp; } }
        public ILogger? Logging { get { return Options.Logger; } }

        #endregion

        #region Constructors

        public WatcherPollerFile(WatcherPollerFileOptions options)
        {
            Options = options;
            CurrentState = WatcherFileState.Stopped;
            DoMonitor = false;
            CheckForPanic = false;
            _lockTimeout = TimeSpan.FromMilliseconds(options.LockTimeoutInMilliseconds);
            _statePrevious = WatcherFileState.Stopped;
            UpdateTimestamp();
        }

        #endregion

        #region Helper methods

        private bool IsSafeModePanicReason(WatcherFilePanicReason reason)
        {
            return Options.SafeModeSensitive &&
                        (reason == WatcherFilePanicReason.NoResponse || reason == WatcherFilePanicReason.Unreachable ||
                        reason == WatcherFilePanicReason.DeSynced || reason == WatcherFilePanicReason.Error ||
                        reason == WatcherFilePanicReason.InvalidState || reason == WatcherFilePanicReason.InvalidTimestamp);
        }

        private void CheckThreadCancellation()
        {
            _cancellationToken?.ThrowIfCancellationRequested();
        }

        private void UpdateTimestamp()
        {
            using (Logging?.BeginScope("UpdateTimestamp"))
            {
                try
                {
                    CheckThreadCancellation();

                    DateTime now = DateTime.UtcNow;
                    LastUpdateTimestamp = now;

                    Logging?.LogTrace("Updated LastUpdateTimestamp for watcher file \"{name}\" to {timestamp}", Options.WatcherFileName, now);
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Watcher file polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Watcher file polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to set last update timestamp: {message}", ex.Message);
                }
            }
        }

        private bool SetPanic(WatcherFilePanicReason reason)
        {
            using (Logging?.BeginScope("SetPanic"))
            {
                bool hasPanic = false;
                try
                {
                    CheckThreadCancellation();

                    if (CheckForPanic)
                    {
                        // In safe mode only no response is a panic
                        if (!InSafeMode || IsSafeModePanicReason(reason))
                        {
                            Logging?.LogTrace("Examining watcher file \"{name}\" for panic condition", Options.WatcherFileName);

                            if (reason != WatcherFilePanicReason.NoPanic)
                            {
                                // only throw panic if previously there was no panic or if it is a no response panic and previously there was something else or no panic
                                if (Panic == WatcherFilePanicReason.NoPanic ||
                                        (!IsSafeModePanicReason(Panic) &&
                                            IsSafeModePanicReason(reason)))
                                {
                                    Logging?.LogCritical("PANIC!!! Panic because of watcher file \"{name}\", panic reason is: {reason}", Options.WatcherFileName, reason);
                                    Panic = reason;

                                    hasPanic = true;
                                }
                                else
                                {
                                    Logging?.LogTrace("Would panic because of watcher file \"{name}\" and reason {newReason}, but this switch already panicked due to the following reason: {existingReason}", Options.WatcherFileName, reason, Panic);

                                    hasPanic = false;
                                }
                            }
                            else
                            {
                                Logging?.LogTrace("Examined watcher file \"{name}\" for panic condition but there was no reason to panic", Options.WatcherFileName);

                                hasPanic = false;
                            }
                        }
                        else
                        {
                            if (Panic == WatcherFilePanicReason.NoPanic && reason != WatcherFilePanicReason.NoPanic)
                            {
                                Logging?.LogTrace("Would panic because of watcher file \"{name}\" and reason {reason}, but watcher file is in safe mode", Options.WatcherFileName, reason);
                            }

                            hasPanic = false;
                        }
                    }
                    else
                    {
                        if (Panic == WatcherFilePanicReason.NoPanic && reason != WatcherFilePanicReason.NoPanic)
                        {
                            Logging?.LogTrace("Would panic because of watcher file \"{name}\" and reason {reason}, but panicing is turned off for this file", Options.WatcherFileName, reason);
                        }

                        hasPanic = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Watcher file polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Watcher file polling thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogCritical(ex, "Fatal error while trying to set panic state: {message}", ex.Message);

                    try
                    {
                        Panic = WatcherFilePanicReason.Error;
                    }
                    catch (Exception exInner)
                    {
                        Logging?.LogCritical(exInner, "Fatal inner error while trying to set panic state in fatal error: {message}", exInner.Message);
                    }

                    hasPanic = true;
                }

                CheckThreadCancellation();

                return hasPanic;
            }
        }

        private bool SetState(WatcherFileState watcherState)
        {
            using (Logging?.BeginScope("SetState"))
            {
                CheckThreadCancellation();

                CurrentState = watcherState;

                if (DoCheckState)
                {
                    Logging?.LogTrace("Verifying state for watcher file \"{name}\": {watcherState}", Options.WatcherFileName, watcherState);

                    switch (watcherState)
                    {
                        case WatcherFileState.NoPanic:
                            Logging?.LogTrace("Watcher file \"{name}\" indicates no panic", Options.WatcherFileName);
                            return false;
                        case WatcherFileState.Stopped:
                            Logging?.LogTrace("Watcher file \"{name}\" indicates stopped", Options.WatcherFileName);
                            return false;
                        case WatcherFileState.Panic:
                            Logging?.LogTrace("Watcher file \"{name}\" indicates panic", Options.WatcherFileName);
                            return SetPanic(WatcherFilePanicReason.Panic);
                        case WatcherFileState.Error:
                            Logging?.LogTrace("Watcher file \"{name}\" indicates error", Options.WatcherFileName);
                            return SetPanic(WatcherFilePanicReason.Error);
                        case WatcherFileState.NoResponse:
                            Logging?.LogTrace("Watcher file \"{name}\" indicates no response of main process", Options.WatcherFileName);
                            return SetPanic(WatcherFilePanicReason.NoResponse);
                        case WatcherFileState.Unknown:
                            Logging?.LogTrace("Watcher file \"{name}\" indicates unknown state", Options.WatcherFileName);
                            return SetPanic(WatcherFilePanicReason.InvalidState);
                        case WatcherFileState.Unreachable:
                            Logging?.LogTrace("Watcher file \"{name}\" unreachable", Options.WatcherFileName);
                            return SetPanic(WatcherFilePanicReason.Unreachable);
                        default:
                            Logging?.LogWarning("Watcher file \"{name}\" has unknown state {state}", Options.WatcherFileName, watcherState);
                            return SetPanic(WatcherFilePanicReason.InvalidState);
                    }
                }
                else
                {
                    Logging?.LogTrace("Not checking state for watcher file \"{name}\" as StateCheck setting was false", Options.WatcherFileName);

                    return false;
                }
            }
        }

        private bool SetStateTimestamp(DateTime stateTimestamp)
        {
            using (Logging?.BeginScope("SetStateTimestamp"))
            {
                CheckThreadCancellation();

                // sync time on init or restart
                if (CurrentStateTimestamp == null || (_statePrevious == WatcherFileState.Stopped && CurrentState != WatcherFileState.Stopped))
                {
                    Logging?.LogDebug("Resetting remote offset for watcher file \"{name}\"", Options.WatcherFileName);
                    _offsetRemote = LastUpdateTimestamp - stateTimestamp;
                    if (_offsetRemote.TotalSeconds > 300 || _offsetRemote.TotalSeconds < -300)
                    {
                        // Show a warning if the offset is larger than plus/minus 5 minutes as the clocks might not be synchronized
                        Logging?.LogWarning("Remote offset for watcher file \"{name}\" is {offset} (this is large, check clock synchronization)", Options.WatcherFileName, _offsetRemote);
                    }
                    else if (_offsetRemote.TotalSeconds > 1 || _offsetRemote.TotalSeconds < -1)
                    {
                        Logging?.LogDebug("Remote offset for watcher file \"{name}\" is {offset}", Options.WatcherFileName, _offsetRemote);
                    }
                }

                _statePrevious = CurrentState;
                CurrentStateTimestamp = stateTimestamp;
#pragma warning disable CS8629
                CurrentStateTimestampAge = (long)Math.Floor((DateTime.UtcNow - CorrectedStateTimestamp.Value).TotalSeconds);
#pragma warning restore CS8629
                if (DoCheckStateTimestamp)
                {
                    if (CurrentState != WatcherFileState.Stopped)
                    {
                        Logging?.LogTrace("Verifying age of corrected state timestamp {timestamp} for watcher file \"{name}\" is between {maxnegativeage} and {maxage} seconds (age is {ageinseconds}s)", CorrectedStateTimestamp.Value, Options.WatcherFileName, Options.MaxUpdateAgeNegativeSecondsCalculated, Options.MaxUpdateAgeInSeconds, CurrentStateTimestampAge);

                        if (CurrentStateTimestampAge < Options.MaxUpdateAgeNegativeSecondsCalculated)
                        {
                            Logging?.LogWarning("Watcher file \"{name}\" has returned negative value of age which exceeds the maximum allowed age of {maxnegativeage} seconds into the negative", Options.WatcherFileName, Options.MaxUpdateAgeNegativeSecondsCalculated);

                            return SetPanic(WatcherFilePanicReason.DeSynced);
                        }
                        else if (CurrentStateTimestampAge > Options.MaxUpdateAgeInSeconds)
                        {
                            Logging?.LogWarning("Watcher file \"{name}\" has returned age which exceeds the maximum allowed age of {maxage} seconds", Options.WatcherFileName, Options.MaxUpdateAgeInSeconds);

                            return SetPanic(WatcherFilePanicReason.NoResponse);
                        }
                        else
                        {
                            Logging?.LogTrace("Watcher file \"{name}\" has returned valid timestamp age of {ageinseconds}s", Options.WatcherFileName, CurrentStateTimestampAge);
                            return false;
                        }

                    }
                    else
                    {
                        Logging?.LogTrace("Not checking state timestamp for watcher file \"{name}\" as current state indicates watcher is stopped (current timestamp is: {timestamp})", Options.WatcherFileName, stateTimestamp);

                        return false;
                    }
                }
                else
                {
                    Logging?.LogTrace("Not checking state timestamp for watcher file \"{name}\" as CheckStateTimestamp setting was false (current timestamp is: {timestamp})", Options.WatcherFileName, stateTimestamp);

                    return false;
                }
            }
        }

        private void WriteToFile()
        {
            try
            {
                FileInfo fi = new(Options.FilePath);
                if (!fi.Exists && !(fi.Directory?.Exists ?? false))
                {
                    if (!string.IsNullOrWhiteSpace(fi.DirectoryName))
                    {
                        Logging?.LogTrace("Creating parent directory \"{directoryname}\" of Watcher file \"{name}\"", fi.DirectoryName, Options.WatcherFileName);
                        try
                        {
                            Directory.CreateDirectory(fi.DirectoryName);
                        }
                        catch (Exception ex)
                        {
                            Logging?.LogWarning(ex, "Error while trying to create parent directory \"{directoryname}\" of Watcher file \"{name}\": {message}", fi.DirectoryName, Options.WatcherFileName, ex.Message);
                        }
                    }
                    else
                    {
                        Logging?.LogWarning("Couldn't create parent directory for Watcher file \"{name}\" because it is unknown", Options.WatcherFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging?.LogWarning(ex, "Error while trying to check for existence of Watcher file \"{name}\": {message}", Options.WatcherFileName, ex.Message);
            }
            WriteToFile(Options.FilePath, CurrentState, CurrentStateTimestamp);
        }

        public static void WriteToFile(string filePath, WatcherFileState state, DateTime? currentStateTimeStamp)
        {
            using (FileStream fs = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new(fs))
                {
                    sw.WriteLine(Enum.GetName(state));
                    if (currentStateTimeStamp.HasValue)
                        sw.WriteLine(currentStateTimeStamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));
                    sw.Flush();
                }
            }

        }

        #endregion

        #region Update methods

        /// <summary>
        /// Update state from outside poller
        /// </summary>
        /// <param name="watcherState"></param>
        public void UpdateState(WatcherFileState watcherState)
        {
            using (Logging?.BeginScope("UpdateState"))
            {
                CheckThreadCancellation();

                CurrentState = watcherState;

                UpdateStateTimestamp();
            }
        }

        /// <summary>
        /// Update state timestamp from outside poller
        /// </summary>
        public void UpdateStateTimestamp()
        {
            using (Logging?.BeginScope("UpdateStateTimestamp"))
            {
                CheckThreadCancellation();

                CurrentStateTimestamp = DateTime.UtcNow;

                CheckThreadCancellation();
            }
        }

        public void WriteEndStatus()
        {
            try
            {
                Logging?.LogDebug("Writing end status to Watcher file \"{name}\"", Options.WatcherFileName);
                WriteToFile();
            }
            catch (Exception ex)
            {
                Logging?.LogWarning(ex, "Error while trying to write end status to Watcher file \"{name}\": {message}", Options.WatcherFileName, ex.Message);
            }
        }

        public void WriteInitialStatus()
        {
            try
            {
                Logging?.LogDebug("Writing initial status and timestamp to Watcher file \"{name}\"", Options.WatcherFileName);
                CurrentStateTimestamp = DateTime.UtcNow;
                WriteToFile();
            }
            catch (Exception ex)
            {
                Logging?.LogWarning(ex, "Error while trying to write initial status to Watcher file \"{name}\": {message}", Options.WatcherFileName, ex.Message);
            }
        }

        public bool WriteFile()
        {
            using (Logging?.BeginScope("WriteFile"))
            {
                try
                {
                    if (DoMonitor)
                    {
                        bool hasPanic = false;
                        Exception? exCaught = null;

                        if (CurrentStateTimestamp.HasValue)
                            hasPanic = SetStateTimestamp(CurrentStateTimestamp.Value) || hasPanic;

                        int retryCount = 0;
                        bool success = true;
                        Logging?.LogDebug("Writing status {status} with timestamp {timestamp} (age {timestampage}s) to watcher file \"{name}\"", CurrentState, CurrentStateTimestamp, CurrentStateTimestampAge, Options.WatcherFileName);
                        do
                        {
                            CheckThreadCancellation();

                            if (!success)
                            {
                                int waitingTime = RandomNumberGenerator.GetInt32(Options.FileAccessRetryMillisecondsMin, Options.FileAccessRetryMillisecondsMax + 1);
                                Logging?.LogTrace("Retrying {retry}/{maxretry} to write to watcher file \"{name}\" in {waitingTime}ms...", retryCount, Options.FileAccessRetryCountMax, Options.WatcherFileName, waitingTime);
                                if (_cancellationToken != null)
                                {
                                    if (_cancellationToken.Value.WaitHandle.WaitOne(waitingTime))
                                        Logging?.LogTrace("Watcher file poller recieved thread cancellation request during wait for file access retry");
                                }
                                else
                                {
                                    Thread.Sleep(waitingTime);
                                }
                            }

                            CheckThreadCancellation();

                            success = false;

                            try
                            {
                                WriteToFile();
                                success = true;
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (ThreadInterruptedException)
                            {
                                throw;
                            }
                            catch (IOException ioex)
                            {
                                Logging?.LogTrace(ioex, "IO exception while trying to write to watcher file \"{name}\": {message}", Options.WatcherFileName, ioex.Message);
                                exCaught = ioex;
                            }
                            catch (System.UnauthorizedAccessException uaex)
                            {
                                Logging?.LogTrace(uaex, "Access exception while trying to write to watcher file \"{name}\": {message}", Options.WatcherFileName, uaex.Message);
                                exCaught = uaex;
                            }
                            catch (Exception ex)
                            {
                                Logging?.LogError(ex, "Error while trying to write to watcher file \"{name}\": {message}", Options.WatcherFileName, ex.Message);
                                return SetPanic(WatcherFilePanicReason.Error) || hasPanic;
                            }

                            retryCount++;
                        }
                        while (!success && retryCount <= Options.FileAccessRetryCountMax);

                        CheckThreadCancellation();

                        if (!success)
                        {
                            if (exCaught is IOException ioex)
                            {
                                Logging?.LogWarning(ioex, "IO exception while trying to write to watcher file \"{name}\": {message}", Options.WatcherFileName, ioex.Message);
                            }
                            else if (exCaught is System.UnauthorizedAccessException uaex)
                            {
                                Logging?.LogWarning(uaex, "Access exception while trying to write to watcher file \"{name}\": {message}", Options.WatcherFileName, uaex.Message);
                            }

                            Logging?.LogWarning("Wasn't able to write to watcher file \"{name}\"", Options.WatcherFileName);
                            hasPanic = SetPanic(WatcherFilePanicReason.Unreachable) || hasPanic;

                            CheckThreadCancellation();
                        }

                        if (hasPanic)
                        {
                            if (Options.WriteStateOnPanic)
                            {
                                switch (Panic)
                                {
                                    case WatcherFilePanicReason.NoResponse:
                                        SetState(WatcherFileState.NoResponse);
                                        break;
                                    case WatcherFilePanicReason.Panic:
                                        SetState(WatcherFileState.Panic);
                                        break;
                                    case WatcherFilePanicReason.DeSynced:
                                    case WatcherFilePanicReason.Error:
                                    case WatcherFilePanicReason.InvalidState:
                                    case WatcherFilePanicReason.InvalidTimestamp:
                                    case WatcherFilePanicReason.Unreachable:
                                        SetState(WatcherFileState.Error);
                                        break;
                                }
                                Logging?.LogWarning("Reached panic condition, immediatly writing back state {state} to watcher file {name} as WriteStateOnPanic option was true", CurrentState, Options.WatcherFileName);
                                WriteToFile();
                            }
                        }

                        if (hasPanic)
                            Logging?.LogWarning("Watcher file \"{name}\" reached panic condition {panicReason} and switched to panic state!", Options.WatcherFileName, Panic);
                        else if (Panic != WatcherFilePanicReason.NoPanic)
                            Logging?.LogDebug("Watcher file \"{name}\" remains in panic state {panicReason}", Options.WatcherFileName, Panic);
                        else
                            Logging?.LogDebug("Watcher file \"{name}\" remains in normal/non-panic state", Options.WatcherFileName);

                        return hasPanic;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Watcher file access thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Watcher file access thread was interrupted");
                    throw;
                }
                catch (Exception exFatal)
                {
                    Logging?.LogCritical(exFatal, "Fatal error while writing watcher file \"{name}\": {message}", Options.WatcherFileName, exFatal.Message);

                    return SetPanic(WatcherFilePanicReason.Error);
                }
                finally
                {
                    UpdateTimestamp();
                }
            }
        }

        public bool ReadFile(bool init = false)
        {
            using (Logging?.BeginScope("ReadFile"))
            {
                try
                {
                    UpdateTimestamp();
                    if (DoMonitor || init)
                    {
                        string? strState = null;
                        string? strTimestamp = null;
                        bool hasPanic = false;
                        Exception? exCaught = null;

                        int retryCount = 0;
                        bool success = false;

                        CheckThreadCancellation();

                        if (init)
                        {
                            // lets reinitialize the timestamp
                            CurrentStateTimestamp = null;
                        }

                        success = true;
                        Logging?.LogDebug("Reading status and timestamp from watcher file \"{name}\"", Options.WatcherFileName);
                        do
                        {
                            CheckThreadCancellation();

                            if (!success)
                            {
                                int waitingTime = RandomNumberGenerator.GetInt32(Options.FileAccessRetryMillisecondsMin, Options.FileAccessRetryMillisecondsMax + 1);
                                Logging?.LogTrace("Retrying {retry}/{maxretry} to read from watcher file \"{name}\" in {waitingTime}ms...", retryCount, Options.FileAccessRetryCountMax, Options.WatcherFileName, waitingTime);
                                if (_cancellationToken != null)
                                {
                                    if (_cancellationToken.Value.WaitHandle.WaitOne(waitingTime))
                                        Logging?.LogTrace("Watcher file poller recieved thread cancellation request during wait for file access retry");
                                }
                                else
                                {
                                    Thread.Sleep(waitingTime);
                                }
                            }

                            CheckThreadCancellation();

                            success = false;

                            try
                            {
                                if (File.Exists(Options.FilePath))
                                {
                                    using (FileStream fs = new(Options.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        using (StreamReader sr = new(fs))
                                        {
                                            strState = sr.ReadLine();
                                            if (!sr.EndOfStream)
                                                strTimestamp = sr.ReadLine();
                                        }
                                    }
                                    exCaught = null;
                                    success = true;
                                }
                                else
                                {
                                    throw new MissingFileException($@"File does not (yet) exist to read from");
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
                            catch (IOException ioex)
                            {
                                Logging?.LogTrace("IO exception while trying to read from watcher file \"{name}\": {message}", Options.WatcherFileName, ioex.Message);
                                exCaught = ioex;
                            }
                            catch (System.UnauthorizedAccessException uaex)
                            {
                                Logging?.LogTrace(uaex, "Access exception while trying to read from watcher file \"{name}\": {message}", Options.WatcherFileName, uaex.Message);
                                exCaught = uaex;
                            }
                            catch (MissingFileException mfex)
                            {
                                Logging?.LogTrace(mfex, "Couldn't find read watcher file \"{name}\": {message}", Options.WatcherFileName, mfex.Message);
                                exCaught = mfex;
                            }
                            catch (Exception ex)
                            {
                                Logging?.LogError(ex, "Error while trying to read from watcher file \"{name}\": {message}", Options.WatcherFileName, ex.Message);
                                return SetPanic(WatcherFilePanicReason.Error) || hasPanic;
                            }

                            retryCount++;
                        }
                        while (!success && retryCount <= Options.FileAccessRetryCountMax);

                        CheckThreadCancellation();

                        UpdateTimestamp();

                        CheckThreadCancellation();

                        if (success)
                        {
                            if (strState != null)
                            {
                                Logging?.LogTrace("Retrieved following state string from watcher file \"{name}\": {statestring}", Options.WatcherFileName, strState);

                                if (Enum.TryParse(strState, true, out WatcherFileState state))
                                {
                                    Logging?.LogDebug("Watcher file \"{name}\" has state {state}", Options.WatcherFileName, Enum.GetName(state));
                                    hasPanic = SetState(state) || hasPanic;
                                }
                                else if (DoCheckState)
                                {
                                    Logging?.LogWarning("Watcher file \"{name}\" has invalid state \"{state}\"", Options.WatcherFileName, strState);
                                    hasPanic = SetPanic(WatcherFilePanicReason.InvalidState) || hasPanic;
                                }
                            }
                            else if (DoCheckState)
                            {
                                Logging?.LogWarning("Watcher file \"{name}\" has invalid empty state", Options.WatcherFileName);
                                hasPanic = SetPanic(WatcherFilePanicReason.InvalidState) || hasPanic;
                            }

                            CheckThreadCancellation();

                            if (strTimestamp != null)
                            {
                                Logging?.LogTrace("Retrieved following state timestamp string from watcher file \"{name}\": {statetimestampstring}", Options.WatcherFileName, strTimestamp);

                                if (DateTime.TryParseExact(strTimestamp, "yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime timestampLocal))
                                {
                                    DateTime timestamp = timestampLocal.ToUniversalTime();
                                    Logging?.LogDebug("Watcher file \"{name}\" has timestamp {timestamp}", Options.WatcherFileName, timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
                                    hasPanic = SetStateTimestamp(timestamp) || hasPanic;
                                    if (CorrectedStateTimestamp.HasValue)
                                    {
                                        Logging?.LogDebug("Watcher file \"{name}\" has corrected timestamp {timestamp} (by offset {offset})", Options.WatcherFileName, CorrectedStateTimestamp, _offsetRemote);
                                        Logging?.LogDebug("Watcher file \"{name}\" has timestamp age of {timestampage}s", Options.WatcherFileName, CurrentStateTimestampAge);
                                    }
                                    else
                                    {
                                        Logging?.LogWarning("Watcher file \"{name}\" has unknown corrected timestamp", Options.WatcherFileName);
                                    }
                                }
                                else if (DoCheckStateTimestamp)
                                {
                                    Logging?.LogWarning("Watcher file \"{name}\" has invalid timestamp \"{timestamp}\"", Options.WatcherFileName, strTimestamp);
                                    hasPanic = SetPanic(WatcherFilePanicReason.InvalidTimestamp) || hasPanic;
                                }
                            }
                            else if (DoCheckStateTimestamp)
                            {
                                Logging?.LogWarning("Watcher file \"{name}\" has invalid empty timestamp", Options.WatcherFileName);
                                hasPanic = SetPanic(WatcherFilePanicReason.InvalidTimestamp) || hasPanic;
                            }
                        }
                        else
                        {
                            if (exCaught is IOException ioex)
                            {
                                Logging?.LogWarning("IO exception while trying to read from watcher file \"{name}\": {message}", Options.WatcherFileName, ioex.Message);
                            }
                            else if (exCaught is System.UnauthorizedAccessException uaex)
                            {
                                Logging?.LogWarning(uaex, "Access exception while trying to read from watcher file \"{name}\": {message}", Options.WatcherFileName, uaex.Message);
                            }
                            else if (exCaught is MissingFileException mfex)
                            {
                                Logging?.LogWarning(mfex, "Couldn't find read watcher file \"{name}\": {message}", Options.WatcherFileName, mfex.Message);
                            }
                            else if (exCaught != null)
                            {
                                Logging?.LogWarning(exCaught, "Unknown error {errortype} while trying to read watcher file \"{name}\": {message}", exCaught.GetType().FullName, Options.WatcherFileName, exCaught.Message);
                            }
                            else
                            {
                                Logging?.LogWarning(exCaught, "Unknown problem while trying to read watcher file \"{name}\"", Options.WatcherFileName);
                            }

                            Logging?.LogWarning("Wasn't able to read from watcher file \"{name}\"", Options.WatcherFileName);
                            hasPanic = SetPanic(WatcherFilePanicReason.Unreachable) || hasPanic;
                            if (CurrentStateTimestamp.HasValue)
                            {
                                hasPanic = SetStateTimestamp(CurrentStateTimestamp.Value) || hasPanic;
                            }
                        }

                        CheckThreadCancellation();

                        if (hasPanic)
                            Logging?.LogWarning("Watcher file \"{name}\" reached panic condition {panicReason} and switched to panic state!", Options.WatcherFileName, Panic);
                        else if (Panic != WatcherFilePanicReason.NoPanic)
                            Logging?.LogDebug("Watcher file \"{name}\" remains in panic state {panicReason}", Options.WatcherFileName, Panic);
                        else
                            Logging?.LogDebug("Watcher file \"{name}\" remains in normal/non-panic state", Options.WatcherFileName);

                        return hasPanic;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Watcher file polling thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Watcher file polling thread was interrupted");
                    throw;
                }
                catch (Exception exFatal)
                {
                    Logging?.LogCritical(exFatal, "Fatal error while reading watcher file \"{name}\": {message}", Options.WatcherFileName, exFatal.Message);

                    return SetPanic(WatcherFilePanicReason.Error);
                }
            }
        }

        #endregion

        #region Check methods

        public WatcherFilePanicReason? GetNewPanic(bool remove = true)
        {
            bool lockTaken = false;
            try
            {
                lockTaken = Monitor.TryEnter(lock_NewPanic, _lockTimeout);
                if (lockTaken)
                {
                    var retVal = (WatcherFilePanicReason?)(_hasNewPanic ? _panicNew : null);
                    if (remove)
                    {
                        _panicNew = WatcherFilePanicReason.NoPanic;
                        _hasNewPanic = false;
                    }
                    return retVal;
                }
                else
                    throw new LockTimeoutException("NewPanic", _lockTimeout);
            }
            finally
            {
                // Ensure that the lock is released.
                if (lockTaken)
                {
                    Monitor.Exit(lock_NewPanic);
                }
            }
        }

        #endregion

        #region Control methods

        public bool ResetPanic()
        {
            using (Logging?.BeginScope("ResetPanic"))
            {
                CheckThreadCancellation();

                if (Panic != WatcherFilePanicReason.NoPanic)
                {
                    Logging?.LogDebug("Resetting panic {reason} for watcher file \"{name}\"", Panic, Options.WatcherFileName);
                    Panic = WatcherFilePanicReason.NoPanic;
                    if (CurrentState != WatcherFileState.NoPanic && CurrentState != WatcherFileState.Stopped)
                    {
                        CurrentState = WatcherFileState.NoPanic;
                    }
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Watcher file \"{name}\" had no panic to reset", Options.WatcherFileName);
                    return false;
                }
            }
        }

        public bool EnterSafeMode()
        {
            using (Logging?.BeginScope("EnterSafeMode"))
            {
                CheckThreadCancellation();

                if (!HoldBack)
                {
                    if (!InSafeMode)
                    {
                        InSafeMode = true;
                        Logging?.LogInformation("Watcher file \"{name}\" entered safe mode", Options.WatcherFileName);
                        return true;
                    }
                    else
                    {
                        Logging?.LogDebug("Watcher file \"{name}\" alreay in safe mode", Options.WatcherFileName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogWarning("Not entering safe mode for watcher file \"{name}\" as we are in holdback mode", Options.WatcherFileName);
                    return false;
                }
            }
        }

        public bool EnterHoldBackMode()
        {
            using (Logging?.BeginScope("EnterHoldBackMode"))
            {
                CheckThreadCancellation();

                if (!HoldBack)
                {
                    HoldBack = true;
                    CheckForPanic = true;
                    Logging?.LogInformation("Watcher file \"{name}\" entered holdback mode", Options.WatcherFileName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Watcher file \"{name}\" already entered holdback mode", Options.WatcherFileName);
                    return false;
                }
            }
        }

        public bool ArmPanicMode()
        {
            using (Logging?.BeginScope("ArmPanicMode"))
            {
                CheckThreadCancellation();

                if (!CheckForPanic || HoldBack)
                {
                    if (CanBeArmed)
                    {
                        CheckForPanic = true;
                        HoldBack = false;
                        Logging?.LogInformation("Watcher file \"{name}\" panic mode armed", Options.WatcherFileName);
                        return true;
                    }
                    else
                    {
                        Logging?.LogWarning("Cannot arm panic mode for watcher file \"{name}\", must be monitoring or holdback mode without panic", Options.WatcherFileName);
                        return false;
                    }
                }
                else
                {
                    Logging?.LogDebug("Watcher file \"{name}\" already armed panic mode", Options.WatcherFileName);
                    return false;
                }
            }
        }

        public bool DisarmPanicMode()
        {
            using (Logging?.BeginScope("DisarmPanicMode"))
            {
                CheckThreadCancellation();

                InSafeMode = false;
                if (HoldBack)
                {
                    HoldBack = false;
                    CheckForPanic = false;
                    Logging?.LogInformation("Watcher file \"{name}\" left holdback mode", Options.WatcherFileName);
                    return true;
                }
                else if (CheckForPanic)
                {
                    CheckForPanic = false;
                    Logging?.LogInformation("Watcher file \"{name}\" panic mode disarmed", Options.WatcherFileName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Watcher file \"{name}\" has not armed panic mode", Options.WatcherFileName);
                    return false;
                }
            }
        }

        public bool StartMonitoring()
        {
            using (Logging?.BeginScope("StartMonitoring"))
            {
                CheckThreadCancellation();

                if (!DoMonitor)
                {
                    DoMonitor = true;
                    UpdateState(WatcherFileState.NoPanic);
                    Logging?.LogInformation("Watcher file \"{name}\" started monitoring", Options.WatcherFileName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Watcher file \"{name}\" already monitoring", Options.WatcherFileName);
                    return false;
                }
            }
        }

        public bool StopMonitoring(bool setStoppedState)
        {
            using (Logging?.BeginScope("StopMonitoring"))
            {
                CheckThreadCancellation();

                CheckForPanic = false;
                HoldBack = false;
                InSafeMode = false;
                if (setStoppedState)
                    UpdateState(WatcherFileState.Stopped);
                if (DoMonitor)
                {
                    DoMonitor = false;
                    Logging?.LogInformation("Watcher file \"{name}\" stopped monitoring", Options.WatcherFileName);
                    return true;
                }
                else
                {
                    Logging?.LogDebug("Watcher file \"{name}\" isn't monitoring", Options.WatcherFileName);
                    return false;
                }
            }
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void RemoveCancellationToken()
        {
            _cancellationToken = null;
        }

        #endregion
    }
}
