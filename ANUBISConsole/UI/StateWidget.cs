using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Extensions;
using Spectre.Console.Rendering;

namespace ANUBISConsole.UI
{
    public class WidgetStateInfo
    {
        public string? Display { get; init; }
        public Color BackgroundColor { get; init; }
        public Color TextColor { get; init; }
    }

    public class StateWidgetOptions
    {
        public int? Width { get; init; }
        public int Height { get; init; } = 1;
        public bool CenterAlign { get; init; }
    }

    public enum BooleanState
    {
        Unknown,
        False,
        True,
    }

    public class StateWidget<T> where T : Enum
    {
        private Dictionary<T, WidgetStateInfo> Mappings { get; init; }
        private StateWidgetOptions Options { get; init; }

        public T? CurrentState { get; set; }

        private readonly int _maxWidth = 0;
        private readonly int _topPadding = 0;
        private readonly int _bottomPadding = 0;

        public StateWidget(StateWidgetOptions options, Dictionary<T, WidgetStateInfo> mappings)
        {
            Options = options;
            Mappings = mappings;
            _maxWidth = options.Width ?? 0;
            var lstValues = mappings.Select(itm => itm.Value.Display ?? itm.Key.ToString()).ToList();
            var maxLength = lstValues.Max(itm => itm.Length) + 2 /* border */;

            if (maxLength > _maxWidth)
            {
                _maxWidth = maxLength;
            }

            if (Options.Height > 0)
            {
                int missingHeight = Options.Height - 1;
                _bottomPadding = missingHeight / 2;
                _topPadding = missingHeight - _bottomPadding;
            }
        }

        public bool HasChange(T? state)
        {
            if (state != null)
            {
                if (!state.Equals(CurrentState))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public IRenderable? GetWidget(T? state)
        {
            ILogger? logging = SharedData.InterfaceLogging;
            using (logging?.BeginScope("StateWidget.GetWidget"))
            {
                try
                {
                    if (state != null)
                    {
                        if (Mappings.TryGetValue(state, out WidgetStateInfo? value))
                        {
                            WidgetStateInfo wsi = value;
                            string strText = wsi.Display ?? state.ToString();
                            int missingWidth = _maxWidth - strText.Length;
                            int halfMissingWidth = missingWidth / 2;
                            int leftPadding = halfMissingWidth;
                            int rightPadding = missingWidth - halfMissingWidth;
                            string strFullLine = strText.PadLeft(strText.Length + leftPadding);
                            strFullLine = strFullLine.PadRight(strFullLine.Length + rightPadding);
                            string strContent = strFullLine;

                            if (_topPadding > 0)
                            {
                                string strEmptyLine = new(' ', strFullLine.Length);
                                string strTopPadding = string.Join("\r\n", Enumerable.Repeat(strEmptyLine, _topPadding));

                                strContent = strTopPadding + "\r\n" + strContent;
                            }

                            if (_bottomPadding > 0)
                            {
                                string strEmptyLine = new(' ', strFullLine.Length);
                                string strBottomPadding = string.Join("\r\n", Enumerable.Repeat(strEmptyLine, _topPadding));

                                strContent = strContent + "\r\n" + strBottomPadding;
                            }

                            IRenderable algText = new Markup(strContent,
                                                                new Style(wsi.TextColor,
                                                                            wsi.BackgroundColor,
                                                                            Decoration.Bold)
                                                            ).Overflow(Overflow.Crop);

                            if (Options.CenterAlign)
                            {
                                algText = Align.Center(algText).MiddleAligned();
                            }

                            return algText;
                        }
                        else
                        {
                            logging?.LogError("No mapping found for state {state} in state widget", state);
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                finally
                {
                    CurrentState = state;
                }
            }
        }
    }
}
