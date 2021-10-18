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
using System.Collections.Generic;
using System.Globalization;
using IText.IO.Font.Otf;
using IText.IO.Util;

namespace IText.IO.Font
{
	public class CidFont : FontProgram
	{
		private string fontName;

		private int pdfFontFlags;

		private ICollection<string> compatibleCmaps;

		internal CidFont(string fontName, ICollection<string> cmaps)
		{
			this.fontName = fontName;
			compatibleCmaps = cmaps;
			fontNames = new FontNames();
			InitializeCidFontNameAndStyle(fontName);
			var fontDesc = CidFontProperties.GetAllFonts().Get(fontNames.GetFontName());
			if (fontDesc == null)
			{
				throw new IOException("There is no such predefined font: {0}").SetMessageParams(fontName);
			}
			InitializeCidFontProperties(fontDesc);
		}

		internal CidFont(string fontName, ICollection<string> cmaps, IDictionary<string, object> fontDescription)
		{
			InitializeCidFontNameAndStyle(fontName);
			InitializeCidFontProperties(fontDescription);
			compatibleCmaps = cmaps;
		}

		public virtual bool CompatibleWith(string cmap)
		{
			if (cmap.Equals(PdfEncodings.IDENTITY_H) || cmap.Equals(PdfEncodings.IDENTITY_V))
			{
				return true;
			}

			return compatibleCmaps != null && compatibleCmaps.Contains(cmap);
		}

		public override int GetKerning(Glyph glyph1, Glyph glyph2)
		{
			return 0;
		}

		public override int GetPdfFontFlags()
		{
			return pdfFontFlags;
		}

		public override bool IsFontSpecific()
		{
			return false;
		}

		public override bool IsBuiltWith(string fontName)
		{
			return Equals(this.fontName, fontName);
		}

		private void InitializeCidFontNameAndStyle(string fontName)
		{
			var nameBase = TrimFontStyle(fontName);
			if (nameBase.Length < fontName.Length)
			{
				fontNames.SetFontName(fontName);
				fontNames.SetStyle(fontName.Substring(nameBase.Length));
			}
			else
			{
				fontNames.SetFontName(fontName);
			}
			fontNames.SetFullName(new[] { new[] { "", "", "", fontNames.GetFontName() } });
		}

		private void InitializeCidFontProperties(IDictionary<string, object> fontDesc)
		{
			fontIdentification.SetPanose((string)fontDesc.Get("Panose"));
			fontMetrics.SetItalicAngle(Convert.ToInt32((string)fontDesc.Get("ItalicAngle"), CultureInfo.InvariantCulture
				));
			fontMetrics.SetCapHeight(Convert.ToInt32((string)fontDesc.Get("CapHeight"), CultureInfo.InvariantCulture
				));
			fontMetrics.SetTypoAscender(Convert.ToInt32((string)fontDesc.Get("Ascent"), CultureInfo.InvariantCulture
				));
			fontMetrics.SetTypoDescender(Convert.ToInt32((string)fontDesc.Get("Descent"), CultureInfo.InvariantCulture
				));
			fontMetrics.SetStemV(Convert.ToInt32((string)fontDesc.Get("StemV"), CultureInfo.InvariantCulture
				));
			pdfFontFlags = Convert.ToInt32((string)fontDesc.Get("Flags"), CultureInfo.InvariantCulture
				);
			var fontBBox = (string)fontDesc.Get("FontBBox");
			var tk = new StringTokenizer(fontBBox, " []\r\n\t\f");
			var llx = Convert.ToInt32(tk.NextToken(), CultureInfo.InvariantCulture);
			var lly = Convert.ToInt32(tk.NextToken(), CultureInfo.InvariantCulture);
			var urx = Convert.ToInt32(tk.NextToken(), CultureInfo.InvariantCulture);
			var ury = Convert.ToInt32(tk.NextToken(), CultureInfo.InvariantCulture);
			fontMetrics.UpdateBbox(llx, lly, urx, ury);
			registry = (string)fontDesc.Get("Registry");
			var uniMap = GetCompatibleUniMap(registry);
			if (uniMap != null)
			{
				var metrics = (IntHashtable)fontDesc.Get("W");
				var cid2Uni = FontCache.GetCid2UniCmap(uniMap);
				avgWidth = 0;
				foreach (int cid in cid2Uni.GetCids())
				{
					int uni = cid2Uni.Lookup(cid);
					var width = metrics.ContainsKey(cid) ? metrics.Get(cid) : DEFAULT_WIDTH;
					var glyph = new Glyph(cid, width, uni);
					avgWidth += glyph.GetWidth();
					ÑodeToGlyph.Put(cid, glyph);
					UnicodeToGlyph.Put(uni, glyph);
				}
				FixSpaceIssue();
				if (ÑodeToGlyph.Count != 0)
				{
					avgWidth /= ÑodeToGlyph.Count;
				}
			}
		}

		private static string GetCompatibleUniMap(string registry)
		{
			var uniMap = "";
			foreach (var name in CidFontProperties.GetRegistryNames().Get(registry + "_Uni"))
			{
				uniMap = name;
				if (name.EndsWith("H"))
				{
					break;
				}
			}
			return uniMap;
		}
	}
}
