using ANUBISConsole.ConfigHelpers;
using ANUBISWatcher.Controlling;
using ANUBISWatcher.Entities;
using ANUBISWatcher.Pollers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Globalization;

namespace ANUBISConsole.UI
{
    public class SensorWidgetOptions
    {
        public int OriginalSensorListIndex { get; set; }

        public SensorWidgetOptions(int originalSensorListIndex)
        {
            OriginalSensorListIndex = originalSensorListIndex;
        }
    }

    public class PollerDetails
    {
        public bool? Responsive { get; set; }
        public DateTime? FirstUnresponsive { get; set; }
        public DateTime? LastUnresponsive { get; set; }
        public long? UpdateAge { get; set; }
        public long? RemainingTillStuck { get; set; }
        public long? MaxUpdateAge { get; set; }
        public long? MinRemainingTillStuck { get; set; }

        public string GetMarkup(string name)
        {
            string strMarkup = $"{name}:";

            if (Responsive.HasValue)
            {
                if (Responsive.Value)
                {
                    strMarkup += $" [{AnubisOptions.Options.defaultColor_Ok}]OK[/]";
                }
                else
                {
                    strMarkup += $" [{AnubisOptions.Options.defaultComposition_Failure.textColor} on {AnubisOptions.Options.defaultComposition_Failure.backgroundColor}]STUCK[/]";
                }
            }

            if (UpdateAge.HasValue)
            {
                strMarkup += $" [{AnubisOptions.Options.defaultColor_Info}]{UpdateAge.Value}[/]";

                if (RemainingTillStuck.HasValue)
                {
                    strMarkup += "/";
                    string strPrefix = "-";
                    var textColor = AnubisOptions.Options.defaultColor_Info;
                    long remaining = RemainingTillStuck.Value;

                    if (remaining <= 0)
                    {
                        remaining *= -1;
                        strPrefix = "+";

                        textColor = AnubisOptions.Options.defaultColor_Warning;
                    }

                    strMarkup += $"[{textColor}]{strPrefix}{remaining}[/]";
                }
            }

            if (MaxUpdateAge.HasValue)
            {
                strMarkup += $" ([{AnubisOptions.Options.defaultColor_Info}]{MaxUpdateAge.Value}[/]";

                if (MinRemainingTillStuck.HasValue)
                {
                    strMarkup += "/";
                    string strPrefix = "-";
                    var textColor = AnubisOptions.Options.defaultColor_Info;
                    long remaining = MinRemainingTillStuck.Value;

                    if (remaining <= 0)
                    {
                        remaining *= -1;
                        strPrefix = "+";

                        textColor = AnubisOptions.Options.defaultColor_Warning;
                    }

                    strMarkup += $"[{textColor}]{strPrefix}{remaining}[/])";
                }
            }

            if (FirstUnresponsive.HasValue && LastUnresponsive.HasValue)
            {
                string strFirstUnresponsive = FirstUnresponsive.Value.ToLocalTime().ToString("HH:mm:ss");
                string strLastUnresponsive = LastUnresponsive.Value.ToLocalTime().ToString("HH:mm:ss");
                strMarkup += $" (first/last=[{AnubisOptions.Options.defaultColor_Info}]{strFirstUnresponsive}[/]/[{AnubisOptions.Options.defaultColor_Info}]{strLastUnresponsive}[/])";
            }

            return strMarkup;
        }

        public void Update(bool? isUnresponsive)
        {
            Update(isUnresponsive, null, null);
        }

        public void Update(bool? isUnresponsive, long? millisecondsSinceLastUpdate, long? alertTimeInMilliseconds)
        {
            if (isUnresponsive.HasValue)
            {
                Responsive = !isUnresponsive.Value;

                if (!Responsive.Value)
                {
                    LastUnresponsive = DateTime.UtcNow;
                    if (!FirstUnresponsive.HasValue)
                    {
                        FirstUnresponsive = LastUnresponsive;
                    }
                }
            }

            if (millisecondsSinceLastUpdate.HasValue)
            {
                long milliseconds = millisecondsSinceLastUpdate.Value;
                UpdateAge = (long)Math.Floor(milliseconds / 1000.0);

                if (!MaxUpdateAge.HasValue || MaxUpdateAge.Value < UpdateAge)
                {
                    MaxUpdateAge = UpdateAge;
                }

                if (alertTimeInMilliseconds.HasValue)
                {
                    RemainingTillStuck = (long)Math.Ceiling((alertTimeInMilliseconds.Value - milliseconds) / 1000.0);

                    if (!MinRemainingTillStuck.HasValue || MinRemainingTillStuck.Value > RemainingTillStuck)
                    {
                        MinRemainingTillStuck = RemainingTillStuck;
                    }
                }
            }
        }
    }

