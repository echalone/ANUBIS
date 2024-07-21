using ANUBISConsole.ConfigHelpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ANUBISConsole.UI
{
    #region Helper classes
    public class SensorRotationData
    {
        public DateTime? NextRotation { get; set; } = null;
        public int SensorCount { get; set; } = 0;
        public int FirstSensorCurrentListIndex { get; set; } = 0;

        public bool RotateSensors { get { return !NextRotation.HasValue || DateTime.UtcNow > NextRotation.Value; } }

        public void UpdateRotation()
        {
            if (NextRotation.HasValue)
                FirstSensorCurrentListIndex++;
            NextRotation = DateTime.UtcNow.AddSeconds(AnubisOptions.Options.rotateSensorsEveryXSeconds);
            if (FirstSensorCurrentListIndex >= SensorCount)
            {
                FirstSensorCurrentListIndex = 0;
            }
        }

        public int GetSensorPosition(int originalSensorListIndex)
        {
            return (FirstSensorCurrentListIndex + originalSensorListIndex) % SensorCount;
        }
    }

    public class LayoutCollection
    {
#pragma warning disable CS8618
        public Layout Root { get; init; }
        public Layout Data { get; init; }
        public Layout Situation { get; init; }
        public Layout Details { get; init; }
        public Layout State { get; init; }
        public Layout Countdowns { get; init; }
        public Layout Readings { get; init; }
        public Layout RemoteFiles { get; init; }
        public Layout Sensors { get; init; }
        public Layout Histories { get; init; }
        public Layout StatusHistory { get; init; }
        public Layout PanicHistory { get; init; }
        public Layout TriggerHistory { get; init; }
        public Layout Prompt { get; init; }
        public Table StateTable { get; init; }
        public Table CountdownTable { get; init; }
        public Table SensorTable { get; init; }
        public Table RemoteTable { get; init; }
        public Table StateHistoryTable { get; init; }
        public Table PanicHistoryTable { get; init; }
        public Table TriggerHistoryTable { get; init; }
#pragma warning restore CS8618
    }

    public class CountdownCollection
    {
#pragma warning disable CS8618
        public CountdownWidget CountdownT0 { get; init; }
        public CountdownWidget? CountdownSafeMode { get; init; }
        public CountdownWidget? CountdownVerifyShutDown { get; init; }
        public CountdownWidget? CountdownSendMails { get; init; }
        public CountdownWidget CountdownCurrentTime { get; init; }
#pragma warning restore CS8618
    }

    public class StateCollection
    {
#pragma warning disable CS8618
        public StateWidget<ControllerStatus> ControllerState { get; init; }
        public StateWidget<BooleanState> HasPanic { get; init; }
        public StateWidget<BooleanState> HasShutDown { get; init; }
        public StateWidget<BooleanState> HasShutDownVerified { get; init; }
        public StateWidget<BooleanState> HasSentMails { get; init; }
        public StateWidget<BooleanState> CanArm { get; init; }
#pragma warning restore CS8618
    }

    public class SensorCollection
    {
#pragma warning disable CS8618
        public List<SensorWidget> RemoteFiles { get; init; }
        public List<SensorWidget> SwitchSensors { get; init; }
#pragma warning restore CS8618
    }

    public class DisplayData
    {
#pragma warning disable CS8618
        public LayoutCollection Layouts { get; set; }
        public StateCollection States { get; init; }
        public CountdownCollection Countdowns { get; init; }
        public SensorCollection Sensors { get; init; }
#pragma warning restore CS8618
    }
    #endregion

    internal class Controlling
    {
#pragma warning disable CS8618
        private static DisplayData Data { get; set; }

        private static SensorRotationData SensorRotation { get; set; }

        private static bool StartedHoldbackMode { get; set; }
        private static bool CanBeArmedInHoldback { get; set; }
        private static bool IsInStartup { get; set; }
        private static bool EnteredSafeMode { get; set; }
        private static bool? ShowArmableState { get; set; }
#pragma warning restore CS8618

        public static void StartUpController(string configName)
        {
            ILogger? logging = SharedData.InterfaceLogging;

            using (logging?.BeginScope("Interface.Controller"))
            {
                bool blHasEnteredScreenMode = false;

                try
                {
                    bool blStartUp = false;
                    StartedHoldbackMode = false;
                    CanBeArmedInHoldback = false;
                    IsInStartup = true;
                    EnteredSafeMode = false;
                    ShowArmableState = null;

                    SharedData.InterfaceLogging?.LogInformation(@"STARTING CONTROLLER with loaded configuration ""{config}""...", AnubisConfig.LoadedConfig);

                    AnsiConsole.Clear();
                    AnsiConsole.Status()
                                .AutoRefresh(true)
                                .Spinner(Spinner.Known.Dots)
                                .SpinnerStyle(AnubisOptions.Options.defaultColor_Ok)
                                .Start("Starting up controller...", ctx =>
                                {
                                    blStartUp = Generator.StartController();
                                });

                    if (blStartUp)
                    {
                        AnsiConsole.MarkupLine($"Controller [{AnubisOptions.Options.defaultColor_Ok}]initialized[/], setting up interface...");
                        Thread.Sleep(1000);
                        AnsiConsole.Clear();

                        AnsiConsole.Cursor.Hide();

                        #region Configure sensor widgets

                        SensorCollection sensorCollection = new()
                        {
                            RemoteFiles = [], //SharedData.Controller?.Files_ReadRemote?.Select(itm => new SensorWidget(itm)).ToList() ?? new List<SensorWidget>(),
                            SwitchSensors = [],
                        };

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
                        int cntRemote = 0;
                        if ((SharedData.Controller?.Files_ReadRemote?.Count ?? 0) > 0)
                        {
                            foreach (var swt in SharedData.Controller.Files_ReadRemote)
                            {
                                sensorCollection.RemoteFiles.Add(new SensorWidget(new SensorWidgetOptions(cntRemote), swt, false));
                                cntRemote++;
                            }
                        }

                        int cntSensors = 0;
                        PollerInfo pollerInfo = new(SharedData.Controller, SharedData.Controller.Poller_Countdown,
                                                                SharedData.Controller.Poller_Fritz, SharedData.Controller.Poller_SwitchBot,
                                                                SharedData.Controller.Poller_ClewareUSB, SharedData.Controller.Poller_WriteFiles,
                                                                SharedData.Controller.Poller_ReadFiles);
                        sensorCollection.SwitchSensors.Add(new SensorWidget(new SensorWidgetOptions(cntSensors), pollerInfo));
                        cntSensors++;

                        if ((SharedData.Controller?.Switches_Fritz?.Count ?? 0) > 0)
                        {
                            foreach (var swt in SharedData.Controller.Switches_Fritz)
                            {
                                sensorCollection.SwitchSensors.Add(new SensorWidget(new SensorWidgetOptions(cntSensors), swt));
                                cntSensors++;
                            }
                        }
                        if ((SharedData.Controller?.Switches_ClewareUSB?.Count ?? 0) > 0)
                        {
                            foreach (var swt in SharedData.Controller.Switches_ClewareUSB)
                            {
                                sensorCollection.SwitchSensors.Add(new SensorWidget(new SensorWidgetOptions(cntSensors), swt));
                                cntSensors++;
                            }
                        }
                        if ((SharedData.Controller?.Switches_SwitchBot?.Count ?? 0) > 0)
                        {
                            foreach (var swt in SharedData.Controller.Switches_SwitchBot)
                            {
                                sensorCollection.SwitchSensors.Add(new SensorWidget(new SensorWidgetOptions(cntSensors), swt));
                                cntSensors++;
                            }
                        }
                        if ((SharedData.Controller?.Files_WriteLocal?.Count ?? 0) > 0)
                        {
                            foreach (var swt in SharedData.Controller.Files_WriteLocal)
                            {
                                sensorCollection.SwitchSensors.Add(new SensorWidget(new SensorWidgetOptions(cntSensors), swt));
                                cntSensors++;
                            }
                        }
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.

                        SensorRotation = new SensorRotationData()
                        {
                            SensorCount = sensorCollection.SwitchSensors.Count,
                        };
                        SensorRotation.UpdateRotation();

                        #endregion

                        #region Configure countdown widgets

                        string strCountdownT0Title = "Countdown T0";
                        if (!(SharedData.Controller?.Poller_Countdown?.Options?.ShutDownOnT0 ?? false))
                        {
                            strCountdownT0Title += " (no shutdown)";
                        }

                        CountdownCollection countdownCollection =
                                new()
                                {
                                    CountdownT0 =
                                        new CountdownWidget(
                                                new CountdownWidgetOptions()
                                                {
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                    ShortlyBeforeT0InMinutes = AnubisOptions.Options.defaultComposition_Countdown_Main.shortlyBeforeModeInMinutes,
                                                    BackgroundColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_Main.compositionShortlyBeforeT0?.backgroundColor,
                                                    TextColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_Main.compositionShortlyBeforeT0?.textColor,
                                                    BackgroundColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_Main.compositionBeforeT0.backgroundColor,
                                                    TextColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_Main.compositionBeforeT0.textColor,
                                                    BackgroundColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_Main.compositionFromT0.backgroundColor,
                                                    TextColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_Main.compositionFromT0.textColor,
                                                    Title = strCountdownT0Title,
                                                }),
                                    CountdownSafeMode =
                                        new CountdownWidget(
                                                new CountdownWidgetOptions()
                                                {
                                                    Height = 0,
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                    ShortlyBeforeT0InMinutes = AnubisOptions.Options.defaultComposition_Countdown_SafeMode.shortlyBeforeModeInMinutes,
                                                    BackgroundColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_SafeMode.compositionShortlyBeforeT0?.backgroundColor,
                                                    TextColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_SafeMode.compositionShortlyBeforeT0?.textColor,
                                                    BackgroundColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_SafeMode.compositionBeforeT0.backgroundColor,
                                                    TextColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_SafeMode.compositionBeforeT0.textColor,
                                                    BackgroundColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_SafeMode.compositionFromT0.backgroundColor,
                                                    TextColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_SafeMode.compositionFromT0.textColor,
                                                    Title = "Safe Mode"
                                                }),
                                    CountdownVerifyShutDown =
                                        new CountdownWidget(
                                                new CountdownWidgetOptions()
                                                {
                                                    Height = 0,
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                    ShortlyBeforeT0InMinutes = AnubisOptions.Options.defaultComposition_Countdown_VerifyShutDown.shortlyBeforeModeInMinutes,
                                                    BackgroundColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_VerifyShutDown.compositionShortlyBeforeT0?.backgroundColor,
                                                    TextColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_VerifyShutDown.compositionShortlyBeforeT0?.textColor,
                                                    BackgroundColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_VerifyShutDown.compositionBeforeT0.backgroundColor,
                                                    TextColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_VerifyShutDown.compositionBeforeT0.textColor,
                                                    BackgroundColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_VerifyShutDown.compositionFromT0.backgroundColor,
                                                    TextColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_VerifyShutDown.compositionFromT0.textColor,
                                                    Title = "Verified ShutDown"
                                                }),
                                    CountdownSendMails =
                                        new CountdownWidget(
                                                new CountdownWidgetOptions()
                                                {
                                                    Height = 0,
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                    ShortlyBeforeT0InMinutes = AnubisOptions.Options.defaultComposition_Countdown_SendMails.shortlyBeforeModeInMinutes,
                                                    BackgroundColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_SendMails.compositionShortlyBeforeT0?.backgroundColor,
                                                    TextColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_SendMails.compositionShortlyBeforeT0?.textColor,
                                                    BackgroundColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_SendMails.compositionBeforeT0.backgroundColor,
                                                    TextColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_SendMails.compositionBeforeT0.textColor,
                                                    BackgroundColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_SendMails.compositionFromT0.backgroundColor,
                                                    TextColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_SendMails.compositionFromT0.textColor,
                                                    Title = "Send Mails"
                                                }),
                                    CountdownCurrentTime =
                                        new CountdownWidget(
                                                new CountdownWidgetOptions()
                                                {
                                                    Height = 0,
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                    ShortlyBeforeT0InMinutes = AnubisOptions.Options.defaultComposition_Countdown_CurrentTime.shortlyBeforeModeInMinutes,
                                                    BackgroundColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_CurrentTime.compositionShortlyBeforeT0?.backgroundColor,
                                                    TextColor_ShortlyBeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_CurrentTime.compositionShortlyBeforeT0?.textColor,
                                                    BackgroundColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_CurrentTime.compositionBeforeT0.backgroundColor,
                                                    TextColor_BeforeT0 = AnubisOptions.Options.defaultComposition_Countdown_CurrentTime.compositionBeforeT0.textColor,
                                                    BackgroundColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_CurrentTime.compositionFromT0.backgroundColor,
                                                    TextColor_FromT0 = AnubisOptions.Options.defaultComposition_Countdown_CurrentTime.compositionFromT0.textColor,
                                                    Title = "Current Time"
                                                }
                                            )
                                };

                        #endregion

                        #region Configuring state widgets

                        StateCollection stateCollection =
                                new()
                                {
                                    CanArm =
                                        new StateWidget<BooleanState>(
                                                new StateWidgetOptions()
                                                {
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft - 2,
                                                },
                                                new()
                                                {
                                                    { BooleanState.Unknown,
                                                        new WidgetStateInfo()
                                                        {
                                                            Display = "",
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_HoldbackArmable.compositionUnknown.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_HoldbackArmable.compositionUnknown.textColor,
                                                        }
                                                    },
                                                    { BooleanState.False,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_HoldbackArmable.compositionFalse.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_HoldbackArmable.compositionFalse.textColor,
                                                            Display = "HOLDBACK"
                                                        }
                                                    },
                                                    { BooleanState.True,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_HoldbackArmable.compositionTrue.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_HoldbackArmable.compositionTrue.textColor,
                                                            Display = "armable",
                                                        }
                                                    },
                                                }),
                                    HasPanic =
                                        new StateWidget<BooleanState>(
                                                new StateWidgetOptions()
                                                {
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                    Height = 3,
                                                },
                                                new()
                                                {
                                                    { BooleanState.Unknown,
                                                        new WidgetStateInfo()
                                                        {
                                                            Display = "",
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Panicked.compositionUnknown.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Panicked.compositionUnknown.textColor,
                                                        }
                                                    },
                                                    { BooleanState.False,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Panicked.compositionFalse.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Panicked.compositionFalse.textColor,
                                                            Display = "no panic"
                                                        }
                                                    },
                                                    { BooleanState.True,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Panicked.compositionTrue.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Panicked.compositionTrue.textColor,
                                                            Display = "PANIC",
                                                        }
                                                    },
                                                }),
                                    HasShutDown =
                                        new StateWidget<BooleanState>(
                                                new StateWidgetOptions()
                                                {
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                },
                                                new()
                                                {
                                                    { BooleanState.Unknown,
                                                        new WidgetStateInfo()
                                                        {
                                                            Display = "",
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_ShutDown.compositionUnknown.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_ShutDown.compositionUnknown.textColor,
                                                        }
                                                    },
                                                    { BooleanState.False,
                                                        new WidgetStateInfo()
                                                        {
                                                            Display = "",
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_ShutDown.compositionFalse.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_ShutDown.compositionFalse.textColor,
                                                        }
                                                    },
                                                    { BooleanState.True,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_ShutDown.compositionTrue.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_ShutDown.compositionTrue.textColor,
                                                            Display = "SHUTDOWN",
                                                        }
                                                    },
                                                }),
                                    HasShutDownVerified =
                                        new StateWidget<BooleanState>(
                                                new StateWidgetOptions()
                                                {
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                },
                                                new()
                                                {
                                                    { BooleanState.Unknown,
                                                        new WidgetStateInfo()
                                                        {
                                                            Display = "",
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_VerifiedShutDown.compositionUnknown.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_VerifiedShutDown.compositionUnknown.textColor,
                                                        }
                                                    },
                                                    { BooleanState.False,
                                                        new WidgetStateInfo()
                                                        {
                                                            Display = "",
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_VerifiedShutDown.compositionFalse.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_VerifiedShutDown.compositionFalse.textColor,
                                                        }
                                                    },
                                                    { BooleanState.True,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_VerifiedShutDown.compositionTrue.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_VerifiedShutDown.compositionTrue.textColor,
                                                            Display = "VERIFIED SHUTDOWN",
                                                        }
                                                    },
                                                }),
                                    HasSentMails =
                                        new StateWidget<BooleanState>(
                                                new StateWidgetOptions()
                                                {
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                },
                                                new()
                                                {
                                                    { BooleanState.Unknown,
                                                        new WidgetStateInfo()
                                                        {
                                                            Display = "",
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_SendMails.compositionUnknown.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_SendMails.compositionUnknown.textColor,
                                                        }
                                                    },
                                                    { BooleanState.False,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_SendMails.compositionFalse.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_SendMails.compositionFalse.textColor,
                                                            Display = "SENDING MAILS",
                                                        }
                                                    },
                                                    { BooleanState.True,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_SendMails.compositionTrue.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_SendMails.compositionTrue.textColor,
                                                            Display = "MAILS SENT",
                                                        }
                                                    },
                                                }),
                                    ControllerState =
                                        new StateWidget<ControllerStatus>(
                                                new StateWidgetOptions()
                                                {
                                                    Width = AnubisOptions.Options.SituationInsideWidthLeft,
                                                    Height = 3,
                                                },
                                                new()
                                                {
                                                    { ControllerStatus.Stopped,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionStopped.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionStopped.textColor,
                                                        }
                                                    },
                                                    { ControllerStatus.Monitoring,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionMonitoring.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionMonitoring.textColor,
                                                        }
                                                    },
                                                    { ControllerStatus.Holdback,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionHoldback.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionHoldback.textColor,
                                                        }
                                                    }
