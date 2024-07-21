using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Extensions;
using Spectre.Console.Rendering;

namespace ANUBISConsole.UI
{
    public class CountdownWidgetOptions
    {
        public string? Title { get; set; }
        public int? Width { get; init; }
        public int Height { get; init; } = 1;
        public int ShortlyBeforeT0InMinutes { get; init; } = 0;
        public bool CenterAlign { get; init; }
        public Color BackgroundColor_BeforeT0 { get; init; }
        public Color? BackgroundColor_ShortlyBeforeT0 { get; init; }
        public Color BackgroundColor_FromT0 { get; init; }
        public Color TextColor_BeforeT0 { get; init; }
        public Color? TextColor_ShortlyBeforeT0 { get; init; }
        public Color TextColor_FromT0 { get; init; }
    }

    public class CountdownWidget
    {
        private CountdownWidgetOptions Options { get; init; }

        private const int _minWidth = 29;
        private readonly int _width = _minWidth;
        private int _topPadding = 0;
        private int _bottomPadding = 0;

        public CountdownWidget(CountdownWidgetOptions options)
        {
            Options = options;
            _width = options.Width ?? _minWidth;
            if (_width < _minWidth)
            {
                _width = _minWidth;
            }
        }

        public void ChangeTitle(string title)
        {
            Options.Title = title;
        }

