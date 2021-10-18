using System.Drawing;
using System.Drawing.Drawing2D;

namespace Svg
{
    /// <summary>
    /// An SVG element to render circles to the document.
    /// </summary>
    [SvgElement("circle")]
    public class SvgCircle : SvgVisualElement
    {
        private GraphicsPath _path;
        
        private SvgUnit _radius;
        private SvgUnit _centerX;
        private SvgUnit _centerY;

        /// <summary>
        /// Gets the center point of the circle.
        /// </summary>
        /// <value>The center.</value>
        public SvgPoint Center
        {
            get { return new SvgPoint(CenterX, CenterY); }
        }

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

        [SvgAttribute("r")]
        public virtual SvgUnit Radius
        {
        	get { return _radius; }
        	set
        	{
        		if(_radius != value)
        		{
        			_radius = value;
        			IsPathDirty = true;
        			OnAttributeChanged(new AttributeEventArgs{ Attribute = "r", Value = value });
        		}
        	}
        }

        /// <summary>
        /// Gets the bounds of the circle.
        /// </summary>
        /// <value>The rectangular bounds of the circle.</value>
        public override RectangleF Bounds
        {
            get { return Path(null).GetBounds(); }
        }

        /// <summary>
        /// Gets the <see cref="GraphicsPath"/> representing this element.
        /// </summary>
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

                _path = new GraphicsPath();
                _path.StartFigure();
								var center = Center.ToDeviceValue(renderer, this);
								var radius = Radius.ToDeviceValue(renderer, UnitRenderingType.Other, this) + halfStrokeWidth;
								_path.AddEllipse(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
                _path.CloseFigure();
            }
            return _path;
        }

        /// <summary>
        /// Renders the circle to the specified <see cref="Graphics"/> object.
        /// </summary>
        /// <param name="graphics">The graphics object.</param>
        protected override void Render(ISvgRenderer renderer)
        {
            // Don't draw if there is no radius set
            if (Radius.Value > 0.0f)
            {
                base.Render(renderer);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgCircle"/> class.
        /// </summary>
        public SvgCircle()
        {
            CenterX = new SvgUnit(0.0f);
            CenterY = new SvgUnit(0.0f);
        }


		public override SvgElement DeepCopy()
		{
			return DeepCopy<SvgCircle>();
		}

		public override SvgElement DeepCopy<T>()
		{
			var newObj = base.DeepCopy<T>() as SvgCircle;
			newObj.CenterX = CenterX;
			newObj.CenterY = CenterY;
			newObj.Radius = Radius;
			return newObj;
		}
    }
}