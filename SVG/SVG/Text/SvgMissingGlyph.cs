namespace Svg
{
    [SvgElement("missing-glyph")]
    public class SvgMissingGlyph : SvgGlyph
    {
        [SvgAttribute("glyph-name")]
        public override string GlyphName
        {
            get { return Attributes["glyph-name"] as string ?? "__MISSING_GLYPH__"; }
            set { Attributes["glyph-name"] = value; }
        }
    }
}