,
                                                    { ControllerStatus.Armed,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionArmed.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionArmed.textColor,
                                                        }
                                                    }
,
                                                    { ControllerStatus.SafeMode,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionSafeMode.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionSafeMode.textColor,
                                                        }
                                                    }
,
                                                    { ControllerStatus.ShutDown,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionShutDown.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionShutDown.textColor,
                                                        }
                                                    }
,
                                                    { ControllerStatus.Triggered,
                                                        new WidgetStateInfo()
                                                        {
                                                            BackgroundColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionTriggered.backgroundColor,
                                                            TextColor = AnubisOptions.Options.defaultComposition_State_Controller.compositionTriggered.textColor,
                                                        }
                                                    }

                                                }
                                            ),
                                };

                        #endregion

                        #region Set up layout

                        Layout lytRoot =
                            new Layout("Root")
                                .SplitRows(
                                    new Layout("Data")
                                        .SplitColumns(
                                            new Layout("Situation")
                                                .SplitRows(
                                                    new Layout("State"),
                                                    new Layout("Countdowns")
                                                ),
                                            new Layout("Details")
                                                .SplitColumns(
                                                    new Layout("Readings")
                                                        .SplitRows(
                                                            new Layout("RemoteFiles"),
                                                            new Layout("Sensors")
                                                        ),
                                                    new Layout("Histories")
                                                        .SplitRows(
                                                            new Layout("StatusHistory"),
                                                            new Layout("PanicHistory"),
                                                            new Layout("TriggerHistory")
                                                        )
                                                )
                                        ),
                                    new Layout("Prompt")
                                );

                        Table tblState =
                            new Table()
                            {
                                Border = TableBorder.Rounded,
                                //Expand = true,
                                ShowHeaders = true,
                                ShowFooters = false,
                                ShowRowSeparators = true,
                            }
                                .AddColumn(
                                    new("")
                                    {
                                        Header = new Markup("[bold]State[/]"),
                                        Alignment = Justify.Center,
                                        NoWrap = true,
                                        Padding = new Padding(0, 0, 0, 0),
                                    })
                                .AddRow("")
                                .AddRow("")
                                .AddRow("")
                                .AddRow("")
                                .AddRow("");

                        Table tblCountdown =
                            new Table()
                            {
                                Border = TableBorder.Rounded,
                                //Expand = true,
                                ShowHeaders = true,
                                ShowFooters = false,
                                ShowRowSeparators = true,
                            }
                                .AddColumn(
                                    new("")
                                    {
                                        Header = new Markup("[bold]Countdowns[/]"),
                                        Alignment = Justify.Center,
                                        NoWrap = true,
                                        Padding = new Padding(0, 0, 0, 0),
                                    })
                                .AddRow("")
                                .AddRow("")
                                .AddRow("")
                                .AddRow("")
                                .AddRow("");

                        Table tblSensors =
                            new Table()
                            {
                                Border = TableBorder.Rounded,
                                Expand = true,
                                ShowHeaders = true,
                                ShowFooters = false,
                                ShowRowSeparators = false,
                            }
                                .AddColumn(
                                    new("")
                                    {
                                        Header = new Markup($"[bold]Sensors for {configName}[/]"),
                                        Alignment = Justify.Left,
                                        NoWrap = true,
                                        Padding = new Padding(0, 0, 0, 0),
                                    });

                        sensorCollection.SwitchSensors.ForEach(itm => tblSensors.AddRow("")); // create as many dummy rows as we need

                        Table tblRemote =
                            new Table()
                            {
                                Border = TableBorder.Rounded,
                                Expand = true,
                                ShowHeaders = true,
                                ShowFooters = false,
                                ShowRowSeparators = false,
                            }
                                .AddColumn(
                                    new("")
                                    {
                                        Header = new Markup("[bold]Remote ANUBIS Computers[/]"),
                                        Alignment = Justify.Left,
                                        NoWrap = true,
                                        Padding = new Padding(0, 0, 0, 0),
                                    });

                        sensorCollection.RemoteFiles.ForEach(itm => tblRemote.AddRow("")); // create as many dummy rows as we need

                        Table tblHistoryStates =
                            new Table()
                            {
                                Border = TableBorder.Rounded,
                                Expand = true,
                                ShowHeaders = true,
                                ShowFooters = false,
                                ShowRowSeparators = false,
                            }
                                .AddColumn(
                                    new("")
                                    {
                                        Header = new Markup($"[bold]Controller state history for {configName}[/]"),
                                        Alignment = Justify.Left,
                                        NoWrap = true,
                                        Padding = new Padding(0, 0, 0, 0),
                                    });

                        Table tblHistoryPanics =
                            new Table()
                            {
                                Border = TableBorder.Rounded,
                                Expand = true,
                                ShowHeaders = true,
                                ShowFooters = false,
                                ShowRowSeparators = false,
                            }
                                .AddColumn(
                                    new("")
                                    {
                                        Header = new Markup($"[bold]Panic history for {configName}[/]"),
                                        Alignment = Justify.Left,
                                        NoWrap = true,
                                        Padding = new Padding(0, 0, 0, 0),
                                    });

                        Table tblHistoryTriggers =
                            new Table()
                            {
                                Border = TableBorder.Rounded,
                                Expand = true,
                                ShowHeaders = true,
                                ShowFooters = false,
                                ShowRowSeparators = false,
                            }
                                .AddColumn(
                                    new("")
                                    {
                                        Header = new Markup($"[bold]Trigger history for {configName}[/]"),
                                        Alignment = Justify.Left,
                                        NoWrap = true,
                                        Padding = new Padding(0, 0, 0, 0),
                                    });

                        LayoutCollection layoutCollection =
                            new()
                            {
                                Root = lytRoot,
                                Data = lytRoot["Data"],
                                Situation = lytRoot["Data"]["Situation"],
                                Details = lytRoot["Data"]["Details"],
                                State = lytRoot["Data"]["Situation"]["State"],
                                Countdowns = lytRoot["Data"]["Situation"]["Countdowns"],
                                Readings = lytRoot["Data"]["Details"]["Readings"],
                                RemoteFiles = lytRoot["Data"]["Details"]["Readings"]["RemoteFiles"],
                                Sensors = lytRoot["Data"]["Details"]["Readings"]["Sensors"],
                                Histories = lytRoot["Data"]["Details"]["Histories"],
                                StatusHistory = lytRoot["Data"]["Details"]["Histories"]["StatusHistory"],
                                PanicHistory = lytRoot["Data"]["Details"]["Histories"]["PanicHistory"],
                                TriggerHistory = lytRoot["Data"]["Details"]["Histories"]["TriggerHistory"],
                                Prompt = lytRoot["Prompt"],

                                StateTable = tblState,
                                CountdownTable = tblCountdown,
                                SensorTable = tblSensors,
                                RemoteTable = tblRemote,
                                StateHistoryTable = tblHistoryStates,
                                PanicHistoryTable = tblHistoryPanics,
                                TriggerHistoryTable = tblHistoryTriggers,
                            };

                        layoutCollection.Prompt.Size = AnubisOptions.Options.commandPromptSize;
                        layoutCollection.Prompt.MinimumSize = AnubisOptions.Options.commandPromptSize;

                        layoutCollection.Situation.Size = AnubisOptions.Options.situationWidthLeft;
                        layoutCollection.Readings.Ratio = AnubisOptions.Options.sensorReadingsWidthRatio;
                        layoutCollection.Histories.Ratio = AnubisOptions.Options.historyWidthRatio;

                        layoutCollection.State.Size = 18;

                        layoutCollection.RemoteFiles.Size = 3 + (sensorCollection.RemoteFiles.Count * 5); // fixed size as big as there are remote files

                        layoutCollection.StatusHistory.Ratio = AnubisOptions.Options.statusHistoryHeightRatio;
                        layoutCollection.PanicHistory.Ratio = AnubisOptions.Options.panicHistoryHeightRatio;
                        layoutCollection.TriggerHistory.Ratio = AnubisOptions.Options.triggerHistoryHeightRatio;

                        layoutCollection.State.Update(tblState);
                        layoutCollection.Countdowns.Update(tblCountdown);
                        layoutCollection.Sensors.Update(tblSensors);

                        layoutCollection.StatusHistory.Update(tblHistoryStates);
                        layoutCollection.PanicHistory.Update(tblHistoryPanics);
                        layoutCollection.TriggerHistory.Update(tblHistoryTriggers);

                        if (sensorCollection.RemoteFiles.Count > 0)
                        {
                            layoutCollection.RemoteFiles.Update(tblRemote);
                        }
                        else
                        {
                            layoutCollection.RemoteFiles.Invisible();
                        }

                        #endregion

                        #region Set up static shared variables
                        Data = new DisplayData()
                        {
                            Countdowns = countdownCollection,
                            States = stateCollection,
                            Layouts = layoutCollection,
                            Sensors = sensorCollection,
                        };
                        #endregion

                        #region Switch to alternate screen buffer
                        if (AnubisOptions.Options.alternateScreenMode)
                        {
                            if (AnsiConsole.Profile.Capabilities.AlternateBuffer)
                            {
                                logging?.LogInformation("Switching to alternate screen buffer now");
                                Console.Write("\x1b[?1049h");
                                blHasEnteredScreenMode = true;
                            }
                            else
                            {
                                logging?.LogWarning("Terminal doesn't support alternate screen buffer, therefore not switching to this mode");
                            }
                        }
                        #endregion

                        UpdateCommandPrompt();

                        DisplayLoop();

                        AnsiConsole.Clear();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red1]Could not initialize controller[/], ending ANUBIS Watcher now");
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    logging?.LogCritical(ex, "Uncaught error of type {type} in controller interface, message was: {message}", ex.GetType().Name, ex.Message);
                }
                finally
                {
                    AnsiConsole.Cursor.Show();
                    AnsiConsole.Clear();
                    if (blHasEnteredScreenMode)
                    {
                        logging?.LogInformation("Switching back to main screen buffer now");
                        Console.Write("\x1b[?1049l");
                    }
                    AnsiConsole.Clear();
                }
            }
        }

        private static void UpdateWaitCommandPrompt()
        {
            Data.Layouts.Prompt.Update(GetWaitForStatusSwitchPrompt());
            AnsiConsole.Clear();
            AnsiConsole.Write(Data.Layouts.Root);
        }

        private static void UpdateCommandPrompt(bool stateSwitchSuccess = true)
        {
            if (IsInStartup)
            {
                UpdateWaitCommandPrompt();
            }
            else
            {
                Data.Layouts.Prompt.Update(GetCommandPrompt(stateSwitchSuccess));
            }
        }

