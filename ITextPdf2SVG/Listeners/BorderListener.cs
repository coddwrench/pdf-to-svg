using System.Drawing;
using IText.Kernel.Geom;
using IText.Kernel.Pdf.Canvas.Parser;
using IText.Kernel.Pdf.Canvas.Parser.Data;
using IText.Kernel.Pdf.Canvas.Parser.Listener;
using Svg;
using Svg.Pathing;
using Point = IText.Kernel.Geom.Point;

namespace RSB.ITextPDF.Pdf2Svg
{
	public class BorderListener : FilteredEventListener
	{
		private readonly SvgDocument _svg;
		private SizeF _pageSize;

		public BorderListener(SvgDocument svg, SizeF pageSize)
		{
			_svg = svg;
			_pageSize = pageSize;
		}

		private PointF MapPoint(Point point, Matrix ctm)
		{
			var vector1 = new Vector((float)point.x, (float)point.y, 1);
			vector1 = vector1.Cross(ctm);
			return new PointF(vector1.Get(Vector.I1), _pageSize.Height - vector1.Get(Vector.I2));
		}

		public override void EventOccurred(IEventData data, EventType type)
		{
			if (type != EventType.RENDER_PATH)
				return;
			var renderInfo = (PathRenderInfo)data;
			var operation = renderInfo.GetOperation();
			if (operation == PathRenderInfo.NO_OP)
			{
				return;
			}
			var ctm = renderInfo.GetCtm();
			var path = renderInfo.GetPath();

			var svgPath = new SvgPath { SpaceHandling = XmlSpaceHandling.@default };

			if (operation == PathRenderInfo.FILL)
			{
				var color = renderInfo.GetFillColor().ParseColor();
				if (color != null)
				{
					svgPath.Fill = new SvgColourServer(color.Value);
					if (color.Value.A < 255)
						svgPath.FillOpacity = color.Value.A / 255f;
				}
				svgPath.FillRule = SvgFillRule.EvenOdd;
			}

			if (operation == PathRenderInfo.STROKE)
			{
				var color = renderInfo.GetStrokeColor().ParseColor();
				if (color != null)
				{
					svgPath.Stroke = new SvgColourServer(color.Value);
					if (color.Value.A < 255)
						svgPath.StrokeOpacity = color.Value.A / 255f;
				}

				svgPath.StrokeWidth = renderInfo.GetLineWidth();
				svgPath.Fill = SvgPaintServer.None;
			}

			var svgPathSegments = new SvgPathSegmentList();
			foreach (var subpath in path.GetSubpaths())
			{
				var startPoint = subpath.GetStartPoint();
				svgPathSegments.Add(new SvgMoveToSegment(MapPoint(startPoint, ctm)));

				foreach (var segment in subpath.GetSegments())
				{
					var points = segment.GetBasePoints();
					if (segment is Line line)
						svgPathSegments.Add(new SvgLineSegment(MapPoint(points[0], ctm), MapPoint(points[1], ctm)));
					else if (segment is BezierCurve curve)
						svgPathSegments.Add(new SvgCubicCurveSegment(MapPoint(points[0], ctm), MapPoint(points[1], ctm), MapPoint(points[2], ctm), MapPoint(points[3], ctm)));
				}
				if (subpath.IsClosed())
					svgPathSegments.Add(new SvgClosePathSegment());

			}
			svgPath.PathData = svgPathSegments;
			_svg.Children.Add(svgPath);
			base.EventOccurred(data, type);
		}
	}
}