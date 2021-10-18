using System.Linq;
using Svg.Pathing;
using System.Drawing.Drawing2D;

namespace Svg
{
    [SvgElement("glyph")]
    public class SvgGlyph : SvgVisualElement
    {
        private GraphicsPath _path;

        /// <summary>
        /// Gets or sets a <see cref="SvgPathSegmentList"/> of path data.
        /// </summary>
        [SvgAttribute("d", true)]
        public SvgPathSegmentList PathData
        {
            get { return Attributes.GetAttribute<SvgPathSegmentList>("d"); }
            set { Attributes["d"] = value; }
        }

        [SvgAttribute("glyph-name", true)]
        public virtual string GlyphName
        {
            get { return Attributes["glyph-name"] as string; }
            set { Attributes["glyph-name"] = value; }
        }
        [SvgAttribute("horiz-adv-x", true)]
        public float HorizAdvX
        {
            get { return (Attributes["horiz-adv-x"] == null ? Parents.OfType<SvgFont>().First().HorizAdvX : (float)Attributes["horiz-adv-x"]); }
            set { Attributes["horiz-adv-x"] = value; }
        }
        [SvgAttribute("unicode", true)]
        public string Unicode
        {
            get { return Attributes["unicode"] as string; }
            set { Attributes["unicode"] = value; }
        }
        [SvgAttribute("vert-adv-y", true)]
        public float VertAdvY
        {
            get { return (Attributes["vert-adv-y"] == null ? Parents.OfType<SvgFont>().First().VertAdvY : (float)Attributes["vert-adv-y"]); }
            set { Attributes["vert-adv-y"] = value; }
        }
        [SvgAttribute("vert-origin-x", true)]
        public float VertOriginX
        {
            get { return (Attributes["vert-origin-x"] == null ? Parents.OfType<SvgFont>().First().VertOriginX : (float)Attributes["vert-origin-x"]); }
            set { Attributes["vert-origin-x"] = value; }
        }
        [SvgAttribute("vert-origin-y", true)]
        public float VertOriginY
        {
            get { return (Attributes["vert-origin-y"] == null ? Parents.OfType<SvgFont>().First().VertOriginY : (float)Attributes["vert-origin-y"]); }
            set { Attributes["vert-origin-y"] = value; }
        }


        /// <summary>
        /// Gets the <see cref="GraphicsPath"/> for this element.
        /// </summary>
        public override GraphicsPath Path(ISvgRenderer renderer)
        {
            if (_path == null || IsPathDirty)
            {
                _path = new GraphicsPath();

                foreach (SvgPathSegment segment in PathData)
                {
                    segment.AddToPath(_path);
                }

                IsPathDirty = false;
            }
            return _path;
        }

        /// <summary>
        /// Gets the bounds of the element.
        /// </summary>
        /// <value>The bounds.</value>
        public override System.Drawing.RectangleF Bounds
        {
            get { return Path(null).GetBounds(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgGlyph"/> class.
        /// </summary>
        public SvgGlyph()
        {
            var pathData = new SvgPathSegmentList();
            Attributes["d"] = pathData;
        }

        public override SvgElement DeepCopy()
        {
            return DeepCopy<SvgGlyph>();
        }

        public override SvgElement DeepCopy<T>()
        {
            var newObj = base.DeepCopy<T>() as SvgGlyph;
            foreach (var pathData in PathData)
                newObj.PathData.Add(pathData.Clone());
            return newObj;

        }
    }
}
