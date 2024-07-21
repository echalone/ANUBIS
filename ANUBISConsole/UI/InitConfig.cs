using ANUBISConsole.ConfigHelpers;
using ANUBISWatcher.Configuration.ConfigFileData;
using ANUBISWatcher.Configuration.ConfigHelpers;
using ANUBISWatcher.Configuration.Serialization;
using ANUBISWatcher.Shared;
using ANUBISWatcher.Triggering.Entities;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Net.NetworkInformation;

namespace ANUBISConsole.UI
{
    internal class InitConfig
    {
        public readonly string CNST_NoConfigLoadedWarning = $"[{AnubisOptions.Options.defaultColor_Warning}]No configuration loaded[/], load a configuration first to enable this option";
        public readonly string CNST_NoConfigLoadedStatus = $"[{AnubisOptions.Options.defaultColor_Warning}]NO[/] configuration loaded";
        public readonly string CNST_SwitchBotDiscoveryWarning = $"[{AnubisOptions.Options.defaultColor_Warning}]Switches controlled by SwitchBot devices might get turned on or off during this procedure[/], make sure to turn them manually on (or off) again, depending on the state they should be in";
        public readonly string CNST_ChoiceInstructionText = $"[{AnubisOptions.Options.defaultColor_Normal}](Press [{AnubisOptions.Options.defaultColor_Choice}]<space>[/] to enable/disable, [{AnubisOptions.Options.defaultColor_Ok}]<enter>[/] to accept)[/]";
        public readonly string CNST_ChoiceMoreText = $"[{AnubisOptions.Options.defaultColor_Normal}](Move up and down for more choices)[/]";

