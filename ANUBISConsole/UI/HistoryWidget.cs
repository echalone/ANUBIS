using ANUBISConsole.ConfigHelpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ANUBISConsole.UI
{
    public class HistoryWidget
    {
        public static IRenderable GetWidget(ControllerStatusHistoryEntry statusEntry)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("HistoryWidget.GetWidget.statusEntry"))
            {
                var tblMain = new Table()
                {
                    Border = TableBorder.None,
                    Expand = true,
                    ShowFooters = false,
                    ShowHeaders = false,
                    ShowRowSeparators = false,
                }
                                .AddColumn("");

                if (statusEntry != null)
                {
                    string strTimestamp = UIHelpers.GetTimestampString(statusEntry.UtcTimestamp) ?? "<unknown time>";
                    string strInfo = "";

                    Color clrStatus = AnubisOptions.Options.defaultColor_Info;
                    Color? clrBackground = null;
                    switch (statusEntry.Status)
                    {
                        case ControllerStatus.Stopped:
                            clrStatus = AnubisOptions.Options.defaultComposition_State_Controller.compositionStopped.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_State_Controller.compositionStopped.backgroundColor;
                            break;
                        case ControllerStatus.Monitoring:
                            clrStatus = AnubisOptions.Options.defaultComposition_State_Controller.compositionMonitoring.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_State_Controller.compositionMonitoring.backgroundColor;
                            break;
                        case ControllerStatus.Holdback:
                            clrStatus = AnubisOptions.Options.defaultComposition_State_Controller.compositionHoldback.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_State_Controller.compositionHoldback.backgroundColor;
                            break;
                        case ControllerStatus.Armed:
                            clrStatus = AnubisOptions.Options.defaultComposition_State_Controller.compositionArmed.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_State_Controller.compositionArmed.backgroundColor;
                            break;
                        case ControllerStatus.SafeMode:
                            clrStatus = AnubisOptions.Options.defaultComposition_State_Controller.compositionSafeMode.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_State_Controller.compositionSafeMode.backgroundColor;
                            if (statusEntry.SafeModeIncludesRemoteFiles)
                            {
                                strInfo += "with remotes";
                            }
                            else
                            {
                                strInfo += "no remotes";
                            }
                            break;
                        case ControllerStatus.ShutDown:
                            clrStatus = AnubisOptions.Options.defaultComposition_State_Controller.compositionShutDown.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_State_Controller.compositionShutDown.backgroundColor;
                            break;
                        case ControllerStatus.Triggered:
                            clrStatus = AnubisOptions.Options.defaultComposition_State_Controller.compositionTriggered.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_State_Controller.compositionTriggered.backgroundColor;
                            if (statusEntry.TriggerIsCountdownT0)
                            {
                                strInfo += "countdown T0";
                            }
                            break;
                    }
                    string strMarkupStatus = new Style(clrStatus, clrBackground, Decoration.Bold).ToMarkup();
                    string strOutFull = $"[{AnubisOptions.Options.defaultColor_Info}]{strTimestamp}: [/][{strMarkupStatus}] {statusEntry.Status} [/]";
                    if (!string.IsNullOrWhiteSpace(strInfo))
                    {
                        strOutFull += $" [{AnubisOptions.Options.defaultColor_Info}]({strInfo})[/]";
                    }

                    var mkpStatus = new Markup(strOutFull);

                    return mkpStatus;
                }
                else
                {
                    return new Markup("");
                }
            }
        }

        public static IRenderable GetWidget(PanicHistoryEntry panicEntry)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("HistoryWidget.GetWidget.panicEntry"))
            {
                var tblMain = new Table()
                {
                    Border = TableBorder.None,
                    Expand = true,
                    ShowFooters = false,
                    ShowHeaders = false,
                    ShowRowSeparators = false,
                }
                                .AddColumn("");

                if (panicEntry != null)
                {
                    #region Header

                    tblMain.AddRow(new Rule($"[{AnubisOptions.Options.defaultColor_Info}]{panicEntry.CountNumber}. Panic: [bold]{panicEntry.Id}[/][/]")
                    {
                        Justification = Justify.Left,
                        Border = BoxBorder.Square,
                        Style = new Style(AnubisOptions.Options.defaultColor_Info),
                    });

                    #endregion

                    #region SwitchType, PanicReason, Timestamp

                    #region SwitchType
                    string strSwitchType = panicEntry.SwitchType.ToString();
                    string strOutSwitchType = $"[{AnubisOptions.Options.defaultColor_Info}]Thrown by: [bold]{strSwitchType}[/][/]";
                    #endregion

                    #region PanicReason
                    string strOutPanic = $"[{AnubisOptions.Options.defaultColor_Info}]Reason: [/]";
                    if (panicEntry.PanicReason != UniversalPanicReason.NoPanic)
                    {
                        if (panicEntry.PanicReason == UniversalPanicReason.Unknown ||
                            panicEntry.PanicReason == UniversalPanicReason.All)
                        {
                            strOutPanic = $"[bold {AnubisOptions.Options.defaultComposition_Failure.textColor} on {AnubisOptions.Options.defaultComposition_Failure.backgroundColor}]";
                        }
                        else
                        {
                            strOutPanic = $"[bold {AnubisOptions.Options.defaultColor_Info}]";
                        }
                        strOutPanic += $"{panicEntry.PanicReason}[/]";
                    }
                    else
                    {
                        strOutPanic += $"[{AnubisOptions.Options.defaultColor_Warning}]NONE[/]";
                    }
                    #endregion

                    #region Timestamp

                    string strTimestamp = UIHelpers.GetTimestampString(panicEntry.UtcTimestamp) ?? "<unknown time>";
                    string strOutTimestamp = $"[{AnubisOptions.Options.defaultColor_Info}]at: [bold]{strTimestamp}[/][/]";

                    #endregion

                    string strOutPanicMarkup = $"{strOutSwitchType}; {strOutPanic}; {strOutTimestamp}";

                    tblMain.AddRow(new Markup(strOutPanicMarkup));

                    #endregion

                    #region Trigger Configs

                    string strTriggerConfigMarkupList = $"[{AnubisOptions.Options.defaultColor_Warning}]<NONE>[/]";
                    if (panicEntry.TriggerConfigs.Count > 0)
                    {
                        strTriggerConfigMarkupList = string.Join("[/], [bold]", panicEntry.TriggerConfigs);
                    }

                    string strOutTriggers = $"[{AnubisOptions.Options.defaultColor_Info}]Executing trigger configs: [bold]{strTriggerConfigMarkupList}[/][/]";
                    tblMain.AddRow(new Markup(strOutTriggers));

                    #endregion

                    return tblMain;
                }
                else
                {
                    return new Markup("");
                }
            }
        }

        public static IRenderable GetWidget(TriggerHistoryEntry triggerEntry)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("HistoryWidget.GetWidget.triggerEntry"))
            {
                var tblMain = new Table()
                {
                    Border = TableBorder.None,
                    Expand = true,
                    ShowFooters = false,
                    ShowHeaders = false,
                    ShowRowSeparators = false,
                }
                                .AddColumn("");

                if (triggerEntry != null)
                {
                    string strTimestamp = UIHelpers.GetTimestampString(triggerEntry.UtcTimestamp) ?? "<unknown time>";

                    Color clrOutcome = AnubisOptions.Options.defaultColor_Info;
                    Color? clrBackground = null;
                    switch (triggerEntry.Outcome)
                    {
                        case TriggerOutcome.Skipped:
                            clrOutcome = AnubisOptions.Options.defaultComposition_Skipped.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_Skipped.backgroundColor;
                            break;
                        case TriggerOutcome.Failed:
                            clrOutcome = AnubisOptions.Options.defaultComposition_Failure.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_Failure.backgroundColor;
                            break;
                        case TriggerOutcome.Success:
                            clrOutcome = AnubisOptions.Options.defaultComposition_Success.textColor;
                            clrBackground = AnubisOptions.Options.defaultComposition_Success.backgroundColor;
                            break;
                    }
                    string strMarkupOutcome = new Style(clrOutcome, clrBackground, Decoration.Bold).ToMarkup();
                    string strOutFull = $"[{AnubisOptions.Options.defaultColor_Info}]{strTimestamp}: Type={triggerEntry.Type}";
                    if (!string.IsNullOrWhiteSpace(triggerEntry.Name))
                    {
                        strOutFull += $"; Name={triggerEntry.Name}";
                    }
                    strOutFull += $"; Outcome=[/][{strMarkupOutcome}] {triggerEntry.Outcome} [/]";

                    var mkpStatus = new Markup(strOutFull);

                    return mkpStatus;
                }
                else
                {
                    return new Markup("");
                }
            }
        }
    }
}