        public IRenderable? GetWidget(TimeSpan? countdown, DateTime? localT0, DateTime? utcT0)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("CountdownWidget.GetWidget"))
            {
                int height = 0;
                int cntLines = 0;
                string? strText_Title = null;
                string? strText_Countdown = null;
                string? strText_LocalT0 = null;
                string? strText_UtcT0 = null;
                string? strFullLine_Title = null;
                string? strFullLine_Countdown = null;
                string? strFullLine_LocalT0 = null;
                string? strFullLine_UtcT0 = null;
                int maxLength = 0;

                if (Options.Title != null && (countdown.HasValue || localT0.HasValue || utcT0.HasValue))
                {
                    strText_Title = Options.Title;
                    if (strText_Title.Length > maxLength)
                        maxLength = strText_Title.Length;
                    cntLines++;
                }

                if (countdown.HasValue)
                {
                    string prefix = "T-";
                    if (countdown.Value.TotalNanoseconds >= 0)
                    {
                        prefix = "T+";
                    }
                    strText_Countdown = prefix + countdown.Value.ToString(@"hh\:mm\:ss");
                    if (strText_Countdown.Length > maxLength)
                        maxLength = strText_Countdown.Length;
                    cntLines++;
                }

                if (localT0.HasValue)
                {
                    strText_LocalT0 = localT0.Value.ToString("dd.MM.yyyy HH:mm:ss") + " (local)";
                    if (strText_LocalT0.Length > maxLength)
                        maxLength = strText_LocalT0.Length;
                    cntLines++;
                }

                if (utcT0.HasValue)
                {
                    strText_UtcT0 = utcT0.Value.ToString("yyyy-MM-dd HH:mm:ss") + " (UTC)";
                    if (strText_UtcT0.Length > maxLength)
                        maxLength = strText_UtcT0.Length;
                    cntLines++;
                }

                if (maxLength < _width)
                {
                    maxLength = _width;
                }

                height = cntLines;
                if (height < Options.Height)
                {
                    height = Options.Height;
                }

                if (height > 0)
                {
                    int missingHeight = height - cntLines;
                    _bottomPadding = missingHeight / 2;
                    _topPadding = missingHeight - _bottomPadding;
                }

                List<string> lstContent = [];

                if (strText_Title != null)
                {
                    int missingWidth = maxLength - strText_Title.Length;
                    int halfMissingWidth = missingWidth / 2;
                    int leftPadding = halfMissingWidth;
                    int rightPadding = missingWidth - halfMissingWidth;
                    strFullLine_Title = strText_Title.PadLeft(strText_Title.Length + leftPadding);
                    strFullLine_Title = strFullLine_Title.PadRight(strFullLine_Title.Length + rightPadding);
                    lstContent.Add(strFullLine_Title);
                }

                if (strText_Countdown != null)
                {
                    int missingWidth = maxLength - strText_Countdown.Length;
                    int halfMissingWidth = missingWidth / 2;
                    int leftPadding = halfMissingWidth;
                    int rightPadding = missingWidth - halfMissingWidth;
                    strFullLine_Countdown = strText_Countdown.PadLeft(strText_Countdown.Length + leftPadding);
                    strFullLine_Countdown = strFullLine_Countdown.PadRight(strFullLine_Countdown.Length + rightPadding);
                    lstContent.Add(strFullLine_Countdown);
                }

                if (strText_LocalT0 != null)
                {
                    int missingWidth = maxLength - strText_LocalT0.Length;
                    int halfMissingWidth = missingWidth / 2;
                    int leftPadding = halfMissingWidth;
                    int rightPadding = missingWidth - halfMissingWidth;
                    strFullLine_LocalT0 = strText_LocalT0.PadLeft(strText_LocalT0.Length + leftPadding);
                    strFullLine_LocalT0 = strFullLine_LocalT0.PadRight(strFullLine_LocalT0.Length + rightPadding);
                    lstContent.Add(strFullLine_LocalT0);
                }

                if (strText_UtcT0 != null)
                {
                    int missingWidth = maxLength - strText_UtcT0.Length;
                    int halfMissingWidth = missingWidth / 2;
                    int leftPadding = halfMissingWidth;
                    int rightPadding = missingWidth - halfMissingWidth;
                    strFullLine_UtcT0 = strText_UtcT0.PadLeft(strText_UtcT0.Length + leftPadding);
                    strFullLine_UtcT0 = strFullLine_UtcT0.PadRight(strFullLine_UtcT0.Length + rightPadding);
                    lstContent.Add(strFullLine_UtcT0);
                }

                string strFullLine = string.Join("\r\n", lstContent);
                string strContent = strFullLine;

                if (_topPadding > 0)
                {
                    string strEmptyLine = new(' ', maxLength);
                    string strTopPadding = string.Join("\r\n", Enumerable.Repeat(strEmptyLine, _topPadding));

                    strContent = strTopPadding + "\r\n" + strContent;
                }

                if (_bottomPadding > 0)
                {
                    string strEmptyLine = new(' ', maxLength);
                    string strBottomPadding = string.Join("\r\n", Enumerable.Repeat(strEmptyLine, _topPadding));

                    strContent = strContent + "\r\n" + strBottomPadding;
                }

                Color clrBackground = Options.BackgroundColor_BeforeT0;
                Color clrText = Options.TextColor_BeforeT0;

                if (countdown.HasValue)
                {
                    if (countdown.Value.TotalNanoseconds >= 0)
                    {
                        clrBackground = Options.BackgroundColor_FromT0;
                        clrText = Options.TextColor_FromT0;
                    }
                    else if (Options.ShortlyBeforeT0InMinutes > 0 &&
                            Options.BackgroundColor_ShortlyBeforeT0.HasValue &&
                            Options.TextColor_ShortlyBeforeT0.HasValue)
                    {

                        if (Math.Abs(countdown.Value.TotalMinutes) < (Options.ShortlyBeforeT0InMinutes))
                        {
                            clrBackground = Options.BackgroundColor_ShortlyBeforeT0.Value;
                            clrText = Options.TextColor_ShortlyBeforeT0.Value;
                        }
                    }
                }

                IRenderable algText = new Markup(strContent,
                                                    new Style(clrText,
                                                                clrBackground,
                                                                Decoration.None)
                                                ).Overflow(Overflow.Crop);

                if (Options.CenterAlign)
                {
                    algText = Align.Center(algText).MiddleAligned();
                }

                return algText;
            }
        }
    }
}
