using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ANUBISConsole.ConfigHelpers
{
#pragma warning disable IDE1006 // Benennungsstile
    public enum ScreenMode
    {
        Default,
        AlternateScreen,
    }

    public class ColorComposition
    {
        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color backgroundColor { get; set; } = Color.Black;
        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color textColor { get; set; } = Color.White;
    }

    public class CountdownComposition
    {
        /// <summary>
        /// The minutes before we change a countdown in the "shortly before" display color mode
        /// </summary>
        public int shortlyBeforeModeInMinutes { get; set; } = 1;
        public ColorComposition compositionBeforeT0 { get; set; } = new ColorComposition();
        public ColorComposition? compositionShortlyBeforeT0 { get; set; } = null;
        public ColorComposition compositionFromT0 { get; set; } = new ColorComposition();
    }

    public class ControllerStateComposition
    {
        public ColorComposition compositionStopped { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionMonitoring { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionHoldback { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionArmed { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionSafeMode { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionShutDown { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionTriggered { get; set; } =
                                    new ColorComposition();
    }

    public class TagComposition
    {
        public ColorComposition compositionHeldBack { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionArmable { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionArmed { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionShutdown { get; set; } =
                                    new ColorComposition();

        public ColorComposition compositionSaved { get; set; } =
                                    new ColorComposition();
    }

    public class BooleanStateComposition
    {
        public ColorComposition compositionUnknown { get; set; } = new ColorComposition();
        public ColorComposition compositionFalse { get; set; } = new ColorComposition();
        public ColorComposition compositionTrue { get; set; } = new ColorComposition();
    }

    public class BeepConfig
    {
        public int hertz { get; set; } = 2000;
        public int milliseconds { get; set; } = 150;
        public int count { get; set; } = 1;
        public int delayMilliseconds { get; set; } = 150;

        [SupportedOSPlatform("windows")]
        private void WindowsPlay()
        {
            new Thread(() => { try { for (int i = 0; !Generator.IsThreadCancelled() && i < count; i++) { Console.Beep(hertz, milliseconds); Generator.WaitMilliseconds(delayMilliseconds); } } catch { } }).Start();
        }

        public void Play()
        {
            if (OperatingSystem.IsWindows())
            {
                WindowsPlay();
            }
        }
    }

    public class AnubisOptions
    {
        public string configDirectory { get; set; } = "configs";
        public int pageSize { get; set; } = 20;
        public bool alternateScreenMode { get; set; } = true;
        public int refreshRateInMilliseconds { get; set; } = 200;
        public int rotateSensorsEveryXSeconds { get; set; } = 10;
        public bool exitAlternateBufferOnStartup { get; set; } = true;
        public bool forceInteractiveConsole { get; set; } = true;
        public ConsoleColor backgroundColor { get; set; } = ConsoleColor.Black;
        public ConsoleColor textColor { get; set; } = ConsoleColor.Gray;
        public bool forceDefaultColors { get; set; } = true;
        public bool hideNonTriggeringPanics { get; set; } = true;
        public bool showMinMaxPower { get; set; } = false;

        public BeepConfig? tone_Panic { get; set; } =
                                new BeepConfig()
                                {
                                    milliseconds = 500,
                                    count = 2,
                                    delayMilliseconds = 250,
                                };
        public BeepConfig? tone_ShutDown { get; set; } =
                                new BeepConfig()
                                {
                                    count = 2,
                                };
        public BeepConfig? tone_SafeMode { get; set; } =
                                new BeepConfig()
                                {
                                };

        /// <summary>
        /// size (of height) of the command prompt in lines
        /// </summary>
        public int commandPromptSize { get; set; } = 3;

        /// <summary>
        /// Maximum amount of state history entries displayed
        /// </summary>
        public int stateHistoryEntryLoadCount { get; set; } = 20;

        /// <summary>
        /// Maximum amount of panic history entries displayed
        /// </summary>
        public int panicHistoryEntryLoadCount { get; set; } = 20;

        /// <summary>
        /// Maximum amount of trigger history entries displayed
        /// </summary>
        public int triggerHistoryEntryLoadCount { get; set; } = 20;

        /// <summary>
        /// Width of our situation column of the controller on the left side
        /// </summary>
        public int situationWidthLeft { get; set; } = 32;

        /// <summary>
        /// Width of our sensor column of the controller on the right side (ratio compared to history width)
        /// </summary>
        public int sensorReadingsWidthRatio { get; set; } = 46;

        /// <summary>
        /// Width of our history column of the controller on the right side (ratio compared to sensor width)
        /// </summary>
        public int historyWidthRatio { get; set; } = 54;

        /// <summary>
        /// Height of our status history row on the right side (ratio compared to other history heights)
        /// </summary>
        public int statusHistoryHeightRatio { get; set; } = 35;

        /// <summary>
        /// Height of our panic history row on the right side (ratio compared to other history heights)
        /// </summary>
        public int panicHistoryHeightRatio { get; set; } = 30;

        /// <summary>
        /// Height of our trigger history row on the right side (ratio compared to other history heights)
        /// </summary>
        public int triggerHistoryHeightRatio { get; set; } = 35;

        /// <summary>
        /// Width inside our situation column of the controller on the left side
        /// </summary>
        [JsonIgnore]
        public int SituationInsideWidthLeft { get; set; }

        #region Colors

        #region default colors

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_Normal { get; set; } = Color.Grey;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_Info { get; set; } = Color.White;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_Ok { get; set; } = Color.Lime;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_Error { get; set; } = Color.Red1;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_Warning { get; set; } = Color.Orange1;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_Choice { get; set; } = Color.Blue;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_Activated { get; set; } = Color.MediumPurple2;


        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_State_Info { get; set; } = Color.White;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_State_Ok { get; set; } = Color.Lime;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_State_Warning { get; set; } = Color.Orange1;

        [JsonConverter(typeof(JsonSpectreColorConverter))]
        public Color defaultColor_State_Error { get; set; } = Color.OrangeRed1;

        public ColorComposition defaultComposition_Skipped { get; set; } =
                                    new ColorComposition()
                                    {
                                        backgroundColor = Color.DarkOrange3,
                                        textColor = Color.White,
                                    };

        public ColorComposition defaultComposition_Success { get; set; } =
                                    new ColorComposition()
                                    {
                                        backgroundColor = Color.DarkGreen,
                                        textColor = Color.White,
                                    };

        public ColorComposition defaultComposition_Failure { get; set; } =
                                    new ColorComposition()
                                    {
                                        backgroundColor = Color.Maroon,
                                        textColor = Color.White,
                                    };
        #endregion

        #region status colors

        public ControllerStateComposition defaultComposition_State_Controller { get; set; } =
                                    new ControllerStateComposition()
                                    {
                                        compositionStopped =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },

                                        compositionMonitoring =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Blue,
                                                textColor = Color.White,
                                            },

                                        compositionHoldback =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkOrange3,
                                                textColor = Color.White,
                                            },

                                        compositionArmed =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.MediumPurple2,
                                                textColor = Color.White,
                                            },

                                        compositionSafeMode =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Orange1,
                                                textColor = Color.Black,
                                            },

                                        compositionShutDown =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Aquamarine3,
                                                textColor = Color.Black,
                                            },

                                        compositionTriggered =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                    };

        public BooleanStateComposition defaultComposition_State_VerifiedShutDown { get; set; } =
                                    new BooleanStateComposition()
                                    {
                                        compositionUnknown =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionFalse =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionTrue =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                    };

        public BooleanStateComposition defaultComposition_State_SendMails { get; set; } =
                                    new BooleanStateComposition()
                                    {
                                        compositionUnknown =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionFalse =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Orange1,
                                                textColor = Color.Black,
                                            },
                                        compositionTrue =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                    };

        public BooleanStateComposition defaultComposition_State_ShutDown { get; set; } =
                                    new BooleanStateComposition()
                                    {
                                        compositionUnknown =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionFalse =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionTrue =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Aquamarine3,
                                                textColor = Color.Black,
                                            },
                                    };

        public BooleanStateComposition defaultComposition_State_Panicked { get; set; } =
                                    new BooleanStateComposition()
                                    {
                                        compositionUnknown =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionFalse =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                        compositionTrue =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Maroon,
                                                textColor = Color.White,
                                            },
                                    };

        public BooleanStateComposition defaultComposition_State_HoldbackArmable { get; set; } =
                                    new BooleanStateComposition()
                                    {
                                        compositionUnknown =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionFalse =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkOrange3,
                                                textColor = Color.White,
                                            },
                                        compositionTrue =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                    };

        #endregion

        #region Tag colors

        public TagComposition defaultComposition_Tags { get; set; } =
                                    new TagComposition()
                                    {
                                        compositionHeldBack =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkOrange3,
                                                textColor = Color.White,
                                            },

                                        compositionArmable =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },

                                        compositionArmed =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.MediumPurple2,
                                                textColor = Color.White,
                                            },

                                        compositionShutdown =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Maroon,
                                                textColor = Color.White,
                                            },

                                        compositionSaved =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Orange1,
                                                textColor = Color.Black,
                                            },
                                    };

        #endregion

        #region Countdown Colors
        public CountdownComposition defaultComposition_Countdown_Main { get; set; } =
                                    new CountdownComposition()
                                    {
                                        shortlyBeforeModeInMinutes = 7,
                                        compositionBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                        compositionShortlyBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Purple_1,
                                                textColor = Color.White,
                                            },
                                        compositionFromT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Maroon,
                                                textColor = Color.White,
                                            },
                                    };
        public CountdownComposition defaultComposition_Countdown_SafeMode { get; set; } =
                                    new CountdownComposition()
                                    {
                                        shortlyBeforeModeInMinutes = 5,
                                        compositionBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                        compositionShortlyBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Purple_1,
                                                textColor = Color.White,
                                            },
                                        compositionFromT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Orange1,
                                                textColor = Color.Black,
                                            },
                                    };

        public CountdownComposition defaultComposition_Countdown_VerifyShutDown { get; set; } =
                                    new CountdownComposition()
                                    {
                                        shortlyBeforeModeInMinutes = 2,
                                        compositionBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionShortlyBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Purple_1,
                                                textColor = Color.White,
                                            },
                                        compositionFromT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                    };

        public CountdownComposition defaultComposition_Countdown_SendMails { get; set; } =
                                    new CountdownComposition()
                                    {
                                        shortlyBeforeModeInMinutes = 10,
                                        compositionBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionShortlyBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Purple_1,
                                                textColor = Color.White,
                                            },
                                        compositionFromT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.DarkGreen,
                                                textColor = Color.White,
                                            },
                                    };

        public CountdownComposition defaultComposition_Countdown_CurrentTime { get; set; } =
                                    new CountdownComposition()
                                    {
                                        shortlyBeforeModeInMinutes = 0,
                                        compositionBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionShortlyBeforeT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                        compositionFromT0 =
                                            new ColorComposition()
                                            {
                                                backgroundColor = Color.Black,
                                                textColor = Color.White,
                                            },
                                    };
        #endregion

        #endregion

        public static AnubisOptions Options { get; set; } = new();

        public AnubisOptions()
        {
            SituationInsideWidthLeft = situationWidthLeft - 2;
        }

        public static void LoadOptions()
        {
            Options = GetOptions();
        }

        public static AnubisOptions GetOptions()
        {
            ILogger? logging = SharedData.InterfaceLogging;

            using (logging?.BeginScope("GetOptions"))
            {
                try
                {
                    string strPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                    using (FileStream sr = File.OpenRead(strPath))
                    {
#pragma warning disable CA1869 // JsonSerializerOptions-Instanzen zwischenspeichern und wiederverwenden
                        return (JsonSerializer.Deserialize<AnubisOptions>(sr, new JsonSerializerOptions() { AllowTrailingCommas = true }) ?? new AnubisOptions()).FullyQualifyPaths();
#pragma warning restore CA1869 // JsonSerializerOptions-Instanzen zwischenspeichern und wiederverwenden
                    }

                }
                catch (Exception ex)
                {
                    logging?.LogError(ex, "While trying to read options from appsettings.json, will use default options: {message}", ex.Message);

                    return new AnubisOptions().FullyQualifyPaths();
                }

            }
        }

        private AnubisOptions FullyQualifyPaths()
        {
            ILogger? logging = SharedData.InterfaceLogging;

            using (logging?.BeginScope("FullyQualifyPaths"))
            {
                try
                {
                    configDirectory = Path.GetFullPath(configDirectory);
                }
                catch (Exception ex)
                {
                    logging?.LogError(ex, "While trying to fully qualify path in configDirectory with value \"{path}\": {message}", configDirectory, ex.Message);
                }
            }

            return this;
        }
    }
#pragma warning restore IDE1006 // Benennungsstile
}
