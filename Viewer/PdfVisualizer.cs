using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml.Linq;
using IText.Kernel.Geom;
using IText.Kernel.Pdf;
using RSB.ITextPDF.Pdf2Svg;

namespace Viewer2
{
	public static class PdfVisualizer
	{
		private static string PageSizeTpClass(PageSize ps)
		{
			return typeof(PageSize).GetFields().FirstOrDefault(_ => _.DeclaringType == typeof(PageSize) && ps.Equals(_.GetValue(null)))
				?.Name.ToLower() ?? "unknown";
		}

		public static TagBuilder Process(byte[] pdf)
		{
			var result = new TagBuilder("div");
			result.AddCssClass("pdf-content");
			var resultInnerHtml = new StringBuilder();
			using (var ms = new MemoryStream(pdf))
			using (var r = new PdfReader(ms))
			using (var d = new PdfDocument(r))
			{
				foreach (var item in new PdfToSvg().Process(d))
				{
					var svg = item.Canvas;
					var page = new TagBuilder("div");
					page.AddCssClass("page");
					page.AddCssClass(PageSizeTpClass(item.PageSize));
					page.AddCssClass(item.Size.Height > item.Size.Width ? "h" : "v");

					var pageInnerHtml = new StringBuilder();
					var canvasWrapper = new TagBuilder("div");
					canvasWrapper.AddCssClass("canvas-wrapper");
					using (var stream = new MemoryStream())
					{
						svg.Write(stream);
						stream.Position = 0;
						using (var reader = new StreamReader(stream))
							canvasWrapper.InnerHtml = XElement.Load(reader).ToString();
					}

					pageInnerHtml.Append(canvasWrapper);
					page.InnerHtml = pageInnerHtml.ToString();
					resultInnerHtml.Append(page);
				}
			}
			result.InnerHtml = resultInnerHtml.ToString();
			return result;
		}
	}
}
