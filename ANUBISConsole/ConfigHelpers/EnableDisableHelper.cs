using ANUBISWatcher.Configuration.ConfigFileData;
using ANUBISWatcher.Shared;
using Spectre.Console;

namespace ANUBISConsole.ConfigHelpers
{
    public enum DeviceEntityLevelType
    {
        Root,
        Group,
        Device,
    }

    public enum DeviceEntityGroupType
    {
        TurnOn,
        Poller,
        Trigger,
    }

    public enum DeviceEntityType
    {
        None,
        Unknown,
        Fritz,
        SwitchBot,
        ClewareUSB,
        OSShutdownTrigger,
        DelayTrigger,
        RemoteReadFile,
        LocalWriteFile,
    }

    public class DeviceEntity
    {
        public DeviceEntityLevelType LevelType { get; set; }
        public DeviceEntityGroupType GroupType { get; init; }
        public DeviceEntityType DeviceType { get; init; }
        public string DeviceName { get; init; }
        public bool Discovered { get; set; }

        public bool Enabled
        {
            get
            {
                if (Device_TurnOn != null)
                {
                    return Device_TurnOn.enabled;
                }
                else if (Device_Poller != null)
                {
                    return Device_Poller.enabled;
                }
                else if (Device_TriggerConfig != null)
                {
                    return Device_TriggerConfig.enabled;
                }
                else if (Device_Poller_Fritz != null)
                {
                    return Device_Poller_Fritz.enabled;
                }
                else if (Device_Poller_SwitchBot != null)
                {
                    return Device_Poller_SwitchBot.enabled;
                }
                else if (Device_Poller_ClewareUSB != null)
                {
                    return Device_Poller_ClewareUSB.enabled;
                }
                else if (Device_Poller_File != null)
                {
                    return Device_Poller_File.enabled;
                }
                else if (Device_Trigger != null)
                {
                    return Device_Trigger.enabled;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (Device_Poller != null)
                {
                    Device_Poller.enabled = value;
                }
                else if (Device_TriggerConfig != null)
                {
                    if (value != Device_TriggerConfig.enabled)
                        Device_TriggerConfig.disabledByDiscoverer = false;
                    Device_TriggerConfig.enabled = value;
                }
                else if (Device_Poller_Fritz != null)
                {
                    if (value != Device_Poller_Fritz.enabled)
                        Device_Poller_Fritz.disabledByDiscoverer = false;
                    Device_Poller_Fritz.enabled = value;
                }
                else if (Device_Poller_SwitchBot != null)
                {
                    if (value != Device_Poller_SwitchBot.enabled)
                        Device_Poller_SwitchBot.disabledByDiscoverer = false;
                    Device_Poller_SwitchBot.enabled = value;
                }
                else if (Device_Poller_ClewareUSB != null)
                {
                    if (value != Device_Poller_ClewareUSB.enabled)
                        Device_Poller_ClewareUSB.disabledByDiscoverer = false;
                    Device_Poller_ClewareUSB.enabled = value;
                }
                else if (Device_Poller_File != null)
                {
                    if (value != Device_Poller_File.enabled)
                        Device_Poller_File.disabledByDiscoverer = false;
                    Device_Poller_File.enabled = value;
                }
                else if (Device_Trigger != null)
                {
                    if (Device_Trigger is SingleDeviceTrigger sdt)
                    {
                        if (value != Device_Trigger.enabled)
                            sdt.disabledByDiscoverer = false;
                    }
                    Device_Trigger.enabled = value;
                }
                else if (Device_TurnOn != null)
                {
                    if (value != Device_TurnOn.enabled)
                        Device_TurnOn.disabledByDiscoverer = false;
                    Device_TurnOn.enabled = value;
                }
                else
                {
                    if (DeviceType != DeviceEntityType.None)
                    {
                        throw new Exception($"Cannot enable unkown device type {DeviceType}");
                    }
                }
            }
        }

        public TurnOnEntity? Device_TurnOn { get; set; }
        public BasePollerData? Device_Poller { get; set; }
        public TriggerConfig? Device_TriggerConfig { get; set; }
        public FritzSwitchConfigOptions? Device_Poller_Fritz { get; set; }
        public SwitchBotSwitchConfigOptions? Device_Poller_SwitchBot { get; set; }
        public ClewareUSBSwitchConfigOptions? Device_Poller_ClewareUSB { get; set; }
        public FileConfigOptions? Device_Poller_File { get; set; }
        public SingleTrigger? Device_Trigger { get; set; }

        public List<DeviceEntity>? Children { get; set; } = null;

        public List<string> DiscoveryProblems { get; set; } = [];

        public override string ToString()
        {
            string prefix = "";
            string postfix = "";

            if (!Discovered)
            {
                prefix += "[italic]";
                postfix = "[/]";
            }

            return prefix + DeviceName.EscapeMarkup() + postfix;
        }

        public string ToString(bool enabledInOkColor)
        {
            string strRetVal = ToString();

            if (enabledInOkColor)
            {
                if (Enabled)
                {
                    strRetVal = $"[{AnubisOptions.Options.defaultColor_Ok}]{strRetVal}[/]";
                }
            }

            return strRetVal;
        }

        public void Select()
        {
            Enabled = true;
        }

        public void Unselect()
        {
            Enabled = false;
        }

        public List<DeviceEntity> GetSelectedChildren(bool all = true)
        {
            if (all)
            {
                List<DeviceEntity> lstSelectedChildren = Children?.Where(itm => itm.Enabled)?.ToList() ?? [];
                List<DeviceEntity> lstSelectedNextLevelChildren = Children?.SelectMany(itm => itm.GetSelectedChildren(true))?.ToList() ?? [];
                if (LevelType == DeviceEntityLevelType.Root && GroupType == DeviceEntityGroupType.TurnOn)
                {
                    if (Children?.All(itm => itm.Enabled) ?? false)
                    {
                        lstSelectedChildren.Add(this);
                    }
                }
                lstSelectedChildren.AddRange(lstSelectedNextLevelChildren);

                return lstSelectedChildren;
            }
            else
            {
                return Children?.Where(itm => itm.Enabled)?.ToList() ?? [];
            }
        }

        public List<DeviceEntity> GetAllChildren()
        {
            List<DeviceEntity> lstChildren = Children ?? [];
            List<DeviceEntity> lstNextLevelChildren = Children?.SelectMany(itm => itm.GetAllChildren())?.ToList() ?? [];
            lstChildren.AddRange(lstNextLevelChildren);

            return lstChildren;
        }

        public List<DeviceEntity> SetSelected(List<DeviceEntity>? selected)
        {
            List<DeviceEntity> lstAll = GetAllChildren();

            if (selected != null)
            {
                lstAll.ForEach(itm => itm.Unselect());
                selected.ForEach(itm => itm.Select());
            }

            return lstAll;
        }

        public TreeNode AddTreeNode(TreeNode parent)
        {
            var node = parent.AddNode(ToString(true));
            if (DiscoveryProblems.Count > 0)
            {
                var ndProblems = node.AddNode("Problems discovering device...");
                DiscoveryProblems.ForEach(itm => ndProblems.AddNode($"[{AnubisOptions.Options.defaultColor_Warning}]{itm.EscapeMarkup()}[/]"));
            }
            Children?.ForEach(itm => itm.AddTreeNode(node));
            return node;
        }

        public TreeNode AddTreeNode(Tree parent)
        {
            var node = parent.AddNode(ToString(true));
            if (DiscoveryProblems.Count > 0)
            {
                var ndProblems = node.AddNode("Problems discovering device...");
                DiscoveryProblems.ForEach(itm => ndProblems.AddNode($"[{AnubisOptions.Options.defaultColor_Warning}]{itm.EscapeMarkup()}[/]"));
            }
            Children?.ForEach(itm => itm.AddTreeNode(node));
            return node;
        }

        public MultiSelectionPrompt<DeviceEntity> AddChoiceGroup(MultiSelectionPrompt<DeviceEntity> multiSelection)
        {
            if (LevelType == DeviceEntityLevelType.Root)
            {
                if (GroupType == DeviceEntityGroupType.TurnOn)
                {
                    multiSelection.AddChoiceGroup(this, Children ?? []);
                }
                else
                {
                    Children?.ForEach(itm => itm.AddChoiceGroup(multiSelection));
                }
                GetSelectedChildren().ForEach(itm => multiSelection.Select(itm));
            }
            else if (LevelType == DeviceEntityLevelType.Group)
            {
                multiSelection.AddChoiceGroup(this, Children ?? []);
            }
            else
            {
                //Children?.ForEach(itm => multiSelection.AddChoice(this));
            }

            return multiSelection;
        }

        #region Constructors
        public DeviceEntity(DeviceEntityLevelType level, DeviceEntityGroupType groupType, string name)
            : this(level, groupType, DeviceEntityType.None, name)
        {
        }

        public DeviceEntity(DeviceEntityLevelType level, DeviceEntityGroupType groupType, DeviceEntityType deviceType, string name)
        {
            LevelType = level;
            GroupType = groupType;
            DeviceType = deviceType;
            Discovered = true;

            if (LevelType == DeviceEntityLevelType.Group && GroupType == DeviceEntityGroupType.Trigger)
            {
                DeviceName = $"Trigger: {name}";
            }
            else if (LevelType == DeviceEntityLevelType.Group && GroupType == DeviceEntityGroupType.Poller)
            {
                DeviceName = $"Poller: {name}";
            }
            else
            {
                DeviceName = name;
            }
        }

        public DeviceEntity(TurnOnEntity device)
        {
            Device_TurnOn = device;
            LevelType = DeviceEntityLevelType.Device;
            GroupType = DeviceEntityGroupType.TurnOn;
            Discovered = device.discovered;
            DiscoveryProblems = device.discoveryProblem;
            DeviceName = device.id ?? "<null>";

            if (device is DeviceTurnOn dvcto)
            {
                switch (dvcto.deviceType)
                {
                    case TriggerType.Fritz:
                        DeviceType = DeviceEntityType.Fritz;
                        break;
                    case TriggerType.SwitchBot:
                        DeviceType = DeviceEntityType.SwitchBot;
                        break;
                    case TriggerType.ClewareUSB:
                        DeviceType = DeviceEntityType.ClewareUSB;
                        break;
                }
            }
        }

        public DeviceEntity(BasePollerData poller)
        {
            Device_Poller = poller;
            LevelType = DeviceEntityLevelType.Group;
            GroupType = DeviceEntityGroupType.Poller;
            Discovered = true;
            DeviceName = "Poller: ";

            if (poller is SwitchBotPollerData)
            {
                DeviceType = DeviceEntityType.SwitchBot;
                DeviceName += "SwitchBot";
            }
            else if (poller is FritzPollerData)
            {
                DeviceType = DeviceEntityType.Fritz;
                DeviceName += "Fritz";
            }
            else if (poller is ClewareUSBPollerData)
            {
                DeviceType = DeviceEntityType.ClewareUSB;
                DeviceName += "Cleware USB";
            }
            else if (poller is LocalFilePollerData)
            {
                DeviceType = DeviceEntityType.LocalWriteFile;
                DeviceName += "Local (write) files";
            }
            else if (poller is RemoteFilePollerData)
            {
                DeviceType = DeviceEntityType.RemoteReadFile;
                DeviceName += "Remote (read) files";
            }
        }

        public DeviceEntity(TriggerConfig triggerConfig)
        {
            Device_TriggerConfig = triggerConfig;
            LevelType = DeviceEntityLevelType.Group;
            GroupType = DeviceEntityGroupType.Trigger;
            Discovered = true;
            DeviceName = $"Trigger: {triggerConfig.id ?? "<null>"}";
            if (triggerConfig.isFallback)
            {
                DeviceName += " (fallback)";
            }
            DeviceType = DeviceEntityType.None;
        }

        public DeviceEntity(FritzSwitchConfigOptions device)
        {
            Device_Poller_Fritz = device;
            LevelType = DeviceEntityLevelType.Device;
            GroupType = DeviceEntityGroupType.Poller;
            DeviceType = DeviceEntityType.Fritz;
            DeviceName = device.switchName ?? "<null>";
            Discovered = device.discovered;
            DiscoveryProblems = device.discoveryProblem;
        }

        public DeviceEntity(SwitchBotSwitchConfigOptions device)
        {
            Device_Poller_SwitchBot = device;
            LevelType = DeviceEntityLevelType.Device;
            GroupType = DeviceEntityGroupType.Poller;
            DeviceType = DeviceEntityType.SwitchBot;
            DeviceName = device.switchName ?? "<null>";
            Discovered = device.discovered;
            DiscoveryProblems = device.discoveryProblem;
        }

        public DeviceEntity(ClewareUSBSwitchConfigOptions device)
        {
            Device_Poller_ClewareUSB = device;
            LevelType = DeviceEntityLevelType.Device;
            GroupType = DeviceEntityGroupType.Poller;
            DeviceType = DeviceEntityType.SwitchBot;
            DeviceName = device.usbSwitchName ?? "<null>";
            Discovered = device.discovered;
            DiscoveryProblems = device.discoveryProblem;
        }

        public DeviceEntity(FileConfigOptions device, bool isWriteFile)
        {
            Device_Poller_File = device;
            LevelType = DeviceEntityLevelType.Device;
            GroupType = DeviceEntityGroupType.Poller;
            if (isWriteFile)
            {
                DeviceType = DeviceEntityType.LocalWriteFile;
            }
            else
            {
                DeviceType = DeviceEntityType.RemoteReadFile;
            }
            DeviceName = device.name ?? "<null>";
            Discovered = device.discovered;
            DiscoveryProblems = device.discoveryProblem;
        }

        public DeviceEntity(SingleTrigger device)
        {
            Device_Trigger = device;
            LevelType = DeviceEntityLevelType.Device;
            GroupType = DeviceEntityGroupType.Trigger;

            if (device is SingleDeviceTrigger trg)
            {
                DeviceName = trg.id ?? "<null>";
                Discovered = trg.discovered;
                DiscoveryProblems = trg.discoveryProblem;
                switch (trg.deviceType)
                {
                    case TriggerType.ClewareUSB:
                        DeviceType = DeviceEntityType.ClewareUSB;
                        break;
                    case TriggerType.Fritz:
                        DeviceType = DeviceEntityType.Fritz;
                        break;
                    case TriggerType.SwitchBot:
                        DeviceType = DeviceEntityType.SwitchBot;
                        break;
                    case TriggerType.OSShutdown:
                        DeviceType = DeviceEntityType.OSShutdownTrigger;
                        break;
                    default:
                        DeviceType = DeviceEntityType.Unknown;
                        break;
                }
            }
            else if (device is SingleDelayTrigger dltrg)
            {
                DeviceName = $"delay {dltrg.milliseconds}ms";
                Discovered = true;
                DeviceType = DeviceEntityType.DelayTrigger;
            }
            else
            {
                DeviceName = "<unknown>";
                Discovered = false;
                DiscoveryProblems = ["unknown type"];
                DeviceType = DeviceEntityType.Unknown;
            }
        }
        #endregion
    }

