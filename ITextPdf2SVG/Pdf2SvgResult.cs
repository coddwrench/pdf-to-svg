using System.Drawing;
using IText.Kernel.Geom;
using Svg;

namespace ITextPdf2SVG
{
	public class Pdf2SvgResult
	{
		public SizeF Size { get; set; }
		public int Orientation { get; set; }
		public PageSize PageSize { get; set; }
		public SvgDocument Canvas { get; set; }
	}
}