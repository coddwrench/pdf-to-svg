using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Svg.ExtensionMethods;

namespace Svg
{
    /// <summary>
    /// Represents and SVG line element.
    /// </summary>
    [SvgElement("line")]
    public class SvgLine : SvgVisualElement
    {
        private SvgUnit _startX;
        private SvgUnit _startY;
        private SvgUnit _endX;
        private SvgUnit _endY;
        private GraphicsPath _path;

        [SvgAttribute("x1")]
        public SvgUnit StartX
        {
            get { return _startX; }
            set 
            { 
            	if(_startX != value)
            	{
            		_startX = value;
            		IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "x1", Value = value });
            	}
            }
        }

        [SvgAttribute("y1")]
        public SvgUnit StartY
        {
            get { return _startY; }
            set 
            { 
            	if(_startY != value)
            	{
            		_startY = value;
            		IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "y1", Value = value });
            	}
            }
        }

        [SvgAttribute("x2")]
        public SvgUnit EndX
        {
            get { return _endX; }
            set 
            { 
            	if(_endX != value)
            	{
            		_endX = value;
            		IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "x2", Value = value });
            	}
            }
        }

        [SvgAttribute("y2")]
        public SvgUnit EndY
        {
            get { return _endY; }
            set 
            { 
            	if(_endY != value)
            	{
            		_endY = value;
            		IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "y2", Value = value });
            	}
            }
        }

        /// <summary>
        /// Gets or sets the marker (end cap) of the path.
        /// </summary>
        [SvgAttribute("marker-end")]
        public Uri MarkerEnd
        {
            get { return Attributes.GetAttribute<Uri>("marker-end").ReplaceWithNullIfNone(); }
            set { Attributes["marker-end"] = value; }
        }


        /// <summary>
        /// Gets or sets the marker (start cap) of the path.
        /// </summary>
        [SvgAttribute("marker-mid")]
        public Uri MarkerMid
        {
            get { return Attributes.GetAttribute<Uri>("marker-mid").ReplaceWithNullIfNone(); }
            set { Attributes["marker-mid"] = value; }
        }


        /// <summary>
        /// Gets or sets the marker (start cap) of the path.
        /// </summary>
        [SvgAttribute("marker-start")]
        public Uri MarkerStart
        {
	        get { return Attributes.GetAttribute<Uri>("marker-start").ReplaceWithNullIfNone(); }
            set { Attributes["marker-start"] = value; }
        }

        public override SvgPaintServer Fill
        {
            get { return null; /* Line can't have a fill */ }
            set
            {
                // Do nothing
            }
        }

        public SvgLine()
        {
        }

        public override GraphicsPath Path(ISvgRenderer renderer)
        {
            if ((_path == null || IsPathDirty) && base.StrokeWidth > 0)
            {
                PointF start = new PointF(StartX.ToDeviceValue(renderer, UnitRenderingType.Horizontal, this), 
                                          StartY.ToDeviceValue(renderer, UnitRenderingType.Vertical, this));
                PointF end = new PointF(EndX.ToDeviceValue(renderer, UnitRenderingType.Horizontal, this), 
                                        EndY.ToDeviceValue(renderer, UnitRenderingType.Vertical, this));

                _path = new GraphicsPath();

                // If it is to render, don't need to consider stroke width.
                // i.e stroke width only to be considered when calculating boundary
                if (renderer != null)
                {
                  _path.AddLine(start, end);
                  IsPathDirty = false;
                }
                else
                {	 // only when calculating boundary 
                  _path.StartFigure();
                  var radius = base.StrokeWidth / 2;
                  _path.AddEllipse(start.X - radius, start.Y - radius, 2 * radius, 2 * radius);
                  _path.AddEllipse(end.X - radius, end.Y - radius, 2 * radius, 2 * radius);
                  _path.CloseFigure();
                }
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
			return DeepCopy<SvgLine>();
		}

		public override SvgElement DeepCopy<T>()
		{
			var newObj = base.DeepCopy<T>() as SvgLine;
			newObj.StartX = StartX;
			newObj.EndX = EndX;
			newObj.StartY = StartY;
			newObj.EndY = EndY;
			if (Fill != null)
				newObj.Fill = Fill.DeepCopy() as SvgPaintServer;

			return newObj;
		}

    }
}