#pragma warning disable CA1859 // Verwenden Sie nach Möglichkeit konkrete Typen, um die Leistung zu verbessern.
        private static IRenderable GetCommandPrompt(bool stateSwitchSuccess = true)
#pragma warning restore CA1859 // Verwenden Sie nach Möglichkeit konkrete Typen, um die Leistung zu verbessern.
        {
            string strRetVal;

            ControllerStatus state = SharedData.CurrentControllerStatus;
            switch (state)
            {
                case ControllerStatus.ShutDown:
                case ControllerStatus.SafeMode:
                case ControllerStatus.Triggered:
                    strRetVal = ControlCommandPrompt.PROMPT_State_SafeMode;
                    break;
                case ControllerStatus.Monitoring:
                    strRetVal = ControlCommandPrompt.PROMPT_State_Monitoring;
                    break;
                case ControllerStatus.Stopped:
                    strRetVal = ControlCommandPrompt.PROMPT_State_Stopped;
                    break;
                case ControllerStatus.Armed:
                    strRetVal = ControlCommandPrompt.PROMPT_State_Armed;
                    break;
                case ControllerStatus.Holdback:
                    if (SharedData.Controller?.CanBeArmed ?? false)
                    {
                        strRetVal = ControlCommandPrompt.PROMPT_State_HoldbackArmable;
                    }
                    else
                    {
                        strRetVal = ControlCommandPrompt.PROMPT_State_Holdback;
                    }
                    break;
                default:
                    strRetVal = $"[{AnubisOptions.Options.defaultColor_Warning}]UNKNOWN STATE[/]";
                    break;
            }

            if (!stateSwitchSuccess)
            {
                strRetVal += $"[{AnubisOptions.Options.defaultColor_Warning}]{ControlCommandPrompt.WARN_StateSwitchFailed}[/]";
            }

            return Align.Left(new Markup(strRetVal), VerticalAlignment.Top);
        }