    public class PollerInfo
    {
        public MainController? Controller { get; set; }
        public PollerDetails Controller_Details { get; set; } = new PollerDetails();

        public CountdownPoller? Poller_Countdown { get; set; }
        public PollerDetails Poller_Countdown_Details { get; set; } = new PollerDetails();

        public FritzPoller? Poller_Fritz { get; set; }
        public PollerDetails Poller_Fritz_Details { get; set; } = new PollerDetails();

        public SwitchBotPoller? Poller_SwitchBot { get; set; }
        public PollerDetails Poller_SwitchBot_Details { get; set; } = new PollerDetails();

        public ClewarePoller? Poller_ClewareUSB { get; set; }
        public PollerDetails Poller_ClewareUSB_Details { get; set; } = new PollerDetails();

        public WatcherFilePoller? Poller_LocalWriteFiles { get; set; }
        public PollerDetails Poller_LocalWriteFiles_Details { get; set; } = new PollerDetails();

        public WatcherFilePoller? Poller_RemoteReadFiles { get; set; }
        public PollerDetails Poller_RemoteReadFiles_Details { get; set; } = new PollerDetails();

        public DateTime? LastDisplayRefresh { get; set; }
        public long? DisplayRefreshMilliseconds { get; set; }
        public long? DisplayRefreshMilliseconds_Min { get; set; }
        public long? DisplayRefreshMilliseconds_Max { get; set; }

        public PollerInfo(MainController? controller, CountdownPoller? countdownPoller, FritzPoller? fritzPoller,
                            SwitchBotPoller? switchBotPoller, ClewarePoller? clewareUsbPoller,
                            WatcherFilePoller? localWriteFilePoller, WatcherFilePoller? remoteReadFilePoller)
        {
            Controller = controller;
            Poller_Countdown = countdownPoller;
            Poller_Fritz = fritzPoller;
            Poller_SwitchBot = switchBotPoller;
            Poller_ClewareUSB = clewareUsbPoller;
            Poller_LocalWriteFiles = localWriteFilePoller;
            Poller_RemoteReadFiles = remoteReadFilePoller;
        }

        public void ResetValues()
        {
            Controller_Details = new PollerDetails();
            Poller_Countdown_Details = new PollerDetails();
            Poller_Fritz_Details = new PollerDetails();
            Poller_SwitchBot_Details = new PollerDetails();
            Poller_ClewareUSB_Details = new PollerDetails();
            Poller_RemoteReadFiles_Details = new PollerDetails();
            Poller_LocalWriteFiles_Details = new PollerDetails();

            LastDisplayRefresh = null;
            DisplayRefreshMilliseconds = null;
            DisplayRefreshMilliseconds_Max = null;
            DisplayRefreshMilliseconds_Min = null;
        }

        public void UpdateValues()
        {
            if (LastDisplayRefresh.HasValue)
            {
                DisplayRefreshMilliseconds = (long)Math.Floor((DateTime.UtcNow - LastDisplayRefresh.Value).TotalMilliseconds);

                if (!DisplayRefreshMilliseconds_Max.HasValue || DisplayRefreshMilliseconds_Max.Value < DisplayRefreshMilliseconds)
                {
                    DisplayRefreshMilliseconds_Max = DisplayRefreshMilliseconds;
                }

                if (!DisplayRefreshMilliseconds_Min.HasValue || DisplayRefreshMilliseconds_Min.Value > DisplayRefreshMilliseconds)
                {
                    DisplayRefreshMilliseconds_Min = DisplayRefreshMilliseconds;
                }
            }

            LastDisplayRefresh = DateTime.UtcNow;

            if (Controller != null)
            {
                Controller_Details.Update(Controller.IsControllerUnresponsive);
            }

            if (Poller_Countdown != null)
            {
                Poller_Countdown_Details.Update(Poller_Countdown.IsPollerUnresponsive);
            }

            if (Poller_Fritz != null)
            {
                Poller_Fritz_Details.Update(Poller_Fritz.IsPollerUnresponsive, Poller_Fritz.MillisecondsSinceLastUpdate, Poller_Fritz.AlertTimeInMilliseconds);
            }

            if (Poller_SwitchBot != null)
            {
                Poller_SwitchBot_Details.Update(Poller_SwitchBot.IsPollerUnresponsive, Poller_SwitchBot.MillisecondsSinceLastUpdate, Poller_SwitchBot.AlertTimeInMilliseconds);
            }

            if (Poller_ClewareUSB != null)
            {
                Poller_ClewareUSB_Details.Update(Poller_ClewareUSB.IsPollerUnresponsive, Poller_ClewareUSB.MillisecondsSinceLastUpdate, Poller_ClewareUSB.AlertTimeInMilliseconds);
            }

            if (Poller_RemoteReadFiles != null)
            {
                Poller_RemoteReadFiles_Details.Update(Poller_RemoteReadFiles.IsPollerUnresponsive, Poller_RemoteReadFiles.MillisecondsSinceLastUpdate, Poller_RemoteReadFiles.AlertTimeInMilliseconds);
            }

            if (Poller_LocalWriteFiles != null)
            {
                Poller_LocalWriteFiles_Details.Update(Poller_LocalWriteFiles.IsPollerUnresponsive, Poller_LocalWriteFiles.MillisecondsSinceLastUpdate, Poller_LocalWriteFiles.AlertTimeInMilliseconds);
            }
        }
    }

