using ANUBISWatcher.Configuration.ConfigFileData;
using ANUBISWatcher.Controlling;
using ANUBISWatcher.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ANUBISWatcher.Shared
{
    public enum InformationRequester : byte
    {
        Controller,
        GUI,
    }

    public enum TriggeredType : byte
    {
        Config = 0,
        ClewareUSBSwitch,
        FritzSwitch,
        SwitchBot,
        OSShutdown,
    }

    public enum TriggerOutcome : byte
    {
        Skipped = 0,
        Failed = 1,
        Success = 2,
    }

    public class TriggerHistoryEntry
    {
        public TriggeredType Type { get; set; }

        public string? Name { get; set; }

        public DateTime UtcTimestamp { get; set; }
        public TriggerOutcome Outcome { get; set; }
        public bool NewForGUI { get; set; }
        public bool NewForController { get; set; }

        public TriggerHistoryEntry() { }

        public TriggerHistoryEntry(TriggeredType type, string name)
            : this()
        {
            Type = type;
            Name = name;
            NewForGUI = true;
            NewForController = true;
        }

        public void AddToHistory(TriggerOutcome outcome)
        {
            Outcome = outcome;
            UtcTimestamp = DateTime.UtcNow;
            SharedData.AddTriggerHistory(this);
        }
    }

    public class PanicHistoryEntry
    {
        public UniversalPanicType SwitchType { get; set; }

        public string? Id { get; set; }

        public List<string> TriggerConfigs { get; set; } = [];

        public DateTime UtcTimestamp { get; set; }
        public UniversalPanicReason PanicReason { get; set; }
        public bool NewForGUI { get; set; }
        public bool NewForController { get; set; }
        public int CountNumber { get; set; }


        public PanicHistoryEntry() { }

        public PanicHistoryEntry(UniversalPanicType type, string? id, UniversalPanicReason reason, List<string>? triggerConfigs)
            : this()
        {
            SwitchType = type;
            Id = id;
            TriggerConfigs = triggerConfigs ?? [];
            PanicReason = reason;
            UtcTimestamp = DateTime.UtcNow;
            NewForGUI = true;
            NewForController = true;
        }

        public void AddToHistory()
        {
            SharedData.AddPanicHistory(this);
        }
    }

    public class ControllerStatusHistoryEntry
    {
        public ControllerStatus Status { get; set; }
        public bool SafeModeIncludesRemoteFiles { get; set; }
        public bool TriggerIsCountdownT0 { get; set; }

        public DateTime UtcTimestamp { get; set; }
        public bool NewForGUI { get; set; }
        public bool NewForController { get; set; }


        public ControllerStatusHistoryEntry() { }

        public ControllerStatusHistoryEntry(ControllerStatus status, bool safeModeIncludesRemoteFiles = false, bool triggerIsCountdownT0 = false)
            : this()
        {
            Status = status;
            SafeModeIncludesRemoteFiles = safeModeIncludesRemoteFiles;
            TriggerIsCountdownT0 = triggerIsCountdownT0;
            UtcTimestamp = DateTime.UtcNow;
            NewForGUI = true;
            NewForController = true;
        }

        public bool AddToHistory()
        {
            return SharedData.AddControllerStatusHistory(this);
        }
    }

    public static class SharedData
    {
        #region Constants

        private const int c_DefaultLockTimeoutInMilliseconds = 1000;
        private const uint c_maxTriggerThreadCount = 30;
        private const uint c_maxBlockTriggerAgeInMinutes = 5;
        private const uint c_timeOfDeathMinutesAfterShutdown = 10;

        private const ushort c_maxTriggerHistory = 1024;
        private const ushort c_maxPanicHistory = 1024;
        private const ushort c_maxControllerStatusHistory = 1024;

        #endregion

        #region Locks

        private static readonly Dictionary<TriggeredType, object> locks_dicTriggeredList =
            new()
            {
                { TriggeredType.Config, new object() },
                { TriggeredType.ClewareUSBSwitch, new object() },
                { TriggeredType.FritzSwitch, new object() },
                { TriggeredType.SwitchBot, new object() },
                { TriggeredType.OSShutdown, new object() },
            };

        private static object lock_TriggerHistory = new();

        private static object lock_PanicHistory = new();

        private static object lock_ControllerStatusHistory = new();

        private static object lock_MainController = new();

        private static object lock_Config = new();

        private static object lock_ShutDownTimestamp = new();

        private static object lock_CountdownData = new();

        private static object lock_TriggerThreadCount = new();

        #endregion

        #region Private static data objects

        public static Microsoft.Extensions.Logging.ILogger? Logging { get; set; }
        public static Microsoft.Extensions.Logging.ILogger? ConfigLogging { get; set; }
        public static Microsoft.Extensions.Logging.ILogger? InterfaceLogging { get; set; }

        private static readonly Dictionary<TriggeredType, List<string>> _dicTriggeredLists =
            new()
            {
                { TriggeredType.Config, new List<string>() },
                { TriggeredType.ClewareUSBSwitch, new List<string>() },
                { TriggeredType.FritzSwitch, new List<string>() },
                { TriggeredType.SwitchBot, new List<string>() },
                { TriggeredType.OSShutdown, new List<string>() },
            };

        private static List<TriggerHistoryEntry> _lstTriggerHistory = [];

        private static List<PanicHistoryEntry> _lstPanicHistory = [];

        private static List<ControllerStatusHistoryEntry> _lstControllerStatusHistory = [];

        private static volatile ControllerStatus _currentControllerStatus = ControllerStatus.Stopped;
        public static ControllerStatus CurrentControllerStatus { get { return _currentControllerStatus; } private set { _currentControllerStatus = value; } }

        private static volatile bool _hasPanic = false;
        private static MainController? _mainController = null;

        private static ConfigFile? _config = null;

        private static volatile bool _hasInformationChange_forController = false;
        private static volatile bool _hasInformationChange_forGUI = false;
        private static volatile uint _triggerThreadCount = 0;
        private static DateTime? _lastTriggerThreadStart = null;

        private static DateTime? _timestampShutDown = null;

        private static CountdownData? _countdownInfo = null;

        #endregion

        #region Public static properties

        public static bool HasPanic { get { return _hasPanic; } private set { _hasPanic = value; } }

        public static CountdownData? CountdownInfo
        {
            get
            {
                bool lockTaken = Monitor.TryEnter(lock_CountdownData, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_CountdownData = newLock;
                        lockTaken = true;
                    }

                    return _countdownInfo;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CountdownData);
                    }
                }
            }
            private set
            {
                bool lockTaken = Monitor.TryEnter(lock_CountdownData, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_CountdownData = newLock;
                        lockTaken = true;
                    }

                    _countdownInfo = value;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_CountdownData);
                    }
                }
            }
        }

        public static DateTime? ShutDownTimestamp
        {
            get
            {
                bool lockTaken = Monitor.TryEnter(lock_ShutDownTimestamp, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_ShutDownTimestamp = newLock;
                        lockTaken = true;
                    }

                    return _timestampShutDown;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_ShutDownTimestamp);
                    }
                }
            }
            private set
            {
                bool lockTaken = Monitor.TryEnter(lock_ShutDownTimestamp, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_ShutDownTimestamp = newLock;
                        lockTaken = true;
                    }

                    _timestampShutDown = value;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_ShutDownTimestamp);
                    }
                }
            }
        }

        public static bool IncreaseTriggerCount()
        {
            bool lockTaken = Monitor.TryEnter(lock_TriggerThreadCount, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_TriggerThreadCount = newLock;
                    lockTaken = true;
                }

                if (_triggerThreadCount >= c_maxTriggerThreadCount)
                {
                    if (!_lastTriggerThreadStart.HasValue ||
                        _lastTriggerThreadStart.Value.AddMinutes(c_maxBlockTriggerAgeInMinutes) < DateTime.UtcNow)
                    {
                        using (Logging?.BeginScope("IncreaseTriggerCount"))
                        {
                            Logging?.LogWarning("Trigger count has reached maximum of {maxtriggercount}, but last of these triggers was started more than {maxblockingminutes} minutes ago, will therefore reset trigger count", c_maxTriggerThreadCount, c_maxBlockTriggerAgeInMinutes);
                            _lastTriggerThreadStart = DateTime.UtcNow;
                            _triggerThreadCount = 1;
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    _triggerThreadCount++;
                    _lastTriggerThreadStart = DateTime.UtcNow;
                    return true;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_TriggerThreadCount);
                }
            }
        }

        public static void DecreaseTriggerCount()
        {
            bool lockTaken = Monitor.TryEnter(lock_TriggerThreadCount, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_TriggerThreadCount = newLock;
                    lockTaken = true;
                }

                _triggerThreadCount--;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_TriggerThreadCount);
                }
            }
        }

        public static List<TriggerHistoryEntry> TriggerHistory
        {
            get
            {
                bool lockTaken = Monitor.TryEnter(lock_TriggerHistory, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_TriggerHistory = newLock;
                        lockTaken = true;
                    }

                    List<TriggerHistoryEntry>? lstCopy =
                        JsonSerializer.Deserialize<List<TriggerHistoryEntry>>(JsonSerializer.Serialize(_lstTriggerHistory));

                    return lstCopy ?? [];
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_TriggerHistory);
                    }
                }

            }
        }

        public static List<PanicHistoryEntry> PanicHistory
        {
            get
            {
                bool lockTaken = Monitor.TryEnter(lock_PanicHistory, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_PanicHistory = newLock;
                        lockTaken = true;
                    }

                    List<PanicHistoryEntry>? lstCopy =
                        JsonSerializer.Deserialize<List<PanicHistoryEntry>>(JsonSerializer.Serialize(_lstPanicHistory));

                    return lstCopy ?? [];
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_PanicHistory);
                    }
                }

            }
        }

        public static List<ControllerStatusHistoryEntry> ControllerStatusHistory
        {
            get
            {
                bool lockTaken = Monitor.TryEnter(lock_ControllerStatusHistory, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_ControllerStatusHistory = newLock;
                        lockTaken = true;
                    }

                    List<ControllerStatusHistoryEntry>? lstCopy =
                        JsonSerializer.Deserialize<List<ControllerStatusHistoryEntry>>(JsonSerializer.Serialize(_lstControllerStatusHistory));

                    return lstCopy ?? [];
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_ControllerStatusHistory);
                    }
                }

            }
        }

        public static ConfigFile? Config
        {
            get
            {
                bool lockTaken = Monitor.TryEnter(lock_Config, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_Config = newLock;
                        lockTaken = true;
                    }

                    return _config;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_Config);
                    }
                }
            }
        }

        public static MainController? Controller
        {
            get
            {
                bool lockTaken = Monitor.TryEnter(lock_MainController, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_MainController = newLock;
                        lockTaken = true;
                    }

                    return _mainController;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_MainController);
                    }
                }
            }
        }

        #endregion

        #region Public static methods

        public static void SetCountdownInfo(CountdownData countdownInfo, int correctBySeconds, bool markAsInformationChange = false)
        {
            CountdownInfo = countdownInfo.GetCopy(correctBySeconds);
            if (markAsInformationChange)
                MarkAsInformationChange();
        }

        public static void RemoveCountdownInfo()
        {
            using (Logging?.BeginScope("RemoveCountdownInfo"))
            {
                Logging?.LogDebug("Resetting countdown information");
                CountdownInfo = null;
            }
        }

        public static void SetMainController(MainController controller)
        {
            bool lockTaken = Monitor.TryEnter(lock_MainController, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_MainController = newLock;
                    lockTaken = true;
                }

                if (_mainController != null)
                    throw new ControllerException("A controller has already been created");

                _mainController = controller;
                MarkAsInformationChange();
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_MainController);
                }
            }
        }

        public static void SetConfig(ConfigFile config)
        {
            bool lockTaken = Monitor.TryEnter(lock_Config, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_Config = newLock;
                    lockTaken = true;
                }

                if (_config != null)
                    throw new ControllerException("A configuration has already been loaded");

                _config = config;
                MarkAsInformationChange();
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_Config);
                }
            }
        }

        public static void RemoveConfig()
        {
            bool lockTaken = Monitor.TryEnter(lock_Config, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_Config = newLock;
                    lockTaken = true;
                }

                _config = null;
                MarkAsInformationChange();
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_Config);
                }
            }
        }

        public static void RemoveMainController()
        {
            bool lockTaken = Monitor.TryEnter(lock_MainController, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_MainController = newLock;
                    lockTaken = true;
                }

                _mainController = null;
                MarkAsInformationChange();
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_MainController);
                }
            }
        }

        public static void EnterSafeMode(bool includeRemoteFiles)
        {
            if (!(_mainController?.IsInSafeMode ?? false) ||
                (includeRemoteFiles && !(_mainController?.AreRemoteFilesInSafeMode ?? false)))
            {
                _mainController?.EnterSafeMode(includeRemoteFiles);
            }
            else
            {
                Logging?.LogTrace("Skipping switching into safe mode as we already are in safe mode" + (includeRemoteFiles ? " (including remotes)" : ""));
            }
        }

        public static bool RequestTriggerEntry(TriggeredType type, string name)
        {
            bool lockTaken = Monitor.TryEnter(locks_dicTriggeredList[type], c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    locks_dicTriggeredList[type] = newLock;
                    lockTaken = true;
                }

                if (!_dicTriggeredLists[type].Contains(name))
                {
                    // entity has not yet been triggered
                    _dicTriggeredLists[type].Add(name); // register it as triggered
                    return true;
                }
                else
                {
                    // entity has already been triggered
                    return false;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(locks_dicTriggeredList[type]);
                }
            }
        }

        public static void InitHistories()
        {
            ShutDownTimestamp = null;
            InitStatusHistory();
            MarkAsInformationChange();
        }

        public static void Reset()
        {
            using (Logging?.BeginScope("Reset"))
            {
                Logging?.LogInformation("Resetting all shared data");
                RemoveCountdownInfo();
                ResetTriggeredLists();
                ResetAllHistories();
                MarkAsInformationChange();
            }
        }

        public static void AddTriggerHistory(TriggerHistoryEntry historyEntity)
        {
            using (Logging?.BeginScope("AddTriggerHistory"))
            {
                bool lockTaken = Monitor.TryEnter(lock_TriggerHistory, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_TriggerHistory = newLock;
                        lockTaken = true;
                    }

                    if (_lstTriggerHistory.Count < c_maxTriggerHistory)
                    {
                        // only add to our history if we're not exceeding the list size
                        Logging?.LogDebug("Adding to trigger history... type: {triggertype}; name: {triggername}; timestamp: {timestamp}; outcome: {outcome}",
                                            historyEntity.Type, historyEntity.Name, historyEntity.UtcTimestamp, historyEntity.Outcome);
                        _lstTriggerHistory.Add(historyEntity);
                        MarkAsInformationChange();
                    }
                    else
                    {
                        Logging?.LogWarning("Not adding the following to trigger history as list already exceeds {maxnumber} number of elements... type: {triggertype}; name: {triggername}; timestamp: {timestamp}; outcome: {outcome}",
                                            c_maxTriggerHistory, historyEntity.Type, historyEntity.Name, historyEntity.UtcTimestamp, historyEntity.Outcome);
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_TriggerHistory);
                    }
                }
            }
        }

        public static void AddPanicHistory(UniversalPanicType type, string? id, UniversalPanicReason reason, List<string>? triggerConfigs)
        {
            PanicHistoryEntry phe = new(type, id, reason, triggerConfigs);
            phe.AddToHistory();
        }

        public static void AddPanicHistory(PanicHistoryEntry historyEntity)
        {
            using (Logging?.BeginScope("AddPanicHistory"))
            {
                bool lockTaken = Monitor.TryEnter(lock_PanicHistory, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_PanicHistory = newLock;
                        lockTaken = true;
                    }

                    if (_lstPanicHistory.Count < c_maxPanicHistory)
                    {
                        // only add to our history if we're not exceeding the list size
                        Logging?.LogDebug("Adding to panic history... switch type: {triggertype}; switch name/id: {triggerid}; timestamp: {timestamp}; panic reason: {outcome}",
                                            historyEntity.SwitchType, historyEntity.Id, historyEntity.UtcTimestamp, historyEntity.PanicReason);
                        _lstPanicHistory.Add(historyEntity);
                        historyEntity.CountNumber = _lstPanicHistory.Count;
                        HasPanic = true;
                        MarkAsInformationChange();
                        // Set the write file state to panic since we didn't trigger the system shutdown via countdown
                        SharedData.Controller?.UpdateWriteFileState(Entities.WatcherFileState.Panic);
                    }
                    else
                    {
                        // also don't set panic as that should already have happened
                        Logging?.LogWarning("Not adding the following to panic history as list already exceeds {maxnumber} number of elements... switch type: {triggertype}; switch name/id: {triggerid}; timestamp: {timestamp}; panic reason: {outcome}",
                                            c_maxPanicHistory, historyEntity.SwitchType, historyEntity.Id, historyEntity.UtcTimestamp, historyEntity.PanicReason);
                    }

                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_PanicHistory);
                    }
                }
            }
        }

        public static bool AddControllerStatusHistory(ControllerStatus status, bool safeModeIncludesRemoteFiles = false, bool triggerIsCountdownT0 = false)
        {
            ControllerStatusHistoryEntry cshe = new(status, safeModeIncludesRemoteFiles, triggerIsCountdownT0);
            return cshe.AddToHistory();
        }

        public static bool AddControllerStatusHistory(ControllerStatusHistoryEntry historyEntity)
        {
            using (Logging?.BeginScope("AddControllerStatusHistory"))
            {
                bool hasChange = false;
                bool lockTaken = Monitor.TryEnter(lock_ControllerStatusHistory, c_DefaultLockTimeoutInMilliseconds);

                try
                {
                    if (!lockTaken)
                    {
                        object newLock = new();
                        Monitor.Enter(newLock);
                        lock_ControllerStatusHistory = newLock;
                        lockTaken = true;
                    }

                    if (_lstControllerStatusHistory.Count < c_maxControllerStatusHistory)
                    {
                        // only add to our history if we're not exceeding the list size
                        Logging?.LogDebug("Adding to status history... status: {triggertype}; timestamp: {timestamp};",
                                            historyEntity.Status, historyEntity.UtcTimestamp);
                        _lstControllerStatusHistory.Add(historyEntity);

                        MarkAsInformationChange();
                    }
                    else
                    {
                        Logging?.LogWarning("Not adding the following to status history as list already exceeds {maxnumber} number of elements... status: {triggertype}; timestamp: {timestamp};",
                                            c_maxControllerStatusHistory, historyEntity.Status, historyEntity.UtcTimestamp);
                    }


                    if (historyEntity.Status == ControllerStatus.ShutDown)
                    {
                        // do not update our current status to shutdown if we are triggered
                        if (CurrentControllerStatus != ControllerStatus.Triggered)
                        {
                            CurrentControllerStatus = historyEntity.Status;
                            hasChange = true;
                        }
                        else
                        {
                            Logging?.LogTrace("Ignoring change to shutdown state as we're already in triggered state");
                        }
                    }
                    else if (historyEntity.Status == ControllerStatus.SafeMode)
                    {
                        // do not update our current status to safemode if we are shutdown or triggered
                        if (CurrentControllerStatus != ControllerStatus.Triggered &&
                            CurrentControllerStatus != ControllerStatus.ShutDown)
                        {
                            CurrentControllerStatus = historyEntity.Status;
                            hasChange = true;
                        }
                        else
                        {
                            Logging?.LogTrace("Ignoring change to safemode state as we're already in triggered or shutdown state");
                        }
                    }
                    else
                    {
                        CurrentControllerStatus = historyEntity.Status;
                        hasChange = true;
                    }

                    if (hasChange)
                    {
                        if (historyEntity.Status == ControllerStatus.ShutDown || historyEntity.Status == ControllerStatus.Triggered)
                        {
                            if (ShutDownTimestamp == null)
                            {
                                ShutDownTimestamp = DateTime.UtcNow;
                                DateTime dtTimeOfDeath = ShutDownTimestamp.Value.AddMinutes(c_timeOfDeathMinutesAfterShutdown).ToLocalTime();
                                Logging?.LogInformation("!!!ANUBIS SYSTEM SHUTDOWN TRIGGERED!!! Timestamp (UTC): {shutdowntimestamp}; probable time of death (local time): {timeofdeath}", ShutDownTimestamp, dtTimeOfDeath);
                            }
                        }
                        else
                        {
                            if (ShutDownTimestamp != null)
                            {
                                ShutDownTimestamp = null;
                                Logging?.LogWarning("System shutdown timestamp has been reset due to status change into non-shutdown-status {newstatus} after having been in system shutdown status", historyEntity.Status);
                            }
                        }
                    }

                    return hasChange;
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lock_ControllerStatusHistory);
                    }
                }
            }
        }

        /// <summary>
        /// Will return true once for any information change other than just countdown time change
        /// (but countdown state changes, like emails triggered etc., will be marked as information change again).
        /// </summary>
        /// <param name="requester"></param>
        /// <returns></returns>
        /// <exception cref="SharedDataException"></exception>
        public static bool HasInformationChange(InformationRequester requester)
        {
            switch (requester)
            {
                case InformationRequester.Controller:
                    if (_hasInformationChange_forController)
                    {
                        _hasInformationChange_forController = false;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case InformationRequester.GUI:
                    if (_hasInformationChange_forGUI)
                    {
                        _hasInformationChange_forGUI = false;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                default:
                    throw new SharedDataException($"Unknown requester: {requester}");
            }
        }

        #endregion

        #region Private static helper methods

        private static void InitStatusHistory()
        {
            new ControllerStatusHistoryEntry(ControllerStatus.Stopped).AddToHistory();
        }

        private static void ResetTriggerConfigCount()
        {
            using (Logging?.BeginScope("ResetTriggerConfigCount"))
            {
                if (Config != null)
                {
                    foreach (var itm in Config.pollersAndTriggers.pollers)
                    {
                        if (itm is ClewareUSBPollerData usbpd)
                        {
                            foreach (var trg in usbpd.generalTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                            foreach (var trg in usbpd.switchTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                        }
                        else if (itm is FritzPollerData fritzpd)
                        {
                            foreach (var trg in fritzpd.generalTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                            foreach (var trg in fritzpd.switchTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                        }
                        else if (itm is SwitchBotPollerData sppd)
                        {
                            foreach (var trg in sppd.generalTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                            foreach (var trg in sppd.switchTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                        }
                        else if (itm is LocalFilePollerData lfpd)
                        {
                            foreach (var trg in lfpd.generalTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                            foreach (var trg in lfpd.fileTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                        }
                        else if (itm is RemoteFilePollerData rfpd)
                        {
                            foreach (var trg in rfpd.generalTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                            foreach (var trg in rfpd.fileTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                        }
                        else if (itm is CountdownPollerData cdpd)
                        {
                            foreach (var trg in cdpd.generalTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                            foreach (var trg in cdpd.countdownTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                        }
                        else if (itm is ControllerData ctpd)
                        {
                            foreach (var trg in ctpd.generalTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                            foreach (var trg in ctpd.pollerTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                        }
                        else if (itm is GeneralData gdpd)
                        {
                            foreach (var trg in gdpd.generalTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                            foreach (var trg in gdpd.fallbackTriggers)
                            {
                                trg.ResetRepeatCount();
                            }
                        }
                    }
                }
            }
        }

        private static void ResetTriggeredLists()
        {
            using (Logging?.BeginScope("ResetTriggeredLists"))
            {
                Logging?.LogDebug("Resetting lists of triggered configuration and switches");
                ResetTriggerEntry(TriggeredType.Config);
                ResetTriggerEntry(TriggeredType.ClewareUSBSwitch);
                ResetTriggerEntry(TriggeredType.FritzSwitch);
                ResetTriggerEntry(TriggeredType.SwitchBot);
                ResetTriggerEntry(TriggeredType.OSShutdown);
                ResetTriggerConfigCount();
            }
        }

        private static void ResetTriggerEntry(TriggeredType type)
        {
            bool lockTaken = Monitor.TryEnter(locks_dicTriggeredList[type], c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    locks_dicTriggeredList[type] = newLock;
                    lockTaken = true;
                }

                _dicTriggeredLists[type] = [];
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(locks_dicTriggeredList[type]);
                }
            }
        }

        private static void MarkAsInformationChange()
        {
            _hasInformationChange_forController = true;
            _hasInformationChange_forGUI = true;
        }

        private static void ResetAllHistories()
        {
            using (Logging?.BeginScope("ResetAllHistories"))
            {
                Logging?.LogDebug("Resetting all history lists");
                ShutDownTimestamp = null;
                ResetTriggerHistory();
                ResetPanicHistory();
                ResetControllerStatusHistory();
            }
        }

        private static void ResetTriggerHistory()
        {
            bool lockTaken = Monitor.TryEnter(lock_TriggerHistory, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_TriggerHistory = newLock;
                    lockTaken = true;
                }

                _lstTriggerHistory = [];
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_TriggerHistory);
                }
            }
        }

        public static void ResetPanicHistory()
        {
            bool lockTaken = Monitor.TryEnter(lock_PanicHistory, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_PanicHistory = newLock;
                    lockTaken = true;
                }

                _lstPanicHistory = [];
                HasPanic = false;
                MarkAsInformationChange();
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_PanicHistory);
                }
            }
        }

        private static void ResetControllerStatusHistory()
        {
            bool lockTaken = Monitor.TryEnter(lock_ControllerStatusHistory, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_ControllerStatusHistory = newLock;
                    lockTaken = true;
                }

                _lstControllerStatusHistory = [];
                CurrentControllerStatus = ControllerStatus.Stopped;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_ControllerStatusHistory);
                }
            }
        }

        public static List<TriggerHistoryEntry> GetTriggerHistory(InformationRequester requester, bool onlyNew = false)
        {
            bool lockTaken = Monitor.TryEnter(lock_TriggerHistory, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_TriggerHistory = newLock;
                    lockTaken = true;
                }

                List<TriggerHistoryEntry> lstRet = TriggerHistory;

                switch (requester)
                {
                    case InformationRequester.Controller:
                        _lstTriggerHistory.ForEach(itm => itm.NewForController = false);
                        if (onlyNew)
                        {
                            lstRet = lstRet.Where(itm => itm.NewForController).ToList();
                        }
                        break;
                    case InformationRequester.GUI:
                        _lstTriggerHistory.ForEach(itm => itm.NewForGUI = false);
                        if (onlyNew)
                        {
                            lstRet = lstRet.Where(itm => itm.NewForGUI).ToList();
                        }
                        break;
                    default:
                        throw new SharedDataException($"Unknown requester: {requester}");
                }

                return lstRet;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_TriggerHistory);
                }
            }
        }

        public static List<PanicHistoryEntry> GetPanicHistory(InformationRequester requester, bool onlyNew = false, bool onlyTriggering = false)
        {
            bool lockTaken = Monitor.TryEnter(lock_PanicHistory, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_PanicHistory = newLock;
                    lockTaken = true;
                }

                List<PanicHistoryEntry> lstRet = PanicHistory;

                switch (requester)
                {
                    case InformationRequester.Controller:
                        _lstPanicHistory.ForEach(itm => itm.NewForController = false);
                        if (onlyNew)
                        {
                            lstRet = lstRet.Where(itm => itm.NewForController).ToList();
                        }
                        break;
                    case InformationRequester.GUI:
                        _lstPanicHistory.ForEach(itm => itm.NewForGUI = false);
                        if (onlyNew)
                        {
                            lstRet = lstRet.Where(itm => itm.NewForGUI).ToList();
                        }
                        break;
                    default:
                        throw new SharedDataException($"Unknown requester: {requester}");
                }

                if (onlyTriggering)
                {
                    lstRet = lstRet.Where(itm => itm.TriggerConfigs.Count > 0).ToList();
                }

                return lstRet;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_PanicHistory);
                }
            }
        }

        public static List<ControllerStatusHistoryEntry> GetControllerStatusHistory(InformationRequester requester, bool onlyNew = false)
        {
            bool lockTaken = Monitor.TryEnter(lock_ControllerStatusHistory, c_DefaultLockTimeoutInMilliseconds);

            try
            {
                if (!lockTaken)
                {
                    object newLock = new();
                    Monitor.Enter(newLock);
                    lock_ControllerStatusHistory = newLock;
                    lockTaken = true;
                }

                List<ControllerStatusHistoryEntry> lstRet = ControllerStatusHistory;

                switch (requester)
                {
                    case InformationRequester.Controller:
                        _lstControllerStatusHistory.ForEach(itm => itm.NewForController = false);
                        if (onlyNew)
                        {
                            lstRet = lstRet.Where(itm => itm.NewForController).ToList();
                        }
                        break;
                    case InformationRequester.GUI:
                        _lstControllerStatusHistory.ForEach(itm => itm.NewForGUI = false);
                        if (onlyNew)
                        {
                            lstRet = lstRet.Where(itm => itm.NewForGUI).ToList();
                        }
                        break;
                    default:
                        throw new SharedDataException($"Unknown requester: {requester}");
                }

                return lstRet;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(lock_ControllerStatusHistory);
                }
            }
        }

        #endregion
    }
}
