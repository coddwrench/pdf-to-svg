using System.Drawing;
using System.Drawing.Drawing2D;

namespace Svg
{
    /// <summary>
    /// Represents and SVG ellipse element.
    /// </summary>
    [SvgElement("ellipse")]
    public class SvgEllipse : SvgVisualElement
    {
        private SvgUnit _radiusX;
        private SvgUnit _radiusY;
        private SvgUnit _centerX;
        private SvgUnit _centerY;
        private GraphicsPath _path;

        [SvgAttribute("cx")]
        public virtual SvgUnit CenterX
        {
            get { return _centerX; }
            set
            {
            	if(_centerX != value)
            	{
            		_centerX = value;
            		IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "cx", Value = value });
            	}
            }
        }

        [SvgAttribute("cy")]
        public virtual SvgUnit CenterY
        {
        	get { return _centerY; }
        	set
        	{
        		if(_centerY != value)
        		{
        			_centerY = value;
        			IsPathDirty = true;
        			OnAttributeChanged(new AttributeEventArgs{ Attribute = "cy", Value = value });
        		}
        	}
        }

        [SvgAttribute("rx")]
        public virtual SvgUnit RadiusX
        {
        	get { return _radiusX; }
        	set
        	{
        		if(_radiusX != value)
        		{
        			_radiusX = value;
        			IsPathDirty = true;
        			OnAttributeChanged(new AttributeEventArgs{ Attribute = "rx", Value = value });
        		}
        	}
        }

        [SvgAttribute("ry")]
        public virtual SvgUnit RadiusY
        {
        	get { return _radiusY; }
        	set
        	{
        		if(_radiusY != value)
        		{
        			_radiusY = value;
        			IsPathDirty = true;
        			OnAttributeChanged(new AttributeEventArgs{ Attribute = "ry", Value = value });
        		}
        	}
        }

        /// <summary>
        /// Gets the bounds of the element.
        /// </summary>
        /// <value>The bounds.</value>
        public override RectangleF Bounds
        {
            get { return Path(null).GetBounds(); }
        }

        /// <summary>
        /// Gets the <see cref="GraphicsPath"/> for this element.
        /// </summary>
        /// <value></value>
        public override GraphicsPath Path(ISvgRenderer renderer)
        {
            if (_path == null || IsPathDirty)
            {
							float halfStrokeWidth = base.StrokeWidth / 2;

							// If it is to render, don't need to consider stroke width.
							// i.e stroke width only to be considered when calculating boundary
							if (renderer != null)
							{
								halfStrokeWidth = 0;
								IsPathDirty = false;
							}

                var center = SvgUnit.GetDevicePoint(_centerX, _centerY, renderer, this);
								var radius = SvgUnit.GetDevicePoint(_radiusX + halfStrokeWidth, _radiusY + halfStrokeWidth, renderer, this);

                _path = new GraphicsPath();
                _path.StartFigure();
                _path.AddEllipse(center.X - radius.X, center.Y - radius.Y, 2 * radius.X, 2 * radius.Y);
                _path.CloseFigure();
            }
            return _path;
        }

        /// <summary>
        /// Renders the <see cref="SvgElement"/> and contents to the specified <see cref="Graphics"/> object.
        /// </summary>
        /// <param name="graphics">The <see cref="Graphics"/> object to render to.</param>
        protected override void Render(ISvgRenderer renderer)
        {
            if (_radiusX.Value > 0.0f && _radiusY.Value > 0.0f)
            {
                base.Render(renderer);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgEllipse"/> class.
        /// </summary>
        public SvgEllipse()
        {
        }



		public override SvgElement DeepCopy()
		{
			return DeepCopy<SvgEllipse>();
		}

		public override SvgElement DeepCopy<T>()
		{
			var newObj = base.DeepCopy<T>() as SvgEllipse;
			newObj.CenterX = CenterX;
			newObj.CenterY = CenterY;
			newObj.RadiusX = RadiusX;
			newObj.RadiusY = RadiusY;
			return newObj;
		}
    }
}