#pragma warning disable CA1859 // Verwenden Sie nach Möglichkeit konkrete Typen, um die Leistung zu verbessern.
        private static IRenderable GetWaitForStatusSwitchPrompt()
#pragma warning restore CA1859 // Verwenden Sie nach Möglichkeit konkrete Typen, um die Leistung zu verbessern.
        {
            return Align.Left(new Markup($"[{AnubisOptions.Options.defaultColor_Activated}]{ControlCommandPrompt.PROMPT_WaitForStateChange}[/]"), VerticalAlignment.Top);
        }

        private static void DisplayLoop()
        {
            ILogger? logging = SharedData.InterfaceLogging;

            using (logging?.BeginScope("DisplayLoop"))
            {
                bool continueLoop = true;

                while (continueLoop)
                {
                    try
                    {
                        try
                        {
                            if (StartedHoldbackMode)
                            {
                                bool blCanBeArmed = SharedData.Controller?.CanBeArmed ?? false;
                                if (blCanBeArmed && !CanBeArmedInHoldback)
                                {
                                    CanBeArmedInHoldback = true;
                                    UpdateCommandPrompt(true);
                                }
                                else if (!blCanBeArmed && CanBeArmedInHoldback)
                                {
                                    CanBeArmedInHoldback = false;
                                    UpdateCommandPrompt(true);
                                }
                            }
                            if (IsInStartup)
                            {
                                if (SharedData.CurrentControllerStatus != ControllerStatus.Stopped)
                                {
                                    IsInStartup = false;
                                    UpdateCommandPrompt();
                                }
                            }
                            if (!EnteredSafeMode)
                            {
                                if (SharedData.Controller?.IsInSafeMode ?? false)
                                {
                                    EnteredSafeMode = true;
                                    AnubisOptions.Options?.tone_SafeMode?.Play();
                                }
                            }
                            else
                            {
                                if (!(SharedData.Controller?.IsInSafeMode ?? false))
                                {
                                    EnteredSafeMode = false;
                                }
                            }
                            UpdateLayouts();

                            AnsiConsole.Clear();
                            AnsiConsole.Write(Data.Layouts.Root);
                        }
                        catch (Exception ex)
                        {
                            logging?.LogError(ex, "While trying to update layout: {message}", ex.Message);
                        }

                        Thread.Sleep(AnubisOptions.Options?.refreshRateInMilliseconds ?? 100);
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true);
                            while (Console.KeyAvailable)
                                key = Console.ReadKey(true);
                            continueLoop = ExecuteCommand(key);
                        }
                    }
                    catch (Exception ex)
                    {
                        logging?.LogError(ex, "While trying to read input key: {message}", ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Returns false if we should exit the loop because (ESC) was hit
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool ExecuteCommand(ConsoleKeyInfo key)
        {
            if (key.Key != ConsoleKey.Escape)
            {
                bool hasUpdatedCommandPrompt = false;

                if (!IsInStartup)
                {
                    if (key.Key == ConsoleKey.T)
                    {
                        if (SharedData.Controller != null)
                        {
                            SharedData.Controller.MailSendingDisabledManually = !SharedData.Controller.MailSendingDisabledManually;
                        }
                    }
                    else
                    {
                        switch (SharedData.CurrentControllerStatus)
                        {
                            case ControllerStatus.Stopped:
                                if (key.Key == ConsoleKey.M)
                                {
                                    StartedHoldbackMode = false;

                                    foreach (SensorWidget sw in Data.Sensors.SwitchSensors)
                                    {
                                        // reset our maximum diagram value
                                        sw?.ResetMinMaxRegisteredPower();
                                    }

                                    UpdateWaitCommandPrompt();
                                    UpdateCommandPrompt(SharedData.Controller?.StartMonitoring() ?? false);
                                    hasUpdatedCommandPrompt = true;
                                }
                                break;
                            case ControllerStatus.Monitoring:
                                if (key.Key == ConsoleKey.S)
                                {
                                    StartedHoldbackMode = false;
                                    UpdateWaitCommandPrompt();
                                    UpdateCommandPrompt(SharedData.Controller?.StopMonitoring(false) ?? false);
                                    hasUpdatedCommandPrompt = true;
                                }
                                else if (key.Key == ConsoleKey.H)
                                {
                                    if (SharedData.Controller?.EnterHoldBackMode() ?? false)
                                    {
                                        UpdateCommandPrompt(true);
                                        StartedHoldbackMode = true;
                                        CanBeArmedInHoldback = false;
                                    }
                                    else
                                    {
                                        StartedHoldbackMode = false;
                                        UpdateCommandPrompt(false);
                                    }
                                    hasUpdatedCommandPrompt = true;
                                }
                                break;
                            case ControllerStatus.Holdback:
                                if (key.Key == ConsoleKey.S)
                                {
                                    StartedHoldbackMode = false;
                                    UpdateWaitCommandPrompt();
                                    UpdateCommandPrompt(SharedData.Controller?.StopMonitoring(false) ?? false);
                                    hasUpdatedCommandPrompt = true;
                                }
                                else if (key.Key == ConsoleKey.D)
                                {
                                    StartedHoldbackMode = false;
                                    UpdateWaitCommandPrompt();
                                    UpdateCommandPrompt(SharedData.Controller?.DisarmPanicMode() ?? false);
                                    hasUpdatedCommandPrompt = true;
                                }
                                else if ((SharedData.Controller?.CanBeArmed ?? false) && key.Key == ConsoleKey.A)
                                {
                                    StartedHoldbackMode = false;
                                    UpdateWaitCommandPrompt();
                                    UpdateCommandPrompt(SharedData.Controller?.ArmPanicMode() ?? false);
                                    hasUpdatedCommandPrompt = true;
                                }
                                break;
                            case ControllerStatus.Armed:
                                if (key.Key == ConsoleKey.S)
                                {
                                    StartedHoldbackMode = false;
                                    UpdateWaitCommandPrompt();
                                    UpdateCommandPrompt(SharedData.Controller?.StopMonitoring(false) ?? false);
                                    hasUpdatedCommandPrompt = true;
                                }
                                else if (key.Key == ConsoleKey.D)
                                {
                                    StartedHoldbackMode = false;
                                    UpdateWaitCommandPrompt();
                                    UpdateCommandPrompt(SharedData.Controller?.DisarmPanicMode() ?? false);
                                    hasUpdatedCommandPrompt = true;
                                }
                                break;
                            case ControllerStatus.SafeMode:
                            case ControllerStatus.Triggered:
                            case ControllerStatus.ShutDown:
                                if (key.Key == ConsoleKey.S)
                                {
                                    StartedHoldbackMode = false;
                                    UpdateWaitCommandPrompt();
                                    UpdateCommandPrompt(SharedData.Controller?.StopMonitoring(false) ?? false);
                                    hasUpdatedCommandPrompt = true;
                                }
                                break;
                        }
                    }
                }


                // Update the prompt if we haven't, just in case a panic was thrown and we can't disarm any more even though it's displaying that
                if (!hasUpdatedCommandPrompt)
                    UpdateCommandPrompt();

                return true;
            }
            else
            {
                return false;
            }
        }

        private static void UpdateLayouts()
        {
            if (SharedData.Controller != null)
            {
                BooleanState sttHasSentMails = BooleanState.Unknown;
                CountdownData? countdownInfo = SharedData.CountdownInfo;
                if (countdownInfo != null)
                {
                    if (countdownInfo.Emails_Sent)
                        sttHasSentMails = BooleanState.True;
                    else if (countdownInfo.Emails_Sending)
                        sttHasSentMails = BooleanState.False;
                }

                ControllerStatus sttControler = SharedData.CurrentControllerStatus;
                BooleanState sttHasPanic = UIHelpers.GetBooleanState(SharedData.Controller.HasPanicked || SharedData.HasPanic);
                BooleanState sttCanArm = BooleanState.Unknown;
                BooleanState sttHasShutDown = UIHelpers.GetBooleanState(SharedData.Controller.HasShutDown);
                BooleanState sttHasShutDownVerified = UIHelpers.GetBooleanState(countdownInfo != null ? countdownInfo.HasVerifiedSystemShutdown : SharedData.Controller.HasShutDownVerified);

                if (SharedData.Controller.CanBeArmed.HasValue && SharedData.CurrentControllerStatus == ControllerStatus.Holdback)
                {
                    if (SharedData.Controller.CanBeArmed.Value)
                    {
                        sttCanArm = BooleanState.True;
                    }
                    else
                    {
                        sttCanArm = BooleanState.False;
                    }
                }

                if (SensorRotation.RotateSensors)
                {
                    SensorRotation.UpdateRotation();
                }

                foreach (SensorWidget sw in Data.Sensors.SwitchSensors)
                {
                    var wdgSensor = sw.GetWidget();
                    int snsRow = SensorRotation.GetSensorPosition(sw.Options.OriginalSensorListIndex);
                    if (wdgSensor != null)
                    {
                        Data.Layouts.SensorTable.UpdateCell(snsRow, 0, wdgSensor);
                    }
                    else
                    {
                        Data.Layouts.SensorTable.UpdateCell(snsRow, 0, "");
                    }
                }

                foreach (SensorWidget sw in Data.Sensors.RemoteFiles)
                {
                    var wdgSensor = sw.GetWidget();
                    if (wdgSensor != null)
                    {
                        Data.Layouts.RemoteTable.UpdateCell(sw.Options.OriginalSensorListIndex, 0, wdgSensor);
                    }
                    else
                    {
                        Data.Layouts.RemoteTable.UpdateCell(sw.Options.OriginalSensorListIndex, 0, "");
                    }
                }

                if (SharedData.HasInformationChange(InformationRequester.GUI))
                {
                    #region Controller status history
                    var lstControllerStatusHistory = SharedData.GetControllerStatusHistory(InformationRequester.GUI)
                                                                .OrderByDescending(itm => itm.UtcTimestamp)
                                                                .Take(AnubisOptions.Options.stateHistoryEntryLoadCount).ToList();
                    if (lstControllerStatusHistory.Count > Data.Layouts.StateHistoryTable.Rows.Count)
                    {
                        for (int i = Data.Layouts.StateHistoryTable.Rows.Count; i < lstControllerStatusHistory.Count; i++)
                        {
                            Data.Layouts.StateHistoryTable.AddRow("");
                        }
                    }
                    else if (lstControllerStatusHistory.Count < Data.Layouts.StateHistoryTable.Rows.Count)
                    {
                        for (int i = Data.Layouts.StateHistoryTable.Rows.Count - 1; i >= lstControllerStatusHistory.Count; i--)
                        {
                            Data.Layouts.StateHistoryTable.RemoveRow(i);
                        }
                    }
                    for (int i = 0; i < lstControllerStatusHistory.Count; i++)
                    {
                        Data.Layouts.StateHistoryTable.UpdateCell(i, 0, HistoryWidget.GetWidget(lstControllerStatusHistory[i]));
                    }
                    #endregion

                    #region Panic history
                    var lstPanicHistory = SharedData.GetPanicHistory(InformationRequester.GUI,
                                                                        onlyTriggering: AnubisOptions.Options.hideNonTriggeringPanics)
                                                        .OrderByDescending(itm => itm.UtcTimestamp)
                                                        .Take(AnubisOptions.Options.panicHistoryEntryLoadCount).ToList();
                    if (lstPanicHistory.Count > Data.Layouts.PanicHistoryTable.Rows.Count)
                    {
                        for (int i = Data.Layouts.PanicHistoryTable.Rows.Count; i < lstPanicHistory.Count; i++)
                        {
                            Data.Layouts.PanicHistoryTable.AddRow("");
                        }
                    }
                    else if (lstPanicHistory.Count < Data.Layouts.PanicHistoryTable.Rows.Count)
                    {
                        for (int i = Data.Layouts.PanicHistoryTable.Rows.Count - 1; i >= lstPanicHistory.Count; i--)
                        {
                            Data.Layouts.PanicHistoryTable.RemoveRow(i);
                        }
                    }
                    for (int i = 0; i < lstPanicHistory.Count; i++)
                    {
                        Data.Layouts.PanicHistoryTable.UpdateCell(i, 0, HistoryWidget.GetWidget(lstPanicHistory[i]));
                    }
                    #endregion

                    #region Trigger history
                    var lstTriggerHistory = SharedData.GetTriggerHistory(InformationRequester.GUI)
                                                        .OrderByDescending(itm => itm.UtcTimestamp)
                                                        .Take(AnubisOptions.Options.triggerHistoryEntryLoadCount).ToList();
                    if (lstTriggerHistory.Count > Data.Layouts.TriggerHistoryTable.Rows.Count)
                    {
                        for (int i = Data.Layouts.TriggerHistoryTable.Rows.Count; i < lstTriggerHistory.Count; i++)
                        {
                            Data.Layouts.TriggerHistoryTable.AddRow("");
                        }
                    }
                    else if (lstTriggerHistory.Count < Data.Layouts.TriggerHistoryTable.Rows.Count)
                    {
                        for (int i = Data.Layouts.TriggerHistoryTable.Rows.Count - 1; i >= lstTriggerHistory.Count; i--)
                        {
                            Data.Layouts.TriggerHistoryTable.RemoveRow(i);
                        }
                    }
                    for (int i = 0; i < lstTriggerHistory.Count; i++)
                    {
                        Data.Layouts.TriggerHistoryTable.UpdateCell(i, 0, HistoryWidget.GetWidget(lstTriggerHistory[i]));
                    }
                    #endregion

                    UpdateCommandPrompt();
                }

                #region Countdowns
                if (countdownInfo != null)
                {
                    var wdgCountdownT0 = Data.Countdowns.CountdownT0.GetWidget(countdownInfo.Countdown_T0, countdownInfo.Timestamp_T0_Local, countdownInfo.Timestamp_T0_UTC);
                    if (wdgCountdownT0 != null)
                    {
                        Data.Layouts.CountdownTable.UpdateCell(0, 0, wdgCountdownT0);
                    }

                    var wdgCountdownSafeMode = Data.Countdowns.CountdownSafeMode?.GetWidget(countdownInfo.Countdown_SafeMode, countdownInfo.Timestamp_SafeMode_Local, countdownInfo.Timestamp_SafeMode_UTC);
                    if (wdgCountdownSafeMode != null)
                    {
                        Data.Layouts.CountdownTable.UpdateCell(1, 0, wdgCountdownSafeMode);
                    }
                    else
                    {
                        Data.Layouts.CountdownTable.UpdateCell(1, 0, "");
                    }

                    var wdgCountdownVerifyShutDown = Data.Countdowns.CountdownVerifyShutDown?.GetWidget(countdownInfo.Countdown_CheckShutDown, countdownInfo.Timestamp_CheckShutDown_Local, countdownInfo.Timestamp_CheckShutDown_UTC);
                    if (wdgCountdownVerifyShutDown != null)
                    {
                        Data.Layouts.CountdownTable.UpdateCell(2, 0, wdgCountdownVerifyShutDown);
                    }

                    bool blHasMailPrio = SharedData.Controller.Poller_ReadFiles?.HasCurrentMailPriority ?? true;
                    bool blMailDisabled = SharedData.Controller.MailSendingDisabledManually;
                    Data.Countdowns.CountdownSendMails?.ChangeTitle("Send Mails" + (blMailDisabled ? " (disabled)" : "") + (blHasMailPrio ? " (prio)" : ""));
                    var wdgCountdownSendMail = Data.Countdowns.CountdownSendMails?.GetWidget(countdownInfo.Countdown_Emails, countdownInfo.Timestamp_Emails_Local, countdownInfo.Timestamp_Emails_UTC);
                    if (wdgCountdownSendMail != null)
                    {
                        Data.Layouts.CountdownTable.UpdateCell(3, 0, wdgCountdownSendMail);
                    }

                    var wdgCountdownCurrentTime = Data.Countdowns.CountdownCurrentTime?.GetWidget(null, DateTime.Now, DateTime.UtcNow);
                    if (wdgCountdownCurrentTime != null)
                    {
                        Data.Layouts.CountdownTable.UpdateCell(4, 0, wdgCountdownCurrentTime);
                    }
                }
                #endregion

                if (Data.States.ControllerState.HasChange(sttControler))
                {
                    var stwController = Data.States.ControllerState.GetWidget(sttControler);
                    if (stwController != null)
                    {
                        Data.Layouts.StateTable.UpdateCell(0, 0, stwController);
                    }
                }

                if (Data.States.HasPanic.HasChange(sttHasPanic))
                {
                    var stwHasPanic = Data.States.HasPanic.GetWidget(sttHasPanic);
                    if (stwHasPanic != null)
                    {
                        Data.Layouts.StateTable.UpdateCell(1, 0, stwHasPanic);
                    }
                    if (Data.States.HasPanic.CurrentState == BooleanState.True || SharedData.HasPanic)
                    {
                        AnubisOptions.Options?.tone_Panic?.Play();
                    }
                }

                if (sttCanArm != BooleanState.Unknown)
                {
                    //if (Data.States.CanArm.HasChange(sttCanArm) || (!ShowArmableState.HasValue || !ShowArmableState.Value)) // or if we changed from hasshutdown displaying
                    {
                        ShowArmableState = true;
                        var stwCanArm = Data.States.CanArm.GetWidget(sttCanArm);
                        if (stwCanArm != null)
                        {
                            List<string> lstPollerCounts = [];
                            var minPollerCount_Countdown = (SharedData.Controller.Poller_Countdown?.Options?.MinPollerCountToArm ?? 0);
                            var minPollerCount_FritzPoller = (SharedData.Controller.Poller_Fritz?.Options?.MinPollerCountToArm ?? 0);
                            var minPollerCount_ClewareUSBPoller = (SharedData.Controller.Poller_ClewareUSB?.Options?.MinPollerCountToArm ?? 0);
                            var minPollerCount_SwitchBotPoller = (SharedData.Controller.Poller_SwitchBot?.Options?.MinPollerCountToArm ?? 0);
                            var minPollerCount_RemoteReadFiles = (SharedData.Controller.Poller_ReadFiles?.Options?.MinPollerCountToArm ?? 0);
                            var minPollerCount_LocalWriteFiles = (SharedData.Controller.Poller_WriteFiles?.Options?.MinPollerCountToArm ?? 0);

                            if (minPollerCount_Countdown > 0)
                            {
                                lstPollerCounts.Add($"Countdown: {UIHelpers.GetPollerCount(SharedData.Controller.Poller_Countdown?.PollerCount ?? 0, minPollerCount_Countdown)}");
                            }
                            if (minPollerCount_FritzPoller > 0)
                            {
                                lstPollerCounts.Add($"Fritz: {UIHelpers.GetPollerCount(SharedData.Controller.Poller_Fritz?.PollerCount ?? 0, minPollerCount_FritzPoller)}");
                            }
                            if (minPollerCount_ClewareUSBPoller > 0)
                            {
                                lstPollerCounts.Add($"USB: {UIHelpers.GetPollerCount(SharedData.Controller.Poller_ClewareUSB?.PollerCount ?? 0, minPollerCount_ClewareUSBPoller)}");
                            }
                            if (minPollerCount_SwitchBotPoller > 0)
                            {
                                lstPollerCounts.Add($"SwitchBot: {UIHelpers.GetPollerCount(SharedData.Controller.Poller_SwitchBot?.PollerCount ?? 0, minPollerCount_SwitchBotPoller)}");
                            }
                            if (minPollerCount_RemoteReadFiles > 0)
                            {
                                lstPollerCounts.Add($"Remote: {UIHelpers.GetPollerCount(SharedData.Controller.Poller_ReadFiles?.PollerCount ?? 0, minPollerCount_RemoteReadFiles)}");
                            }
                            if (minPollerCount_LocalWriteFiles > 0)
                            {
                                lstPollerCounts.Add($"Local: {UIHelpers.GetPollerCount(SharedData.Controller.Poller_WriteFiles?.PollerCount ?? 0, minPollerCount_LocalWriteFiles)}");
                            }

                            if (lstPollerCounts.Count > 0)
                            {
                                string strPollerCounts = string.Join("\r\n", lstPollerCounts);

                                Table tblStateAndPollerCount =
                                    new Table()
                                    {
                                        Border = TableBorder.None,
                                        //Expand = true,
                                        ShowFooters = false,
                                        ShowHeaders = false,
                                        ShowRowSeparators = false,
#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
                                        Width = AnubisOptions.Options.SituationInsideWidthLeft,
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.
                                    }
                                    .AddColumn(new TableColumn("") { Alignment = Justify.Left })
                                    .AddRow(stwCanArm)
                                    .AddRow(Align.Left(new Markup($"[{AnubisOptions.Options.defaultColor_Info}]{strPollerCounts}[/]")));

                                Data.Layouts.StateTable.UpdateCell(2, 0, tblStateAndPollerCount);
                            }
                            else
                            {
                                Data.Layouts.StateTable.UpdateCell(2, 0, stwCanArm);
                            }
                        }
                    }
                }
                else
                {
                    if (Data.States.HasShutDown.HasChange(sttHasShutDown) || (!ShowArmableState.HasValue || ShowArmableState.Value)) // or if we changed from CanArm displaying
                    {
                        ShowArmableState = false;
                        var stwHasShutDown = Data.States.HasShutDown.GetWidget(sttHasShutDown);
                        if (stwHasShutDown != null)
                        {
                            Data.Layouts.StateTable.UpdateCell(2, 0, stwHasShutDown);
                        }
                        if (SharedData.Controller.HasShutDown)
                        {
                            AnubisOptions.Options?.tone_ShutDown?.Play();
                        }
                    }
                }

                if (Data.States.HasShutDownVerified.HasChange(sttHasShutDownVerified))
                {
                    var stwHasShutDownVerified = Data.States.HasShutDownVerified.GetWidget(sttHasShutDownVerified);
                    if (stwHasShutDownVerified != null)
                    {
                        Data.Layouts.StateTable.UpdateCell(3, 0, stwHasShutDownVerified);
                    }
                }

                if (Data.States.HasSentMails.HasChange(sttHasSentMails))
                {
                    var stwHasSentMails = Data.States.HasSentMails.GetWidget(sttHasSentMails);
                    if (stwHasSentMails != null)
                    {
                        Data.Layouts.StateTable.UpdateCell(4, 0, stwHasSentMails);
                    }
                }
            }
            else
            {
                throw new Exception("Unexpected empty controller");
            }
        }
    }
}
