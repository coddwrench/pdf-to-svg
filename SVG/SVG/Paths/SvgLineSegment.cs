using System.Drawing;

namespace Svg.Pathing
{
    public sealed class SvgLineSegment : SvgPathSegment
    {
        public SvgLineSegment(PointF start, PointF end)
        {
            Start = start;
            End = end;
        }

        public override void AddToPath(System.Drawing.Drawing2D.GraphicsPath graphicsPath)
        {
            graphicsPath.AddLine(Start, End);
        }
        
        public override string ToString()
		{
        	return "L" + End.ToSvgString();
		}

    }
}