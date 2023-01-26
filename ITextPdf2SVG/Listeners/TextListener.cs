using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using IText.IO.Font;
using IText.Kernel.Geom;
using IText.Kernel.Pdf.Canvas.Parser;
using IText.Kernel.Pdf.Canvas.Parser.Data;
using IText.Kernel.Pdf.Canvas.Parser.Listener;
using Svg;
using Svg.Transforms;

namespace ITextPdf2SVG.Listeners
{
    public class TextListener : FilteredEventListener
    {
        private readonly SvgDocument _svg;
        private SizeF _pageSize;

        private static readonly Regex _fontRegex = new Regex(@"(?<=\+)[a-zA-Z\s]+");
        private static readonly Regex _fontStyleRegex = new Regex(@"[-,][\w\s]+$");

        private bool _textBegin = false;

        public TextListener(SvgDocument svg, SizeF pageSize)
        {
            _svg = svg;
            _pageSize = pageSize;
        }

        private static SvgFontStyle GetFontStyle(FontNames fontNames)
        {
            var fontName = fontNames.GetFontName();
            var fontStyleRegex = _fontStyleRegex.Match(fontName);
            var r = SvgFontStyle.Normal;
            if (fontStyleRegex.Success)
            {
                var result = fontStyleRegex.Value.ToLower();
                if (result.Contains("oblique"))
                    r = SvgFontStyle.Oblique;
                if (result.Contains("italic"))
                    r = SvgFontStyle.Italic;
            }

            return r;
        }

        private static SvgFontWeight GetFontWeight(FontNames fontNames)
        {
            var fontName = fontNames.GetFontName();
            var fontStyleRegex = _fontStyleRegex.Match(fontName);
            var r = SvgFontWeight.Normal;
            if (fontStyleRegex.Success)
            {
                var result = fontStyleRegex.Value.ToLower();
                if (result.Contains("bold"))
                    r = SvgFontWeight.Bold;
                else if (result.Contains("bolder"))
                    r = SvgFontWeight.Bolder;
                else if (result.Contains("lighter"))
                    r = SvgFontWeight.Lighter;
            }

            return r;
        }

        public string FontFamilyResolver(string font)
        {
            if (font.Contains("ArialMT"))
                return "Arial";
            if (font.Contains("Roboto"))
                return "Roboto";
            return font;
        }

        public override void EventOccurred(IEventData data, EventType type)
        {
            if (!_textBegin && type.Equals(EventType.BEGIN_TEXT))
            {
                _textBegin = true;
                return;
            }
            if (_textBegin && type.Equals(EventType.END_TEXT))
            {
                _textBegin = false;
                return;
            }

            if (!_textBegin || !type.Equals(EventType.RENDER_TEXT))
                return;

            var renderInfo = (TextRenderInfo) data;
            var baseFont = renderInfo.Font;

            var fontProgram = baseFont.GetFontProgram();
            var originalFontName = fontProgram.ToString();
            var fontRegex = _fontRegex.Match(originalFontName);
            var fontName = fontRegex.Success ? fontRegex.Value : originalFontName;

            var fontNames = fontProgram.GetFontNames();
            var bottomLeftText = renderInfo.GetBaseline().GetStartPoint();
            var tm = renderInfo.GetTextMatrix();
            var fontSize = new Vector(0, renderInfo.GetFontSize(), 0).Cross(tm).Length();
            // renderInfo.GetCharacterRenderInfos();
            var fillColor = renderInfo.GetFillColor();

            var color = fillColor.ParseColor() ?? Color.Black;

            var y = _pageSize.Height - bottomLeftText.Get(Vector.I2);
            var sb = new StringBuilder();

            float? endXPosition = null;
            float? startXPosition = null;

            foreach (var textRenderInfo in renderInfo.CharacterRenderInfos)
            {
                var letter = textRenderInfo.Text;
                var x = textRenderInfo.DescentLine
                    .GetStartPoint()
                    .Get(Vector.I1);

                if (!startXPosition.HasValue)
                    startXPosition = x;

                if (string.IsNullOrWhiteSpace(letter) ||
                    endXPosition != null && (endXPosition - x > renderInfo.GetCharSpacing() / 2f))
                {
                    if (!string.IsNullOrWhiteSpace(letter))
                        sb.Append(letter);

                    if (sb.Length > 0)
                    {
                        _svg.Children.Add(new SvgText(sb.ToString())
                        {
                            FontFamily = FontFamilyResolver(fontName),
                            Transforms = new SvgTransformCollection {new SvgTranslate(startXPosition.Value, y)},
                            FontSize = new SvgUnit(fontSize),
                            Fill = new SvgColourServer(color),
                            FontWeight = GetFontWeight(fontNames),
                            FontStyle = GetFontStyle(fontNames)
                        });
                    }
                    endXPosition = null;
                    startXPosition = null;
                    sb = new StringBuilder();
                    continue;
                }

                sb.Append(letter);
                endXPosition = textRenderInfo.DescentLine.GetEndPoint().Get(Vector.I1);
            }

            if (sb.Length > 0 && startXPosition != null)
            {
                _svg.Children.Add(new SvgText(sb.ToString())
                {
                    FontFamily = FontFamilyResolver(fontName),
                    Transforms = new SvgTransformCollection {new SvgTranslate(startXPosition.Value, y)},
                    FontSize = new SvgUnit(fontSize),
                    Fill = new SvgColourServer(color),
                    FontWeight = GetFontWeight(fontNames),
                    FontStyle = GetFontStyle(fontNames)
                });
            }

            base.EventOccurred(data, type);
        }
    }
}