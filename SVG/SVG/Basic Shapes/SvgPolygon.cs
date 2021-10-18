using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using Svg.ExtensionMethods;

namespace Svg
{
    /// <summary>
    /// SvgPolygon defines a closed shape consisting of a set of connected straight line segments.
    /// </summary>
    [SvgElement("polygon")]
    public class SvgPolygon : SvgVisualElement
    {
        private GraphicsPath _path;
        
        /// <summary>
        /// The points that make up the SvgPolygon
        /// </summary>
        [SvgAttribute("points")]
        public SvgPointCollection Points
        {
            get { return Attributes["points"] as SvgPointCollection; }
            set { Attributes["points"] = value; IsPathDirty = true; }
        }

        /// <summary>
        /// Gets or sets the marker (end cap) of the path.
        /// </summary>
        [SvgAttribute("marker-end")]
        public virtual Uri MarkerEnd
        {
            get { return Attributes.GetAttribute<Uri>("marker-end").ReplaceWithNullIfNone(); }
            set { Attributes["marker-end"] = value; }
        }


        /// <summary>
        /// Gets or sets the marker (start cap) of the path.
        /// </summary>
        [SvgAttribute("marker-mid")]
        public virtual Uri MarkerMid
        {
            get { return Attributes.GetAttribute<Uri>("marker-mid").ReplaceWithNullIfNone(); }
            set { Attributes["marker-mid"] = value; }
        }


        /// <summary>
        /// Gets or sets the marker (start cap) of the path.
        /// </summary>
        [SvgAttribute("marker-start")]
        public virtual Uri MarkerStart
        {
            get { return Attributes.GetAttribute<Uri>("marker-start").ReplaceWithNullIfNone(); }
            set { Attributes["marker-start"] = value; }
        }

        public override GraphicsPath Path(ISvgRenderer renderer)
        {
            if (_path == null || IsPathDirty)
            {
                _path = new GraphicsPath();
                _path.StartFigure();

                try
                {
                    var points = Points;
                    for (int i = 2; (i + 1) < points.Count; i += 2)
                    {
                        var endPoint = SvgUnit.GetDevicePoint(points[i], points[i + 1], renderer, this);

                      // If it is to render, don't need to consider stroke width.
                        // i.e stroke width only to be considered when calculating boundary
                        if (renderer == null)
                        {
                          var radius = base.StrokeWidth / 2;
                          _path.AddEllipse(endPoint.X - radius, endPoint.Y - radius, 2 * radius, 2 * radius);
                          continue;
                        }

                        //first line
                        if (_path.PointCount == 0)
                        {
                            _path.AddLine(SvgUnit.GetDevicePoint(points[i - 2], points[i - 1], renderer, this), endPoint);
                        }
                        else
                        {
                            _path.AddLine(_path.GetLastPoint(), endPoint);
                        }
                    }
                }
                catch
                {
                    Trace.TraceError("Error parsing points");
                }

                _path.CloseFigure();
                if (renderer != null)
                  IsPathDirty = false;
            }
            return _path;
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

        public override RectangleF Bounds
        {
            get { return Path(null).GetBounds(); }
        }


		public override SvgElement DeepCopy()
		{
			return DeepCopy<SvgPolygon>();
		}

		public override SvgElement DeepCopy<T>()
		{
			var newObj = base.DeepCopy<T>() as SvgPolygon;
			newObj.Points = new SvgPointCollection();
			foreach (var pt in Points)
				newObj.Points.Add(pt);
			return newObj;
		}
    }
}