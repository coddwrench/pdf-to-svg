using System.Drawing;

namespace ITextPdf2SVG
{
	public static class ColorHelper
	{
		public static Color? ParseColor(this IText.Kernel.Colors.Color @this)
		{
			Color? color;
			var colors = @this.GetColorValue();
			if (colors.Length == 1)
				color = Color.FromArgb((int)(255 * (1 - colors[0])), Color.Black);
			else if (colors.Length == 3)
				color = Color.FromArgb((int)(255 * colors[0]), (int)(255 * colors[1]), (int)(255 * colors[2]));
			else if (colors.Length == 4)
				color = Color.FromArgb((int)(255 * colors[0]), (int)(255 * colors[1]), (int)(255 * colors[2]), (int)(255 * colors[3]));
			else
				color = null;
			return color;
		}
	}
}