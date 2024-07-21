using ANUBISWatcher.Configuration.Serialization;
using ANUBISWatcher.Entities;
using ANUBISWatcher.Shared;
using System.Text.RegularExpressions;

namespace ANUBISWatcher.Configuration.ConfigFileData
{
    public enum ExampleTypes
    {
        None,

        // PC1 - T0 min 2 hours - disabled SwitchBot Poller
        Real_PC1_T0min2hours_AirOld_NoSBPoller,
        Real_PC1_T0min2hours_AirNew_NoSBPoller,
        Real_PC1_T0min2hours_AirNone_NoSBPoller,
        // PC1 - T0 min 2 hours - enabled SwitchBot Poller
        Real_PC1_T0min2hours_AirOld,
        Real_PC1_T0min2hours_AirNew,
        Real_PC1_T0min2hours_AirNone,

        // PC2 - T0 min 2 hours - disabled SwitchBot Poller
        Real_PC2_T0min2hours_AirOld_NoSBPoller,
        Real_PC2_T0min2hours_AirNew_NoSBPoller,
        Real_PC2_T0min2hours_AirNone_NoSBPoller,
        // PC2 - T0 min 2 hours - enabled SwitchBot Poller
        Real_PC2_T0min2hours_AirOld,
        Real_PC2_T0min2hours_AirNew,
        Real_PC2_T0min2hours_AirNone,

        // PC1 - T0 min 1 hour - disabled SwitchBot Poller
        Real_PC1_T0min1hour_AirOld_NoSBPoller,
        Real_PC1_T0min1hour_AirNew_NoSBPoller,
        Real_PC1_T0min1hour_AirNone_NoSBPoller,
        // PC1 - T0 min 1 hour - enabled SwitchBot Poller
        Real_PC1_T0min1hour_AirOld,
        Real_PC1_T0min1hour_AirNew,
        Real_PC1_T0min1hour_AirNone,

        // PC2 - T0 min 1 hour - disabled SwitchBot Poller
        Real_PC2_T0min1hour_AirOld_NoSBPoller,
        Real_PC2_T0min1hour_AirNew_NoSBPoller,
        Real_PC2_T0min1hour_AirNone_NoSBPoller,
        // PC2 - T0 min 1 hour - enabled SwitchBot Poller
        Real_PC2_T0min1hour_AirOld,
        Real_PC2_T0min1hour_AirNew,
        Real_PC2_T0min1hour_AirNone,

        // PC1 - T0 in 30 mins - disabled SwitchBot Poller
        Real_PC1_T0in30mins_AirOld_NoSBPoller,
        Real_PC1_T0in30mins_AirNew_NoSBPoller,
        Real_PC1_T0in30mins_AirNone_NoSBPoller,
        // PC1 - T0 in 30 mins - enabled SwitchBot Poller
        Real_PC1_T0in30mins_AirOld,
        Real_PC1_T0in30mins_AirNew,
        Real_PC1_T0in30mins_AirNone,

        // PC2 - T0 in 30 mins - disabled SwitchBot Poller
        Real_PC2_T0in30mins_AirOld_NoSBPoller,
        Real_PC2_T0in30mins_AirNew_NoSBPoller,
        Real_PC2_T0in30mins_AirNone_NoSBPoller,
        // PC2 - T0 in 30 mins - enabled SwitchBot Poller
        Real_PC2_T0in30mins_AirOld,
        Real_PC2_T0in30mins_AirNew,
        Real_PC2_T0in30mins_AirNone,

        // PC1 - T0 in 10 mins - disabled SwitchBot Poller
        Real_PC1_T0in10mins_AirOld_NoSBPoller,
        Real_PC1_T0in10mins_AirNew_NoSBPoller,
        Real_PC1_T0in10mins_AirNone_NoSBPoller,
        // PC1 - T0 in 10 mins - enabled SwitchBot Poller
        Real_PC1_T0in10mins_AirOld,
        Real_PC1_T0in10mins_AirNew,
        Real_PC1_T0in10mins_AirNone,

        // PC2 - T0 in 10 mins - disabled SwitchBot Poller
        Real_PC2_T0in10mins_AirOld_NoSBPoller,
        Real_PC2_T0in10mins_AirNew_NoSBPoller,
        Real_PC2_T0in10mins_AirNone_NoSBPoller,
        // PC2 - T0 in 10 mins - enabled SwitchBot Poller
        Real_PC2_T0in10mins_AirOld,
        Real_PC2_T0in10mins_AirNew,
        Real_PC2_T0in10mins_AirNone,

        // PC1 - T0 min 3 hours - disabled SwitchBot Poller
        Real_PC1_T0min3hours_AirOld_NoSBPoller,
        Real_PC1_T0min3hours_AirNew_NoSBPoller,
        Real_PC1_T0min3hours_AirNone_NoSBPoller,
        // PC1 - T0 min 3 hours - enabled SwitchBot Poller
        Real_PC1_T0min3hours_AirOld,
        Real_PC1_T0min3hours_AirNew,
        Real_PC1_T0min3hours_AirNone,

        // PC2 - T0 min 3 hours - disabled SwitchBot Poller
        Real_PC2_T0min3hours_AirOld_NoSBPoller,
        Real_PC2_T0min3hours_AirNew_NoSBPoller,
        Real_PC2_T0min3hours_AirNone_NoSBPoller,
        // PC2 - T0 min 3 hours - enabled SwitchBot Poller
        Real_PC2_T0min3hours_AirOld,
        Real_PC2_T0min3hours_AirNew,
        Real_PC2_T0min3hours_AirNone,

        // PC1 - T0 no shutdown trigger - disabled SwitchBot Poller
        Real_PC1_T0NoTrigger_AirOld_NoSBPoller,
        Real_PC1_T0NoTrigger_AirNew_NoSBPoller,
        Real_PC1_T0NoTrigger_AirNone_NoSBPoller,
        // PC1 - T0 no shutdown trigger - enabled SwitchBot Poller
        Real_PC1_T0NoTrigger_AirOld,
        Real_PC1_T0NoTrigger_AirNew,
        Real_PC1_T0NoTrigger_AirNone,

        // PC2 - T0 no shutdown trigger - disabled SwitchBot Poller
        Real_PC2_T0NoTrigger_AirOld_NoSBPoller,
        Real_PC2_T0NoTrigger_AirNew_NoSBPoller,
        Real_PC2_T0NoTrigger_AirNone_NoSBPoller,
        // PC2 - T0 no shutdown trigger - enabled SwitchBot Poller
        Real_PC2_T0NoTrigger_AirOld,
        Real_PC2_T0NoTrigger_AirNew,
        Real_PC2_T0NoTrigger_AirNone,
    }

