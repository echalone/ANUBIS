using ANUBISClewareAPI;
using ANUBISFritzAPI;
using ANUBISSwitchBotAPI;
using ANUBISWatcher.Configuration.ConfigFileData;
using ANUBISWatcher.Entities;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher.Configuration.ConfigHelpers
{
    public class SwitchBotStatusInfo
    {
        public SwitchBotDevice Device { get; set; }
        public bool Found { get; set; }
        public bool? TurnedOnSuccessfully { get; set; }

        public SwitchBotStatusInfo(SwitchBotDevice device)
        {
            Device = device;
        }
    }

    public class FritzSwitchStatusInfo
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public bool FoundName { get; set; }
        public bool SwitchPresent { get; set; }
        public List<string> DiscoveryProblems { get; set; } = [];
    }

    public class DiscoveryInfo
    {
        public List<SwitchBotStatusInfo>? SwitchBots { get; set; } = null;
        public List<FritzSwitchStatusInfo>? FritzSwitches { get; set; } = null;
        public List<ClewareDeviceMappingInfo>? ClewareUSBSwitches { get; set; } = null;
        public List<ConfigFileData.FileConfigOptions>? RemoteReadFiles { get; set; } = null;
        public List<ConfigFileData.FileConfigOptions>? LocalWriteFiles { get; set; } = null;
    }

    public static class Discoverer
    {
        public static bool IsAny(string? id)
        {
            var idCheck = id?.Trim()?.ToLower();
            return string.IsNullOrWhiteSpace(idCheck) || idCheck == "*";
        }

        public static bool ContainsAny(string?[]? id)
        {
            return id?.Any(itm => IsAny(itm)) ?? true;
        }

        public static bool ContainsSameId(string? idFind, string?[]? idCompare, bool ignoreCase)
        {
            var opt = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            return idCompare?.Any(itm => itm == idFind || (itm?.Equals(idFind, opt) ?? false)) ?? false;
        }

        public static bool ContainsSameOrAnyId(string? idFind, string?[]? idCompare, bool ignoreCase)
        {
            var opt = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            return idCompare?.Any(itm => IsAny(itm) || itm == idFind || (itm?.Equals(idFind, opt) ?? false)) ?? true;
        }

        public static bool IsAny(UniversalPanicReason? panic)
        {
            return !panic.HasValue || panic == UniversalPanicReason.All;
        }

        public static bool ContainsAny(UniversalPanicReason[]? panic)
        {
            return panic?.Any(itm => IsAny(itm)) ?? true;
        }

        public static bool ContainsSamePanic(UniversalPanicReason? panicFind, UniversalPanicReason[]? panicCompare)
        {
            return panicCompare?.Any(itm => itm == panicFind) ?? false;
        }

        public static bool ContainsSameOrAnyPanic(UniversalPanicReason? panicFind, UniversalPanicReason[]? panicCompare)
        {
            return panicCompare?.Any(itm => IsAny(itm) || itm == panicFind) ?? true;
        }

        public static bool IsAny(SwitchBotPanicReason? panic)
        {
            return IsAny(panic != null ?
                            Generator.GetUniversalPanicReason(panic) :
                            null);
        }

        public static bool ContainsAny(SwitchBotPanicReason[]? panic)
        {
            return ContainsAny(Generator.GetUniversalPanicReasons(panic));
        }

        public static bool ContainsSamePanic(UniversalPanicReason? panicFind, SwitchBotPanicReason[]? panicCompare)
        {
            return ContainsSamePanic(panicFind, Generator.GetUniversalPanicReasons(panicCompare));
        }

        public static bool ContainsSameOrAnyPanic(UniversalPanicReason? panicFind, SwitchBotPanicReason[]? panicCompare)
        {
            return ContainsSameOrAnyPanic(panicFind, Generator.GetUniversalPanicReasons(panicCompare));
        }

        public static bool IsAny(FritzPanicReason? panic)
        {
            return IsAny(panic != null ?
                            Generator.GetUniversalPanicReason(panic) :
                            null);
        }

        public static bool ContainsAny(FritzPanicReason[]? panic)
        {
            return ContainsAny(Generator.GetUniversalPanicReasons(panic));
        }

        public static bool ContainsSamePanic(UniversalPanicReason? panicFind, FritzPanicReason[]? panicCompare)
        {
            return ContainsSamePanic(panicFind, Generator.GetUniversalPanicReasons(panicCompare));
        }

        public static bool ContainsSameOrAnyPanic(UniversalPanicReason? panicFind, FritzPanicReason[]? panicCompare)
        {
            return ContainsSameOrAnyPanic(panicFind, Generator.GetUniversalPanicReasons(panicCompare));
        }

        public static bool IsAny(ClewareUSBPanicReason? panic)
        {
            return IsAny(panic != null ?
                            Generator.GetUniversalPanicReason(panic) :
                            null);
        }

        public static bool ContainsAny(ClewareUSBPanicReason[]? panic)
        {
            return ContainsAny(Generator.GetUniversalPanicReasons(panic));
        }

        public static bool ContainsSamePanic(UniversalPanicReason? panicFind, ClewareUSBPanicReason[]? panicCompare)
        {
            return ContainsSamePanic(panicFind, Generator.GetUniversalPanicReasons(panicCompare));
        }

        public static bool ContainsSameOrAnyPanic(UniversalPanicReason? panicFind, ClewareUSBPanicReason[]? panicCompare)
        {
            return ContainsSameOrAnyPanic(panicFind, Generator.GetUniversalPanicReasons(panicCompare));
        }

        public static bool IsAny(WatcherFilePanicReason? panic)
        {
            return IsAny(panic != null ?
                            Generator.GetUniversalPanicReason(panic) :
                            null);
        }

        public static bool ContainsAny(WatcherFilePanicReason[]? panic)
        {
            return ContainsAny(Generator.GetUniversalPanicReasons(panic));
        }

        public static bool ContainsSamePanic(UniversalPanicReason? panicFind, WatcherFilePanicReason[]? panicCompare)
        {
            return ContainsSamePanic(panicFind, Generator.GetUniversalPanicReasons(panicCompare));
        }

        public static bool ContainsSameOrAnyPanic(UniversalPanicReason? panicFind, WatcherFilePanicReason[]? panicCompare)
        {
            return ContainsSameOrAnyPanic(panicFind, Generator.GetUniversalPanicReasons(panicCompare));
        }

        public static DiscoveryInfo DiscoverDevices(bool tryTurnOnSwitchBots)
        {
            return new DiscoveryInfo()
            {
                SwitchBots = DiscoverSwitchBotDevices(tryTurnOnSwitchBots),
                FritzSwitches = DiscoverFritzDevices(),
                ClewareUSBSwitches = DiscoverClewareUSBDevices(),
                LocalWriteFiles = DiscoverLocalWriteFiles(),
                RemoteReadFiles = DiscoverRemoteReadFiles(),
            };
        }

        public static void SetEnabledAccordingToDiscovered()
        {
            if (SharedData.Config != null)
            {
                SetEnabledAccordingToDiscovered(SharedData.Config);
            }
        }
        public static void SetEnabledAccordingToDiscovered(ConfigFile configFile)
        {
            SetEnabledAccordingToDiscoveredInPollerConfiguration(configFile);
            SetEnabledAccordingToDiscoveredInTriggerConfiguration(configFile);
            SetEnabledAccordingToDiscoveredInTurnOnConfiguration(configFile);
        }

        private static void SetEnabledAccordingToDiscoveredInPollerConfiguration(ConfigFile? configFile)
        {
            ILogger? logging = SharedData.ConfigLogging;

            using (logging?.BeginScope("SetEnabledAccordingToDiscoveredInPollerConfiguration"))
            {
                foreach (var exc in (configFile?.pollersAndTriggers?.pollers ?? []))
                {
                    if (exc is ClewareUSBPollerData excClewareUSB)
                    {
                        var switches = excClewareUSB.switches;

                        foreach (var swt in switches)
                        {
                            bool enabledBeforeDiscoverer = swt.enabled;
                            swt.enabled = (swt.disabledByDiscoverer || swt.enabled) && swt.discovered;
                            if (enabledBeforeDiscoverer)
                            {
                                swt.disabledByDiscoverer = !swt.enabled;
                            }
                            else if (swt.enabled)
                            {
                                swt.disabledByDiscoverer = false;
                            }
                        }
                    }
                    else if (exc is FritzPollerData excFritz)
                    {
                        var switches = excFritz.switches;

                        foreach (var swt in switches)
                        {
                            bool enabledBeforeDiscoverer = swt.enabled;
                            swt.enabled = (swt.disabledByDiscoverer || swt.enabled) && swt.discovered;
                            if (enabledBeforeDiscoverer)
                            {
                                swt.disabledByDiscoverer = !swt.enabled;
                            }
                            else if (swt.enabled)
                            {
                                swt.disabledByDiscoverer = false;
                            }
                        }
                    }
                    else if (exc is SwitchBotPollerData excSwitchBot)
                    {
                        var switches = excSwitchBot.switches;

                        foreach (var swt in switches)
                        {
                            bool enabledBeforeDiscoverer = swt.enabled;
                            swt.enabled = (swt.disabledByDiscoverer || swt.enabled) && swt.discovered;
                            if (enabledBeforeDiscoverer)
                            {
                                swt.disabledByDiscoverer = !swt.enabled;
                            }
                            else if (swt.enabled)
                            {
                                swt.disabledByDiscoverer = false;
                            }
                        }
                    }
                    else if (exc is RemoteFilePollerData excRemoteFile)
                    {
                        var files = excRemoteFile.files;

                        foreach (var fil in files)
                        {
                            bool enabledBeforeDiscoverer = fil.enabled;
                            fil.enabled = (fil.disabledByDiscoverer || fil.enabled) && fil.discovered;
                            if (enabledBeforeDiscoverer)
                            {
                                fil.disabledByDiscoverer = !fil.enabled;
                            }
                            else if (fil.enabled)
                            {
                                fil.disabledByDiscoverer = false;
                            }
                        }
                    }
                    else if (exc is LocalFilePollerData excLocalFile)
                    {
                        var files = excLocalFile.files;

                        foreach (var fil in files)
                        {
                            bool enabledBeforeDiscoverer = fil.enabled;
                            fil.enabled = (fil.disabledByDiscoverer || fil.enabled) && fil.discovered;
                            if (enabledBeforeDiscoverer)
                            {
                                fil.disabledByDiscoverer = !fil.enabled;
                            }
                            else if (fil.enabled)
                            {
                                fil.disabledByDiscoverer = false;
                            }
                        }
                    }
                }
            }
        }

        private static void SetEnabledAccordingToDiscoveredInTriggerConfiguration(ConfigFile? configFile)
        {
            ILogger? logging = SharedData.ConfigLogging;

            using (logging?.BeginScope("SetEnabledAccordingToDiscoveredInTriggerConfiguration"))
            {
                foreach (var trgcfg in (configFile?.pollersAndTriggers?.triggerConfigs ?? []))
                {
                    bool blAnyEnabledTrigger = false;
                    foreach (var trg in trgcfg.triggers)
                    {
                        if (trg is SingleDeviceTrigger trgdvc)
                        {
                            bool enabledBeforeDiscoverer = trgdvc.enabled;
                            trgdvc.enabled = (trgdvc.disabledByDiscoverer || trgdvc.enabled) && trgdvc.discovered;
                            if (enabledBeforeDiscoverer)
                            {
                                trgdvc.disabledByDiscoverer = !trgdvc.enabled;
                            }
                            else if (trgdvc.enabled)
                            {
                                trgdvc.disabledByDiscoverer = false;
                            }
                            blAnyEnabledTrigger = blAnyEnabledTrigger || trgdvc.enabled;
                        }
                    }
                    bool enabledTrgCfgBeforeDiscoverer = trgcfg.enabled;
                    trgcfg.enabled = (trgcfg.disabledByDiscoverer || trgcfg.enabled) && blAnyEnabledTrigger;
                    if (enabledTrgCfgBeforeDiscoverer)
                    {
                        trgcfg.disabledByDiscoverer = !trgcfg.enabled;
                    }
                    else if (trgcfg.enabled)
                    {
                        trgcfg.disabledByDiscoverer = false;
                    }
                }
            }
        }

        private static void SetEnabledAccordingToDiscoveredInTurnOnConfiguration(ConfigFile? configFile)
        {
            ILogger? logging = SharedData.ConfigLogging;

            using (logging?.BeginScope("SetEnabledAccordingToDiscoveredInTurnOnConfiguration"))
            {
                foreach (var tondvc in (configFile?.turnOn ?? []))
                {
                    if (tondvc.disableIfNotDiscovered)
                    {
                        bool enabledBeforeDiscoverer = tondvc.enabled;
                        tondvc.enabled = (tondvc.disabledByDiscoverer || tondvc.enabled) && tondvc.discovered;
                        if (enabledBeforeDiscoverer)
                        {
                            tondvc.disabledByDiscoverer = !tondvc.enabled;
                        }
                        else if (tondvc.enabled)
                        {
                            tondvc.disabledByDiscoverer = false;
                        }
                    }
                }
            }
        }

        public static void SetDiscoveryInConfiguration(DiscoveryInfo discovererInfo)
        {
            SetDiscoveryInPollerConfiguration(discovererInfo);
            SetDiscoveryInTriggerConfiguration(discovererInfo);
            SetDiscoveryInTurnOnConfiguration(discovererInfo);
        }

        private static void SetDiscoveryInPollerConfiguration(DiscoveryInfo discovererInfo)
        {
            ILogger? logging = SharedData.ConfigLogging;

            using (logging?.BeginScope("SetDiscoveryInPollerConfiguration"))
            {
                foreach (var exc in (SharedData.Config?.pollersAndTriggers?.pollers ?? []))
                {
                    if (exc is ClewareUSBPollerData excClewareUSB)
                    {
                        var mappings = SharedData.Config?.clewareUSBMappings;
                        var switches = excClewareUSB.switches;

                        foreach (var swt in switches)
                        {
                            var info = discovererInfo.ClewareUSBSwitches?.FirstOrDefault(itm => itm.Name == swt.usbSwitchName);
                            swt.discoveryProblem = [];
                            if (info != null)
                            {
                                if (info.FoundId && info.FoundName)
                                {
                                    if (mappings?.Any(itm => itm.id == info.Id && itm.name == info.Name) ?? false)
                                    {
                                        swt.discovered = true;
                                    }
                                    else
                                    {
                                        swt.discovered = false;
                                        swt.discoveryProblem.Add(@$"Couldn't discover USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}""");
                                    }
                                }
                                else if (!info.FoundId && info.FoundName)
                                {
                                    swt.discovered = false;
                                    swt.discoveryProblem.Add(@$"Couldn't find USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}""");
                                }
                                else
                                {
                                    swt.discovered = false;
                                    swt.discoveryProblem.Add($@"Unexpected state for USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}"", id found value was: {info.FoundId} and name found value was: {info.FoundName}");
                                }
                            }
                            else
                            {
                                swt.discovered = false;
                                swt.discoveryProblem.Add(@$"Couldn't find USB Cleware mapping for switch with name ""{swt.usbSwitchName}""");
                            }
                        }
                    }
                    else if (exc is FritzPollerData excFritz)
                    {
                        var switches = excFritz.switches;

                        foreach (var swt in switches)
                        {
                            var dvc = discovererInfo.FritzSwitches?.FirstOrDefault(itm => itm.Name == swt.switchName);
                            swt.discoveryProblem = [];
                            if (dvc != null)
                            {
                                if (dvc.FoundName)
                                {
                                    if (dvc.SwitchPresent)
                                    {
                                        swt.discovered = true;
                                    }
                                    else
                                    {
                                        swt.discovered = false;
                                        swt.discoveryProblem = dvc.DiscoveryProblems;
                                    }
                                }
                                else
                                {
                                    swt.discovered = false;
                                    swt.discoveryProblem.Add(@$"Couldn't find Fritz switch with name ""{swt.switchName}"", although we should have found it");
                                }
                            }
                            else
                            {
                                swt.discovered = false;
                                swt.discoveryProblem.Add(@$"Couldn't find Fritz switch with name ""{swt.switchName}""");
                            }
                        }
                    }
                    else if (exc is SwitchBotPollerData excSwitchBot)
                    {
                        var switches = excSwitchBot.switches;

                        foreach (var swt in switches)
                        {
                            var dvc = discovererInfo.SwitchBots?.FirstOrDefault(itm => itm.Device.Name == swt.switchName);
                            swt.discoveryProblem = [];
                            if (dvc != null)
                            {
                                if (dvc.Device.CloudServiceEnabled)
                                {
                                    if (!string.IsNullOrWhiteSpace(dvc.Device.HubId))
                                    {
                                        if (dvc.Device.DeviceType == SwitchBotDeviceType.Bot)
                                        {
                                            if (dvc.Found)
                                            {
                                                if (dvc.TurnedOnSuccessfully ?? true)
                                                {
                                                    swt.discovered = true;
                                                }
                                                else
                                                {
                                                    swt.discovered = false;
                                                    swt.discoveryProblem.Add(@$"SwitchBot device with name ""{swt.switchName}"" could not be turned on, probably not detected or hub disconnected");
                                                }
                                            }
                                            else
                                            {
                                                swt.discovered = false;
                                                swt.discoveryProblem.Add(@$"SwitchBot device with name ""{swt.switchName}"" had unknown power, probably not detected");
                                            }
                                        }
                                        else
                                        {
                                            swt.discovered = false;
                                            swt.discoveryProblem.Add(@$"SwitchBot device with name ""{swt.switchName}"" of wrong type ""{dvc.Device.DeviceType}"", should be Bot");
                                        }
                                    }
                                    else
                                    {
                                        swt.discovered = false;
                                        swt.discoveryProblem.Add(@$"HubId of connected SwitchBot Hub not found for SwitchBot switch with name ""{swt.switchName}""");
                                    }
                                }
                                else
                                {
                                    swt.discovered = false;
                                    swt.discoveryProblem.Add(@$"Cloudservices not enabled for SwitchBot switch with name ""{swt.switchName}""");
                                }
                            }
                            else
                            {
                                swt.discovered = false;
                                swt.discoveryProblem.Add(@$"Couldn't find SwitchBot switch with name ""{swt.switchName}""");
                            }
                        }
                    }
                    else if (exc is RemoteFilePollerData excRemoteFile)
                    {
                        var files = excRemoteFile.files;

                        foreach (var fil in files)
                        {
                            var cfg = discovererInfo.RemoteReadFiles?.FirstOrDefault(itm => itm.name == fil.name);
                            fil.discoveryProblem = [];
                            if (cfg != null)
                            {
                                fil.discovered = cfg.discovered;
                                if (!fil.discovered)
                                {
                                    fil.discoveryProblem.Add(@$"Couldn't find remote file with name ""{fil.name}""");
                                }
                            }
                            else
                            {
                                fil.discovered = false;
                                fil.discoveryProblem.Add(@$"Couldn't find definition for remote file with name ""{fil.name}""");
                            }
                        }
                    }
                    else if (exc is LocalFilePollerData excLocalFile)
                    {
                        var files = excLocalFile.files;

                        foreach (var fil in files)
                        {
                            var cfg = discovererInfo.LocalWriteFiles?.FirstOrDefault(itm => itm.name == fil.name);
                            fil.discoveryProblem = [];
                            if (cfg != null)
                            {
                                fil.discovered = cfg.discovered;
                                if (!fil.discovered)
                                {
                                    fil.discoveryProblem.Add(@$"Couldn't find local file with name ""{fil.name}""");
                                }
                            }
                            else
                            {
                                fil.discovered = false;
                                fil.discoveryProblem.Add(@$"Couldn't find definition for local file with name ""{fil.name}""");
                            }
                        }
                    }
                }
            }
        }

        private static void SetDiscoveryInTriggerConfiguration(DiscoveryInfo discovererInfo)
        {
            ILogger? logging = SharedData.ConfigLogging;

            using (logging?.BeginScope("SetDiscoveryInTriggerConfiguration"))
            {
                foreach (var trgcfg in (SharedData.Config?.pollersAndTriggers?.triggerConfigs ?? []))
                {
                    foreach (var trg in trgcfg.triggers)
                    {
                        if (trg is SingleDeviceTrigger trgdvc)
                        {
                            if (trgdvc.deviceType == TriggerType.ClewareUSB)
                            {
                                var mappings = SharedData.Config?.clewareUSBMappings;
                                var info = discovererInfo.ClewareUSBSwitches?.FirstOrDefault(itm => itm.Name == trgdvc.id);
                                trgdvc.discoveryProblem = [];
                                if (info != null)
                                {
                                    if (info.FoundId && info.FoundName)
                                    {
                                        if (mappings?.Any(itm => itm.id == info.Id && itm.name == info.Name) ?? false)
                                        {
                                            trgdvc.discovered = true;
                                        }
                                        else
                                        {
                                            trgdvc.discovered = false;
                                            trgdvc.discoveryProblem.Add(@$"Couldn't discover USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}""");
                                        }
                                    }
                                    else if (!info.FoundId && info.FoundName)
                                    {
                                        trgdvc.discovered = false;
                                        trgdvc.discoveryProblem.Add(@$"Couldn't find USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}""");
                                    }
                                    else
                                    {
                                        trgdvc.discovered = false;
                                        trgdvc.discoveryProblem.Add($@"Unexpected state for USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}"", id found value was: {info.FoundId} and name found value was: {info.FoundName}");
                                    }
                                }
                                else
                                {
                                    trgdvc.discovered = false;
                                    trgdvc.discoveryProblem.Add(@$"Couldn't find USB Cleware mapping for switch with name ""{trgdvc.id}""");
                                }
                            }
                            else if (trgdvc.deviceType == TriggerType.SwitchBot)
                            {
                                var dvc = discovererInfo.SwitchBots?.FirstOrDefault(itm => itm.Device.Name == trgdvc.id);
                                trgdvc.discoveryProblem = [];
                                if (dvc != null)
                                {
                                    if (dvc.Device.CloudServiceEnabled)
                                    {
                                        if (!string.IsNullOrWhiteSpace(dvc.Device.HubId))
                                        {
                                            if (dvc.Device.DeviceType == SwitchBotDeviceType.Bot)
                                            {
                                                if (dvc.Found)
                                                {
                                                    if (dvc.TurnedOnSuccessfully ?? true)
                                                    {
                                                        trgdvc.discovered = true;
                                                    }
                                                    else
                                                    {
                                                        trgdvc.discovered = false;
                                                        trgdvc.discoveryProblem.Add(@$"SwitchBot device with name ""{trgdvc.id}"" could not be turned on, probably not detected or hub disconnected");
                                                    }
                                                }
                                                else
                                                {
                                                    trgdvc.discovered = false;
                                                    trgdvc.discoveryProblem.Add(@$"SwitchBot device with name ""{trgdvc.id}"" had unknown power, probably not detected");
                                                }
                                            }
                                            else
                                            {
                                                trgdvc.discovered = false;
                                                trgdvc.discoveryProblem.Add(@$"SwitchBot device with name ""{trgdvc.id}"" of wrong type ""{dvc.Device.DeviceType}"", should be Bot");
                                            }
                                        }
                                        else
                                        {
                                            trgdvc.discovered = false;
                                            trgdvc.discoveryProblem.Add(@$"HubId of connected SwitchBot Hub not found for SwitchBot switch with name ""{trgdvc.id}""");
                                        }
                                    }
                                    else
                                    {
                                        trgdvc.discovered = false;
                                        trgdvc.discoveryProblem.Add(@$"Cloudservices not enabled for SwitchBot switch with name ""{trgdvc.id}""");
                                    }
                                }
                                else
                                {
                                    trgdvc.discovered = false;
                                    trgdvc.discoveryProblem.Add(@$"Couldn't find SwitchBot switch with name ""{trgdvc.id}""");
                                }
                            }
                            else if (trgdvc.deviceType == TriggerType.Fritz)
                            {
                                var dvc = discovererInfo.FritzSwitches?.FirstOrDefault(itm => itm.Name == trgdvc.id);
                                trgdvc.discoveryProblem = [];
                                if (dvc != null)
                                {
                                    if (dvc.FoundName)
                                    {
                                        if (dvc.SwitchPresent)
                                        {
                                            trgdvc.discovered = true;
                                        }
                                        else
                                        {
                                            trgdvc.discovered = false;
                                            trgdvc.discoveryProblem = dvc.DiscoveryProblems;
                                        }
                                    }
                                    else
                                    {
                                        trgdvc.discovered = false;
                                        trgdvc.discoveryProblem.Add(@$"Couldn't find Fritz switch with name ""{trgdvc.id}"", although we should have found it");
                                    }
                                }
                                else
                                {
                                    trgdvc.discovered = false;
                                    trgdvc.discoveryProblem.Add(@$"Couldn't find Fritz switch with name ""{trgdvc.id}""");
                                }
                            }
                            else if (trgdvc.deviceType == TriggerType.OSShutdown)
                            {
                                trgdvc.discovered = true;
                            }
                            else
                            {
                                trgdvc.discovered = false;
                                trgdvc.discoveryProblem.Add($"Unknown deviceType {trgdvc.deviceType}");
                                logging?.LogError("Unknown deviceType {type} for trigger", trgdvc.deviceType);
                            }
                        }
                    }
                }
            }
        }

        private static void SetDiscoveryInTurnOnConfiguration(DiscoveryInfo discovererInfo)
        {
            ILogger? logging = SharedData.ConfigLogging;

            using (logging?.BeginScope("SetDiscoveryInTurnOnConfiguration"))
            {
                foreach (var entton in (SharedData.Config?.turnOn ?? []))
                {
                    if (entton is DeviceTurnOn tondvc)
                    {
                        if (tondvc.deviceType == TriggerType.ClewareUSB)
                        {
                            var mappings = SharedData.Config?.clewareUSBMappings;
                            var info = discovererInfo.ClewareUSBSwitches?.FirstOrDefault(itm => itm.Name == tondvc.id);
                            tondvc.discoveryProblem = [];
                            if (info != null)
                            {
                                if (info.FoundId && info.FoundName)
                                {
                                    if (mappings?.Any(itm => itm.id == info.Id && itm.name == info.Name) ?? false)
                                    {
                                        tondvc.discovered = true;
                                    }
                                    else
                                    {
                                        tondvc.discovered = false;
                                        tondvc.discoveryProblem.Add(@$"Couldn't discover USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}""");
                                    }
                                }
                                else if (!info.FoundId && info.FoundName)
                                {
                                    tondvc.discovered = false;
                                    tondvc.discoveryProblem.Add(@$"Couldn't find USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}""");
                                }
                                else
                                {
                                    tondvc.discovered = false;
                                    tondvc.discoveryProblem.Add($@"Unexpected state for USB Cleware switch with id ""{info.Id}"" and mapped name ""{info.Name}"", id found value was: {info.FoundId} and name found value was: {info.FoundName}");
                                }
                            }
                            else
                            {
                                tondvc.discovered = false;
                                tondvc.discoveryProblem.Add(@$"Couldn't find USB Cleware mapping for switch with name ""{tondvc.id}""");
                            }
                        }
                        else if (tondvc.deviceType == TriggerType.SwitchBot)
                        {
                            var dvc = discovererInfo.SwitchBots?.FirstOrDefault(itm => itm.Device.Name == tondvc.id);
                            tondvc.discoveryProblem = [];
                            if (dvc != null)
                            {
                                if (dvc.Device.CloudServiceEnabled)
                                {
                                    if (!string.IsNullOrWhiteSpace(dvc.Device.HubId))
                                    {
                                        if (dvc.Device.DeviceType == SwitchBotDeviceType.Bot)
                                        {
                                            if (dvc.Found)
                                            {
                                                if (dvc.TurnedOnSuccessfully ?? true)
                                                {
                                                    tondvc.discovered = true;
                                                }
                                                else
                                                {
                                                    tondvc.discovered = false;
                                                    tondvc.discoveryProblem.Add(@$"SwitchBot device with name ""{tondvc.id}"" could not be turned on, probably not detected or hub disconnected");
                                                }
                                            }
                                            else
                                            {
                                                tondvc.discovered = false;
                                                tondvc.discoveryProblem.Add(@$"SwitchBot device with name ""{tondvc.id}"" had unknown power, probably not detected");
                                            }
                                        }
                                        else
                                        {
                                            tondvc.discovered = false;
                                            tondvc.discoveryProblem.Add(@$"SwitchBot device with name ""{tondvc.id}"" of wrong type ""{dvc.Device.DeviceType}"", should be Bot");
                                        }
                                    }
                                    else
                                    {
                                        tondvc.discovered = false;
                                        tondvc.discoveryProblem.Add(@$"HubId of connected SwitchBot Hub not found for SwitchBot switch with name ""{tondvc.id}""");
                                    }
                                }
                                else
                                {
                                    tondvc.discovered = false;
                                    tondvc.discoveryProblem.Add(@$"Cloudservices not enabled for SwitchBot switch with name ""{tondvc.id}""");
                                }
                            }
                            else
                            {
                                tondvc.discovered = false;
                                tondvc.discoveryProblem.Add(@$"Couldn't find SwitchBot switch with name ""{tondvc.id}""");
                            }
                        }
                        else if (tondvc.deviceType == TriggerType.Fritz)
                        {
                            var dvc = discovererInfo.FritzSwitches?.FirstOrDefault(itm => itm.Name == tondvc.id);
                            tondvc.discoveryProblem = [];
                            if (dvc != null)
                            {
                                if (dvc.FoundName)
                                {
                                    if (dvc.SwitchPresent)
                                    {
                                        tondvc.discovered = true;
                                    }
                                    else
                                    {
                                        tondvc.discovered = false;
                                        tondvc.discoveryProblem = dvc.DiscoveryProblems;
                                    }
                                }
                                else
                                {
                                    tondvc.discovered = false;
                                    tondvc.discoveryProblem.Add(@$"Couldn't find Fritz switch with name ""{tondvc.id}"", although we should have found it");
                                }
                            }
                            else
                            {
                                tondvc.discovered = false;
                                tondvc.discoveryProblem.Add(@$"Couldn't find Fritz switch with name ""{tondvc.id}""");
                            }
                        }
                        else if (tondvc.deviceType == TriggerType.OSShutdown)
                        {
                            tondvc.discovered = false;
                            tondvc.discoveryProblem.Add($"Not allowed deviceType {tondvc.deviceType} for turn on configuration");
                            logging?.LogError("Unknown deviceType {type} for turn on configuration", tondvc.deviceType);
                        }
                        else
                        {
                            tondvc.discovered = false;
                            tondvc.discoveryProblem.Add($"Unknown deviceType {tondvc.deviceType}");
                            logging?.LogError("Unknown deviceType {type} for turn on configuration", tondvc.deviceType);
                        }
                    }
                    else if (entton is MessageTurnOn mto)
                    {
                        entton.discovered = true;
                        entton.discoveryProblem = [];
                    }
                    else
                    {
                        logging?.LogError("Unknown turn on type {type} for id {id}", entton.GetType().FullName, entton.id);
                    }
                }
            }
        }

        public static List<SwitchBotStatusInfo>? DiscoverSwitchBotDevices(bool tryTurnOn)
        {
            ILogger? logging = SharedData.ConfigLogging;

            using (logging?.BeginScope("DiscoverSwitchBotDevices"))
            {
                SwitchBotAPI? api = Generator.GetSwitchBotAPI(true, true);

                if (api != null)
                {
                    List<SwitchBotStatusInfo> lstRetVal = [];

                    var lstFound = api.GetSwitchBotList(false);
                    foreach (var switchBot in lstFound)
                    {
                        SwitchBotStatusInfo sbsi = new(switchBot);
                        if (!string.IsNullOrWhiteSpace(sbsi.Device.Id))
                        {
                            try
                            {
                                try
                                {
                                    SwitchBotStateContent state = api.GetSwitchBotState(sbsi.Device.Id);
                                    sbsi.Found = state.Power != SwitchBotPowerState.Unknown;

                                    if (tryTurnOn)
                                    {
                                        if (sbsi.Found)
                                        {
                                            var resp = api.TurnSwitchBotOn(sbsi.Device.Id);
                                            if (resp.Power == SwitchBotPowerState.On)
                                            {
                                                sbsi.TurnedOnSuccessfully = true;
                                            }
                                            else
                                            {
                                                logging?.LogError("Power state of SwitchBot with name \"{name}\" was {state} after tying to turn it on during discovery", switchBot.Name, resp.Power);
                                                sbsi.TurnedOnSuccessfully = false;
                                            }
                                        }
                                        else
                                        {
                                            sbsi.TurnedOnSuccessfully = false;
                                        }
                                    }
                                    else
                                    {
                                        sbsi.TurnedOnSuccessfully = null;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logging?.LogError(ex, "While trying to turn on SwitchBot with name \"{name}\" for discovery: {message}", switchBot.Name, ex.Message);
                                    sbsi.TurnedOnSuccessfully = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                logging?.LogError(ex, "While trying to get state for SwitchBot with name \"{name}\": {message}", switchBot.Name, ex.Message);
                                sbsi.Found = false;
                                if (tryTurnOn)
                                {
                                    sbsi.TurnedOnSuccessfully = false;
                                }
                                else
                                {
                                    sbsi.TurnedOnSuccessfully = null;
                                }
                            }
                        }
                        lstRetVal.Add(sbsi);
                    }

                    return lstRetVal;
                }
                else
                {
                    return null;
                }
            }
        }

        public static List<FritzSwitchStatusInfo>? DiscoverFritzDevices()
        {
            List<FritzSwitchStatusInfo> lstFritzSwitches = [];

            FritzAPI? api = Generator.GetFritzAPI();

            if (api != null)
            {
                Dictionary<string, string>? dicFritzSwitches = api.GetCachedSwitchNames();

                if (dicFritzSwitches != null)
                {
                    foreach (string strName in dicFritzSwitches.Keys)
                    {
                        var switchPresence = api.GetSwitchPresenceByName(strName);

                        FritzSwitchStatusInfo fssi = new()
                        {
                            Name = strName,
                            Id = dicFritzSwitches[strName],
                            FoundName = true,
                            SwitchPresent = switchPresence == SwitchPresence.Present,
                        };

                        if (switchPresence == SwitchPresence.Missing)
                        {
                            fssi.DiscoveryProblems.Add("Switch not present");
                        }
                        else if (switchPresence == SwitchPresence.Error)
                        {
                            fssi.DiscoveryProblems.Add("Error while trying to discover switch presence");
                        }
                        else if (switchPresence == SwitchPresence.NameNotFound)
                        {
                            fssi.DiscoveryProblems.Add("Name not found");
                        }
                        else if (switchPresence != SwitchPresence.Present)
                        {
                            fssi.DiscoveryProblems.Add($"Unknown problem: {switchPresence}");
                        }

                        lstFritzSwitches.Add(fssi);
                    }
                }
            }

            return lstFritzSwitches;
        }

        public static List<ClewareDeviceMappingInfo>? DiscoverClewareUSBDevices()
        {
            ClewareAPI? api = Generator.GetClewareAPI(true);

            if (api != null)
            {
                return api.GetUSBSwitchesNameIdMapping();
            }
            else
            {
                return null;
            }
        }

        public static List<ConfigFileData.FileConfigOptions>? DiscoverRemoteReadFiles()
        {
            List<ConfigFileData.FileConfigOptions>? lstFiles = Generator.GetRemoteReadFileOptions();

            lstFiles?.ForEach(itm => itm.discovered = File.Exists(itm.path));

            return lstFiles;
        }

        public static List<ConfigFileData.FileConfigOptions>? DiscoverLocalWriteFiles()
        {
            using (SharedData.ConfigLogging?.BeginScope("DiscoverLocalWriteFiles"))
            {
                List<ConfigFileData.FileConfigOptions>? lstFiles = Generator.GetLocalWriteFileOptions();

                if (lstFiles != null)
                {
                    foreach (ConfigFileData.FileConfigOptions file in lstFiles)
                    {
                        file.discovered = false;
                        if (!string.IsNullOrEmpty(file.path))
                        {
                            try
                            {
                                string? strDirectory = Path.GetDirectoryName(file.path);

                                if (strDirectory != null && !Path.Exists(strDirectory))
                                {
                                    SharedData.ConfigLogging?.LogTrace("Trying to create new directory \"{directory}\" for status file", strDirectory);
                                    Directory.CreateDirectory(strDirectory);
                                }

                                using (FileStream fs = new(file.path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                                {
                                    fs.Close();
                                }
                                file.discovered = true;
                            }
                            catch (Exception ex)
                            {
                                SharedData.ConfigLogging?.LogError(ex, @"While trying to create local file ""{path}"" for initializing: {message}", file.path, ex.Message);
                            }
                        }
                    }

                }

                return lstFiles;
            }
        }
    }
}
