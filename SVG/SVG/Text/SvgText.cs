namespace Svg
{
    /// <summary>
    /// The <see cref="SvgText"/> element defines a graphics element consisting of text.
    /// </summary>
    [SvgElement("text")]
    public class SvgText : SvgTextBase
    {
        /// <summary>
        /// Initializes the <see cref="SvgText"/> class.
        /// </summary>
        public SvgText() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgText"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        public SvgText(string text)
            : this()
        {
            Text = text;
        }

        public override SvgElement DeepCopy()
        {
            return DeepCopy<SvgText>();
        }

        public override SvgElement DeepCopy<T>()
        {
            var newObj = base.DeepCopy<T>() as SvgText;
            newObj.TextAnchor = TextAnchor;
            newObj.WordSpacing = WordSpacing;
            newObj.LetterSpacing = LetterSpacing;
            newObj.Font = Font;
            newObj.FontFamily = FontFamily;
            newObj.FontSize = FontSize;
            newObj.FontWeight = FontWeight;
            newObj.X = X;
            newObj.Y = Y;
            return newObj;
        }
    }
}