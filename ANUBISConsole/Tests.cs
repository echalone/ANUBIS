using ANUBISClewareAPI;
using ANUBISFritzAPI;
using ANUBISSwitchBotAPI;
using ANUBISWatcher.Configuration.ConfigFileData;
using ANUBISWatcher.Configuration.ConfigHelpers;
using ANUBISWatcher.Configuration.Serialization;
using ANUBISWatcher.Entities;
using ANUBISWatcher.Options;
using ANUBISWatcher.Pollers;
using ANUBISWatcher.Shared;
using ANUBISWatcher.Triggering;
using Microsoft.Extensions.Logging;

namespace ANUBISWatcher
{
    internal class Tests
    {
        internal static void DiscoveryTests()
        {
            //Tests.StartTests(loggerMain);
            Console.WriteLine("Reading in config and doing some tests...");

            ConfigFile configFile = Examples.GetExample(ExampleTypes.Real_PC1_T0in10mins_AirNew);

            ConfigFileManager.WriteConfig(@"C:\tmp\testConfig.json", configFile);

            var retVal = ConfigFileManager.ReadAndLoadConfig(@"C:\tmp\testConfig.json", true, true);
            var newConfig = retVal.Item1;
#pragma warning disable IDE0059
            var discoveryInfo = retVal.Item2;
#pragma warning restore IDE0059

            if (newConfig != null)
            {
                ConfigFileManager.WriteConfig(@"C:\tmp\testConfig2.json", newConfig);

            }

            Console.WriteLine("press any key to turn on devices");
            Console.ReadKey();

            Console.WriteLine("Turning on devices, follow any manual step instructions given to you through warnings in the logging console...");
            bool turnedOn = Generator.TurnOnDevices();

            if (turnedOn)
            {
                Console.WriteLine("Successfully turned on all devices");
            }
            else
            {
                Console.WriteLine("Could not turn on all devices");
            }

            Console.WriteLine("press any key to rediscover devices");
            Console.ReadKey();

#pragma warning disable IDE0059 // Unnötige Zuweisung eines Werts.
            DiscoveryInfo? rediscovered = ConfigFileManager.ReloadConfig(true, true);
#pragma warning restore IDE0059 // Unnötige Zuweisung eines Werts.

            Console.WriteLine("Rediscovered devices, press any key to continue...");
            Console.ReadKey();


            /*
        Console.WriteLine("press any key to start controller");
        Console.ReadKey();

        Console.WriteLine("Starting controller...");
        bool hasEntered = Generator.StartController();

        if (hasEntered)
        {
            Console.WriteLine("Successfully started controller");
        }
        else
        {
            Console.WriteLine("Could not start controller");
        }

        Console.WriteLine("press any key to enter holdback mode");
        Console.ReadKey();

        Console.WriteLine("Entering holdback mode...");
        hasEntered = SharedData.Controller?.EnterHoldBackMode() ?? false;

        if(hasEntered)
        {
            Console.WriteLine("Successfully entered holdback mode");
        }
        else
        {
            Console.WriteLine("Could not enter holdback mode");
        }

        Console.WriteLine("press any key to enter armed mode");
        Console.ReadKey();

        Console.WriteLine("Entering armed mode...");
        hasEntered = SharedData.Controller?.ArmPanicMode() ?? false;

        if (hasEntered)
        {
            Console.WriteLine("Successfully entered armed mode");
        }
        else
        {
            Console.WriteLine("Could not enter armed mode");
        }

        Console.WriteLine("press any key to stop monitoring");
        Console.ReadKey();

        Console.WriteLine("Stop monitoring...");
        hasEntered = SharedData.Controller?.StopMonitoring(false) ?? false;

        if (hasEntered)
        {
            Console.WriteLine("Successfully stopped monitoring");
        }
        else
        {
            Console.WriteLine("Could not stop monitoring");
        }

        Console.WriteLine("press any key to start monitoring");
        Console.ReadKey();

        Console.WriteLine("Start monitoring...");
        hasEntered = SharedData.Controller?.StartMonitoring() ?? false;

        if (hasEntered)
        {
            Console.WriteLine("Successfully started monitoring");
        }
        else
        {
            Console.WriteLine("Could not start monitoring");
        }

        Console.WriteLine("press any key to enter holdback mode");
        Console.ReadKey();

        Console.WriteLine("Entering holdback mode...");
        hasEntered = SharedData.Controller?.EnterHoldBackMode() ?? false;

        if (hasEntered)
        {
            Console.WriteLine("Successfully entered holdback mode");
        }
        else
        {
            Console.WriteLine("Could not enter holdback mode");
        }

        Console.WriteLine("press any key to enter armed mode");
        Console.ReadKey();

        Console.WriteLine("Entering armed mode...");
        hasEntered = SharedData.Controller?.ArmPanicMode() ?? false;

        if (hasEntered)
        {
            Console.WriteLine("Successfully entered armed mode");
        }
        else
        {
            Console.WriteLine("Could not enter armed mode");
        }

        Console.WriteLine("press any key to enter safe mode");
        Console.ReadKey();

        hasEntered = SharedData.Controller?.EnterSafeMode(false) ?? false;

        if (hasEntered)
        {
            Console.WriteLine("Successfully entered safe mode");
        }
        else
        {
            Console.WriteLine("Could not enter safe mode");
        }

        Console.WriteLine("press any key to stop controller");
        Console.ReadKey();

        Console.WriteLine("Stopping controller...");
        hasEntered = Generator.StopController();

        if (hasEntered)
        {
            Console.WriteLine("Successfully stopped controller");
        }
        else
        {
            Console.WriteLine("Could not stop controller");
        }
            */

        }

        internal static void StartTests(ILogger loggerMain)
        {
            bool testWrongSwitchBotId = false;
            bool testSwitchBot = false;
            bool testFritz = false;
            bool testFritzDirect = false;
            bool testSwitchBotPoller = false;
            bool testWatcherFilePoller_Write = false;
            bool testWatcherFilePoller_Read = false;
            bool testUSBDirect = false;
            bool testUSBPoller = false;
            bool testTrigger = true;

            try
            {
                if (testTrigger)
                {
                    Test_Trigger(loggerMain);
                }

                if (testWatcherFilePoller_Write)
                {
                    Test_FilePollerWrite(loggerMain);
                }

                if (testWatcherFilePoller_Read)
                {
                    Test_FilePollerRead(loggerMain);
                }

                if (testSwitchBot)
                {
                    Test_SwitchBotDirect(testWrongSwitchBotId);
                }

                if (testSwitchBotPoller)
                {
                    Test_SwitchBotPoller(loggerMain);
                }

                if (testFritz)
                {
                    Test_FritzPoller(loggerMain);
                }

                if (testFritzDirect)
                {
                    Test_FritzDirect(loggerMain);
                }

                if (testUSBDirect)
                {
                    Test_USBDirect(loggerMain);
                }

                if (testUSBPoller)
                {
                    Test_USBPoller(loggerMain);
                }
            }
            catch (Exception ex)
            {
                loggerMain.LogError(ex, "While conducting FritzAPI tests: {message}", ex.Message);
                Console.WriteLine($"Error ({ex.GetType().FullName}): {ex.Message}");
            }


        }

