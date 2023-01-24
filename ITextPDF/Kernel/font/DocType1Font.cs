/*

This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/

using System;
using IText.IO;
using IText.IO.Font;
using IText.IO.Font.Cmap;
using IText.IO.Font.Otf;
using IText.Kernel.Pdf;
using IText.Logger;

namespace IText.Kernel.Font
{
	internal class DocType1Font : Type1Font, IDocFontProgram
	{
		private PdfStream _fontFile;

		private PdfName _fontFileName;

		private PdfName _subtype;

		private int _missingWidth;

		private DocType1Font(string fontName)
			: base(fontName)
		{
		}

		internal static Type1Font CreateFontProgram(PdfDictionary fontDictionary, FontEncoding fontEncoding, CMapToUnicode
			 toUnicode)
		{
			var baseFontName = fontDictionary.GetAsName(PdfName.BaseFont);
			string baseFont;
			if (baseFontName != null)
			{
				baseFont = baseFontName.GetValue();
			}
			else
			{
				baseFont = FontUtil.CreateRandomFontName();
			}
			if (!fontDictionary.ContainsKey(PdfName.FontDescriptor))
			{
				Type1Font type1StdFont;
				try
				{
					//if there are no font modifiers, cached font could be used,
					//otherwise a new instance should be created.
					type1StdFont = (Type1Font)FontProgramFactory.CreateFont(baseFont, true);
				}
				catch (Exception)
				{
					type1StdFont = null;
				}
				if (type1StdFont != null)
				{
					return type1StdFont;
				}
			}
			var fontProgram = new DocType1Font(baseFont);
			var fontDesc = fontDictionary.GetAsDictionary(PdfName.FontDescriptor);
			fontProgram._subtype = fontDesc != null ? fontDesc.GetAsName(PdfName.Subtype) : null;
			FillFontDescriptor(fontProgram, fontDesc);
			var firstCharNumber = fontDictionary.GetAsNumber(PdfName.FirstChar);
			var firstChar = firstCharNumber != null ? Math.Max(firstCharNumber.IntValue(), 0) : 0;
			var widths = FontUtil.ConvertSimpleWidthsArray(fontDictionary.GetAsArray(PdfName.Widths), firstChar, fontProgram
				.GetMissingWidth());
			fontProgram.avgWidth = 0;
			var glyphsWithWidths = 0;
			for (var i = 0; i < 256; i++)
			{
				var glyph = new Glyph(i, widths[i], fontEncoding.GetUnicode(i));
				fontProgram.СodeToGlyph.Put(i, glyph);
				if (glyph.HasValidUnicode())
				{
					//FontEncoding.codeToUnicode table has higher priority
					if (fontEncoding.ConvertToByte(glyph.GetUnicode()) == i)
					{
						fontProgram.UnicodeToGlyph.Put(glyph.GetUnicode(), glyph);
					}
				}
				else
				{
					if (toUnicode != null)
					{
						glyph.SetChars(toUnicode.Lookup(i));
					}
				}
				if (widths[i] > 0)
				{
					glyphsWithWidths++;
					fontProgram.avgWidth += widths[i];
				}
			}
			if (glyphsWithWidths != 0)
			{
				fontProgram.avgWidth /= glyphsWithWidths;
			}
			return fontProgram;
		}

		public virtual PdfStream GetFontFile()
		{
			return _fontFile;
		}

		public virtual PdfName GetFontFileName()
		{
			return _fontFileName;
		}

		public virtual PdfName GetSubtype()
		{
			return _subtype;
		}

		/// <summary>Returns false, because we cannot rely on an actual font subset and font name.</summary>
		/// <param name="fontName">a font name or path to a font program</param>
		/// <returns>return false.</returns>
		public override bool IsBuiltWith(string fontName)
		{
			return false;
		}

		public virtual int GetMissingWidth()
		{
			return _missingWidth;
		}

		internal static void FillFontDescriptor(DocType1Font font, PdfDictionary fontDesc)
		{
			if (fontDesc == null)
			{
				var logger = LogManager.GetLogger(typeof(FontUtil));
				logger.Warn(LogMessageConstant.FONT_DICTIONARY_WITH_NO_FONT_DESCRIPTOR);
				return;
			}
			var v = fontDesc.GetAsNumber(PdfName.Ascent);
			if (v != null)
			{
				font.SetTypoAscender(v.IntValue());
			}
			v = fontDesc.GetAsNumber(PdfName.Descent);
			if (v != null)
			{
				font.SetTypoDescender(v.IntValue());
			}
			v = fontDesc.GetAsNumber(PdfName.CapHeight);
			if (v != null)
			{
				font.SetCapHeight(v.IntValue());
			}
			v = fontDesc.GetAsNumber(PdfName.XHeight);
			if (v != null)
			{
				font.SetXHeight(v.IntValue());
			}
			v = fontDesc.GetAsNumber(PdfName.ItalicAngle);
			if (v != null)
			{
				font.SetItalicAngle(v.IntValue());
			}
			v = fontDesc.GetAsNumber(PdfName.StemV);
			if (v != null)
			{
				font.SetStemV(v.IntValue());
			}
			v = fontDesc.GetAsNumber(PdfName.StemH);
			if (v != null)
			{
				font.SetStemH(v.IntValue());
			}
			v = fontDesc.GetAsNumber(PdfName.FontWeight);
			if (v != null)
			{
				font.SetFontWeight(v.IntValue());
			}
			v = fontDesc.GetAsNumber(PdfName.MissingWidth);
			if (v != null)
			{
				font._missingWidth = v.IntValue();
			}
			var fontStretch = fontDesc.GetAsName(PdfName.FontStretch);
			if (fontStretch != null)
			{
				font.SetFontStretch(fontStretch.GetValue());
			}
			var bboxValue = fontDesc.GetAsArray(PdfName.FontBBox);
			if (bboxValue != null)
			{
				var bbox = new int[4];
				//llx
				bbox[0] = bboxValue.GetAsNumber(0).IntValue();
				//lly
				bbox[1] = bboxValue.GetAsNumber(1).IntValue();
				//urx
				bbox[2] = bboxValue.GetAsNumber(2).IntValue();
				//ury
				bbox[3] = bboxValue.GetAsNumber(3).IntValue();
				if (bbox[0] > bbox[2])
				{
					var t = bbox[0];
					bbox[0] = bbox[2];
					bbox[2] = t;
				}
				if (bbox[1] > bbox[3])
				{
					var t = bbox[1];
					bbox[1] = bbox[3];
					bbox[3] = t;
				}
				font.SetBbox(bbox);
				// If ascender or descender in font descriptor are zero, we still want to get more or less correct valuee for
				// text extraction, stamping etc. Thus we rely on font bbox in this case
				if (font.GetFontMetrics().GetTypoAscender() == 0 && font.GetFontMetrics().GetTypoDescender() == 0)
				{
					float maxAscent = Math.Max(bbox[3], font.GetFontMetrics().GetTypoAscender());
					float minDescent = Math.Min(bbox[1], font.GetFontMetrics().GetTypoDescender());
					font.SetTypoAscender((int)(maxAscent * 1000 / (maxAscent - minDescent)));
					font.SetTypoDescender((int)(minDescent * 1000 / (maxAscent - minDescent)));
				}
			}
			var fontFamily = fontDesc.GetAsString(PdfName.FontFamily);
			if (fontFamily != null)
			{
				font.SetFontFamily(fontFamily.GetValue());
			}
			var flagsValue = fontDesc.GetAsNumber(PdfName.Flags);
			if (flagsValue != null)
			{
				var flags = flagsValue.IntValue();
				if ((flags & 1) != 0)
				{
					font.SetFixedPitch(true);
				}
				if ((flags & 262144) != 0)
				{
					font.SetBold(true);
				}
			}
			PdfName[] fontFileNames = { PdfName.FontFile, PdfName.FontFile2, PdfName.FontFile3 };
			foreach (var fontFile in fontFileNames)
			{
				if (fontDesc.ContainsKey(fontFile))
				{
					font._fontFileName = fontFile;
					font._fontFile = fontDesc.GetAsStream(fontFile);
					break;
				}
			}
		}
	}
}
