using System;
using System.Drawing.Drawing2D;
using Svg.ExtensionMethods;
using Svg.Pathing;

namespace Svg
{
    /// <summary>
    /// Represents an SVG path element.
    /// </summary>
    [SvgElement("path")]
    public class SvgPath : SvgVisualElement
    {
        private GraphicsPath _path;

        /// <summary>
        /// Gets or sets a <see cref="SvgPathSegmentList"/> of path data.
        /// </summary>
        [SvgAttribute("d", true)]
        public SvgPathSegmentList PathData
        {
        	get { return Attributes.GetAttribute<SvgPathSegmentList>("d"); }
            set
            {
            	Attributes["d"] = value;
            	value._owner = this;
                IsPathDirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the length of the path.
        /// </summary>
        [SvgAttribute("pathLength", true)]
        public float PathLength
        {
            get { return Attributes.GetAttribute<float>("pathLength"); }
            set { Attributes["pathLength"] = value; }
        }

		
        /// <summary>
        /// Gets or sets the marker (end cap) of the path.
        /// </summary>
        [SvgAttribute("marker-end", true)]
		public Uri MarkerEnd
        {
			get { return Attributes.GetAttribute<Uri>("marker-end").ReplaceWithNullIfNone(); }
			set { Attributes["marker-end"] = value; }
		}


		/// <summary>
		/// Gets or sets the marker (start cap) of the path.
		/// </summary>
        [SvgAttribute("marker-mid", true)]
		public Uri MarkerMid
		{
			get { return Attributes.GetAttribute<Uri>("marker-mid").ReplaceWithNullIfNone(); }
			set { Attributes["marker-mid"] = value; }
		}


		/// <summary>
		/// Gets or sets the marker (start cap) of the path.
		/// </summary>
        [SvgAttribute("marker-start", true)]
		public Uri MarkerStart
		{
			get { return Attributes.GetAttribute<Uri>("marker-start").ReplaceWithNullIfNone(); }
			set { Attributes["marker-start"] = value; }
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

        internal void OnPathUpdated()
        {
            IsPathDirty = true;
            OnAttributeChanged(new AttributeEventArgs{ Attribute = "d", Value = Attributes.GetAttribute<SvgPathSegmentList>("d") });
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
        /// Initializes a new instance of the <see cref="SvgPath"/> class.
        /// </summary>
        public SvgPath()
        {
            var pathData = new SvgPathSegmentList();
            Attributes["d"] = pathData;
            pathData._owner = this;
        }

		/// <summary>
		/// Renders the stroke of the <see cref="SvgVisualElement"/> to the specified <see cref="ISvgRenderer"/>
		/// </summary>
		/// <param name="renderer">The <see cref="ISvgRenderer"/> object to render to.</param>
		protected internal override bool RenderStroke(ISvgRenderer renderer)
		{
            var result = base.RenderStroke(renderer);
            var path = Path(renderer);

            if (MarkerStart != null)
            {
                SvgMarker marker = OwnerDocument.GetElementById<SvgMarker>(MarkerStart.ToString());
                marker.RenderMarker(renderer, this, path.PathPoints[0], path.PathPoints[0], path.PathPoints[1]);
            }

            if (MarkerMid != null)
            {
                SvgMarker marker = OwnerDocument.GetElementById<SvgMarker>(MarkerMid.ToString());
                for (int i = 1; i <= path.PathPoints.Length - 2; i++)
                    marker.RenderMarker(renderer, this, path.PathPoints[i], path.PathPoints[i - 1], path.PathPoints[i], path.PathPoints[i + 1]);
            }

            if (MarkerEnd != null)
            {
                SvgMarker marker = OwnerDocument.GetElementById<SvgMarker>(MarkerEnd.ToString());
                marker.RenderMarker(renderer, this, path.PathPoints[path.PathPoints.Length - 1], path.PathPoints[path.PathPoints.Length - 2], path.PathPoints[path.PathPoints.Length - 1]);
            }
                
            return result;
		}

		public override SvgElement DeepCopy()
		{
			return DeepCopy<SvgPath>();
		}

		public override SvgElement DeepCopy<T>()
		{
			var newObj = base.DeepCopy<T>() as SvgPath;
			foreach (var pathData in PathData)
				newObj.PathData.Add(pathData.Clone());
			newObj.PathLength = PathLength;
			newObj.MarkerStart = MarkerStart;
			newObj.MarkerEnd = MarkerEnd;
			return newObj;

		}
    }
}