    public class Examples
    {
        public static void SaveAllExamples(string destination)
        {
            foreach(ExampleTypes type in GetAllExampleTypes())
            {
                if(type != ExampleTypes.None)
                {
                    string? strName = Enum.GetName(type);
                    if (!string.IsNullOrWhiteSpace(strName))
                    {
                        string strFileName = Regex.Replace(strName, "^Real_", "real") + ".json";
                        string strFullPath = Path.Combine(destination, strFileName);
                        ConfigFile configFile = Examples.GetExample(type);

                        ConfigFileManager.WriteConfig(strFullPath, configFile);
                    }
                }
            }
        }

        public static ExampleTypes[] GetAllExampleTypes()
        {
            return [
                ExampleTypes.None,
                // T0 min 2 hours - disabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0min2hours_AirOld_NoSBPoller,
                ExampleTypes.Real_PC2_T0min2hours_AirOld_NoSBPoller,
                ExampleTypes.Real_PC1_T0min2hours_AirNew_NoSBPoller,
                ExampleTypes.Real_PC2_T0min2hours_AirNew_NoSBPoller,
                ExampleTypes.Real_PC1_T0min2hours_AirNone_NoSBPoller,
                ExampleTypes.Real_PC2_T0min2hours_AirNone_NoSBPoller,
                // T0 min 2 hours - enabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0min2hours_AirOld,
                ExampleTypes.Real_PC2_T0min2hours_AirOld,
                ExampleTypes.Real_PC1_T0min2hours_AirNew,
                ExampleTypes.Real_PC2_T0min2hours_AirNew,
                ExampleTypes.Real_PC1_T0min2hours_AirNone,
                ExampleTypes.Real_PC2_T0min2hours_AirNone,

                // T0 min 1 hour - disabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0min1hour_AirOld_NoSBPoller,
                ExampleTypes.Real_PC2_T0min1hour_AirOld_NoSBPoller,
                ExampleTypes.Real_PC1_T0min1hour_AirNew_NoSBPoller,
                ExampleTypes.Real_PC2_T0min1hour_AirNew_NoSBPoller,
                ExampleTypes.Real_PC1_T0min1hour_AirNone_NoSBPoller,
                ExampleTypes.Real_PC2_T0min1hour_AirNone_NoSBPoller,
                // T0 min 1 hour - enabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0min1hour_AirOld,
                ExampleTypes.Real_PC2_T0min1hour_AirOld,
                ExampleTypes.Real_PC1_T0min1hour_AirNew,
                ExampleTypes.Real_PC2_T0min1hour_AirNew,
                ExampleTypes.Real_PC1_T0min1hour_AirNone,
                ExampleTypes.Real_PC2_T0min1hour_AirNone,

                // T0 in 30 mins - disabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0in30mins_AirOld_NoSBPoller,
                ExampleTypes.Real_PC2_T0in30mins_AirOld_NoSBPoller,
                ExampleTypes.Real_PC1_T0in30mins_AirNew_NoSBPoller,
                ExampleTypes.Real_PC2_T0in30mins_AirNew_NoSBPoller,
                ExampleTypes.Real_PC1_T0in30mins_AirNone_NoSBPoller,
                ExampleTypes.Real_PC2_T0in30mins_AirNone_NoSBPoller,
                // T0 in 30 mins - enabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0in30mins_AirOld,
                ExampleTypes.Real_PC2_T0in30mins_AirOld,
                ExampleTypes.Real_PC1_T0in30mins_AirNew,
                ExampleTypes.Real_PC2_T0in30mins_AirNew,
                ExampleTypes.Real_PC1_T0in30mins_AirNone,
                ExampleTypes.Real_PC2_T0in30mins_AirNone,

                // T0 in 10 mins - disabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0in10mins_AirOld_NoSBPoller,
                ExampleTypes.Real_PC2_T0in10mins_AirOld_NoSBPoller,
                ExampleTypes.Real_PC1_T0in10mins_AirNew_NoSBPoller,
                ExampleTypes.Real_PC2_T0in10mins_AirNew_NoSBPoller,
                ExampleTypes.Real_PC1_T0in10mins_AirNone_NoSBPoller,
                ExampleTypes.Real_PC2_T0in10mins_AirNone_NoSBPoller,
                // T0 in 10 mins - enabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0in10mins_AirOld,
                ExampleTypes.Real_PC2_T0in10mins_AirOld,
                ExampleTypes.Real_PC1_T0in10mins_AirNew,
                ExampleTypes.Real_PC2_T0in10mins_AirNew,
                ExampleTypes.Real_PC1_T0in10mins_AirNone,
                ExampleTypes.Real_PC2_T0in10mins_AirNone,

                // T0 min 3 hours - disabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0min3hours_AirOld_NoSBPoller,
                ExampleTypes.Real_PC2_T0min3hours_AirOld_NoSBPoller,
                ExampleTypes.Real_PC1_T0min3hours_AirNew_NoSBPoller,
                ExampleTypes.Real_PC2_T0min3hours_AirNew_NoSBPoller,
                ExampleTypes.Real_PC1_T0min3hours_AirNone_NoSBPoller,
                ExampleTypes.Real_PC2_T0min3hours_AirNone_NoSBPoller,
                // T0 min 3 hours - enabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0min3hours_AirOld,
                ExampleTypes.Real_PC2_T0min3hours_AirOld,
                ExampleTypes.Real_PC1_T0min3hours_AirNew,
                ExampleTypes.Real_PC2_T0min3hours_AirNew,
                ExampleTypes.Real_PC1_T0min3hours_AirNone,
                ExampleTypes.Real_PC2_T0min3hours_AirNone,

                // T0 no shutdown trigger - disabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0NoTrigger_AirOld_NoSBPoller,
                ExampleTypes.Real_PC2_T0NoTrigger_AirOld_NoSBPoller,
                ExampleTypes.Real_PC1_T0NoTrigger_AirNew_NoSBPoller,
                ExampleTypes.Real_PC2_T0NoTrigger_AirNew_NoSBPoller,
                ExampleTypes.Real_PC1_T0NoTrigger_AirNone_NoSBPoller,
                ExampleTypes.Real_PC2_T0NoTrigger_AirNone_NoSBPoller,
                // T0 no shutdown trigger - enabled SwitchBot Poller
                ExampleTypes.Real_PC1_T0NoTrigger_AirOld,
                ExampleTypes.Real_PC2_T0NoTrigger_AirOld,
                ExampleTypes.Real_PC1_T0NoTrigger_AirNew,
                ExampleTypes.Real_PC2_T0NoTrigger_AirNew,
                ExampleTypes.Real_PC1_T0NoTrigger_AirNone,
                ExampleTypes.Real_PC2_T0NoTrigger_AirNone,
            ];
        }

