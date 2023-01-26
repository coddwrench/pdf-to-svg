using System.Collections.Generic;
using System.Drawing;
using IText.Kernel.Pdf;
using IText.Kernel.Pdf.Canvas.Parser;
using IText.Kernel.Pdf.Canvas.Parser.Listener;
using ITextPdf2SVG.Listeners;
using Svg;
using Svg.Transforms;

namespace ITextPdf2SVG
{
	public class PdfToSvg
	{
		private Pdf2SvgResult ProcessPage(PdfPage page)
		{
			var rotation = page.GetRotation();
			var size = page.PageSize;

			var width = size.GetWidth();
			var height = size.GetHeight();

			var svg = new SvgDocument
			{
				Width = new SvgUnit(SvgUnitType.Percentage, 100),
				Height = new SvgUnit(SvgUnitType.Percentage, 100),
				ViewBox = new SvgViewBox(0, 0, width, height)
			};

			if (rotation != 0)
				svg.Transforms = new SvgTransformCollection { new SvgRotate(-rotation) };
			var pageSize = new SizeF(width, height);
			var listener = new FilteredEventListener();
			listener.AttachEventListener(new TextListener(svg, pageSize));
			listener.AttachEventListener(new ImageListener(svg, pageSize));
			listener.AttachEventListener(new BorderListener(svg, pageSize));
			listener.AttachEventListener(new TextBlockListener(svg, pageSize));

            
            var processor = new PdfCanvasProcessor(listener);
			processor.ProcessPageContent(page);

			return new Pdf2SvgResult
			{
				Canvas = svg,
				Orientation = 0,
				PageSize = page.GetDocument().GetDefaultPageSize(),
				Size = pageSize
			};
		}
		public IEnumerable<Pdf2SvgResult> Process(PdfDocument document)
		{
			var numberOfPages = document.GetNumberOfPages();
			for (var i = 1; i <= numberOfPages; i++)
			{
				var page = document.GetPage(i);
				yield return ProcessPage(page);
			}
		}
	}
}