        private static void Test_Trigger(ILogger loggerMain)
        {
            var clewareApi = Generator.GetClewareAPI();
            var fritzApi = Generator.GetFritzAPI();
            var switchBotApi = Generator.GetSwitchBotAPI();

            fritzApi?.Login(true);
            fritzApi?.LoadSwitchNames();
            Dictionary<string, string>? dicFritzSwitches = fritzApi?.GetCachedSwitchNames();
            List<string> lstFritzSwitches = dicFritzSwitches?.Keys?.Where(itm => fritzApi?.GetSwitchPresenceByName(itm) == SwitchPresence.Present)?.ToList() ?? [];
            List<SwitchBotDevice> lstSwitchBot = switchBotApi?.GetSwitchBotList() ?? [];
            Dictionary<string, long> dicUsbSwitches = clewareApi?.GetUSBSwitchNames() ?? [];
            List<string> lstUsbSwitches = dicUsbSwitches?.Keys?.ToList() ?? [];

            Console.WriteLine("\r\nFound the following USB switches:");
            lstUsbSwitches.ForEach(itm => Console.WriteLine($"\t*) {itm}"));

            Console.WriteLine("\r\nFound the following switchbot switches:");
            lstSwitchBot.ForEach(itm => Console.WriteLine($"\t*) {itm.Name} (cse: {itm.CloudServiceEnabled}, hubid: {itm.HubId})"));

            Console.WriteLine("\r\nFound the following fritz switches:");
            lstFritzSwitches.ForEach(itm => Console.WriteLine($"\t*) {itm}"));

            Console.WriteLine("\r\nPress any key to turn on all switches...");
            Console.ReadKey();

            Console.WriteLine("Turning on USB Switch OldSwitch");
            clewareApi?.TurnUSBSwitchOnByName("OldSwitch");

            Console.WriteLine("Turning on SwitchBot ANUBIS_SwitchBot");
            switchBotApi?.TurnSwitchBotOnByName("ANUBIS_SwitchBot");

            Console.WriteLine("Turning on Fritz Switch ANUBIS_Ventil");
            fritzApi?.TurnSwitchOnByName("ANUBIS_Ventil");


            Console.WriteLine("Press any key to start trigger thread...");
            Console.ReadKey();

            loggerMain?.LogInformation("Launching trigger thread...");

            TriggerConfiguration? config = Generator.GetTriggerConfiguration("mytest1");
            if (config != null)
                TriggerController.StartTriggerThread(config, false);
            /*

            Console.WriteLine("Press any key to start three trigger configurations...");
            Console.ReadKey();

            loggerMain?.LogInformation("Launching three trigger configurations...");

            TriggerController.StartTriggerThread(Generator.GetTriggerConfiguration("mytest1"));
            TriggerController.StartTriggerThread(Generator.GetTriggerConfiguration("mytest2"));
            TriggerController.StartTriggerThread(Generator.GetTriggerConfiguration("mytest1"));

            loggerMain?.LogInformation("All three trigger configurations have been launched");

            Console.WriteLine("Press any key to start one more trigger configuration...");
            Console.ReadKey();

            TriggerController.StartTriggerThread(Generator.GetTriggerConfiguration("mytest4"));
            */

            Console.WriteLine("Press any key to display history");
            Console.ReadKey();

            List<TriggerHistoryEntry> lstHistory = SharedData.TriggerHistory;

            Console.WriteLine("History is...");
            if (lstHistory.Count > 0)
            {
                lstHistory.ForEach(itm => Console.WriteLine($"\t*) Timestamp: {itm.UtcTimestamp}; Type: {itm.Type}; Name: {itm.Name}; Outcome: {itm.Outcome}"));
            }
            else
            {
                Console.WriteLine("\t<none>");
            }

            Console.WriteLine("Press any key to end program");
            Console.ReadKey();

            Generator.CancelGlobalCancellationToken();
        }

        public class UpdateThreadInfo
        {
            public WatcherFilePoller? Poller { get; set; }
            public CancellationToken? Token { get; set; }
            public ILogger? Logger { get; set; }
        }