        public bool MainConfiguration()
        {
            ConfigFile? config = null;
            DiscoveryInfo? discoveryInfo = null;

            ILogger? logging = SharedData.InterfaceLogging;

            using (logging?.BeginScope("Interface.InitConfig"))
            {
                bool blExit = false;
                bool blStartControlling = false;
                AnsiConsole.MarkupLine(CNST_NoConfigLoadedStatus);
                do
                {
                    try
                    {
                        var selPromptMainAction = new SelectionPrompt<string>()
                                                        .Title("What do you want to do?")
                                                        .PageSize(AnubisOptions.Options.pageSize)
                                                        .MoreChoicesText(CNST_ChoiceMoreText);

                        selPromptMainAction.HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));
                        //selPromptMainAction.DisabledStyle = new Style(AnubisOptions.Options.defaultColor_Normal);

                        selPromptMainAction.AddChoices(InitConfigCommands.CHOICE_ShowVersions,
                                                        InitConfigCommands.CHOICE_LoadConfigFromFile,
                                                        InitConfigCommands.CHOICE_LoadExampleConfig,
                                                        InitConfigCommands.CHOICE_ShowEnabledDevicesInLoadedConfig,
                                                        InitConfigCommands.CHOICE_EnabledDisableDevices_TurnOn,
                                                        InitConfigCommands.CHOICE_EnabledDisableDevices_Pollers,
                                                        InitConfigCommands.CHOICE_EnabledDisableDevices_Triggers,
                                                        InitConfigCommands.CHOICE_EditConfigValues,
                                                        InitConfigCommands.CHOICE_SaveConfigToFile,
                                                        InitConfigCommands.CHOICE_TurnOnDevices,
                                                        InitConfigCommands.CHOICE_RediscoverDevices,
                                                        InitConfigCommands.CHOICE_RediscoverDevicesTurnOnSwitchBot,
                                                        InitConfigCommands.CHOICE_ResetCountdown,
                                                        InitConfigCommands.CHOICE_LaunchController,
                                                        InitConfigCommands.CHOICE_Exit);

                        var actStart = AnsiConsole.Prompt(selPromptMainAction);

                        AnsiConsole.Clear();

                        if (AnubisConfig.LoadedConfig != null)
                        {
                            string? strT0Time = GetT0Time(AnubisOptions.Options.defaultColor_Info);

                            AnsiConsole.MarkupLine($"Loaded configuration: [{AnubisOptions.Options.defaultColor_Ok}]{AnubisConfig.LoadedConfig}[/]{(strT0Time != null ? $"\r\nT0: {strT0Time}" : "")}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine(CNST_NoConfigLoadedStatus);
                        }

                        if (actStart == InitConfigCommands.CHOICE_ShowVersions)
                        {
                            VersionCollection versions = Generator.GetVersions();
                            AnsiConsole.MarkupLine("These are the software versions of the different ANUBIS parts/domains...");
                            AnsiConsole.MarkupLine($"\tANUBIS Interface name:    [b]{versions.AnubisInterfaceName}[/]");
                            AnsiConsole.MarkupLine($"\tANUBIS Interface version: [b]{versions.AnubisInterface}[/]" + (!string.IsNullOrWhiteSpace(versions.AnubisInterface_Copyright) ? $" ({versions.AnubisInterface_Copyright})" : ""));
                            AnsiConsole.MarkupLine($"\tANUBIS Watcher:           [b]{versions.AnubisWatcher}[/]" + (!string.IsNullOrWhiteSpace(versions.AnubisInterface_Copyright) ? $" ({versions.AnubisInterface_Copyright})" : ""));
                            AnsiConsole.MarkupLine($"\tANUBIS Fritz Api:         [b]{versions.ApiFritz}[/]" + (!string.IsNullOrWhiteSpace(versions.AnubisInterface_Copyright) ? $" ({versions.AnubisInterface_Copyright})" : ""));
                            AnsiConsole.MarkupLine($"\tANUBIS SwitchBot Api:     [b]{versions.ApiSwitchBot}[/]" + (!string.IsNullOrWhiteSpace(versions.AnubisInterface_Copyright) ? $" ({versions.AnubisInterface_Copyright})" : ""));
                            AnsiConsole.MarkupLine($"\tANUBIS ClewareUSB Api:    [b]{versions.ApiClewareUsb}[/]" + (!string.IsNullOrWhiteSpace(versions.AnubisInterface_Copyright) ? $" ({versions.AnubisInterface_Copyright})" : ""));
                            if(!string.IsNullOrWhiteSpace(versions.LicenseText))
                            {
                                AnsiConsole.MarkupLine($"\r\n==================== License =====================");
                                AnsiConsole.MarkupLine(versions.LicenseText.EscapeMarkup());
                                AnsiConsole.MarkupLine($"==================================================");
                            }
                            AnsiConsole.MarkupLine("");
                        }
                        else if (actStart == InitConfigCommands.CHOICE_LoadConfigFromFile)
                        {
                            AnsiConsole.MarkupLine($"Files will be loaded from directory \"[bold]{AnubisOptions.Options.configDirectory}[/]\"");

                            List<AnubisConfig> lstConfigs = AnubisConfig.DiscoverConfigs();

                            var selPromptLoadFile = new SelectionPrompt<AnubisConfig>()
                                    .Title("Which configuration do you want to load?")
                                    .PageSize(AnubisOptions.Options.pageSize);

                            selPromptLoadFile.HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));
                            //selPromptLoadFile.DisabledStyle = new Style(AnubisOptions.Options.defaultColor_Normal);

                            selPromptLoadFile.MoreChoicesText(CNST_ChoiceMoreText)
                                                .AddChoices(AnubisConfig.GetNone())
                                                .AddChoices(lstConfigs);

                            var cfgChosen = AnsiConsole.Prompt(selPromptLoadFile);

                            if (cfgChosen != null && cfgChosen.FullPath != AnubisConfig.CNST_None)
                            {
                                AnsiConsole.Status()
                                            .AutoRefresh(true)
                                            .Spinner(Spinner.Known.Dots)
                                            .SpinnerStyle(AnubisOptions.Options.defaultColor_Ok)
                                            .Start("Loading config and discovering devices...", ctx =>
                                            {
                                                var retVal = ConfigFileManager.ReadAndLoadConfig(cfgChosen.FullPath, true, false);
                                                config = retVal.Item1;
                                                discoveryInfo = retVal.Item2;

                                                if (config != null && SharedData.Config != null && discoveryInfo != null)
                                                {
                                                    AnsiConsole.Clear();

                                                    string? strT0Time = GetT0Time(AnubisOptions.Options.defaultColor_Info);

                                                    AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Ok}]Successfully loaded configuration ""{cfgChosen.FileName}"" from file[/]{(strT0Time != null ? $"\r\nT0: {strT0Time}" : "")}");
                                                    AnubisConfig.LoadedConfig = cfgChosen.FileName;
                                                    SharedData.InterfaceLogging?.LogInformation(@"Loaded configuration ""{config}""", AnubisConfig.LoadedConfig);
                                                }
                                                else
                                                {
                                                    AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Error}]Unable to load configuration ""{cfgChosen.FileName}""[/]");
                                                }
                                            });
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_LoadExampleConfig)
                        {
                            var selPromptLoadExample =
                                new SelectionPrompt<ExampleTypes>()
                                    .Title("Which example configuration do you want to load?")
                                    .PageSize(AnubisOptions.Options.pageSize);

                            selPromptLoadExample.HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));
                            //selPromptLoadExample.DisabledStyle = new Style(AnubisOptions.Options.defaultColor_Normal);

                            selPromptLoadExample.MoreChoicesText(CNST_ChoiceMoreText)
                                                .AddChoices(Examples.GetAllExampleTypes());

                            var cfgChosen = AnsiConsole.Prompt(selPromptLoadExample);

                            if (cfgChosen != ExampleTypes.None)
                            {
                                AnsiConsole.Status()
                                            .AutoRefresh(true)
                                            .Spinner(Spinner.Known.Dots)
                                            .SpinnerStyle(AnubisOptions.Options.defaultColor_Ok)
                                            .Start($@"Loading example config ""{cfgChosen}"" and discovering devices...", ctx =>
                                            {
                                                config = Examples.GetExample(cfgChosen);
                                                discoveryInfo = ConfigFileManager.LoadConfig(config, true, false);

                                                if (config != null && SharedData.Config != null && discoveryInfo != null)
                                                {
                                                    AnsiConsole.Clear();
                                                    
                                                    string? strT0Time = GetT0Time(AnubisOptions.Options.defaultColor_Info);

                                                    AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Ok}]Successfully loaded example configuration ""{cfgChosen}""[/]{(strT0Time != null ? $"\r\nT0: {strT0Time}" : "")}");
                                                    AnubisConfig.LoadedConfig = cfgChosen.ToString() + " (example)";
                                                    SharedData.InterfaceLogging?.LogInformation(@"Loaded configuration ""{config}""", AnubisConfig.LoadedConfig);
                                                }
                                                else
                                                {
                                                    AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Error}]Unable to load example configuration ""{cfgChosen}""[/]");
                                                }
                                            });
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_EditConfigValues)
                        {
                            if (SharedData.Config != null)
                            {
                                bool blReturn = false;

                                do
                                {
                                    string? strT0Time = GetT0Time(AnubisOptions.Options.defaultColor_Info);

                                    AnsiConsole.Clear();
                                    AnsiConsole.MarkupLine($"Loaded configuration: [{AnubisOptions.Options.defaultColor_Ok}]{AnubisConfig.LoadedConfig}[/]{(strT0Time != null ? $"\r\nT0: {strT0Time}" : "")}");

                                    var spEdit = new SelectionPrompt<EditValuesHelper>()
                                                        .Title("What do you want to edit?")
                                                        .PageSize(AnubisOptions.Options.pageSize)
                                                        .MoreChoicesText(CNST_ChoiceMoreText)
                                                        .HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));

                                    //spEdit.DisabledStyle = new Style(AnubisOptions.Options.defaultColor_Normal);

                                    spEdit.AddChoice(EditValuesHelper.GetBackDummy());
                                    EditValuesHelper.AddToSelection(spEdit, true, true, ((CountdownPollerData?)SharedData.Config.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData))?.options);
                                    EditValuesHelper.AddToSelection(spEdit, true, true, ((CountdownPollerData?)SharedData.Config.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is CountdownPollerData))?.mailingOptions);
                                    EditValuesHelper.AddToSelection(spEdit, true, true, ((ControllerData?)SharedData.Config.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is ControllerData))?.options);
                                    EditValuesHelper.AddToSelection(spEdit, true, true, SharedData.Config.fritzApiSettings);
                                    EditValuesHelper.AddToSelection(spEdit, true, true, SharedData.Config.switchApiSettings);

                                    var actEdit = AnsiConsole.Prompt(spEdit);

                                    if (actEdit.IsBackDummy())
                                    {
                                        blReturn = true;
                                    }
                                    else if (!actEdit.IsDummy)
                                    {
                                        actEdit.UpdateValue();
                                    }

                                } while (!blReturn);
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_SaveConfigToFile)
                        {
                            if (SharedData.Config != null)
                            {
                                AnsiConsole.MarkupLine($"Files will be saved in directory \"[bold]{AnubisOptions.Options.configDirectory}[/]\"");

                                List<AnubisConfig> lstConfigs = AnubisConfig.DiscoverConfigs();

                                var selPromptSaveFile =
                                    new SelectionPrompt<AnubisConfig>()
                                        .Title("In what file do you want to save the current configuration?")
                                        .PageSize(AnubisOptions.Options.pageSize);

                                selPromptSaveFile.HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));
                                //selPromptSaveFile.DisabledStyle = new Style(AnubisOptions.Options.defaultColor_Normal);

                                selPromptSaveFile.MoreChoicesText(CNST_ChoiceMoreText)
                                                    .AddChoices(AnubisConfig.GetNone())
                                                    .AddChoices(AnubisConfig.GetNew())
                                                    .AddChoices(lstConfigs);

                                var cfgChosen = AnsiConsole.Prompt(selPromptSaveFile);

                                if (cfgChosen != null && cfgChosen.FullPath != AnubisConfig.CNST_None)
                                {
                                    string strFullPath = cfgChosen.FullPath;

                                    if (strFullPath == AnubisConfig.CNST_New)
                                    {
                                        var name = AnsiConsole.Ask("Please enter name for new config", AnubisConfig.CNST_None.EscapeMarkup());
                                        var escName = name.EscapeMarkup();

                                        if (!string.IsNullOrWhiteSpace(name) && name != AnubisConfig.CNST_None)
                                        {
                                            if (!lstConfigs.Any(itm => itm.FileName.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                                            {
                                                strFullPath = AnubisConfig.GetFullPathByName(name);
                                            }
                                            else
                                            {
                                                AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Error}]Configuration with name ""{escName}"" already exists[/], choose the config from the list to overwrite it");
                                            }
                                        }
                                        else
                                        {
                                            strFullPath = "";
                                        }
                                    }

                                    if (!string.IsNullOrWhiteSpace(strFullPath) && strFullPath != AnubisConfig.CNST_New && strFullPath != AnubisConfig.CNST_None)
                                    {
                                        var name = Path.GetFileNameWithoutExtension(strFullPath);
                                        var escName = name.EscapeMarkup();
                                        if (AnsiConsole.Confirm(@$"Do you want to write configuration to ""{escName}""?", false))
                                        {
                                            AnsiConsole.Status()
                                                        .AutoRefresh(true)
                                                        .Spinner(Spinner.Known.Dots)
                                                        .SpinnerStyle(AnubisOptions.Options.defaultColor_Ok)
                                                        .Start("Saving config...", ctx =>
                                                        {
                                                            if (ConfigFileManager.WriteConfig(strFullPath))
                                                            {
                                                                AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Ok}]Successfully saved configuration to ""{escName}""[/]");
                                                            }
                                                            else
                                                            {
                                                                AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Error}]Unable to save configuration to ""{escName}""[/]");
                                                            }
                                                        });
                                        }
                                        else
                                        {
                                            AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Did not save configuration due to user choice[/]");
                                        }
                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Warning}]Did not save configuration[/]");
                                    }
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_ShowEnabledDevicesInLoadedConfig)
                        {
                            if (SharedData.Config != null)
                            {
                                List<DeviceEntity> lstDeviceGroups = EnableDisableHelper.GetAllDevices();
                                AnsiConsole.MarkupLine($"The tree of devices has the following legend: [{AnubisOptions.Options.defaultColor_Ok}]discovered and enabled[/], discovered and disabled, [italic]not discovered[/], [{AnubisOptions.Options.defaultColor_Ok} italic]not discovered and enabled[/]");
                                Tree trRoot = new("List of devices");
                                lstDeviceGroups.ForEach(itm => itm.AddTreeNode(trRoot));

                                AnsiConsole.Write(trRoot);
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_EnabledDisableDevices_TurnOn)
                        {
                            if (SharedData.Config != null)
                            {
                                DeviceEntity dvcTurnOnRoot = EnableDisableHelper.GetTurnOnDevices();
                                AnsiConsole.MarkupLine("The list of devices to turn on has the following legend: discovered, [italic]not discovered[/]");

                                var mspDevices =
                                    new MultiSelectionPrompt<DeviceEntity>()
                                        .PageSize(AnubisOptions.Options.pageSize)
                                        .Title("Enable/disable devices to turn on")
                                        .MoreChoicesText(CNST_ChoiceMoreText)
                                        .InstructionsText(CNST_ChoiceInstructionText)
                                        .NotRequired()
                                        .HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));

                                dvcTurnOnRoot.AddChoiceGroup(mspDevices);

                                var selDevices = AnsiConsole.Prompt(mspDevices);

                                dvcTurnOnRoot.SetSelected(selDevices);

                                AnsiConsole.MarkupLine("Devices to turn on have been enabled/disabled accordingly");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_EnabledDisableDevices_Pollers)
                        {
                            if (SharedData.Config != null)
                            {
                                DeviceEntity dvcPollersRoot = EnableDisableHelper.GetPollerDevices();
                                AnsiConsole.MarkupLine("The list of devices in pollers has the following legend: discovered, [italic]not discovered[/]");

                                var mspDevices =
                                    new MultiSelectionPrompt<DeviceEntity>()
                                        .PageSize(AnubisOptions.Options.pageSize)
                                        .Title("Enable/disable devices and pollers")
                                        .MoreChoicesText(CNST_ChoiceMoreText)
                                        .InstructionsText(CNST_ChoiceInstructionText)
                                        .Mode(SelectionMode.Independent)
                                        .NotRequired()
                                        .HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));

                                dvcPollersRoot.AddChoiceGroup(mspDevices);

                                var selDevices = AnsiConsole.Prompt(mspDevices);

                                dvcPollersRoot.SetSelected(selDevices);

                                AnsiConsole.MarkupLine("Devices in pollers and pollers have been enabled/disabled accordingly");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_EnabledDisableDevices_Triggers)
                        {
                            if (SharedData.Config != null)
                            {
                                DeviceEntity dvcTriggersRoot = EnableDisableHelper.GetTriggerDevices();
                                AnsiConsole.MarkupLine("The list of devices to trigger has the following legend: discovered, [italic]not discovered[/]");

                                var mspDevices =
                                    new MultiSelectionPrompt<DeviceEntity>()
                                        .PageSize(AnubisOptions.Options.pageSize)
                                        .Title("Enable/disable devices and triggers configs")
                                        .MoreChoicesText(CNST_ChoiceMoreText)
                                        .InstructionsText(CNST_ChoiceInstructionText)
                                        .Mode(SelectionMode.Independent)
                                        .NotRequired()
                                        .HighlightStyle(new Style(AnubisOptions.Options.defaultColor_Choice));

                                dvcTriggersRoot.AddChoiceGroup(mspDevices);

                                var selDevices = AnsiConsole.Prompt(mspDevices);

                                dvcTriggersRoot.SetSelected(selDevices);

                                AnsiConsole.MarkupLine("Devices in triggers and trigger configs have been enabled/disabled accordingly");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_TurnOnDevices)
                        {
                            if (SharedData.Config != null)
                            {
                                long cntTurnOn = Generator.GetTurnOnDeviceCount();
                                if (cntTurnOn > 0)
                                {
                                    AnsiConsole.MarkupLine($"After you have confirmed to turn on the devices: [{AnubisOptions.Options.defaultColor_Warning}]Make sure to [underline]follow any manual step instructions given to you through warnings in the logging console[/] during the procedure.[/]");
                                    if (AnsiConsole.Confirm(@$"Do you want to [bold]turn on {cntTurnOn} devices now[/] (SwitchBot devices may get turned off in the process)?", false))
                                    {
                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Make sure to [underline]follow any manual step instructions given to you through warnings in the logging console[/] during the procedure.[/]");
                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Make sure all switches that are controlled by SwitchBot devices [underline]are immediately turned back on manually by you[/] when they get turned off during this sequence[/]");
                                        AnsiConsole.MarkupLine(@"(this may happen as SwitchBots may only be able to turn off switches, even if ""turned on"", but have to be turned on to be in the ""On"" state so ANUBIS knows when they got turned off again)");

                                        long aprxTime = Generator.GetTurnOnTimeInSeconds();

                                        AnsiConsole.Status()
                                                    .AutoRefresh(true)
                                                    .Spinner(Spinner.Known.Dots)
                                                    .SpinnerStyle(AnubisOptions.Options.defaultColor_Ok)
                                                    .Start($"Turning on devices (will take about {aprxTime} seconds)...", ctx =>
                                                    {
                                                        if (Generator.TurnOnDevices())
                                                        {
                                                            AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Ok}]Successfully turned on all devices[/]");
                                                        }
                                                        else
                                                        {
                                                            AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Error}]Unable to turn on all devices[/]");
                                                        }
                                                    });
                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Not turning on switches due to user choice[/]");
                                    }
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Found no devices to turn on[/]");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_RediscoverDevices || actStart == InitConfigCommands.CHOICE_RediscoverDevicesTurnOnSwitchBot)
                        {
                            if (SharedData.Config != null)
                            {
                                if (actStart == InitConfigCommands.CHOICE_RediscoverDevices ||
                                    AnsiConsole.Confirm(@$"Do you want to rediscover devices now, including trying to turn on all SwitchBot switches (SwitchBot devices may get turned off in the process)?", false))
                                {
                                    if (actStart == InitConfigCommands.CHOICE_RediscoverDevicesTurnOnSwitchBot)
                                        AnsiConsole.MarkupLine(CNST_SwitchBotDiscoveryWarning);
                                    AnsiConsole.Status()
                                            .AutoRefresh(true)
                                            .Spinner(Spinner.Known.Dots)
                                            .SpinnerStyle(AnubisOptions.Options.defaultColor_Ok)
                                            .Start("Rediscovering devices...", ctx =>
                                            {
                                                discoveryInfo = ConfigFileManager.ReloadConfig(true, actStart == InitConfigCommands.CHOICE_RediscoverDevicesTurnOnSwitchBot);
                                                config = SharedData.Config;

                                                if (config != null && discoveryInfo != null)
                                                {
                                                    AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Ok}]Successfully rediscovered devices[/]");
                                                }
                                                else
                                                {
                                                    AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Error}]Unable to rediscovered devices[/]");
                                                }
                                            });
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Not discovering devices due to user choice[/]");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_ResetCountdown)
                        {
                            if (SharedData.Config != null)
                            {
                                CountdownPollerData? cpd = (CountdownPollerData?)SharedData.Config.pollersAndTriggers.pollers.FirstOrDefault(itm => itm.enabled && itm is CountdownPollerData);
                                if (cpd != null)
                                {
                                    string? strT0Time = GetT0Time(AnubisOptions.Options.defaultColor_Info);

                                    if (strT0Time != null)
                                    {

                                        if (actStart == InitConfigCommands.CHOICE_RediscoverDevices ||
                                            AnsiConsole.Confirm(@$"Do you want to reset the Countdown T0 to the configuration default now (your current Countdown T0 of {strT0Time} will be overwritten)?", false))
                                        {
                                            cpd.options.CalculateCountdownT0();
                                            strT0Time = GetT0Time(AnubisOptions.Options.defaultColor_Info);

                                            AnsiConsole.Clear();
                                            AnsiConsole.MarkupLine($@"Loaded configuration: [{AnubisOptions.Options.defaultColor_Ok}]{AnubisConfig.LoadedConfig}[/]{(strT0Time != null ? $"\r\nT0: {strT0Time}" : "")}");

                                            if (strT0Time != null)
                                            {
                                                AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Info}]Countdown has been reset to {strT0Time}[/]");
                                            }
                                            else
                                            {
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]There seems to have been a problem resetting the countdown, no new T0 time found[/]");
                                            }
                                        }
                                        else
                                        {
                                            AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Not resetting Countdown T0 due to user choice[/]");
                                        }

                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]No Countdown T0 calculated[/]");
                                    }
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]No Countdown loaded[/]");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_LaunchController)
                        {
                            if (SharedData.Config != null)
                            {
                                CountdownPollerData? cpd = (CountdownPollerData?)SharedData.Config.pollersAndTriggers.pollers.FirstOrDefault(itm => itm.enabled && itm is CountdownPollerData);
                                ControllerData? cd = (ControllerData?)SharedData.Config.pollersAndTriggers.pollers.FirstOrDefault(itm => itm.enabled && itm is ControllerData);
                                if (cd != null)
                                {
                                    if (cpd != null)
                                    {
                                        DateTime? dtT0Local = cpd.options.countdownT0TimestampLocal;
                                        DateTime? dtT0UTC = cpd.options.countdownT0TimestampUTC;
                                        if (dtT0Local != null && dtT0UTC != null)
                                        {
                                            string? strT0Time = GetT0Time(AnubisOptions.Options.defaultColor_Info);
                                            bool blTriggersShutdown = cpd.options.shutDownOnT0;
                                            bool blCountdownTooSoon = DateTime.UtcNow.AddMinutes(5) > dtT0UTC;
                                            string? strFallbackConfigName = null;
                                            long cntFallbackDevices = 0;

                                            bool blSendMailAfterMinutes = cd.options.sendMailEarliestAfterMinutes > 0 && cpd.mailingOptions.countdownSendMailMinutes > 0;
                                            bool blSendInfoMail = cpd.mailingOptions.enabled && cpd.mailingOptions.sendInfoMails && cpd.mailingOptions.mailConfig_Info.Length > 0;
                                            bool blSendEmergencyMail = cpd.mailingOptions.enabled && cpd.mailingOptions.sendEmergencyMails && cpd.mailingOptions.mailConfig_Emergency.Length > 0;
                                            bool blHasMailServer = !string.IsNullOrWhiteSpace(cpd.mailingOptions.mailSettings_SmtpServer);
                                            bool blHasMailFrom = !string.IsNullOrWhiteSpace(cpd.mailingOptions.mailSettings_FromAddress);
                                            bool blHasMailUserAndPassword = string.IsNullOrWhiteSpace(cpd.mailingOptions.mailSettings_User) || !string.IsNullOrEmpty(cpd.mailingOptions.mailSettings_Password);
                                            bool blHasMailSimulation = cpd.mailingOptions.simulateMails;
                                            bool blSendEarliestOk = cd.options.sendMailEarliestAfterMinutes < cpd.mailingOptions.countdownSendMailMinutes;
                                            string? strMailSimulateTo = cpd.mailingOptions.mailAddress_Simulate;
                                            bool blMailProblem = (blSendInfoMail || blSendEmergencyMail) &&
                                                                    (((!blHasMailSimulation || !string.IsNullOrWhiteSpace(strMailSimulateTo)) &&
                                                                     (!blHasMailServer || !blHasMailFrom || !blHasMailUserAndPassword)) ||
                                                                        (!blSendMailAfterMinutes || !blSendEarliestOk));

                                            if (blSendInfoMail && blSendMailAfterMinutes)
                                            {
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Activated}]Countdown will send info mails[/]");
                                            }
                                            if (blSendEmergencyMail && blSendMailAfterMinutes)
                                            {
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Activated}]Countdown will send emergency mails[/]");
                                            }
                                            if ((blSendInfoMail || blSendEmergencyMail) && blSendMailAfterMinutes)
                                            {
                                                if (blHasMailSimulation)
                                                {
                                                    if (string.IsNullOrWhiteSpace(strMailSimulateTo))
                                                    {
                                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Countdown will only simulate mail sending[/]");
                                                    }
                                                    else
                                                    {
                                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Countdown will only simulate mail sending to address \"{strMailSimulateTo}\"[/]");
                                                    }
                                                }
                                                else
                                                {
                                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Activated} rapidblink]Countdown will send emails FOR REAL[/]");
                                                }
                                                if (blHasMailServer)
                                                {
                                                    try
                                                    {
                                                        Ping png = new();
                                                        PingReply rep = png.Send(cpd.mailingOptions.mailSettings_SmtpServer, 10000);

                                                        if (rep.Status != IPStatus.Success)
                                                        {
                                                            AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING: Mail server \"{cpd.mailingOptions.mailSettings_SmtpServer}\" not pingable[/], returned status was: {rep.Status}");
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING: Mail server \"{cpd.mailingOptions.mailSettings_SmtpServer}\" not pingable[/], error was: {ex.Message}");
                                                    }
                                                }
                                                if (!blTriggersShutdown)
                                                {
                                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]NOTE: Since countdown will not shutdown on T0 MAKE SURE ANUBIS IS CHECKING IF THE SYSTEM HAS SHUT DOWN ON AT LEAST ONE SENSOR or no mails will be sent even if mail sending isn't itself checking for shutdown, since mail sending will only work if the system went into shutdown or triggered state.[/]");
                                                }
                                            }
                                            else
                                            {
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning} underline]Countdown will not send any mail[/]");
                                            }
                                            if (blMailProblem)
                                            {
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING: Problems with the mail options were detected, see following messages[/]");

                                                if (!blHasMailSimulation || !string.IsNullOrWhiteSpace(strMailSimulateTo))
                                                {
                                                    if (!blHasMailServer)
                                                    {
                                                        AnsiConsole.MarkupLine($"Mailing problem: [{AnubisOptions.Options.defaultColor_Warning}]No mail server was configured[/]");
                                                    }
                                                    if (!blHasMailUserAndPassword)
                                                    {
                                                        AnsiConsole.MarkupLine($"Mailing problem: [{AnubisOptions.Options.defaultColor_Warning}]No password was configured even though a user was defined[/]");
                                                    }
                                                    if (!blHasMailFrom)
                                                    {
                                                        AnsiConsole.MarkupLine($"Mailing problem: [{AnubisOptions.Options.defaultColor_Warning}]No from mail address was defined[/]");
                                                    }
                                                }
                                                if (!blSendEarliestOk)
                                                {
                                                    AnsiConsole.MarkupLine($"Mailing problem: [{AnubisOptions.Options.defaultColor_Warning}]The value of minutes after sending mails at the earliest is larger or equal to the minutes to send the mails after T0[/]");
                                                }
                                                if (!blSendMailAfterMinutes)
                                                {
                                                    AnsiConsole.MarkupLine($"Mailing problem: [{AnubisOptions.Options.defaultColor_Warning}]Either value of minutes after sending mails at the earliest is 0 or value of minutes to send the mails after T0 is 0.[/] This will result in no mails being sent even if mailing is enabled.");
                                                }
                                            }

                                            AnsiConsole.WriteLine("");

                                            var fallbackTriggerConfig = Generator.GetTriggerConfiguration("dummyNotFound_ignoreWarning_" + Guid.NewGuid().ToString());
                                            if (fallbackTriggerConfig != null)
                                            {
                                                strFallbackConfigName = fallbackTriggerConfig.Name.EscapeMarkup();
                                                cntFallbackDevices = fallbackTriggerConfig.Triggers.Count(itm => itm is not DelayTrigger);
                                            }

                                            if (strFallbackConfigName != null)
                                            {
                                                if (cntFallbackDevices > 0)
                                                {
                                                    AnsiConsole.MarkupLine($"The trigger configuration \"[bold]{strFallbackConfigName}[/]\" [{AnubisOptions.Options.defaultColor_Ok}]is configured as the fallback[/] trigger configuration and will [{AnubisOptions.Options.defaultColor_Ok}]trigger a shutdown[/] of [bold]{cntFallbackDevices} devices[/]");
                                                }
                                                else
                                                {
                                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING[/]: The trigger configuration \"[bold]{strFallbackConfigName}[bold]\"), was defined but [{AnubisOptions.Options.defaultColor_Error}]no enabled devices in this trigger configuration were found[/]!");
                                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING: The countdown has no valid fallback trigger configuration![/]");
                                                }
                                            }
                                            else
                                            {
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING[/]: [{AnubisOptions.Options.defaultColor_Error}]No enabled fallback trigger configuration[/] was found!");
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING: The countdown has no valid fallback trigger configuration![/]");
                                            }

                                            if (blCountdownTooSoon)
                                            {
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING: The countdown is less than 5 minutes into the future![/]");
                                            }

                                            if (blTriggersShutdown)
                                            {
                                                string[] arrTriggerConfigsT0 = Generator.GetTriggerConfigurationNames(UniversalPanicType.Countdown, ConstantTriggerIDs.ID_Countdown_T0, UniversalPanicReason.NoPanic, false);
                                                List<string> lstEnabledTriggerConfigsT0 = [];
                                                long cntT0Devices = 0;

                                                foreach (var singleTriggerConfig in arrTriggerConfigsT0)
                                                {
                                                    var triggerConfig = Generator.GetTriggerConfiguration(singleTriggerConfig);
                                                    if (triggerConfig != null)
                                                    {
                                                        lstEnabledTriggerConfigsT0.Add(triggerConfig.Name.EscapeMarkup());
                                                        cntT0Devices += triggerConfig.Triggers.Count(itm => itm is not DelayTrigger);
                                                    }
                                                }

                                                if (lstEnabledTriggerConfigsT0.Count > 0)
                                                {
                                                    string strTriggerConfigs = string.Join("[bold]\", \"[/]", lstEnabledTriggerConfigsT0);
                                                    if (cntT0Devices > 0)
                                                    {
                                                        AnsiConsole.MarkupLine($"The countdown [{AnubisOptions.Options.defaultColor_Activated}]will trigger a shutdown[/] of [bold]{cntT0Devices} devices[/] through {lstEnabledTriggerConfigsT0.Count} trigger configurations (these are: \"[bold]{strTriggerConfigs}[/]\")");
                                                    }
                                                    else
                                                    {
                                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Error}]ERROR[/]: The countdown would [bold]TRY[/] to trigger a shutdown through {lstEnabledTriggerConfigsT0.Count} trigger configurations (these are: \"[bold]{strTriggerConfigs}[/]\"), but [{AnubisOptions.Options.defaultColor_Error}]no enabled devices in these trigger configurations were found[/]!");
                                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING: The countdown will not trigger a shutdown![/]");
                                                    }
                                                }
                                                else
                                                {
                                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Error}]ERROR[/]: The countdown would [bold]TRY[/] to trigger a shutdown, but [{AnubisOptions.Options.defaultColor_Error}]no enabled trigger configurations or devices to trigger were found[/]!");
                                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]WARNING: The countdown will not trigger a shutdown![/]");
                                                }
                                            }
                                            else
                                            {
                                                AnsiConsole.MarkupLine($"The countdown will [{AnubisOptions.Options.defaultColor_Warning}]NOT TRIGGER A SHUTDOWN ON T0[/] (the shutDownOnT0 option was set to false)");
                                            }

                                            AnsiConsole.WriteLine("");

                                            AnsiConsole.WriteLine("==================================================");
                                            AnsiConsole.WriteLine("");
                                            if (AnsiConsole.Confirm(@$"Do you want to [bold rapidblink]LAUNCH THE COUNTDOWN[/] with a local T0 Countdown Time {strT0Time} now?", false))
                                            {
                                                blStartControlling = true;
                                                blExit = true;
                                            }
                                            else
                                            {
                                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Not launching countdown due to user choice[/]");
                                            }
                                        }
                                        else
                                        {
                                            AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Error}]Local or UTC T0 time was not generated for countdown poller, cannot launch countdown[/]");
                                        }
                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Error}]No enabled countdown poller found, cannot launch countdown[/]");
                                    }
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Error}]No enabled controller found, cannot launch countdown[/]");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine(CNST_NoConfigLoadedWarning);
                            }
                        }
                        else if (actStart == InitConfigCommands.CHOICE_Exit)
                        {
                            if (AnsiConsole.Confirm(@$"Do you want to exit ANUBIS Watcher now?", true))
                            {
                                AnsiConsole.MarkupLine($@"[bold]Exiting ANUBIS Watcher[/]");
                                blExit = true;
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[{AnubisOptions.Options.defaultColor_Warning}]Not exiting ANUBIS Watcher due to user choice[/]");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($@"[{AnubisOptions.Options.defaultColor_Error}]Unknown option ""{actStart}""[/]");
                        }
                    }
                    catch (Exception ex)
                    {
                        logging?.LogCritical(ex, "Uncaught error of type {type} in init config interface, message was: {message}", ex.GetType().Name, ex.Message);
                    }
                }
                while (!blExit);

                return blStartControlling;
            }
        }

        public static string? GetT0Time(Color color)
        {
            string? strT0Time = null;

            if (SharedData.Config != null)
            {
                CountdownPollerData? cpd = (CountdownPollerData?)SharedData.Config.pollersAndTriggers.pollers.FirstOrDefault(itm => itm.enabled && itm is CountdownPollerData);
                if (cpd != null)
                {
                    DateTime? dtT0Local = cpd.options.countdownT0TimestampLocal;
                    DateTime? dtT0UTC = cpd.options.countdownT0TimestampUTC;

                    if (dtT0Local.HasValue && dtT0UTC.HasValue)
                    {
                        string strT0Local = dtT0Local.Value.ToString("dd.MM.yyyy HH:mm:ss");
                        string strT0UTC = dtT0UTC.Value.ToString("yyyy-MM-dd HH:mm:ss");
                        string strTPrefix = "-";

                        var tsTMinus = DateTime.UtcNow - dtT0UTC.Value;
                        if(tsTMinus.TotalNanoseconds >= 0)
                        {
                            strTPrefix = "+";
                        }
                        string strTMinus = strTPrefix + tsTMinus.ToString(@"hh\:mm\:ss");

                        strT0Time = $"[{color} bold]{strT0Local}[/] ([{color} bold]{strT0UTC}[/] UTC; T{strTMinus})";
                    }
                }
            }

            return strT0Time;
        }

        public static string GetCurrentTime(Color color)
        {
            string strCurrentLocal = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            string strCurrentUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            return $"[{color} bold]{strCurrentLocal}[/] ([{color} bold]{strCurrentUTC}[/] UTC)";
        }
    }
}