    public class SensorWidget
    {
        public SensorWidgetOptions Options { get; set; }

        public PollerInfo? Pollers { get; set; }
        public FritzPollerSwitch? FritzSwitch { get; init; }
        public SwitchBotPollerSwitch? SwitchBotSwitch { get; init; }
        public ClewarePollerSwitch? ClewareUSBSwitch { get; init; }
        public WatcherPollerFile? LocalWriteFile { get; init; }
        public WatcherPollerFile? RemoteReadFile { get; init; }
        public double? MaxPowerRegistered { get; set; }
        public double? MinPowerRegistered { get; set; }

        public SensorWidget(SensorWidgetOptions options)
        {
            Options = options;
        }

        public SensorWidget(SensorWidgetOptions options, PollerInfo pollerInfo)
            : this(options)
        {
            Pollers = pollerInfo;
        }

        public SensorWidget(SensorWidgetOptions options, FritzPollerSwitch fritzSwitch)
            : this(options)
        {
            FritzSwitch = fritzSwitch;
        }

        public SensorWidget(SensorWidgetOptions options, SwitchBotPollerSwitch switchBotSwitch)
            : this(options)
        {
            SwitchBotSwitch = switchBotSwitch;
        }

        public SensorWidget(SensorWidgetOptions options, ClewarePollerSwitch clewareUSBSwitch)
            : this(options)
        {
            ClewareUSBSwitch = clewareUSBSwitch;
        }

        public SensorWidget(SensorWidgetOptions options, WatcherPollerFile wachterFile, bool isLocalWriteFile = true)
            : this(options)
        {
            if (isLocalWriteFile)
            {
                LocalWriteFile = wachterFile;
            }
            else
            {
                RemoteReadFile = wachterFile;
            }
        }

        private static string? GetTags(bool hasShutDown, bool canBeArmed, bool heldBack, bool saved, bool isArmed)
        {
            List<string> lstTags = [];

            if (heldBack)
            {

                lstTags.Add($"[bold {AnubisOptions.Options.defaultComposition_Tags.compositionHeldBack.textColor} on {AnubisOptions.Options.defaultComposition_Tags.compositionHeldBack.backgroundColor}] held back [/]");
            }
            if (SharedData.CurrentControllerStatus == ControllerStatus.Holdback)
            {
                if (canBeArmed)
                {
                    lstTags.Add($"[bold {AnubisOptions.Options.defaultComposition_Tags.compositionArmable.textColor} on {AnubisOptions.Options.defaultComposition_Tags.compositionArmable.backgroundColor}] armable [/]");
                }
            }
            if (SharedData.CurrentControllerStatus >= ControllerStatus.Armed)
            {
                if (isArmed)
                {
                    lstTags.Add($"[bold {AnubisOptions.Options.defaultComposition_Tags.compositionArmed.textColor} on {AnubisOptions.Options.defaultComposition_Tags.compositionArmed.backgroundColor}] armed [/]");
                }
            }
            if (hasShutDown)
            {
                lstTags.Add($"[bold {AnubisOptions.Options.defaultComposition_Tags.compositionShutdown.textColor} on {AnubisOptions.Options.defaultComposition_Tags.compositionShutdown.backgroundColor}] shutdown [/]");
            }
            if (saved)
            {
                lstTags.Add($"[bold {AnubisOptions.Options.defaultComposition_Tags.compositionSaved.textColor} on {AnubisOptions.Options.defaultComposition_Tags.compositionSaved.backgroundColor}] saved [/]");
            }
            if (lstTags.Count > 0)
            {
                return string.Join(' ', lstTags);
            }
            else
            {
                return null;
            }
        }

        public void ResetMinMaxRegisteredPower()
        {
            MinPowerRegistered = null;
            MaxPowerRegistered = null;
            Pollers?.ResetValues();
        }

        public IRenderable? GetWidget()
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("SensorWidget.GetWidget"))
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

