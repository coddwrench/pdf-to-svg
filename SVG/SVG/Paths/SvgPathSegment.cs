using System.Drawing;
using System.Drawing.Drawing2D;

namespace Svg.Pathing
{
    public abstract class SvgPathSegment
    {
        private PointF _start;
        private PointF _end;

        public PointF Start
        {
            get { return _start; }
            set { _start = value; }
        }

        public PointF End
        {
            get { return _end; }
            set { _end = value; }
        }

        protected SvgPathSegment()
        {
        }

        protected SvgPathSegment(PointF start, PointF end)
        {
            Start = start;
            End = end;
        }

        public abstract void AddToPath(GraphicsPath graphicsPath);

		public SvgPathSegment Clone()
		{
			return MemberwiseClone() as SvgPathSegment;
		}
    }
}
