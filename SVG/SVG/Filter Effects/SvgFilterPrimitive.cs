namespace Svg.FilterEffects
{
    public abstract class SvgFilterPrimitive : SvgElement
    {
        public const string SourceGraphic = "SourceGraphic";
        public const string SourceAlpha = "SourceAlpha";
        public const string BackgroundImage = "BackgroundImage";
        public const string BackgroundAlpha = "BackgroundAlpha";
        public const string FillPaint = "FillPaint";
        public const string StrokePaint = "StrokePaint";

        [SvgAttribute("in")]
        public string Input
        {
            get { return Attributes.GetAttribute<string>("in"); }
            set { Attributes["in"] = value; }
        }

        [SvgAttribute("result")]
        public string Result
        {
            get { return Attributes.GetAttribute<string>("result"); }
            set { Attributes["result"] = value; }
        }

        protected SvgFilter Owner
        {
            get { return (SvgFilter)Parent; }
        }

        public abstract void Process(ImageBuffer buffer);
    }
}