                if (FritzSwitch != null)
                {
                    #region Header

                    tblMain.AddRow(new Rule($"[{AnubisOptions.Options.defaultColor_Info}]Fritz switch: [bold]{FritzSwitch.Options.SwitchName}[/][/]")
                    {
                        Justification = Justify.Left,
                        Border = BoxBorder.Square,
                        Style = new Style(AnubisOptions.Options.defaultColor_Info),
                    });

                    #endregion

                    #region State, Presence, Panic

                    #region State
                    string strState = FritzSwitch.CurrentState.ToString();
                    Color clrState = AnubisOptions.Options.defaultColor_State_Info;
                    switch (FritzSwitch.CurrentState)
                    {
                        case ANUBISFritzAPI.SwitchState.On:
                            clrState = AnubisOptions.Options.defaultColor_State_Ok;
                            break;
                        case ANUBISFritzAPI.SwitchState.Error:
                        case ANUBISFritzAPI.SwitchState.NameNotFound:
                            clrState = AnubisOptions.Options.defaultColor_State_Error;
                            break;
                        case ANUBISFritzAPI.SwitchState.Unknown:
                        case ANUBISFritzAPI.SwitchState.Off:
                            clrState = AnubisOptions.Options.defaultColor_State_Warning;
                            break;
                    }
                    string strMarkupState = new Style(clrState).ToMarkup();
                    string strOutState = $"[{AnubisOptions.Options.defaultColor_Info}]State: [/][{strMarkupState}]{strState}[/]";
                    #endregion

                    #region Presence
                    string strPresent = FritzSwitch.CurrentPresence.ToString();
                    Color clrPresent = AnubisOptions.Options.defaultColor_State_Info;
                    switch (FritzSwitch.CurrentPresence)
                    {
                        case ANUBISFritzAPI.SwitchPresence.Present:
                            clrPresent = AnubisOptions.Options.defaultColor_State_Ok;
                            break;
                        case ANUBISFritzAPI.SwitchPresence.Error:
                        case ANUBISFritzAPI.SwitchPresence.NameNotFound:
                            clrPresent = AnubisOptions.Options.defaultColor_State_Error;
                            break;
                        case ANUBISFritzAPI.SwitchPresence.Missing:
                            clrPresent = AnubisOptions.Options.defaultColor_State_Warning;
                            break;
                    }
                    string strMarkupPresence = new Style(clrPresent).ToMarkup();
                    string strOutPresence = $"[{AnubisOptions.Options.defaultColor_Info}]Presence: [/][{strMarkupPresence}]{strPresent}[/]";
                    #endregion

                    #region Panic
                    string strPanicReason = $"[{AnubisOptions.Options.defaultColor_Ok}]NONE[/]";
                    if (FritzSwitch.Panic != FritzPanicReason.NoPanic)
                    {
                        strPanicReason = $"[bold {AnubisOptions.Options.defaultComposition_Failure.textColor} on {AnubisOptions.Options.defaultComposition_Failure.backgroundColor}] {FritzSwitch.Panic} [/]";
                    }
                    string strOutPanic = $"[{AnubisOptions.Options.defaultColor_Info}]Panic reason: [/]{strPanicReason}";
                    #endregion

                    string strOutMarkup = $"{strOutState}; {strOutPresence}; {strOutPanic}";

                    tblMain.AddRow(new Markup(strOutMarkup));

                    #endregion

                    #region Power
                    if (FritzSwitch.CurrentPower.HasValue)
                    {
                        if (!MaxPowerRegistered.HasValue || FritzSwitch.CurrentPower.Value > MaxPowerRegistered)
                        {
                            MaxPowerRegistered = FritzSwitch.CurrentPower.Value;
                        }
                        if (!MinPowerRegistered.HasValue || FritzSwitch.CurrentPower.Value < MinPowerRegistered)
                        {
                            MinPowerRegistered = FritzSwitch.CurrentPower.Value;
                        }
                        double maxVal = MaxPowerRegistered.Value * 1.1;
                        if (maxVal < 1000)
                        {
                            maxVal = 1000;
                        }

                        var bcValues =
                            new BarChart()
                            {
                                Culture = CultureInfo.CurrentCulture,
                                ShowValues = true,
                                MaxValue = maxVal,
                            };

                        Color clrPower = AnubisOptions.Options.defaultColor_State_Ok;
                        double? warnMin = null;
                        double? warnMax = null;
                        double? errMin = null;
                        double? errMax = null;
                        if (FritzSwitch.Options.MinPower.HasValue)
                        {
                            errMin = FritzSwitch.Options.MinPower.Value;
                        }
                        if (FritzSwitch.Options.MaxPower.HasValue)
                        {
                            errMax = FritzSwitch.Options.MaxPower.Value;
                        }
                        if (FritzSwitch.Options.MinPowerWarn.HasValue)
                        {
                            warnMin = FritzSwitch.Options.MinPowerWarn;
                        }
                        else
                        {
                            if (errMin.HasValue)
                            {
                                if (errMax.HasValue)
                                {
                                    double diff = errMax.Value - errMin.Value;
                                    double offset = diff * 0.2;
                                    warnMin = Math.Ceiling(errMin.Value + offset);
                                }
                                else
                                {
                                    warnMin = errMin.Value * 1.2;
                                }
                            }
                        }
                        if (FritzSwitch.Options.MaxPowerWarn.HasValue)
                        {
                            warnMax = FritzSwitch.Options.MaxPowerWarn;
                        }
                        else
                        {
                            if (errMax.HasValue)
                            {
                                if (errMin.HasValue)
                                {
                                    double diff = errMax.Value - errMin.Value;
                                    double offset = diff * 0.2;
                                    warnMax = Math.Floor(errMax.Value - offset);
                                }
                                else
                                {
                                    warnMax = errMax.Value * 0.8;
                                }
                            }
                        }

                        if (errMin.HasValue && FritzSwitch.CurrentPower.Value <= errMin.Value)
                        {
                            clrPower = AnubisOptions.Options.defaultColor_Error;
                        }
                        else if (warnMin.HasValue && FritzSwitch.CurrentPower.Value <= warnMin.Value)
                        {
                            clrPower = AnubisOptions.Options.defaultColor_Warning;
                        }
                        else if (errMax.HasValue && FritzSwitch.CurrentPower.Value >= errMax.Value)
                        {
                            clrPower = AnubisOptions.Options.defaultColor_Error;
                        }
                        else if (warnMax.HasValue && FritzSwitch.CurrentPower.Value >= warnMax.Value)
                        {
                            clrPower = AnubisOptions.Options.defaultColor_Warning;
                        }

                        bcValues.AddItem($"[{AnubisOptions.Options.defaultColor_Info}]Power (mW):[/]", FritzSwitch.CurrentPower.Value, clrPower);
                        tblMain.AddRow(bcValues);
                    }
                    #endregion

                    #region Min/Max Power

                    if (AnubisOptions.Options.showMinMaxPower)
                    {
                        if (MinPowerRegistered.HasValue || MaxPowerRegistered.HasValue)
                        {
                            tblMain.AddRow(new Markup($"[{AnubisOptions.Options.defaultColor_Info}]min/max power (mW): {MinPowerRegistered?.ToString() ?? "???"}/{MaxPowerRegistered?.ToString() ?? "???"}[/]"));
                        }
                    }

                    #endregion

                    #region Timestamps (update), Tags

                    #region LastUpdate Timestamp

                    strOutMarkup = $"[{AnubisOptions.Options.defaultColor_Info}]Last update: {UIHelpers.GetTimeSinceString(FritzSwitch.LastUpdateTimestamp) ?? "<none>"}[/]";
                    #endregion

                    #region Tags

                    string? strTags = GetTags(FritzSwitch.HasShutDown, FritzSwitch.CanBeArmed, FritzSwitch.HoldBack, FritzSwitch.InSafeMode, FritzSwitch.CheckForPanic);
                    if (!string.IsNullOrWhiteSpace(strTags))
                    {
                        strOutMarkup += $"; {strTags}";
                    }

                    #endregion

                    tblMain.AddRow(new Markup(strOutMarkup));

                    #endregion

                    #region LastAutoOff Timestamp
                    string? strLastAutoOff = UIHelpers.GetTimestampString(FritzSwitch.LastAutoPowerOff);
                    if (strLastAutoOff != null)
                    {
                        tblMain.AddRow(new Markup($"[{AnubisOptions.Options.defaultColor_Info}]Last auto-off: {strLastAutoOff}[/]"));
                    }
                    #endregion

                    return tblMain;
                }
                else if (SwitchBotSwitch != null)
                {
                    #region Header

                    tblMain.AddRow(new Rule($"[{AnubisOptions.Options.defaultColor_Info}]SwitchBot switch: [bold]{SwitchBotSwitch.Options.SwitchBotName}[/][/]")
                    {
                        Justification = Justify.Left,
                        Border = BoxBorder.Square,
                        Style = new Style(AnubisOptions.Options.defaultColor_Info),
                    });

                    #endregion

                    #region State, Panic

                    #region State
                    string strState = SwitchBotSwitch.CurrentState.ToString();
                    Color clrState = AnubisOptions.Options.defaultColor_Info;
                    switch (SwitchBotSwitch.CurrentState)
                    {
                        case ANUBISSwitchBotAPI.SwitchBotPowerState.On:
                            clrState = AnubisOptions.Options.defaultColor_State_Ok;
                            break;
                        case ANUBISSwitchBotAPI.SwitchBotPowerState.Off:
                            clrState = AnubisOptions.Options.defaultColor_State_Warning;
                            break;
                        case ANUBISSwitchBotAPI.SwitchBotPowerState.Unknown:
                            clrState = AnubisOptions.Options.defaultColor_State_Error;
                            break;
                    }
                    string strStateMarkup = new Style(clrState).ToMarkup();
                    string strOutState = $"[{AnubisOptions.Options.defaultColor_Info}]State: [/][{strStateMarkup}]{strState}[/]";
                    #endregion

                    #region Panic
                    string strPanicReason = $"[{AnubisOptions.Options.defaultColor_Ok}]NONE[/]";
                    if (SwitchBotSwitch.Panic != SwitchBotPanicReason.NoPanic)
                    {
                        strPanicReason = $"[bold {AnubisOptions.Options.defaultComposition_Failure.textColor} on {AnubisOptions.Options.defaultComposition_Failure.backgroundColor}] {SwitchBotSwitch.Panic} [/]";
                    }
                    string strOutPanic = $"[{AnubisOptions.Options.defaultColor_Info}]Panic reason: [/]{strPanicReason}";
                    #endregion

                    string strOutMarkup = $"{strOutState}; {strOutPanic}";

                    tblMain.AddRow(new Markup(strOutMarkup));

                    #endregion

                    #region Battery

                    if (SwitchBotSwitch.CurrentBattery.HasValue)
                    {
                        var bcValues =
                            new BarChart()
                            {
                                Culture = CultureInfo.CurrentCulture,
                                ShowValues = true,
                                MaxValue = 100,
                            };

                        Color clrBattery = AnubisOptions.Options.defaultColor_State_Ok;
                        if (SwitchBotSwitch.CurrentBattery.Value <= 5)
                        {
                            clrBattery = AnubisOptions.Options.defaultColor_Error;
                        }
                        else if (SwitchBotSwitch.CurrentBattery.Value <= 10)
                        {
                            clrBattery = AnubisOptions.Options.defaultColor_Warning;
                        }

                        bcValues.AddItem($"[{AnubisOptions.Options.defaultColor_Info}]Battery (%):[/]", SwitchBotSwitch.CurrentBattery.Value, clrBattery);
                        tblMain.AddRow(bcValues);
                    }

                    #endregion

                    #region Timestamps (update), Tags

                    #region LastUpdate Timestamp

                    strOutMarkup = $"[{AnubisOptions.Options.defaultColor_Info}]Last update: {UIHelpers.GetTimeSinceString(SwitchBotSwitch.LastUpdateTimestamp) ?? "<none>"}[/]";

                    #endregion

                    #region Tags

                    string? strTags = GetTags(SwitchBotSwitch.HasShutDown, SwitchBotSwitch.CanBeArmed, SwitchBotSwitch.HoldBack, SwitchBotSwitch.InSafeMode, SwitchBotSwitch.CheckForPanic);
                    if (!string.IsNullOrWhiteSpace(strTags))
                    {
                        strOutMarkup += $"; {strTags}";
                    }

                    #endregion

                    tblMain.AddRow(new Markup(strOutMarkup));

                    #endregion

                    #region LastAutoOff Timestamp

                    string? strLastAutoOff = UIHelpers.GetTimestampString(SwitchBotSwitch.LastAutoPowerOff);
                    if (strLastAutoOff != null)
                    {
                        tblMain.AddRow(new Markup($"[{AnubisOptions.Options.defaultColor_Info}]Last auto-off: {strLastAutoOff}[/]"));
                    }

                    #endregion

                    return tblMain;
                }
                else if (ClewareUSBSwitch != null)
                {
                    #region Header

                    tblMain.AddRow(new Rule($"[{AnubisOptions.Options.defaultColor_Info}]Cleware USB switch: [bold]{ClewareUSBSwitch.Options.USBSwitchName}[/][/]")
                    {
                        Justification = Justify.Left,
                        Border = BoxBorder.Square,
                        Style = new Style(AnubisOptions.Options.defaultColor_Info),
                    });

                    #endregion

                    #region State, Panic

                    #region State
                    string strState = ClewareUSBSwitch.CurrentState.ToString();
                    Color clrState = AnubisOptions.Options.defaultColor_Info;
                    switch (ClewareUSBSwitch.CurrentState)
                    {
                        case ANUBISClewareAPI.USBSwitchState.On:
                            clrState = AnubisOptions.Options.defaultColor_State_Ok;
                            break;
                        case ANUBISClewareAPI.USBSwitchState.Error:
                        case ANUBISClewareAPI.USBSwitchState.NameNotFound:
                        case ANUBISClewareAPI.USBSwitchState.SwitchNotFound:
                            clrState = AnubisOptions.Options.defaultColor_State_Error;
                            break;
                        case ANUBISClewareAPI.USBSwitchState.Unknown:
                        case ANUBISClewareAPI.USBSwitchState.Off:
                            clrState = AnubisOptions.Options.defaultColor_State_Warning;
                            break;
                    }
                    string strStateMarkup = new Style(clrState).ToMarkup();
                    string strOutState = $"[{AnubisOptions.Options.defaultColor_Info}]State: [/][{strStateMarkup}]{strState}[/]";
                    #endregion

                    #region Panic
                    string strPanicReason = $"[{AnubisOptions.Options.defaultColor_Ok}]NONE[/]";
                    if (ClewareUSBSwitch.Panic != ClewareUSBPanicReason.NoPanic)
                    {
                        strPanicReason = $"[bold {AnubisOptions.Options.defaultComposition_Failure.textColor} on {AnubisOptions.Options.defaultComposition_Failure.backgroundColor}] {ClewareUSBSwitch.Panic} [/]";
                    }
                    string strOutPanic = $"[{AnubisOptions.Options.defaultColor_Info}]Panic reason: [/]{strPanicReason}";
                    #endregion

                    string strOutMarkup = $"{strOutState}; {strOutPanic}";

                    tblMain.AddRow(new Markup(strOutMarkup));

                    #endregion

                    #region Timestamps (update), Tags

                    #region LastUpdate Timestamp

                    strOutMarkup = $"[{AnubisOptions.Options.defaultColor_Info}]Last update: {UIHelpers.GetTimeSinceString(ClewareUSBSwitch.LastUpdateTimestamp) ?? "<none>"}[/]";

                    #endregion

                    #region Tags

                    string? strTags = GetTags(ClewareUSBSwitch.HasShutDown, ClewareUSBSwitch.CanBeArmed, ClewareUSBSwitch.HoldBack, ClewareUSBSwitch.InSafeMode, ClewareUSBSwitch.CheckForPanic);
                    if (!string.IsNullOrWhiteSpace(strTags))
                    {
                        strOutMarkup += $"; {strTags}";
                    }

                    #endregion

                    tblMain.AddRow(new Markup(strOutMarkup));

                    #endregion

                    #region LastAutoOff Timestamp

                    string? strLastAutoOff = UIHelpers.GetTimestampString(ClewareUSBSwitch.LastAutoPowerOff);
                    if (strLastAutoOff != null)
                    {
                        tblMain.AddRow(new Markup($"[{AnubisOptions.Options.defaultColor_Info}]Last auto-off: {strLastAutoOff}[/]"));
                    }

                    #endregion

                    return tblMain;
                }
                else if (LocalWriteFile != null || RemoteReadFile != null)
                {
#pragma warning disable CS8600 // Das NULL-Literal oder ein möglicher NULL-Wert wird in einen Non-Nullable-Typ konvertiert.
                    WatcherPollerFile wpf = LocalWriteFile ?? RemoteReadFile;
#pragma warning restore CS8600 // Das NULL-Literal oder ein möglicher NULL-Wert wird in einen Non-Nullable-Typ konvertiert.

                    #region Header

                    string strTitleText = "Remote ANUBIS computer";
                    if (LocalWriteFile != null)
                    {
                        strTitleText = "Local ANUBIS status file";
                    }

#pragma warning disable CS8602 // Das NULL-Literal oder ein möglicher NULL-Wert wird in einen Non-Nullable-Typ konvertiert.
                    tblMain.AddRow(new Rule($"[{AnubisOptions.Options.defaultColor_Info}]{strTitleText}: [bold]{wpf.Options.WatcherFileName}[/][/]")
                    {
                        Justification = Justify.Left,
                        Border = BoxBorder.Square,
                        Style = new Style(AnubisOptions.Options.defaultColor_Info),
                    });
#pragma warning restore CS8602 // Das NULL-Literal oder ein möglicher NULL-Wert wird in einen Non-Nullable-Typ konvertiert.

                    #endregion

                    #region State

                    string strState = wpf.CurrentState.ToString();
                    Color clrState = AnubisOptions.Options.defaultColor_Info;
                    switch (wpf.CurrentState)
                    {
                        case WatcherFileState.Stopped:
                            clrState = AnubisOptions.Options.defaultColor_State_Info;
                            break;
                        case WatcherFileState.NoPanic:
                            clrState = AnubisOptions.Options.defaultColor_State_Ok;
                            break;
                        case WatcherFileState.NoResponse:
                        case WatcherFileState.Unreachable:
                        case WatcherFileState.Panic:
                            clrState = AnubisOptions.Options.defaultColor_State_Error;
                            break;
                        case WatcherFileState.Error:
                        case WatcherFileState.Unknown:
                            clrState = AnubisOptions.Options.defaultColor_State_Warning;
                            break;
                    }

                    string strStateMarkup = new Style(clrState).ToMarkup();
                    string strOutState = $"[{AnubisOptions.Options.defaultColor_Info}]State: [/][{strStateMarkup}]{strState}[/]";

                    if (RemoteReadFile != null)
                    {
                        string? strCurrentStateTS = UIHelpers.GetTimeSinceString(wpf.CorrectedStateTimestamp);
                        if (strCurrentStateTS != null)
                        {
                            strCurrentStateTS += " (corrected by " + wpf.OffsetRemote.ToString(@"hh\:mm\:ss") + ")";
                            strOutState += $" (timestamp: {strCurrentStateTS})";
                        }
                    }

                    tblMain.AddRow(new Markup($"{strOutState}"));

                    #endregion

                    #region Panic

                    string strPanicReason = $"[{AnubisOptions.Options.defaultColor_Ok}]NONE[/]";
                    if (wpf.Panic != WatcherFilePanicReason.NoPanic)
                    {
                        strPanicReason = $"[bold {AnubisOptions.Options.defaultComposition_Failure.textColor} on {AnubisOptions.Options.defaultComposition_Failure.backgroundColor}] {wpf.Panic} [/]";
                    }
                    string strOutPanic = $"[{AnubisOptions.Options.defaultColor_Info}]Panic reason: [/]{strPanicReason}";

                    tblMain.AddRow(new Markup($"{strOutPanic}"));

                    #endregion

                    #region Timestamps (last update), Tags

                    #region LastUpdate Timestamp

                    string strOutMarkup = $"[{AnubisOptions.Options.defaultColor_Info}]Last update: {UIHelpers.GetTimeSinceString(wpf.LastUpdateTimestamp) ?? "<none>"}[/]";

                    #endregion

                    #region Tags

                    string? strTags = GetTags(false, wpf.CanBeArmed, wpf.HoldBack, wpf.InSafeMode, wpf.DoCheckAny);
                    if (!string.IsNullOrWhiteSpace(strTags))
                    {
                        strOutMarkup += $"; {strTags}";
                    }

                    #endregion

                    tblMain.AddRow(new Markup(strOutMarkup));

                    #endregion

                    return tblMain;
                }
                else if (Pollers != null)
                {
                    Pollers.UpdateValues();

                    #region Header

                    tblMain.AddRow(new Rule($"[{AnubisOptions.Options.defaultColor_Info}]Pollers and refresh rate[/]")
                    {
                        Justification = Justify.Left,
                        Border = BoxBorder.Square,
                        Style = new Style(AnubisOptions.Options.defaultColor_Info),
                    });

                    #endregion

                    #region Pollers and refresh rate

                    string strMarkup = "";

                    if (Pollers.Controller != null)
                    {
                        strMarkup += Pollers.Controller_Details.GetMarkup("Controller") + "; ";
                    }

                    if (Pollers.Poller_Countdown != null)
                    {
                        strMarkup += Pollers.Poller_Countdown_Details.GetMarkup("Countdown") + "; ";
                    }

                    if (Pollers.Poller_Fritz != null)
                    {
                        strMarkup += Pollers.Poller_Fritz_Details.GetMarkup("Fritz") + "; ";
                    }

                    if (Pollers.Poller_SwitchBot != null)
                    {
                        strMarkup += Pollers.Poller_SwitchBot_Details.GetMarkup("SwitchBot") + "; ";
                    }

                    if (Pollers.Poller_ClewareUSB != null)
                    {
                        strMarkup += Pollers.Poller_ClewareUSB_Details.GetMarkup("Usb") + "; ";
                    }

                    if (Pollers.Poller_RemoteReadFiles != null)
                    {
                        strMarkup += Pollers.Poller_RemoteReadFiles_Details.GetMarkup("RemoteFiles") + "; ";
                    }

                    if (Pollers.Poller_LocalWriteFiles != null)
                    {
                        strMarkup += Pollers.Poller_LocalWriteFiles_Details.GetMarkup("LocalFiles") + "; ";
                    }

                    if (Pollers.DisplayRefreshMilliseconds.HasValue)
                    {
                        strMarkup += $"Refresh: [{AnubisOptions.Options.defaultColor_Info}]{Pollers.DisplayRefreshMilliseconds.Value,4}[/]";
                        if (Pollers.DisplayRefreshMilliseconds_Min.HasValue && Pollers.DisplayRefreshMilliseconds_Max.HasValue)
                        {
                            strMarkup += $" (min/max=[{AnubisOptions.Options.defaultColor_Info}]{Pollers.DisplayRefreshMilliseconds_Min.Value}[/]/" +
                                         $"[{AnubisOptions.Options.defaultColor_Info}]{Pollers.DisplayRefreshMilliseconds_Max.Value}[/])";

                        }
                    }

                    tblMain.AddRow(new Markup(strMarkup));

                    #endregion

                    return tblMain;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