        private static void UpdateThread(object? objInfo)
        {
            UpdateThreadInfo? info = (UpdateThreadInfo?)objInfo;
            if (info?.Poller != null)
            {
                info?.Logger?.LogInformation("Started update thread");
                try
                {
                    while (true)
                    {
                        WatcherFilePanicReason? panic = info?.Poller?.Options?.Files?.FirstOrDefault()?.GetNewPanic();

                        if (panic != null)
                        {
                            Console.WriteLine($"Recieved new panic from file: {Enum.GetName(panic.Value)}");
                        }

                        info?.Poller?.UpdateStateTimestamp();
                        if (info?.Token.HasValue ?? false)
                        {
                            if (info.Token.Value.WaitHandle.WaitOne(1000))
                            {
                                return;
                            }
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
                finally
                {
                    info?.Logger?.LogInformation("Stopping update thread");
                }
            }
        }

        private static void Test_FilePollerWrite(ILogger loggerMain)
        {
            var loggerWatcherFilePoller = Generator.GetLogger("WatcherFile.Poller");
            var loggerWatcherFilePollerFile = Generator.GetLogger("WatcherFile.Poller.File");

            WatcherPollerFile fileMain = new(new WatcherPollerFileOptions()
            {
                WatcherFileName = "DefaultWrite",
                FilePath = @"E:\TmpTestDirectory2\DefaultWrite.awf",
                Logger = loggerWatcherFilePollerFile,
                MaxUpdateAgeInSeconds = 15,
                WriteStateOnPanic = true,
            });

            WatcherFilePoller poller = new(new WatcherFilePollerOptions()
            {
                Files = [fileMain],
                SleepTimeInMilliseconds = 5000,
                Logger = loggerWatcherFilePoller,
                WriteTo = true,
            });

            Console.WriteLine("Press any key to initialize Watcher file write poller");
            Console.ReadKey();

            loggerMain.LogInformation("Initializing Watcher file write poller");
            poller.InitializeFiles();
            loggerMain.LogInformation("Watcher file write poller initialized");

            Console.WriteLine("Press any key to start Watcher file write poller updater");
            Console.ReadKey();

            loggerMain.LogInformation("Starting Watcher file write poller updater");
            var token = new CancellationTokenSource();
            var updateInfo = new UpdateThreadInfo()
            {
                Poller = poller,
                Token = token.Token,
                Logger = loggerMain,
            };
            var updateThread = new Thread(new ParameterizedThreadStart(UpdateThread));
            updateThread.Start(updateInfo);
            loggerMain.LogInformation("Watcher file write poller updater started");

            Console.WriteLine("Press any key to start Watcher file write poller");
            Console.ReadKey();

            loggerMain.LogInformation("Starting Watcher file write poller");
            poller.StartPollingThread();
            loggerMain.LogInformation("Watcher file write poller started");

            Console.WriteLine("Poller started, press any key to arm panic mode...");
            Console.ReadKey();

            if (poller.IsPollerUnresponsive)
            {
                loggerMain.LogCritical("Poller is unresponsive, now that was unexpected");
            }
            else
            {
                loggerMain.LogInformation("Poller remains responsive, as expected");
            }

            loggerMain.LogInformation("Arming panic mode");
            poller.ArmPanicMode(false);
            loggerMain.LogInformation("Panic mode armed");

            Console.WriteLine("Poller panic mode armed, press K to stop update thread, press P to declare panic, press S to switch to safe mode, press any other key to skip safe mode and end poller...");
            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.K)
            {
                token.Cancel();

                Console.WriteLine("Stopped update thread, press S to switch to safe mode, press any other key to skip safe mode and end poller...");
                key = Console.ReadKey();
            }
            else if (key.Key == ConsoleKey.P)
            {
                poller.UpdateState(WatcherFileState.Panic);

                Console.WriteLine("Updated state to panic, press S to switch to safe mode, press any other key to skip safe mode and end poller...");
                key = Console.ReadKey();
            }

            if (key.Key == ConsoleKey.S)
            {

                loggerMain.LogInformation("Entering safe mode");
                poller.EnterSafeMode(false);
                loggerMain.LogInformation("Safe mode entered");

                Console.WriteLine("Poller in safe mode, press K to stop update thread, press P to declare panic, press any other key to end poller...");
                key = Console.ReadKey();

                if (key.Key == ConsoleKey.K)
                {
                    token.Cancel();

                    Console.WriteLine("Stopped update thread, press any key to end poller...");
                    Console.ReadKey();
                }
                else if (key.Key == ConsoleKey.P)
                {
                    poller.UpdateState(WatcherFileState.Panic);

                    Console.WriteLine("Updated state to panic, press any key to end poller...");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("Skipping safe mode and ending poller now");
            }

            loggerMain.LogInformation("Stopping Watcher file poller");
            poller.StopPollingThread(true);
            token?.Cancel();
            loggerMain.LogInformation("Watcher file poller stopped");

            Console.WriteLine("Poller stopped, press any key to end application");
            Console.ReadKey();

        }

        private static void Test_FilePollerRead(ILogger loggerMain)
        {
            var loggerWatcherFilePoller = Generator.GetLogger("WatcherFile.Poller");
            var loggerWatcherFilePollerFile = Generator.GetLogger("WatcherFile.Poller.File");

            WatcherPollerFile fileMain = new(new WatcherPollerFileOptions()
            {
                WatcherFileName = "DefaultWrite",
                FilePath = @"\\Babylon\TestShare\DefaultWrite.awf",
                Logger = loggerWatcherFilePollerFile,
                MaxUpdateAgeInSeconds = 15,
                WriteStateOnPanic = true,
            });

            WatcherFilePoller poller = new(new WatcherFilePollerOptions()
            {
                Files = [fileMain],
                SleepTimeInMilliseconds = 5000,
                Logger = loggerWatcherFilePoller,
            });

            Console.WriteLine("Press any key to start Watcher file write poller updater");
            Console.ReadKey();

            loggerMain.LogInformation("Starting Watcher file write poller updater");
            var token = new CancellationTokenSource();
            var updateInfo = new UpdateThreadInfo()
            {
                Poller = poller,
                Token = token.Token,
                Logger = loggerMain,
            };
            var updateThread = new Thread(new ParameterizedThreadStart(UpdateThread));
            updateThread.Start(updateInfo);
            loggerMain.LogInformation("Watcher file write poller updater started");

            Console.WriteLine("Press S to skip initializing Watcher file read poller or press any other key to initialize Watcher file read poller");
            var key = Console.ReadKey();

            if (key.Key != ConsoleKey.S)
            {
                loggerMain.LogInformation("Initializing Watcher file read poller");
                poller.InitializeFiles();
                loggerMain.LogInformation("Watcher file read poller initialized");
            }
            else
            {
                loggerMain.LogInformation("Skipping initialization of Watcher file read poller");
            }

            Console.WriteLine("Press any key to start Watcher file read poller");
            Console.ReadKey();

            loggerMain.LogInformation("Starting Watcher file read poller");
            poller.StartPollingThread();
            loggerMain.LogInformation("Watcher file read poller started");

            Console.WriteLine("Poller started, press any key to arm panic mode...");
            Console.ReadKey();

            loggerMain.LogInformation("Arming panic mode");
            poller.ArmPanicMode(false);
            loggerMain.LogInformation("Panic mode armed");

            Console.WriteLine("Poller panic mode armed, press S to switch to safe mode, press any other key to skip safe mode and end poller...");
            key = Console.ReadKey();

            if (key.Key == ConsoleKey.S)
            {
                loggerMain.LogInformation("Entering safe mode");
                poller.EnterSafeMode(false);
                loggerMain.LogInformation("Safe mode entered");

                Console.WriteLine("Poller in safe mode, press any key to end poller...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Skipping safe mode and ending poller");
            }

            loggerMain.LogInformation("Stopping Watcher file poller");
            poller.StopPollingThread(true);
            loggerMain.LogInformation("Watcher file poller stopped");

            Console.WriteLine("Poller stopped, press any key to end application");
            Console.ReadKey();

        }

        private static void Test_SwitchBotDirect(bool testWrongSwitchBotId)
        {
            var loggerSwitchBotApi = Generator.GetLogger("SwitchBot.Api");

            Console.WriteLine("Setting up SwitchBot Api...");
            var switchBotApi = new SwitchBotAPI(new SwitchBotAPIOptions()
            {
                //BaseUrl = "https://fritz.box/",
                //BaseUrl = "https://google.com/",
                Token = "33a1813627634407129112a8923f378db4e3371d3227309d703fcd7525aa800aacc2707c3380dcdee9e9fe9d907f4d3d",
                Secret = "e9d1a54c20b5ff4167e491ecf2243398",
                Logger = loggerSwitchBotApi,
                ReloadNamesIfNotFound = true,
                //AutoRetryCount = 0,
                //IgnoreSSLError = true,
            });
            Console.WriteLine("Retrieving switch bots...");
            var lstBots = switchBotApi.GetSwitchBotList();

            Console.WriteLine("Retrieved the following bots...");
            lstBots.ForEach(itm => Console.WriteLine($"\tId: {itm.Id}, Name: {itm.Name}, Type: {itm.DeviceType}, CloudServiceEnabled: {itm.CloudServiceEnabled}"));

            Console.WriteLine("Loading switch bots...");
            switchBotApi.LoadSwitchBotNames();

            Dictionary<string, string>? dicBots = switchBotApi.GetCachedSwitchBotNames();
            Console.WriteLine("Loaded the following bots...");
#pragma warning disable CS8602
            foreach (KeyValuePair<string, string> kvp in dicBots)
#pragma warning restore CS8602
            {
                Console.WriteLine($"\tName: {kvp.Key} => Id: {kvp.Value}");
            }

            Console.WriteLine("\r\n------------------------------\r\n");

            if (testWrongSwitchBotId)
            {
                try
                {
                    Console.WriteLine("Getting state of unknown bot id 1234");
                    SwitchBotStateContent stateX = switchBotApi.GetSwitchBotState("1234");
                    Console.WriteLine("Unexcpected successful retrieving of non-existing bot by id");
                }
                catch (SwitchBotAPIResponseException httpex)
                {
                    Console.WriteLine($@"Expected http exception ""{httpex.GetType().FullName}"": {httpex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Excpected exception ""{ex.GetType().FullName}"": {ex.Message}");
                }

                Console.WriteLine("\r\n------------------------------\r\n");

                try
                {
                    Console.WriteLine("Getting state of unknown bot ANUBIS_SwitchBotX");
                    SwitchBotStateContent stateX = switchBotApi.GetSwitchBotStateByName("ANUBIS_SwitchBotX");
                    Console.WriteLine("Unexcpected successful retrieving of non-existing bot");
                }
                catch (SwitchBotAPINameNotFoundException nnfex)
                {
                    Console.WriteLine($@"Expected NameNotFound exception ""{nnfex.GetType().FullName}"": {nnfex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Unexcpected exception ""{ex.GetType().FullName}"": {ex.Message}");
                }

                Console.WriteLine("\r\n------------------------------\r\n");

            }

            Console.WriteLine("Getting start-state of bot ANUBIS_SwitchBot");
            SwitchBotStateContent stateStart = switchBotApi.GetSwitchBotStateByName("ANUBIS_SwitchBot");
            Console.WriteLine("State of bot ANUBIS_SwitchBot at start...");
            Console.WriteLine($"\tId: {stateStart.DeviceId}");
            Console.WriteLine($"\tType: {stateStart.DeviceType}");
            Console.WriteLine($"\tMode: {stateStart.Mode}");
            Console.WriteLine($"\tPower: {stateStart.Power}");
            Console.WriteLine($"\tBattery: {stateStart.Battery}");
            Console.WriteLine($"\tFirmware Version: {stateStart.Version}");
            Console.WriteLine($"\tHub Id: {stateStart.HubId}");

            Console.WriteLine("\r\n------------------------------\r\n");

            Console.WriteLine("Turning switch bot ANUBIS_SwitchBot on");
            var stateOn = switchBotApi.TurnSwitchBotOnByName("ANUBIS_SwitchBot", true);
            Console.WriteLine("State of bot ANUBIS_SwitchBot on turning on...");
            Console.WriteLine($"\tPower: {stateOn.Power}");
            Console.WriteLine($"\tBattery: {stateOn.Battery}");
            Console.WriteLine($"\tCode: {stateOn.Code}");
            Console.WriteLine($"\tConnectedToHub: {stateOn.ConnectedToHub}");
            Console.WriteLine("Waiting 5 seconds...");
            Thread.Sleep(5000);

            Console.WriteLine("\r\n------------------------------\r\n");

            Console.WriteLine("Getting state of bot ANUBIS_SwitchBot after turning it on");
            SwitchBotStateContent stateAfterOn = switchBotApi.GetSwitchBotStateByName("ANUBIS_SwitchBot");
            Console.WriteLine("State of bot ANUBIS_SwitchBot after turning it on...");
            Console.WriteLine($"\tId: {stateAfterOn.DeviceId}");
            Console.WriteLine($"\tType: {stateAfterOn.DeviceType}");
            Console.WriteLine($"\tMode: {stateAfterOn.Mode}");
            Console.WriteLine($"\tPower: {stateAfterOn.Power}");
            Console.WriteLine($"\tBattery: {stateAfterOn.Battery}");
            Console.WriteLine($"\tFirmware Version: {stateAfterOn.Version}");
            Console.WriteLine($"\tHub Id: {stateAfterOn.HubId}");

            Console.WriteLine("\r\n------------------------------\r\n");

            Console.WriteLine("Pressing switch bot ANUBIS_SwitchBot");
            var statePress = switchBotApi.PressSwitchBotByName("ANUBIS_SwitchBot");
            Console.WriteLine($"\tPower: {statePress.Power}");
            Console.WriteLine($"\tBattery: {statePress.Battery}");
            Console.WriteLine($"\tCode: {stateOn.Code}");
            Console.WriteLine($"\tConnectedToHub: {stateOn.ConnectedToHub}");
            Console.WriteLine("Waiting 5 seconds...");
            Thread.Sleep(5000);

            Console.WriteLine("\r\n------------------------------\r\n");

            Console.WriteLine("Getting state of bot ANUBIS_SwitchBot after pressing it");
            SwitchBotStateContent stateAfterPress = switchBotApi.GetSwitchBotStateByName("ANUBIS_SwitchBot");
            Console.WriteLine("State of bot ANUBIS_SwitchBot after pressing it...");
            Console.WriteLine($"\tId: {stateAfterPress.DeviceId}");
            Console.WriteLine($"\tType: {stateAfterPress.DeviceType}");
            Console.WriteLine($"\tMode: {stateAfterPress.Mode}");
            Console.WriteLine($"\tPower: {stateAfterPress.Power}");
            Console.WriteLine($"\tBattery: {stateAfterPress.Battery}");
            Console.WriteLine($"\tFirmware Version: {stateAfterPress.Version}");
            Console.WriteLine($"\tHub Id: {stateAfterPress.HubId}");

            Console.WriteLine("\r\n------------------------------\r\n");

            Console.WriteLine("Turning switch bot ANUBIS_SwitchBot off");
            var stateOff = switchBotApi.TurnSwitchBotOffByName("ANUBIS_SwitchBot", true);
            Console.WriteLine("State of bot ANUBIS_SwitchBot on turning off...");
            Console.WriteLine($"\tPower: {stateOff.Power}");
            Console.WriteLine($"\tBattery: {stateOff.Battery}");
            Console.WriteLine($"\tCode: {stateOn.Code}");
            Console.WriteLine($"\tConnectedToHub: {stateOn.ConnectedToHub}");
            //Console.WriteLine("Waiting 5 seconds...");
            //Thread.Sleep(5000);

            Console.WriteLine("\r\n------------------------------\r\n");

            Console.WriteLine("Getting state of bot ANUBIS_SwitchBot after turning it off");
            SwitchBotStateContent stateAfterOff = switchBotApi.GetSwitchBotStateByName("ANUBIS_SwitchBot");
            Console.WriteLine("State of bot ANUBIS_SwitchBot after turning it off...");
            Console.WriteLine($"\tId: {stateAfterOff.DeviceId}");
            Console.WriteLine($"\tType: {stateAfterOff.DeviceType}");
            Console.WriteLine($"\tMode: {stateAfterOff.Mode}");
            Console.WriteLine($"\tPower: {stateAfterOff.Power}");
            Console.WriteLine($"\tBattery: {stateAfterOff.Battery}");
            Console.WriteLine($"\tFirmware Version: {stateAfterOff.Version}");
            Console.WriteLine($"\tHub Id: {stateAfterOff.HubId}");

            Console.WriteLine("\r\n------------------------------\r\n");

            Console.WriteLine("Turning switch bot ANUBIS_SwitchBot off for 2nd time");
            var stateOff2 = switchBotApi.TurnSwitchBotOffByName("ANUBIS_SwitchBot", true);
            Console.WriteLine("State of bot ANUBIS_SwitchBot on turning off for 2nd time...");
            Console.WriteLine($"\tPower: {stateOff2.Power}");
            Console.WriteLine($"\tBattery: {stateOff2.Battery}");
            Console.WriteLine($"\tCode: {stateOff2.Code}");
            Console.WriteLine($"\tConnectedToHub: {stateOff2.ConnectedToHub}");

            Console.WriteLine("\r\n------------------------------\r\n");

            Console.WriteLine("Getting end-state of bot ANUBIS_SwitchBot");
            SwitchBotStateContent stateEnd = switchBotApi.GetSwitchBotStateByName("ANUBIS_SwitchBot");
            Console.WriteLine("State of bot ANUBIS_SwitchBot at end...");
            Console.WriteLine($"\tId: {stateEnd.DeviceId}");
            Console.WriteLine($"\tType: {stateEnd.DeviceType}");
            Console.WriteLine($"\tMode: {stateEnd.Mode}");
            Console.WriteLine($"\tPower: {stateEnd.Power}");
            Console.WriteLine($"\tBattery: {stateEnd.Battery}");
            Console.WriteLine($"\tFirmware Version: {stateEnd.Version}");
            Console.WriteLine($"\tHub Id: {stateEnd.HubId}");

        }

        private static void Test_SwitchBotPoller(ILogger loggerMain)
        {
            var loggerSwitchBotApi = Generator.GetLogger("SwitchBot.Api");
            var loggerSwitchBotPoller = Generator.GetLogger("SwitchBot.Poller");
            var loggerSwitchBotPollerSwitch = Generator.GetLogger("SwitchBot.Poller.Switch");

            Console.WriteLine("Setting up SwitchBot Api...");
            var switchBotApi = new SwitchBotAPI(new SwitchBotAPIOptions()
            {
                //BaseUrl = "https://fritz.box/",
                //BaseUrl = "https://google.com/",
                Token = "33a1813627634407129112a8923f378db4e3371d3227309d703fcd7525aa800aacc2707c3380dcdee9e9fe9d907f4d3d",
                Secret = "e9d1a54c20b5ff4167e491ecf2243398",
                Logger = loggerSwitchBotApi,
                ReloadNamesIfNotFound = true,
                //AutoRetryCount = 0,
                //IgnoreSSLError = true,
            });
            Console.WriteLine("Retrieving switch bots...");
            var lstBots = switchBotApi.GetSwitchBotList();

            Console.WriteLine("Retrieved the following bots...");
            lstBots.ForEach(itm => Console.WriteLine($"\tId: {itm.Id}, Name: {itm.Name}, Type: {itm.DeviceType}, CloudServiceEnabled: {itm.CloudServiceEnabled}"));

            Console.WriteLine("Loading switch bots...");
            switchBotApi.LoadSwitchBotNames();

            Dictionary<string, string>? dicBots = switchBotApi.GetCachedSwitchBotNames();
            Console.WriteLine("Loaded the following bots...");
#pragma warning disable CS8602
            foreach (KeyValuePair<string, string> kvp in dicBots)
#pragma warning restore CS8602
            {
                Console.WriteLine($"\tName: {kvp.Key} => Id: {kvp.Value}");
            }

            Console.WriteLine("\r\n------------------------------\r\n");

            SwitchBotPollerSwitch switchMain = new(new SwitchBotPollerSwitchOptions()
            {
                Api = switchBotApi,
                SwitchBotName = "ANUBIS_SwitchBot",
                //MinBattery = 90,
                Logger = loggerSwitchBotPollerSwitch,
                TurnOffOnPanic = true,
                // StateCheck = true,
                //EnterSafeMode = false,
                //StrictStateCheck = true,
                //StrictBatteryCheck = true,
                //ZeroBatteryIsValidPanic = true,
            });

            Dictionary<string, string>? dicSwitches = switchBotApi.GetCachedSwitchBotNames() ?? throw new Exception("Error while trying to copy cached switch names");
            loggerMain.LogInformation("Found the following switch bots: {@switches}", dicSwitches);

            Console.WriteLine("Found the following switch bots with their ids:");
            if (dicSwitches.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in dicSwitches)
                {
                    Console.WriteLine($"\t{kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                Console.WriteLine("<NONE>");
            }

            Console.WriteLine("Press any key to set Switch bot ON or press F to turn Switch bot Off or press S to skip this step");
            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.S)
            {
                Console.WriteLine("Skipped turning on switch bot");
            }
            else if (key.Key == ConsoleKey.F)
            {
                switchBotApi.TurnSwitchBotOffByName(switchMain.Options.SwitchBotName);
                Console.WriteLine("Switch bot turned off");
            }
            else
            {
                switchBotApi.TurnSwitchBotOnByName(switchMain.Options.SwitchBotName);
                Console.WriteLine("Switch bot turned on");
            }

            Console.WriteLine("Press any key to start Switch bot poller");
            Console.ReadKey();

            loggerMain.LogInformation("Starting Switch bot poller");
            SwitchBotPoller poller = new(new SwitchBotPollerOptions()
            {
                Switches = [switchMain],
                //SleepTimeInMilliseconds = 15000,
                Logger = loggerSwitchBotPoller,
            });
            poller.StartPollingThread();
            loggerMain.LogInformation("Switch bot poller started");

            Console.WriteLine("Poller started, press any key to arm panic mode...");
            Console.ReadKey();

            loggerMain.LogInformation("Arming panic mode");
            poller.ArmPanicMode(false);
            loggerMain.LogInformation("Panic mode armed");

            Console.WriteLine("Poller panic mode armed, press any key to switch to safe mode...");
            Console.ReadKey();

            loggerMain.LogInformation("Entering safe mode");
            poller.EnterSafeMode(false);
            loggerMain.LogInformation("Safe mode entered");

            Console.WriteLine("Poller in safe mode, press any key to end poller...");
            Console.ReadKey();

            loggerMain.LogInformation("Stopping Switch bot poller");
            //Generator.CancelGlobalCancellationToken();
            //Generator.InitCancellationToken();
            poller.StopPollingThread();
            loggerMain.LogInformation("Switch bot poller stopped");

            Console.WriteLine("Poller stopped, press any key to restart");
            Console.ReadKey();

            poller.StartPollingThread();

            Console.WriteLine("Press any key to end poller...");
            Console.ReadKey();

            loggerMain.LogInformation("Stopping Switch bot poller");
            //poller.StopPollingThread();
            Generator.CancelGlobalCancellationToken();
            loggerMain.LogInformation("Switch bot poller stopped");

            Console.WriteLine("Poller stopped, press any key to end application");
            Console.ReadKey();
        }

        private static void Test_USBDirect(ILogger loggerMain)
        {
            var loggerClewareApi = Generator.GetLogger("Cleware.Api");

            var clewareApi = new ClewareAPI(new ClewareAPIOptions()
            {
                USBSwitchNameList = new Dictionary<string, long>()
                {
                    { "OldSwitch", 563239 },
                    { "NewSwitch1", 563248 },
                    { "NewSwitch2", 563249 },
                    { "MissingSwitch", 563250 },
                },
                Logger = loggerClewareApi,
                //AutoRetryCount = 0,
                //IgnoreSSLError = true,
            });

            Console.WriteLine("Getting list of ids");
            var switchIds = clewareApi.GetUSBSwitchIds();
            Console.WriteLine("List: ");
            foreach (var singleSwitch in switchIds)
            {
                Console.WriteLine($"\t{singleSwitch}");
            }

            Console.WriteLine("Getting list of names");
            var switchNames = clewareApi.GetUSBSwitchNames();
            Console.WriteLine("List: ");
            foreach (var singleSwitch in switchNames)
            {
                Console.WriteLine($"\t{singleSwitch}");
            }

            Console.WriteLine("Press any key for next step...");
            Console.ReadKey();

            Console.WriteLine($"Getting switch state OldSwitch");
            Console.WriteLine($"Switch state result: {clewareApi.GetUSBSwitchStateByName("OldSwitch")}");

            Console.WriteLine("Turning on OldSwitch");
            var resultOn1b = clewareApi.TurnUSBSwitchOnByName("OldSwitch");
            Console.WriteLine($"Turning on result: {resultOn1b}");

            Console.WriteLine($"Getting switch state OldSwitch");
            Console.WriteLine($"Switch state result: {clewareApi.GetUSBSwitchStateByName("OldSwitch")}");

            Console.WriteLine("Press any key for next step...");
            Console.ReadKey();

            Console.WriteLine("Turning on OldSwitch");
            var resultOn1a = clewareApi.TurnUSBSwitchOnByName("OldSwitch");
            Console.WriteLine($"Turning on result: {resultOn1a}");

            Console.WriteLine($"Getting switch state OldSwitch");
            Console.WriteLine($"Switch state result: {clewareApi.GetUSBSwitchStateByName("OldSwitch")}");

            Console.WriteLine("Turning off OldSwitch");
            var resultOff1a = clewareApi.TurnUSBSwitchOffByName("OldSwitch");
            Console.WriteLine($"Turning off result: {resultOff1a}");

            Console.WriteLine($"Getting switch state OldSwitch");
            Console.WriteLine($"Switch state result: {clewareApi.GetUSBSwitchStateByName("OldSwitch")}");


            Console.WriteLine("Press any key for next step...");
            Console.ReadKey();

            Console.WriteLine("Turning on OldSwitch nonsecure");
            var resultOn1x = clewareApi.TurnUSBSwitchOnByName("OldSwitch", false);
            Console.WriteLine($"Turning on result: {resultOn1x}");

            Console.WriteLine($"Getting switch state OldSwitch");
            Console.WriteLine($"Switch state result: {clewareApi.GetUSBSwitchStateByName("OldSwitch")}");

            Console.WriteLine("Press any key for next step...");
            Console.ReadKey();

            Console.WriteLine("Turning off OldSwitch nonsecure");
            var resultOff1x = clewareApi.TurnUSBSwitchOffByName("OldSwitch", false);
            Console.WriteLine($"Turning off result: {resultOff1x}");

            Console.WriteLine($"Getting switch state OldSwitch");
            Console.WriteLine($"Switch state result: {clewareApi.GetUSBSwitchStateByName("OldSwitch")}");

            Console.WriteLine("Press any key for next step...");
            Console.ReadKey();

            loggerMain.LogInformation("Starting with second ClewareAPI tests");


            Console.WriteLine($"Turning on {switchIds[0]}");
            var resultOn1 = clewareApi.TurnUSBSwitchOn(switchIds[0]);
            Console.WriteLine($"Turning on {switchIds[0]} result: {resultOn1}");

            Console.WriteLine($"Getting switch state {switchIds[0]}");
            var resultState1 = clewareApi.GetUSBSwitchState(switchIds[0]);
            Console.WriteLine($"Switch state result: {resultState1}");

            Console.WriteLine("Press any key for next step...");
            Console.ReadKey();

            Console.WriteLine($"Turning off {switchIds[0]}");
            var resultOff1 = clewareApi.TurnUSBSwitchOff(switchIds[0]);
            Console.WriteLine($"Turning off result: {resultOff1}");

            Console.WriteLine($"Getting switch state {switchIds[0]}");
            var resultState2 = clewareApi.GetUSBSwitchState(switchIds[0]);
            Console.WriteLine($"Switch state result: {resultState2}");

            Console.WriteLine("Press any key for next step...");
            Console.ReadKey();

            Console.WriteLine($"Turning on {switchIds[0]}");
            var resultOn2 = clewareApi.TurnUSBSwitchOn(switchIds[0]);
            Console.WriteLine($"Turning on result: {resultOn2}");

            Console.WriteLine($"Getting switch state {switchIds[0]}");
            var resultState3 = clewareApi.GetUSBSwitchState(switchIds[0]);
            Console.WriteLine($"Switch state result: {resultState3}");

            Console.WriteLine("Press any key for next step...");
            Console.ReadKey();

            Console.WriteLine($"Turning on {switchIds[0]}");
            var resultOff3 = clewareApi.TurnUSBSwitchOn(switchIds[0]);
            Console.WriteLine($"Turning off result: {resultOff3}");

            Console.WriteLine($"Getting switch state {switchIds[0]}");
            var resultState4 = clewareApi.GetUSBSwitchState(switchIds[0]);
            Console.WriteLine($"Switch state result: {resultState4}");

            loggerMain.LogInformation("FritzAPI tests successfully ended");
        }

        private static void Test_FritzDirect(ILogger loggerMain)
        {
            var loggerFritzApi = Generator.GetLogger("Fritz.Api");

            var fritzApi = new FritzAPI(new FritzAPIOptions()
            {
                BaseUrl = "http://fritz.box",
                User = "fritz2029",
                Password = "sommer0174",
                Logger = loggerFritzApi,
                //AutoRetryCount = 0,
                //IgnoreSSLError = true,
            });

            fritzApi.Login();
            Console.WriteLine($"Session: {fritzApi.SessionId}");
            Console.WriteLine("Turning on");
            var resultOn1b = fritzApi.TurnSwitchOnByName("Mainiac");
            Console.WriteLine($"Turning on result: {resultOn1b}");

            Console.WriteLine("Turning on");
            var resultOn1a = fritzApi.TurnSwitchOnByName("ANUBIS_Main");
            Console.WriteLine($"Turning on result: {resultOn1a}");
            Console.WriteLine("Turning off");
            var resultOff1a = fritzApi.TurnSwitchOffByName("ANUBIS_Main");
            Console.WriteLine($"Turning off result: {resultOff1a}");


            loggerMain.LogInformation("Starting with second FritzAPI tests");


            fritzApi.Login();
            Console.WriteLine("Successful login");
            Console.WriteLine("Getting list");
            var switches = fritzApi.GetSwitchList();
            Console.WriteLine("List: ");
            foreach (var singleSwitch in switches)
            {
                Console.WriteLine($"\t{singleSwitch}");
            }
            Console.WriteLine("Turning on");
            var resultOn1 = fritzApi.TurnSwitchOn(switches[0]);
            Console.WriteLine($"Turning on result: {resultOn1}");
            Console.WriteLine($"IsLoggedIn: {fritzApi.IsLoggedIn()}");

            Console.WriteLine("Getting switch state");
            var resultState1 = fritzApi.GetSwitchState(switches[0]);
            Console.WriteLine($"Switch state result: {resultState1}");

            Console.WriteLine("Getting switch presence");
            var resultPresence1 = fritzApi.GetSwitchPresence(switches[0]);
            Console.WriteLine($"Switch state presence: {resultPresence1}");

            Console.WriteLine("Getting switch power");
            Thread.Sleep(30000);
            var resultPower1 = fritzApi.GetSwitchPower(switches[0]);
            Console.WriteLine($"Switch power result: {resultPower1}");

            Console.WriteLine("Getting switch name");
            var resultName1 = fritzApi.GetSwitchName(switches[0]);
            Console.WriteLine($"Switch name result: {resultName1}");

            Console.WriteLine("Turning off");
            var resultOff1 = fritzApi.TurnSwitchOff(switches[0]);
            Console.WriteLine($"Turning off result: {resultOff1}");

            Thread.Sleep(1000);
            Console.WriteLine("Toggle switch on");
            var resultToggleOn1 = fritzApi.ToggleSwitchState(switches[0]);
            Console.WriteLine($"Toggle switch on result: {resultToggleOn1}");

            Thread.Sleep(1000);
            Console.WriteLine("Toggle switch off");
            var resultToggleOff1 = fritzApi.ToggleSwitchState(switches[0]);
            Console.WriteLine($"Toggle switch off result: {resultToggleOff1}");

            Console.WriteLine("Logout");
            fritzApi.Logout();
            Console.WriteLine($"IsLoggedIn: {fritzApi.IsLoggedIn()}");

            //Console.WriteLine($"Checking SID: {fritzApi.CheckSID()}");
            Console.WriteLine("Turning on");
            var resultOn2 = fritzApi.TurnSwitchOn(switches[0]);
            Console.WriteLine($"Turning on result: {resultOn2}");

            Console.WriteLine("Getting switch state");
            var resultState2 = fritzApi.GetSwitchState(switches[0]);
            Console.WriteLine($"Switch state result: {resultState2}");

            Console.WriteLine("Getting switch presence");
            var resultPresence2 = fritzApi.GetSwitchPresence(switches[0]);
            Console.WriteLine($"Switch state presence: {resultPresence2}");

            Console.WriteLine("Getting switch power");
            Thread.Sleep(30000);
            var resultPower2 = fritzApi.GetSwitchPower(switches[0]);
            Console.WriteLine($"Switch power result: {resultPower2}");

            Console.WriteLine("Getting switch name");
            var resultName2 = fritzApi.GetSwitchName(switches[0]);
            Console.WriteLine($"Switch name result: {resultName2}");

            Console.WriteLine("Turning off");
            var resultOff2 = fritzApi.TurnSwitchOff(switches[0]);
            Console.WriteLine($"Turning off result: {resultOff2}");

            Thread.Sleep(1000);
            Console.WriteLine("Toggle switch on");
            var resultToggleOn2 = fritzApi.ToggleSwitchState(switches[0]);
            Console.WriteLine($"Toggle switch on result: {resultToggleOn2}");

            Thread.Sleep(1000);
            Console.WriteLine("Toggle switch off");
            var resultToggleOff2 = fritzApi.ToggleSwitchState(switches[0]);
            Console.WriteLine($"Toggle switch off result: {resultToggleOff2}");

            loggerMain.LogInformation("FritzAPI tests successfully ended");
        }

        private static void Test_FritzPoller(ILogger loggerMain)
        {
            var loggerFritzApi = Generator.GetLogger("Fritz.Api");
            var loggerFritzPoller = Generator.GetLogger("Fritz.Poller");
            var loggerFritzPollerSwitch = Generator.GetLogger("Fritz.Poller.Switch");

            var fritzApi = new FritzAPI(new FritzAPIOptions()
            {
                BaseUrl = "http://fritz.box",
                User = "fritz2029",
                Password = "sommer0174",
                Logger = loggerFritzApi,
                //AutoRetryCount = 0,
                //IgnoreSSLError = true,
            });


            loggerMain.LogInformation("Logging in to FritzAPI");
            fritzApi.Login();

            FritzPollerSwitch switchMain = new(new FritzPollerSwitchOptions()
            {
                Api = fritzApi,
                SwitchName = "ANUBIS_Main",
                LowPowerCutOff = 300,
                MinPower = 500,
                MaxPower = 200000,
                SafeModePowerUpAlarm = 250,
                Logger = loggerFritzPollerSwitch,
                TurnOffOnPanic = true,
                //fritzApi, "Main", true, 10, null, logger: fritzApi.Options.Logger
            });

            FritzPollerSwitch switchVentil = new(new FritzPollerSwitchOptions()
            {
                Api = fritzApi,
                SwitchName = "ANUBIS_Ventil",
                LowPowerCutOff = 100,
                MinPower = 100,
                MaxPower = 100000,
                SafeModePowerUpAlarm = 100,
                Logger = loggerFritzPollerSwitch,
                TurnOffOnPanic = true,
                //fritzApi, "Main", true, 10, null, logger: fritzApi.Options.Logger
            });

            Dictionary<string, string>? dicSwitches = fritzApi.GetCachedSwitchNames() ?? throw new Exception("Error while trying to copy cached switch names");
            loggerMain.LogInformation("Found the following switches: {@switches}", dicSwitches);

            Console.WriteLine("Found the following switches with their ids:");
            if (dicSwitches.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in dicSwitches)
                {
                    Console.WriteLine($"\t{kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                Console.WriteLine("<NONE>");
            }

            Console.WriteLine("Press any key to start Fritz poller");
            Console.ReadKey();

            loggerMain.LogInformation("Starting Fritz poller");
            FritzPoller poller = new(new FritzPollerOptions()
            {
                Switches = [switchMain, switchVentil],
                SleepTimeInMilliseconds = 5000,
                Logger = loggerFritzPoller
            });
            poller.StartPollingThread();
            loggerMain.LogInformation("Fritz poller started");

            Console.WriteLine("Poller started, press any key to arm holdback mode...");
            Console.ReadKey();
            poller.EnterHoldBackMode(false);

            Console.WriteLine("In holdback mode, press any key to arm panic mode...");
            Console.WriteLine("Press S to skip arming panic mode, Press R to reset any panic...");
            var key = Console.ReadKey();
            bool armed = false;

            while (key.Key != ConsoleKey.S && !armed)
            {
                if (key.Key == ConsoleKey.R)
                {
                    loggerMain.LogInformation("Resetting panic");
                    poller.ResetPanic();
                }
                else
                {
                    loggerMain.LogInformation("Arming panic mode");
                    armed = poller.ArmPanicMode(false);
                }

                if (armed)
                {
                    loggerMain.LogInformation("Panic mode armed");
                }
                else
                {
                    Console.WriteLine("Press S to skip arming panic mode, Press R to reset any panic...");
                    key = Console.ReadKey();
                }
            }



            Console.WriteLine("Press any key to switch to safe mode...");
            Console.ReadKey();

            loggerMain.LogInformation("Entering safe mode");
            poller.EnterSafeMode(false);
            loggerMain.LogInformation("Safe mode entered");

            Console.WriteLine("Poller in safe mode, press any key to end poller...");
            Console.ReadKey();

            loggerMain.LogInformation("Stopping Fritz poller");
            poller.StopPollingThread();
            loggerMain.LogInformation("Fritz poller stopped");

            Console.WriteLine("Poller stopped, press any key to end application");
            Console.ReadKey();
        }

        private static void Test_USBPoller(ILogger loggerMain)
        {
            var loggerClewareApi = Generator.GetLogger("Cleware.Api");
            var loggerClewarePoller = Generator.GetLogger("Cleware.Poller");
            var loggerClewarePollerSwitch = Generator.GetLogger("Cleware.Poller.Switch");


            var clewareApi = new ClewareAPI(new ClewareAPIOptions()
            {
                USBSwitchNameList = new Dictionary<string, long>()
                {
                    { "OldSwitch", 563239 },
                    { "NewSwitch1", 563248 },
                    { "NewSwitch2", 563249 },
                    { "MissingSwitch", 563250 },
                },
                Logger = loggerClewareApi,
                //USBSwitchCommand_Path = @"C:\Users\Markus\source\repos\EndlessTest\bin\Release\net6.0\EndlessTest.exe",
                //CommandTimeoutSeconds = 3,
                //AutoRetryCount = 0,
                //IgnoreSSLError = true,
            });


            ClewarePollerSwitch switchOld = new(new ClewarePollerSwitchOptions()
            {
                Api = clewareApi,
                USBSwitchName = "OldSwitch",
                SafeModeTurnOnAlarm = true,
                Logger = loggerClewarePollerSwitch,
                TurnOffOnPanic = true,
                //fritzApi, "Main", true, 10, null, logger: fritzApi.Options.Logger
            });

            ClewarePollerSwitch switchNew = new(new ClewarePollerSwitchOptions()
            {
                Api = clewareApi,
                USBSwitchName = "NewSwitch1",
                SafeModeTurnOnAlarm = true,
                Logger = loggerClewarePollerSwitch,
                TurnOffOnPanic = true,
                //fritzApi, "Main", true, 10, null, logger: fritzApi.Options.Logger
            });

            Dictionary<string, long> dicSwitches = clewareApi.GetUSBSwitchNames() ?? throw new Exception("Error while trying to get switch names");
            loggerMain.LogInformation("Found the following switches: {@switches}", dicSwitches);

            Console.WriteLine("Found the following switches with their ids:");
            if (dicSwitches.Count > 0)
            {
                foreach (KeyValuePair<string, long> kvp in dicSwitches)
                {
                    Console.WriteLine($"\t{kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                Console.WriteLine("<NONE>");
            }

            Console.WriteLine("Press any key to start Cleware poller");
            Console.ReadKey();

            loggerMain.LogInformation("Starting Cleware poller");
            ClewarePoller poller = new(new ClewarePollerOptions()
            {
                Switches = [switchOld, switchNew],
                //SleepTimeInMilliseconds = 20000,
                Logger = loggerClewarePoller
            });
            poller.StartPollingThread();
            loggerMain.LogInformation("Cleware poller started");

            Console.WriteLine("Poller started, press any key to turn switches on or press S to skip this step...");
            var key = Console.ReadKey();

            if (key.Key != ConsoleKey.S)
            {
                clewareApi.TurnUSBSwitchOnByName(switchOld.Options.USBSwitchName);
                clewareApi.TurnUSBSwitchOnByName(switchNew.Options.USBSwitchName);

                Console.WriteLine("Switches turned");
            }

            Console.WriteLine("Press any key to arm panic mode...");
            Console.ReadKey();

            loggerMain.LogInformation("Arming panic mode");
            poller.ArmPanicMode(false);
            loggerMain.LogInformation("Panic mode armed");

            Console.WriteLine("Poller panic mode armed, press any key to switch to safe mode...");
            Console.ReadKey();

            loggerMain.LogInformation("Entering safe mode");
            poller.EnterSafeMode(false);
            loggerMain.LogInformation("Safe mode entered");

            Console.WriteLine("Poller in safe mode, press any key to end poller...");
            Console.ReadKey();

            loggerMain.LogInformation("Stopping Cleware poller");
            poller.StopPollingThread();
            loggerMain.LogInformation("Cleware poller stopped");

            Console.WriteLine("Poller stopped, press any key to end application");
            Console.ReadKey();
        }

    }
}
