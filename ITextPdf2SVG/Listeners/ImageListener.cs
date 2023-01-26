using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using IText.Kernel.Geom;
using IText.Kernel.Pdf;
using IText.Kernel.Pdf.Canvas.Parser;
using IText.Kernel.Pdf.Canvas.Parser.Data;
using IText.Kernel.Pdf.Canvas.Parser.Listener;
using IText.Kernel.Pdf.Xobject;
using Svg;
using Svg.Transforms;
using Rectangle = System.Drawing.Rectangle;

namespace ITextPdf2SVG.Listeners
{

	public class ImageListener : FilteredEventListener
	{
		private readonly SvgDocument _svg;
		private SizeF _pageSize;

		public ImageListener(SvgDocument svg, SizeF pageSize)
		{
			_svg = svg;
			_pageSize = pageSize;
		}

		public override void EventOccurred(IEventData data, EventType type)
		{
			if (type != EventType.RENDER_IMAGE)
				return;

			var renderInfo = (ImageRenderInfo) data;
			var imageObject = renderInfo.GetImage();

			var imageBytes = imageObject.GetImageBytes();
			var smask = imageObject.GetPdfObject().GetAsStream(PdfName.SMask);
	
			using (var bms = new MemoryStream(imageBytes))
			using (var image = new Bitmap(bms))
			{
				Bitmap output;
				if (smask != null)
				{
					var maskImageObject = new PdfImageXObject(smask);
					var maskBytes = maskImageObject.GetImageBytes();
					using (var maskImage = new Bitmap(new MemoryStream(maskBytes)))
					{
						output = GenerateMaskedImage(image, maskImage);
					}
				}
				else
				{
					output = image;
				}

				var ctm = renderInfo.GetImageCtm();

				using (var ms = new MemoryStream())
				{
					output.Save(ms, ImageFormat.Png);
					var base64 = Convert.ToBase64String(ms.GetBuffer());
					var height = ctm.Get(Matrix.I22);
					_svg.Children.Add(new SvgImage
					{
						Href = $"data:image/png;base64,{base64}",
						Width = ctm.Get(Matrix.I11),
						Height = height,
						Transforms = new SvgTransformCollection
						{
							new SvgTranslate(ctm.Get(Matrix.I31), _pageSize.Height - ctm.Get(Matrix.I32) - height)
						}
					});
				}
			}

			base.EventOccurred(data, type);
		}

		private Bitmap GenerateMaskedImage(Bitmap image, Bitmap mask)
		{
			var output = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
			var rect = new Rectangle(0, 0, image.Width, image.Height);
			var bitsMask = mask.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			var bitsInput = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			var bitsOutput = output.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				for (var y = 0; y < image.Height; y++)
				{
					var ptrMask = (byte*)bitsMask.Scan0 + y * bitsMask.Stride;
					var ptrInput = (byte*)bitsInput.Scan0 + y * bitsInput.Stride;
					var ptrOutput = (byte*)bitsOutput.Scan0 + y * bitsOutput.Stride;
					
					for (var x = 0; x < image.Width; x++)
					{
						ptrOutput[4 * x] = ptrInput[4 * x];
						ptrOutput[4 * x + 1] = ptrInput[4 * x + 1];
						ptrOutput[4 * x + 2] = ptrInput[4 * x + 2];
						ptrOutput[4 * x + 3] = ptrMask[4 * x];
					}
				}
			}

			mask.UnlockBits(bitsMask);
			image.UnlockBits(bitsInput);
			output.UnlockBits(bitsOutput);

			return output;
		}
	}
}