    public class EnableDisableHelper
    {
        public static DeviceEntity GetTurnOnDevices()
        {
            return GetTurnOnDevices(SharedData.Config);
        }

        public static DeviceEntity GetTurnOnDevices(ConfigFile? configFile)
        {
            DeviceEntity dvcRoot =
                new(DeviceEntityLevelType.Root, DeviceEntityGroupType.TurnOn, "Devices to turn on")
                {
                    Children = []
                };

            if (configFile != null)
            {
                foreach (var dvc in configFile.turnOn)
                {
                    dvcRoot.Children.Add(new DeviceEntity(dvc));
                }
            }

            return dvcRoot;
        }

        public static DeviceEntity GetPollerDevices()
        {
            return GetPollerDevices(SharedData.Config);
        }

        public static DeviceEntity GetPollerDevices(ConfigFile? configFile)
        {
            DeviceEntity dvcRoot = new(DeviceEntityLevelType.Root, DeviceEntityGroupType.Poller, "Devices in pollers")
            {
                Children = []
            };

            if (configFile != null)
            {
                foreach (var poller in configFile.pollersAndTriggers.pollers)
                {
                    DeviceEntity dvcPoller = new(poller)
                    {
                        Children = []
                    };

                    if (poller is FritzPollerData plrFritz)
                    {
                        foreach (var dvc in plrFritz.switches)
                        {
                            dvcPoller.Children.Add(new DeviceEntity(dvc));
                        }
                        dvcRoot.Children.Add(dvcPoller);
                    }
                    else if (poller is SwitchBotPollerData plrSwitchBot)
                    {
                        foreach (var dvc in plrSwitchBot.switches)
                        {
                            dvcPoller.Children.Add(new DeviceEntity(dvc));
                        }
                        dvcRoot.Children.Add(dvcPoller);
                    }
                    else if (poller is ClewareUSBPollerData plrCleware)
                    {
                        foreach (var dvc in plrCleware.switches)
                        {
                            dvcPoller.Children.Add(new DeviceEntity(dvc));
                        }
                        dvcRoot.Children.Add(dvcPoller);
                    }
                    else if (poller is LocalFilePollerData plrLocalFile)
                    {
                        foreach (var dvc in plrLocalFile.files)
                        {
                            dvcPoller.Children.Add(new DeviceEntity(dvc, true));
                        }
                        dvcRoot.Children.Add(dvcPoller);
                    }
                    else if (poller is RemoteFilePollerData plrRemoteFile)
                    {
                        foreach (var dvc in plrRemoteFile.files)
                        {
                            dvcPoller.Children.Add(new DeviceEntity(dvc, false));
                        }
                        dvcRoot.Children.Add(dvcPoller);
                    }
                }
            }

            return dvcRoot;
        }

        public static DeviceEntity GetTriggerDevices()
        {
            return GetTriggerDevices(SharedData.Config);
        }

        public static DeviceEntity GetTriggerDevices(ConfigFile? configFile)
        {
            DeviceEntity dvcRoot = new(DeviceEntityLevelType.Root, DeviceEntityGroupType.Trigger, "Devices in triggers")
            {
                Children = []
            };

            if (configFile != null)
            {
                foreach (var trgcfg in configFile.pollersAndTriggers.triggerConfigs)
                {
                    DeviceEntity dvcTriggerConfig = new(trgcfg)
                    {
                        Children = []
                    };
                    dvcRoot.Children.Add(dvcTriggerConfig);

                    foreach (var trg in trgcfg.triggers)
                    {
                        dvcTriggerConfig.Children.Add(new DeviceEntity(trg));
                    }
                }
            }

            return dvcRoot;
        }

        public static List<DeviceEntity> GetAllDevices()
        {
            return GetAllDevices(SharedData.Config);
        }

        public static List<DeviceEntity> GetAllDevices(ConfigFile? configFile)
        {
            return [GetTurnOnDevices(configFile), GetPollerDevices(configFile), GetTriggerDevices(configFile)];
        }
    }
}