        public static ClewareUSBConfigMapping[] GetDefaultUSBMapping()
        {
            return [
                new ClewareUSBConfigMapping()
                {
                    name = "ANUBIS_USB_Backup",
                    id = 563239,
                },
                new ClewareUSBConfigMapping()
                {
                    name = "ANUBIS_USB_Primary",
                    id = 563248,
                },
                new ClewareUSBConfigMapping()
                {
                    name = "ANUBIS_USB_Secondary",
                    id = 563249,
                },
            ];
        }

        public static ConfigFile GetExample(ExampleTypes type)
        {
            if (type == ExampleTypes.Real_PC1_T0min2hours_AirNone)
            {
                #region Config file example: PC1; T0-2h; Air: None; SwitchBot Poller: enabled
                return new ConfigFile()
                {
                    switchApiSettings = new SwitchBotAPIConfigSettings()
                    {
                        token = "33a1813627634407129112a8923f378db4e3371d3227309d703fcd7525aa800aacc2707c3380dcdee9e9fe9d907f4d3d",
                        secret = "e9d1a54c20b5ff4167e491ecf2243398",
                    },
                    fritzApiSettings = new FritzAPIConfigSettings()
                    {
                        user = "fritz2029",
                        password = "sommer0174",
                    },
                    clewareApiSettings = new ClewareAPIConfigSettings()
                    {
                    },

                    clewareUSBMappings = GetDefaultUSBMapping(),

                    turnOn = [
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_PC_Primary",
                            waitSecondsAfterTurnOn = 1,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_PC_Secondary",
                            waitSecondsAfterTurnOn = 1,
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_FingerBot_ManuallyTurnOn",
                            message = "Manually turn on power switch on power distributor controlled by FingerBot \"ANUBIS_FingerBot\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.SwitchBot,
                            id = "ANUBIS_SwitchBot",
                            waitSecondsAfterTurnOn = 5,
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_SwitchBot_ManuallyTurnBackOn",
                            message = "Manually turn back on power switch on power distributor controlled by SwitchBot \"ANUBIS_SwitchBot\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_MainSwitch",
                            waitSecondsAfterTurnOn = 30, // time for WLAN power plug to come online
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_WLANPowerPlug",
                            message = "Manually turn on WLAN power plug \"ANUBIS_WLANPowerPlug\" that is plugged in into ANUBIS Fritz Main Switch \"ANUBIS_MainSwitch\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.ClewareUSB,
                            id = "ANUBIS_USB_Primary",
                            waitSecondsAfterTurnOn = 50, // time for valve switches to come online
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_Ventil_AM",
                            waitSecondsAfterTurnOn = 1,
                            disableIfNotDiscovered = false,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_Ventil_EB",
                            waitSecondsAfterTurnOn = 1,
                            disableIfNotDiscovered = false,
                        }
                    ],

                    pollersAndTriggers = new PollerAndTriggerConfig()
                    {
                        pollers =
                        [
                            new SwitchBotPollerData()
                            {
                                options = new SwitchBotPollerConfigOptions()
                                {
                                    sleepTimeInMilliseconds = 55000,
                                },
                                switches = [
                                    new SwitchBotSwitchConfigOptions()
                                    {
                                        switchName = "ANUBIS_SwitchBot",
                                        turnOffOnPanic = true,
                                    },
                                ],
                                generalTriggers = [
                                    new PollerTriggerConfig_SwitchBot
                                    {
                                        panic = [
                                            SwitchBotPanicReason.All,
                                        ],
                                        config = "tcGeneralPanic",
                                    }
                                ],
                                switchTriggers = [
                                    new PollerTriggerConfigWithId_SwitchBot
                                    {
                                        id = ["*"],
                                        panic = [
                                            SwitchBotPanicReason.All,
                                        ],
                                        config = "tcGeneralPanic",
                                    }
                                ],
                            },
                            new FritzPollerData()
                            {
                                options = new FritzPollerConfigOptions()
                                {
                                    sleepTimeInMilliseconds = 20000,
                                },
                                switches = [
                                    new FritzSwitchConfigOptions()
                                    {
                                        switchName = "ANUBIS_PC_Primary",
                                        enterSafeMode = false,
                                        lowPowerCutOff = 2900,
                                        safeModePowerUpAlarm = 100,
                                        minPower = 3100,
                                        maxPower = 35000,
                                    },
                                    new FritzSwitchConfigOptions()
                                    {
                                        switchName = "ANUBIS_PC_Secondary",
                                        enterSafeMode = false,
                                        lowPowerCutOff = 1600,
                                        safeModePowerUpAlarm = 50,
                                        minPower = 1700,
                                        maxPower = 8000,
                                    },
                                    new FritzSwitchConfigOptions()
                                    {
                                        switchName = "ANUBIS_MainSwitch",
                                        turnOffOnPanic = true,
                                        markShutDownIfOff = true,
                                        lowPowerCutOff = 50000,
                                        safeModePowerUpAlarm = 1500,
                                        minPower = 85000,
                                        maxPower = 135000,
                                    },
                                    new FritzSwitchConfigOptions()
                                    {
                                        switchName = "ANUBIS_Ventil_AM",
                                        safeModeSensitive = false,
                                        lowPowerCutOff = 350,
                                        turnOffOnLowPower = false,
                                        safeModePowerUpAlarm = 500,
                                        minPower = 16000,
                                        maxPower = 23500,
                                    },
                                    new FritzSwitchConfigOptions()
                                    {
                                        switchName = "ANUBIS_Ventil_EB",
                                        safeModeSensitive = false,
                                        lowPowerCutOff = 350,
                                        turnOffOnLowPower = false,
                                        safeModePowerUpAlarm = 500,
                                        minPower = 16000,
                                        maxPower = 23500,
                                    },
                                ],
                                generalTriggers = [

                                     new PollerTriggerConfig_Fritz
                                     {
                                         panic = [
                                             FritzPanicReason.All,
                                         ],
                                         config = "tcGeneralPanic"
                                     }
                                ],
                                switchTriggers = [
                                    new PollerTriggerConfigWithId_Fritz
                                    {
                                        id = ["ANUBIS_MainSwitch",
                                            "ANUBIS_Ventil_AM",
                                            "ANUBIS_Ventil_EB"],
                                        panic = [
                                                FritzPanicReason.All
                                             ],
                                        config = "tcGeneralPanic"
                                    },
                                    new PollerTriggerConfigWithId_Fritz
                                    {
                                        id = ["ANUBIS_PC_Primary"],
                                        panic = [
                                                FritzPanicReason.All
                                             ],
                                        config = "tcPrimaryPCPanic"
                                    },
                                    new PollerTriggerConfigWithId_Fritz
                                    {
                                        id = ["ANUBIS_PC_Secondary"],
                                        panic = [
                                                FritzPanicReason.All
                                             ],
                                        config = "tcSecondaryPCPanic"
                                    },
                                ],
                            },
                            new ClewareUSBPollerData()
                            {
                                options = new ClewareUSBPollerConfigOptions()
                                {
                                    sleepTimeInMilliseconds = 35000,
                                },
                                switches = [
                                    new ClewareUSBSwitchConfigOptions()
                                    {
                                        usbSwitchName = "ANUBIS_USB_Primary",
                                        turnOffOnPanic = true,
                                        markShutDownIfOff = true,

                                    },
                                ],
                                generalTriggers =
                                [

                                     new PollerTriggerConfig_ClewareUSB
                                     {
                                         panic = [
                                             ClewareUSBPanicReason.All,
                                         ],
                                         config = "tcGeneralPanic"
                                     }
                                 ],
                                switchTriggers =
                                 [
                                     new PollerTriggerConfigWithId_ClewareUSB
                                     {
                                         id = ["*"],
                                         panic = [
                                                ClewareUSBPanicReason.SwitchNotFound,
                                             ClewareUSBPanicReason.UnknownState,
                                             ClewareUSBPanicReason.CommandTimeout,
                                             ClewareUSBPanicReason.CommandError,
                                             ClewareUSBPanicReason.InvalidState
                                             ],
                                         config = "tcPrimaryPCPanic"
                                     },
                                     new PollerTriggerConfigWithId_ClewareUSB
                                     {
                                         id = ["*"],
                                         panic = [
                                                ClewareUSBPanicReason.All
                                             ],
                                         config = "tcGeneralPanic"
                                     }
                                 ],
                            },
                            new RemoteFilePollerData()
                            {
                                options = new FilePollerConfigOptions()
                                {
                                    sleepTimeInMilliseconds = 23000,
                                },
                                files = [
                                    new FileConfigOptions
                                    {
                                        name = "ANUBIS_RemoteFile_Secondary",
                                        path = $@"\\anubis-pc2\anubis\status.anubis",
                                        mailPriority = false,
                                    },
                                ],
                                generalTriggers =
                                [

                                     new PollerTriggerConfig_File
                                     {
                                         panic = [
                                             WatcherFilePanicReason.All,
                                         ],
                                         config = "tcGeneralPanic"
                                     }
                                 ],
                                fileTriggers =
                                 [
                                     new PollerTriggerConfigWithId_File
                                     {
                                         id = ["ANUBIS_RemoteFile_Secondary"],
                                         panic = [
                                                WatcherFilePanicReason.Panic
                                             ],
                                         config = "tcGeneralPanic",
                                         maxRepeats = 1
                                     },
                                     new PollerTriggerConfigWithId_File
                                     {
                                         id = ["ANUBIS_RemoteFile_Secondary"],
                                         panic = [
                                                WatcherFilePanicReason.All
                                             ],
                                         config = "tcSecondaryPCPanic"
                                     }
                                 ],
                            },
                            new LocalFilePollerData()
                            {
                                options = new FilePollerConfigOptions()
                                {
                                    sleepTimeInMilliseconds = 23000,
                                },
                                files = [
                                    new FileConfigOptions
                                    {
                                        name = "ANUBIS_LocalFile_Primary",
                                        path = $@"C:\shares\anubis\status.anubis",
                                        writeStateOnPanic = true,
                                    },
                                ],
                                generalTriggers =
                                [

                                     new PollerTriggerConfig_File
                                     {
                                         panic = [
                                             WatcherFilePanicReason.All,
                                         ],
                                         config = "tcGeneralPanic"
                                     }
                                 ],
                                fileTriggers =
                                 [
                                     new PollerTriggerConfigWithId_File
                                     {
                                         id = ["*"],
                                         panic = [
                                             WatcherFilePanicReason.All,
                                         ],
                                         config = "tcGeneralPanic"
                                     }
                                 ],
                            },
                            new CountdownPollerData()
                            {
                                options = new CountdownConfigOptions()
                                {
                                    checkShutDownAfterMinutes = 5,
                                    countdownT0MinutesInFuture = 120,
                                },
                                mailingOptions = new MailingConfigOptions()
                                {
                                    mailAddress_Simulate = "echalone@hotmail.com",
                                    mailConfig_Emergency = ["mailconfigs/EMS.anubismail"],
                                    mailConfig_Info = ["mailconfigs/infoExit.anubismail", "mailconfigs/infoFriends.anubismail"],
                                    sendEmergencyMails = true,
                                    sendInfoMails = true,
                                    enabled = false,
                                    simulateMails = true,
                                    countdownSendMailMinutes = 540,
                                },
                                generalTriggers =
                                 [
                                     new PollerTriggerConfig_Universal
                                     {
                                         panic = [
                                             UniversalPanicReason.All,
                                         ],
                                         config = "tcGeneralPanic"
                                     }
                                 ],
                                countdownTriggers =
                                 [
                                     new PollerTriggerConfigWithId_Universal
                                     {
                                         id = [ConstantTriggerIDs.ID_Countdown_T0],
                                         panic = [
                                             UniversalPanicReason.NoPanic,
                                         ],
                                         config = "tcCountdownTrigger"
                                     },
                                     new PollerTriggerConfigWithId_Universal
                                     {
                                         id = [ConstantTriggerIDs.ID_MainController],
                                         panic = [
                                             UniversalPanicReason.All,
                                         ],
                                         config = "tcPrimaryPCPanic"
                                     },
                                     new PollerTriggerConfigWithId_Universal
                                     {
                                         id = ["*"],
                                         panic = [
                                             UniversalPanicReason.All,
                                         ],
                                         config = "tcGeneralPanic"
                                     }
                                 ],
                            },
                            new ControllerData()
                            {
                                options = new ControllerConfigOptions()
                                {
                                    sendMailEarliestAfterMinutes = 180,
                                },
                                generalTriggers =
                                 [
                                     new PollerTriggerConfig_Universal
                                     {
                                         panic = [UniversalPanicReason.GeneralError],
                                         config = "tcPrimaryPCPanic",
                                     },
                                     new PollerTriggerConfig_Universal
                                     {
                                         panic = [UniversalPanicReason.Error],
                                         config = "tcGeneralPanic",
                                         maxRepeats = 1,
                                     },
                                     new PollerTriggerConfig_Universal
                                     {
                                         panic = [UniversalPanicReason.All],
                                         config = "tcGeneralPanic",
                                     }
                                 ],
                                pollerTriggers =
                                 [
                                     new PollerTriggerConfigWithId_Universal
                                     {
                                         id = [ConstantTriggerIDs.ID_Poller_Countdown],
                                         panic = [UniversalPanicReason.All],
                                         config = "tcPrimaryPCPanic",
                                     },
                                     new PollerTriggerConfigWithId_Universal
                                     {
                                         id = ["*"],
                                         panic = [UniversalPanicReason.All],
                                         config = "tcGeneralPanic",
                                         maxRepeats = 1,
                                     },
                                 ]
                            },
                            new GeneralData()
                            {
                                generalTriggers =
                                 [
                                     new PollerTriggerConfigWithId_Universal
                                     {
                                         id = ["*"],
                                         panic = [UniversalPanicReason.All],
                                         config = "tcGeneralPanic",
                                         maxRepeats = 10,
                                     }
                                 ],
                                fallbackTriggers =
                                 [
                                     new PollerTriggerConfigWithId_Universal
                                     {
                                         id = ["*"],
                                         panic = [UniversalPanicReason.All],
                                         config = "tcGeneralPanic",
                                     }
                                 ],
                            },
                        ],
                        triggerConfigs =
                        [
                            new TriggerConfig
                            {
                                id = "tcCountdownTrigger",
                                isFallback = false,
                                repeatable = true,
                                triggers =
                                [
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.ClewareUSB,
                                        id = "ANUBIS_USB_Primary",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.Fritz,
                                        id = "ANUBIS_MainSwitch",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.SwitchBot,
                                        id = "ANUBIS_SwitchBot",
                                        repeatable = true,
                                    },
                                ]
                            },
                            new TriggerConfig
                            {
                                id = "tcGeneralPanic",
                                isFallback = true,
                                repeatable = true,
                                triggers =
                                [
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.ClewareUSB,
                                        id = "ANUBIS_USB_Primary",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.Fritz,
                                        id = "ANUBIS_MainSwitch",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.SwitchBot,
                                        id = "ANUBIS_SwitchBot",
                                        repeatable = true,
                                    },
                                ]
                            },
                            new TriggerConfig
                            {
                                id = "tcGeneralPanicOnce",
                                isFallback = false,
                                repeatable = false,
                                triggers =
                                [
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.ClewareUSB,
                                        id = "ANUBIS_USB_Primary",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.Fritz,
                                        id = "ANUBIS_MainSwitch",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.SwitchBot,
                                        id = "ANUBIS_SwitchBot",
                                        repeatable = true,
                                    },
                                ]
                            },
                            new TriggerConfig
                            {
                                id = "tcPrimaryPCPanic",
                                isFallback = false,
                                repeatable = false,
                                triggers =
                                [
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.ClewareUSB,
                                        id = "ANUBIS_USB_Primary",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.Fritz,
                                        id = "ANUBIS_MainSwitch",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.SwitchBot,
                                        id = "ANUBIS_SwitchBot",
                                        repeatable = true,
                                    },
                                    new SingleDelayTrigger
                                    {
                                        milliseconds = 10000,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.OSShutdown,
                                        id = "windowspoweroff",
                                        repeatable = true,
                                    },
                                    new SingleDelayTrigger
                                    {
                                        milliseconds = 30000,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.Fritz,
                                        id = "ANUBIS_PC_Primary",
                                        repeatable = true,
                                    },
                                ]
                            },
                            new TriggerConfig
                            {
                                id = "tcSecondaryPCPanic",
                                isFallback = false,
                                repeatable = true,
                                triggers =
                                [
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.ClewareUSB,
                                        id = "ANUBIS_USB_Primary",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.Fritz,
                                        id = "ANUBIS_MainSwitch",
                                        repeatable = true,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.SwitchBot,
                                        id = "ANUBIS_SwitchBot",
                                        repeatable = true,
                                    },
                                    new SingleDelayTrigger
                                    {
                                        milliseconds = 45000,
                                    },
                                    new SingleDeviceTrigger
                                    {
                                        deviceType = TriggerType.Fritz,
                                        id = "ANUBIS_PC_Secondary",
                                        repeatable = true,
                                    },
                                ]
                            },
                        ],
                    }
                };
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min2hours_AirNone)
            {
                #region Config file example: PC2; T0-2h; Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                cf.turnOn = [
                    new DeviceTurnOn()
                    {
                        deviceType = TriggerType.Fritz,
                        id = "ANUBIS_PC_Primary",
                        waitSecondsAfterTurnOn = 1,
                    },
                    new DeviceTurnOn()
                    {
                        deviceType = TriggerType.Fritz,
                        id = "ANUBIS_PC_Secondary",
                        waitSecondsAfterTurnOn = 1,
                    },
                    new DeviceTurnOn()
                    {
                        deviceType = TriggerType.ClewareUSB,
                        id = "ANUBIS_USB_Secondary",
                        waitSecondsAfterTurnOn = 50, // time for valve switches to come online
                    },
                ];

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is ClewareUSBPollerData) is ClewareUSBPollerData cupd)
                {
                    cupd.switches = [
                        new ClewareUSBSwitchConfigOptions()
                        {
                            usbSwitchName = "ANUBIS_USB_Secondary",
                            turnOffOnPanic = true,
                            markShutDownIfOff = true,

                        },
                    ];

                    cupd.switchTriggers = [
                        new PollerTriggerConfigWithId_ClewareUSB
                        {
                            id = ["*"],
                            panic = [
                                ClewareUSBPanicReason.SwitchNotFound,
                                ClewareUSBPanicReason.UnknownState,
                                ClewareUSBPanicReason.CommandTimeout,
                                ClewareUSBPanicReason.CommandError,
                                ClewareUSBPanicReason.InvalidState
                                ],
                            config = "tcSecondaryPCPanic"
                        },
                        new PollerTriggerConfigWithId_ClewareUSB
                        {
                            id = ["*"],
                            panic = [
                                ClewareUSBPanicReason.All
                                ],
                            config = "tcGeneralPanic"
                        }
                    ];
                }

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is RemoteFilePollerData) is RemoteFilePollerData rfpd)
                {
                    rfpd.files = [
                        new FileConfigOptions
                        {
                            name = "ANUBIS_RemoteFile_Primary",
                            path = $@"/media/shares/anubis-pc1/status.anubis",
                            mailPriority = true,
                        },
                    ];

                    rfpd.fileTriggers = [
                        new PollerTriggerConfigWithId_File
                        {
                            id = ["ANUBIS_RemoteFile_Primary"],
                            panic = [
                                WatcherFilePanicReason.Panic
                                ],
                            config = "tcGeneralPanic",
                            maxRepeats = 1
                        },
                        new PollerTriggerConfigWithId_File
                        {
                            id = ["ANUBIS_RemoteFile_Primary"],
                            panic = [
                                WatcherFilePanicReason.All
                                ],
                            config = "tcPrimaryPCPanic"
                        }
                    ];
                }

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is LocalFilePollerData) is LocalFilePollerData lfpd)
                {
                    lfpd.files = [
                        new FileConfigOptions
                        {
                            name = "ANUBIS_LocalFile_Secondary",
                            path = $@"/media/shares/anubis/status.anubis",
                            writeStateOnPanic = true,
                        },
                    ];

                    lfpd.fileTriggers = [
                        new PollerTriggerConfigWithId_File
                        {
                            id = ["*"],
                            panic = [
                                WatcherFilePanicReason.All,
                            ],
                            config = "tcGeneralPanic"
                        }
                    ];
                }

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.countdownTriggers = [
                        new PollerTriggerConfigWithId_Universal
                        {
                            id = [ConstantTriggerIDs.ID_Countdown_T0],
                            panic = [
                                UniversalPanicReason.NoPanic,
                            ],
                            config = "tcCountdownTrigger"
                        },
                        new PollerTriggerConfigWithId_Universal
                        {
                            id = [ConstantTriggerIDs.ID_MainController],
                            panic = [
                                             UniversalPanicReason.All,
                            ],
                            config = "tcSecondaryPCPanic"
                        },
                        new PollerTriggerConfigWithId_Universal
                        {
                            id = ["*"],
                            panic = [
                                             UniversalPanicReason.All,
                            ],
                            config = "tcGeneralPanic"
                        }
                    ];
                }

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is ControllerData) is ControllerData mcpd)
                {
                    mcpd.generalTriggers = [
                        new PollerTriggerConfig_Universal
                        {
                            panic = [UniversalPanicReason.GeneralError],
                            config = "tcSecondaryPCPanic",
                        },
                        new PollerTriggerConfig_Universal
                        {
                            panic = [UniversalPanicReason.Error],
                            config = "tcGeneralPanic",
                            maxRepeats = 1,
                        },
                        new PollerTriggerConfig_Universal
                        {
                            panic = [UniversalPanicReason.All],
                            config = "tcGeneralPanic",
                        }
                    ];

                    mcpd.pollerTriggers = [
                        new PollerTriggerConfigWithId_Universal
                        {
                            id = [ConstantTriggerIDs.ID_Poller_Countdown],
                            panic = [UniversalPanicReason.All],
                            config = "tcSecondaryPCPanic",
                        },
                        new PollerTriggerConfigWithId_Universal
                        {
                            id = ["*"],
                            panic = [UniversalPanicReason.All],
                            config = "tcGeneralPanic",
                            maxRepeats = 1,
                        },
                    ];
                }

                cf.pollersAndTriggers.triggerConfigs = [
                    new TriggerConfig
                    {
                        id = "tcCountdownTrigger",
                        isFallback = false,
                        repeatable = true,
                        triggers =
                        [
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.ClewareUSB,
                                id = "ANUBIS_USB_Secondary",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.Fritz,
                                id = "ANUBIS_MainSwitch",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.SwitchBot,
                                id = "ANUBIS_SwitchBot",
                                repeatable = true,
                            },
                        ]
                    },
                    new TriggerConfig
                    {
                        id = "tcGeneralPanic",
                        isFallback = true,
                        repeatable = true,
                        triggers =
                        [
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.ClewareUSB,
                                id = "ANUBIS_USB_Secondary",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.Fritz,
                                id = "ANUBIS_MainSwitch",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.SwitchBot,
                                id = "ANUBIS_SwitchBot",
                                repeatable = true,
                            },
                        ]
                    },
                    new TriggerConfig
                    {
                        id = "tcGeneralPanicOnce",
                        isFallback = false,
                        repeatable = false,
                        triggers =
                        [
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.ClewareUSB,
                                id = "ANUBIS_USB_Secondary",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.Fritz,
                                id = "ANUBIS_MainSwitch",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.SwitchBot,
                                id = "ANUBIS_SwitchBot",
                                repeatable = true,
                            },
                        ]
                    },
                    new TriggerConfig
                    {
                        id = "tcPrimaryPCPanic",
                        isFallback = false,
                        repeatable = true,
                        triggers =
                        [
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.ClewareUSB,
                                id = "ANUBIS_USB_Secondary",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.Fritz,
                                id = "ANUBIS_MainSwitch",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.SwitchBot,
                                id = "ANUBIS_SwitchBot",
                                repeatable = true,
                            },
                            new SingleDelayTrigger
                            {
                                milliseconds = 45000,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.Fritz,
                                id = "ANUBIS_PC_Primary",
                                repeatable = true,
                            },
                        ]
                    },
                    new TriggerConfig
                    {
                        id = "tcSecondaryPCPanic",
                        isFallback = false,
                        repeatable = false,
                        triggers =
                        [
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.ClewareUSB,
                                id = "ANUBIS_USB_Secondary",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.Fritz,
                                id = "ANUBIS_MainSwitch",
                                repeatable = true,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.SwitchBot,
                                id = "ANUBIS_SwitchBot",
                                repeatable = true,
                            },
                            new SingleDelayTrigger
                            {
                                milliseconds = 10000,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.OSShutdown,
                                id = "linuxreboot",
                                repeatable = true,
                            },
                            new SingleDelayTrigger
                            {
                                milliseconds = 30000,
                            },
                            new SingleDeviceTrigger
                            {
                                deviceType = TriggerType.Fritz,
                                id = "ANUBIS_PC_Secondary",
                                repeatable = true,
                            },
                        ]
                    },
                ];

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min2hours_AirOld)
            {
                #region Config file example: PC1; T0-2h; Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is FritzPollerData) is FritzPollerData fpd)
                {
                    var fms = fpd.switches.FirstOrDefault(itm => itm.switchName == "ANUBIS_MainSwitch");
                    if (fms != null)
                    {
                        fms.lowPowerCutOff = 100000;
                        fms.minPowerWarn = 165000;
                        fms.minPower = 150000;
                        fms.maxPower = 330000;
                    }
                }

                cf.turnOn = [
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_PC_Primary",
                            waitSecondsAfterTurnOn = 1,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_PC_Secondary",
                            waitSecondsAfterTurnOn = 1,
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_FingerBot_ManuallyTurnOn",
                            message = "Manually turn on power switch on power distributor controlled by FingerBot \"ANUBIS_FingerBot\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.SwitchBot,
                            id = "ANUBIS_SwitchBot",
                            waitSecondsAfterTurnOn = 5,
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_SwitchBot_ManuallyTurnBackOn",
                            message = "Manually turn back on power switch on power distributor controlled by SwitchBot \"ANUBIS_SwitchBot\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_MainSwitch",
                            waitSecondsAfterTurnOn = 30, // time for WLAN power plug to come online
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_WLANPowerPlug",
                            message = "Manually turn on WLAN power plug \"ANUBIS_WLANPowerPlug\" that is plugged in into ANUBIS Fritz Main Switch \"ANUBIS_MainSwitch\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.ClewareUSB,
                            id = "ANUBIS_USB_Primary",
                            waitSecondsAfterTurnOn = 50, // time for valve switches to come online
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_Ventil_AM",
                            waitSecondsAfterTurnOn = 1,
                            disableIfNotDiscovered = false,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_Ventil_EB",
                            waitSecondsAfterTurnOn = 1,
                            disableIfNotDiscovered = false,
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_WLANAirPower",
                            message = "Manually turn on WLAN power plug \"ANUBIS_WLANAirPower\" that is plugged in into power distributer \"ANUBIS Hauptverteiler\" now to turn on air supply!",
                            waitSecondsAfterTurnOn = 30,
                        }
                    ];

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min2hours_AirNew)
            {
                #region Config file example: PC1; T0-2h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is FritzPollerData) is FritzPollerData fpd)
                {
                    var fms = fpd.switches.FirstOrDefault(itm => itm.switchName == "ANUBIS_MainSwitch");
                    if (fms != null)
                    {
                        fms.lowPowerCutOff = 150000;
                        fms.minPowerWarn = 235000;
                        fms.maxPowerWarn = 800000;
                        fms.minPower = 215000;
                        fms.maxPower = 830000;
                    }
                }

                cf.turnOn = [
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_PC_Primary",
                            waitSecondsAfterTurnOn = 1,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_PC_Secondary",
                            waitSecondsAfterTurnOn = 1,
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_FingerBot_ManuallyTurnOn",
                            message = "Manually turn on power switch on power distributor controlled by FingerBot \"ANUBIS_FingerBot\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.SwitchBot,
                            id = "ANUBIS_SwitchBot",
                            waitSecondsAfterTurnOn = 5,
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_SwitchBot_ManuallyTurnBackOn",
                            message = "Manually turn back on power switch on power distributor controlled by SwitchBot \"ANUBIS_SwitchBot\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_MainSwitch",
                            waitSecondsAfterTurnOn = 30, // time for WLAN power plug to come online
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_WLANPowerPlug",
                            message = "Manually turn on WLAN power plug \"ANUBIS_WLANPowerPlug\" that is plugged in into ANUBIS Fritz Main Switch \"ANUBIS_MainSwitch\" now!",
                            waitSecondsAfterTurnOn = 90,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.ClewareUSB,
                            id = "ANUBIS_USB_Primary",
                            waitSecondsAfterTurnOn = 50, // time for valve switches to come online
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_Ventil_AM",
                            waitSecondsAfterTurnOn = 1,
                            disableIfNotDiscovered = false,
                        },
                        new DeviceTurnOn()
                        {
                            deviceType = TriggerType.Fritz,
                            id = "ANUBIS_Ventil_EB",
                            waitSecondsAfterTurnOn = 1,
                            disableIfNotDiscovered = false,
                        },
                        new MessageTurnOn()
                        {
                            id = "ANUBIS_WLANAirPower",
                            message = "Manually turn on WLAN power plug \"ANUBIS_WLANAirPower\" that is plugged in into power distributer \"ANUBIS Hauptverteiler\" now to turn on air supply!",
                            waitSecondsAfterTurnOn = 30,
                        }
                    ];

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min2hours_AirOld)
            {
                #region Config file example: PC2; T0-2h; Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is FritzPollerData) is FritzPollerData fpd)
                {
                    var fms = fpd.switches.FirstOrDefault(itm => itm.switchName == "ANUBIS_MainSwitch");
                    if (fms != null)
                    {
                        fms.lowPowerCutOff = 100000;
                        fms.minPowerWarn = 165000;
                        fms.minPower = 150000;
                        fms.maxPower = 330000;
                    }
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min2hours_AirNew)
            {
                #region Config file example: PC2; T0-2h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is FritzPollerData) is FritzPollerData fpd)
                {
                    var fms = fpd.switches.FirstOrDefault(itm => itm.switchName == "ANUBIS_MainSwitch");
                    if (fms != null)
                    {
                        fms.lowPowerCutOff = 150000;
                        fms.minPowerWarn = 235000;
                        fms.maxPowerWarn = 800000;
                        fms.minPower = 215000;
                        fms.maxPower = 830000;
                    }
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min2hours_AirNone_NoSBPoller)
            {
                #region Config file example: PC1; T0-2h; Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is SwitchBotPollerData) is SwitchBotPollerData sbpd)
                {
                    sbpd.enabled = false;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min2hours_AirOld_NoSBPoller)
            {
                #region Config file example: PC1; T0-2h; Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is SwitchBotPollerData) is SwitchBotPollerData sbpd)
                {
                    sbpd.enabled = false;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min2hours_AirNew_NoSBPoller)
            {
                #region Config file example: PC1; T0-2h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is SwitchBotPollerData) is SwitchBotPollerData sbpd)
                {
                    sbpd.enabled = false;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min2hours_AirNone_NoSBPoller)
            {
                #region Config file example: PC2; T0-2h; Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is SwitchBotPollerData) is SwitchBotPollerData sbpd)
                {
                    sbpd.enabled = false;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min2hours_AirOld_NoSBPoller)
            {
                #region Config file example: PC2; T0-2h; Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is SwitchBotPollerData) is SwitchBotPollerData sbpd)
                {
                    sbpd.enabled = false;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min2hours_AirNew_NoSBPoller)
            {
                #region Config file example: PC2; T0-2h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is SwitchBotPollerData) is SwitchBotPollerData sbpd)
                {
                    sbpd.enabled = false;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min1hour_AirNone)
            {
                #region Config file example: PC1; T0-1h; Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min1hour_AirOld)
            {
                #region Config file example: PC1; T0-1h; Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min1hour_AirNew)
            {
                #region Config file example: PC1; T0-1h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min1hour_AirNone)
            {
                #region Config file example: PC2; T0-1h; Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min1hour_AirOld)
            {
                #region Config file example: PC2; T0-1h; Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min1hour_AirNew)
            {
                #region Config file example: PC2; T0-1h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min1hour_AirNone_NoSBPoller)
            {
                #region Config file example: PC1; T0-1h; Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min1hour_AirOld_NoSBPoller)
            {
                #region Config file example: PC1; T0-1h; Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min1hour_AirNew_NoSBPoller)
            {
                #region Config file example: PC1; T0-1h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min1hour_AirNone_NoSBPoller)
            {
                #region Config file example: PC2; T0-1h; Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min1hour_AirOld_NoSBPoller)
            {
                #region Config file example: PC2; T0-1h; Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min1hour_AirNew_NoSBPoller)
            {
                #region Config file example: PC2; T0-1h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 60;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min3hours_AirNone)
            {
                #region Config file example: PC1; T0-3h; Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min3hours_AirOld)
            {
                #region Config file example: PC1; T0-3h; Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min3hours_AirNew)
            {
                #region Config file example: PC1; T0-3h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min3hours_AirNone)
            {
                #region Config file example: PC2; T0-3h; Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min3hours_AirOld)
            {
                #region Config file example: PC2; T0-3h; Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min3hours_AirNew)
            {
                #region Config file example: PC2; T0-3h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min3hours_AirNone_NoSBPoller)
            {
                #region Config file example: PC1; T0-3h; Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min3hours_AirOld_NoSBPoller)
            {
                #region Config file example: PC1; T0-3h; Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0min3hours_AirNew_NoSBPoller)
            {
                #region Config file example: PC1; T0-3h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min3hours_AirNone_NoSBPoller)
            {
                #region Config file example: PC2; T0-3h; Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min3hours_AirOld_NoSBPoller)
            {
                #region Config file example: PC2; T0-3h; Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0min3hours_AirNew_NoSBPoller)
            {
                #region Config file example: PC2; T0-3h; Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 180;
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in30mins_AirNone)
            {
                #region Config file example: PC1; T0-30mins (no rounding to next hour); Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in30mins_AirOld)
            {
                #region Config file example: PC1; T0-30mins (no rounding to next hour); Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in30mins_AirNew)
            {
                #region Config file example: PC1; T0-30mins (no rounding to next hour); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in30mins_AirNone)
            {
                #region Config file example: PC2; T0-30mins (no rounding to next hour); Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in30mins_AirOld)
            {
                #region Config file example: PC2; T0-30mins (no rounding to next hour); Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in30mins_AirNew)
            {
                #region Config file example: PC2; T0-30mins (no rounding to next hour); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in30mins_AirNone_NoSBPoller)
            {
                #region Config file example: PC1; T0-30mins (no rounding to next hour); Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in30mins_AirOld_NoSBPoller)
            {
                #region Config file example: PC1; T0-30mins (no rounding to next hour); Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in30mins_AirNew_NoSBPoller)
            {
                #region Config file example: PC1; T0-30mins (no rounding to next hour); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in30mins_AirNone_NoSBPoller)
            {
                #region Config file example: PC2; T0-30mins (no rounding to next hour); Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in30mins_AirOld_NoSBPoller)
            {
                #region Config file example: PC2; T0-30mins (no rounding to next hour); Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in30mins_AirNew_NoSBPoller)
            {
                #region Config file example: PC2; T0-30mins (no rounding to next hour); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 7;
                    cdpd.options.countdownT0MinutesInFuture = 30;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in10mins_AirNone)
            {
                #region Config file example: PC1; T0-10mins (no rounding to next hour); Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in10mins_AirOld)
            {
                #region Config file example: PC1; T0-10mins (no rounding to next hour); Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in10mins_AirNew)
            {
                #region Config file example: PC1; T0-10mins (no rounding to next hour); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in10mins_AirNone)
            {
                #region Config file example: PC2; T0-10mins (no rounding to next hour); Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in10mins_AirOld)
            {
                #region Config file example: PC2; T0-10mins (no rounding to next hour); Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in10mins_AirNew)
            {
                #region Config file example: PC2; T0-10mins (no rounding to next hour); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in10mins_AirNone_NoSBPoller)
            {
                #region Config file example: PC1; T0-10mins (no rounding to next hour); Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in10mins_AirOld_NoSBPoller)
            {
                #region Config file example: PC1; T0-10mins (no rounding to next hour); Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0in10mins_AirNew_NoSBPoller)
            {
                #region Config file example: PC1; T0-10mins (no rounding to next hour); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in10mins_AirNone_NoSBPoller)
            {
                #region Config file example: PC2; T0-10mins (no rounding to next hour); Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in10mins_AirOld_NoSBPoller)
            {
                #region Config file example: PC2; T0-10mins (no rounding to next hour); Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0in10mins_AirNew_NoSBPoller)
            {
                #region Config file example: PC2; T0-10mins (no rounding to next hour); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.countdownT0MinutesInFuture = 10;
                    cdpd.options.countdownT0RoundToNextHour = false;
                }

                cf.turnOn.Where(itm => itm.waitSecondsAfterTurnOn == 90).ToList().ForEach(itm => itm.waitSecondsAfterTurnOn = 30);

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0NoTrigger_AirNone)
            {
                #region Config file example: PC1; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0NoTrigger_AirOld)
            {
                #region Config file example: PC1; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0NoTrigger_AirNew)
            {
                #region Config file example: PC1; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0NoTrigger_AirNone)
            {
                #region Config file example: PC2; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: None; SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0NoTrigger_AirOld)
            {
                #region Config file example: PC2; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: Old (green MAS Turbo-Flo); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0NoTrigger_AirNew)
            {
                #region Config file example: PC2; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: enabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0NoTrigger_AirNone_NoSBPoller)
            {
                #region Config file example: PC1; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0NoTrigger_AirOld_NoSBPoller)
            {
                #region Config file example: PC1; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC1_T0NoTrigger_AirNew_NoSBPoller)
            {
                #region Config file example: PC1; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC1_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0NoTrigger_AirNone_NoSBPoller)
            {
                #region Config file example: PC2; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: None; SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNone_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0NoTrigger_AirOld_NoSBPoller)
            {
                #region Config file example: PC2; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: New (green MAS Turbo-Flo); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirOld_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else if (type == ExampleTypes.Real_PC2_T0NoTrigger_AirNew_NoSBPoller)
            {
                #region Config file example: PC2; T0 in 8 hours but no shutdown trigger (only manual triggering); Air: New (yellow FAS Turbo-Flow); SwitchBot Poller: disabled
                var cf = GetExample(ExampleTypes.Real_PC2_T0min2hours_AirNew_NoSBPoller);

                if (cf.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData) is CountdownPollerData cdpd)
                {
                    cdpd.options.countdownT0MinutesInFuture = 480;
                    cdpd.options.countdownAutoSafeModeMinutes = 0;
                    cdpd.options.shutDownOnT0 = false;
                    cdpd.mailingOptions.countdownSendMailMinutes = 300;
                    AddEmptyCheckTrigger(cdpd);
                }

                return cf;
                #endregion
            }
            else
            {
                throw new Exception($"Unknown example type: {type}");
            }
        }

        private static void AddEmptyCheckTrigger(CountdownPollerData cdpd)
        {
            var lstTriggers = cdpd.countdownTriggers.ToList();
            lstTriggers.Add(new PollerTriggerConfigWithId_Universal()
            {
                panic = [UniversalPanicReason.CheckConditionViolation],
                id = ["SystemShutDownUnverified"],
                config = "",
                enabled = true,
                maxRepeats = 0,
            });
            cdpd.countdownTriggers = [.. lstTriggers];
        }
    }
}
