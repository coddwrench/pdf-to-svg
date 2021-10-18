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
using System.Diagnostics;
using System.Text;
using IText.IO;
using IText.IO.Font;
using IText.IO.Font.Cmap;
using IText.IO.Font.Otf;
using IText.IO.Source;
using IText.IO.Util;
using IText.Kernel.Pdf;
using IText.Logger;
using IOException = System.IO.IOException;

namespace IText.Kernel.Font
{
	public class PdfType0Font : PdfFont
	{
		/// <summary>The code length shall not be greater than 4.</summary>
		private const int MAX_CID_CODE_LENGTH = 4;

		private static readonly byte[] rotbits = { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

		/// <summary>CIDFont Type0 (Type1 outlines).</summary>
		protected internal const int CID_FONT_TYPE_0 = 0;

		/// <summary>CIDFont Type2 (TrueType outlines).</summary>
		protected internal const int CID_FONT_TYPE_2 = 2;

		protected internal bool vertical;

		protected internal CMapEncoding cmapEncoding;

		//longTag is actually ordered set of usedGlyphs, shall be renamed in 7.2
		protected internal ICollection<int> longTag;

		protected internal int cidFontType;

		protected internal char[] specificUnicodeDifferences;

		internal PdfType0Font(TrueTypeFont ttf, string cmap)
		{
			if (!PdfEncodings.IDENTITY_H.Equals(cmap) && !PdfEncodings.IDENTITY_V.Equals(cmap))
			{
				throw new PdfException(PdfException.OnlyIdentityCMapsSupportsWithTrueType);
			}
			if (!ttf.GetFontNames().AllowEmbedding())
			{
				throw new PdfException(PdfException.CannotBeEmbeddedDueToLicensingRestrictions).SetMessageParams(ttf.GetFontNames
					().GetFontName() + ttf.GetFontNames().GetStyle());
			}
			fontProgram = ttf;
			embedded = true;
			vertical = cmap.EndsWith("V");
			cmapEncoding = new CMapEncoding(cmap);
			longTag = new SortedSet<int>();
			cidFontType = CID_FONT_TYPE_2;
			if (ttf.IsFontSpecific())
			{
				specificUnicodeDifferences = new char[256];
				var bytes = new byte[1];
				for (var k = 0; k < 256; ++k)
				{
					bytes[0] = (byte)k;
					var s = PdfEncodings.ConvertToString(bytes, null);
					var ch = s.Length > 0 ? s[0] : '?';
					specificUnicodeDifferences[k] = ch;
				}
			}
		}

		// Note. Make this constructor protected. Only PdfFontFactory (kernel level) will
		// be able to create Type0 font based on predefined font.
		// Or not? Possible it will be convenient construct PdfType0Font based on custom CidFont.
		// There is no typography features in CJK fonts.
		internal PdfType0Font(CidFont font, string cmap)
		{
			if (!CidFontProperties.IsCidFont(font.GetFontNames().GetFontName(), cmap))
			{
				throw new PdfException("Font {0} with {1} encoding is not a cjk font.").SetMessageParams(font.GetFontNames
					().GetFontName(), cmap);
			}
			fontProgram = font;
			vertical = cmap.EndsWith("V");
			var uniMap = GetCompatibleUniMap(fontProgram.GetRegistry());
			cmapEncoding = new CMapEncoding(cmap, uniMap);
			longTag = new SortedSet<int>();
			cidFontType = CID_FONT_TYPE_0;
		}

		internal PdfType0Font(PdfDictionary fontDictionary)
			: base(fontDictionary)
		{
			newFont = false;
			var cidFont = fontDictionary.GetAsArray(PdfName.DescendantFonts).GetAsDictionary(0);
			var cmap = fontDictionary.Get(PdfName.Encoding);
			var toUnicode = fontDictionary.Get(PdfName.ToUnicode);
			var toUnicodeCMap = FontUtil.ProcessToUnicode(toUnicode);
			if (cmap.IsName() && (PdfEncodings.IDENTITY_H.Equals(((PdfName)cmap).GetValue()) || PdfEncodings.IDENTITY_V
				.Equals(((PdfName)cmap).GetValue())))
			{
				if (toUnicodeCMap == null)
				{
					var uniMap = GetUniMapFromOrdering(GetOrdering(cidFont));
					toUnicodeCMap = FontUtil.GetToUnicodeFromUniMap(uniMap);
					if (toUnicodeCMap == null)
					{
						toUnicodeCMap = FontUtil.GetToUnicodeFromUniMap(PdfEncodings.IDENTITY_H);
						var logger = LogManager.GetLogger(typeof(PdfType0Font));
						logger.Error(MessageFormatUtil.Format(LogMessageConstant.UNKNOWN_CMAP, uniMap));
					}
				}
				fontProgram = DocTrueTypeFont.CreateFontProgram(cidFont, toUnicodeCMap);
				cmapEncoding = CreateCMap(cmap, null);
				Debug.Assert(fontProgram is IDocFontProgram);
				embedded = ((IDocFontProgram)fontProgram).GetFontFile() != null;
			}
			else
			{
				var cidFontName = cidFont.GetAsName(PdfName.BaseFont).GetValue();
				var uniMap = GetUniMapFromOrdering(GetOrdering(cidFont));
				if (uniMap != null && uniMap.StartsWith("Uni") && CidFontProperties.IsCidFont(cidFontName, uniMap))
				{
					try
					{
						fontProgram = FontProgramFactory.CreateFont(cidFontName);
						cmapEncoding = CreateCMap(cmap, uniMap);
						embedded = false;
					}
					catch (IOException)
					{
						fontProgram = null;
						cmapEncoding = null;
					}
				}
				else
				{
					if (toUnicodeCMap == null)
					{
						toUnicodeCMap = FontUtil.GetToUnicodeFromUniMap(uniMap);
					}
					if (toUnicodeCMap != null)
					{
						fontProgram = DocTrueTypeFont.CreateFontProgram(cidFont, toUnicodeCMap);
						cmapEncoding = CreateCMap(cmap, uniMap);
					}
				}
				if (fontProgram == null)
				{
					throw new PdfException(MessageFormatUtil.Format(PdfException.CannotRecogniseDocumentFontWithEncoding, cidFontName
						, cmap));
				}
			}
			// DescendantFonts is a one-element array specifying the CIDFont dictionary that is the descendant of this Type 0 font.
			var cidFontDictionary = fontDictionary.GetAsArray(PdfName.DescendantFonts).GetAsDictionary(0);
			// Required according to the spec
			var subtype = cidFontDictionary.GetAsName(PdfName.Subtype);
			if (PdfName.CIDFontType0.Equals(subtype))
			{
				cidFontType = CID_FONT_TYPE_0;
			}
			else
			{
				if (PdfName.CIDFontType2.Equals(subtype))
				{
					cidFontType = CID_FONT_TYPE_2;
				}
				else
				{
					LogManager.GetLogger(GetType()).Error(LogMessageConstant.FAILED_TO_DETERMINE_CID_FONT_SUBTYPE);
				}
			}
			longTag = new SortedSet<int>();
			subset = false;
		}

		public static string GetUniMapFromOrdering(string ordering)
		{
			switch (ordering)
			{
				case "CNS1":
					{
						return "UniCNS-UTF16-H";
					}

				case "Japan1":
					{
						return "UniJIS-UTF16-H";
					}

				case "Korea1":
					{
						return "UniKS-UTF16-H";
					}

				case "GB1":
					{
						return "UniGB-UTF16-H";
					}

				case "Identity":
					{
						return "Identity-H";
					}

				default:
					{
						return null;
					}
			}
		}

		public override Glyph GetGlyph(int unicode)
		{
			// TODO handle unicode value with cmap and use only glyphByCode
			var glyph = GetFontProgram().GetGlyph(unicode);
			if (glyph == null && (glyph = notdefGlyphs.Get(unicode)) == null)
			{
				// Handle special layout characters like sfthyphen (00AD).
				// This glyphs will be skipped while converting to bytes
				var notdef = GetFontProgram().GetGlyphByCode(0);
				if (notdef != null)
				{
					glyph = new Glyph(notdef, unicode);
				}
				else
				{
					glyph = new Glyph(-1, 0, unicode);
				}
				notdefGlyphs.Put(unicode, glyph);
			}
			return glyph;
		}

		public override bool ContainsGlyph(int unicode)
		{
			if (cidFontType == CID_FONT_TYPE_0)
			{
				if (cmapEncoding.IsDirect())
				{
					return fontProgram.GetGlyphByCode(unicode) != null;
				}

				return GetFontProgram().GetGlyph(unicode) != null;
			}

			if (cidFontType == CID_FONT_TYPE_2)
			{
				if (fontProgram.IsFontSpecific())
				{
					var b = PdfEncodings.ConvertToBytes((char)unicode, "symboltt");
					return b.Length > 0 && fontProgram.GetGlyph(b[0] & 0xff) != null;
				}

				return GetFontProgram().GetGlyph(unicode) != null;
			}

			throw new PdfException("Invalid CID font type: " + cidFontType);
		}

		public override byte[] ConvertToBytes(string text)
		{
			var len = text.Length;
			var buffer = new ByteBuffer();
			var i = 0;
			if (fontProgram.IsFontSpecific())
			{
				var b = PdfEncodings.ConvertToBytes(text, "symboltt");
				len = b.Length;
				for (var k = 0; k < len; ++k)
				{
					var glyph = fontProgram.GetGlyph(b[k] & 0xff);
					if (glyph != null)
					{
						ConvertToBytes(glyph, buffer);
					}
				}
			}
			else
			{
				for (var k = 0; k < len; ++k)
				{
					int val;
					if (TextUtil.IsSurrogatePair(text, k))
					{
						val = TextUtil.ConvertToUtf32(text, k);
						k++;
					}
					else
					{
						val = text[k];
					}
					var glyph = GetGlyph(val);
					if (glyph.GetCode() > 0)
					{
						ConvertToBytes(glyph, buffer);
					}
					else
					{
						//getCode() could be either -1 or 0
						var nullCode = cmapEncoding.GetCmapCode(0);
						buffer.Append(nullCode >> 8);
						buffer.Append(nullCode);
					}
				}
			}
			return buffer.ToByteArray();
		}

		public override byte[] ConvertToBytes(GlyphLine glyphLine)
		{
			if (glyphLine != null)
			{
				// prepare and count total length in bytes
				var totalByteCount = 0;
				for (var i = glyphLine.start; i < glyphLine.end; i++)
				{
					totalByteCount += cmapEncoding.GetCmapBytesLength(glyphLine.Get(i).GetCode());
				}
				// perform actual conversion
				var bytes = new byte[totalByteCount];
				var offset = 0;
				for (var i = glyphLine.start; i < glyphLine.end; i++)
				{
					longTag.Add(glyphLine.Get(i).GetCode());
					offset = cmapEncoding.FillCmapBytes(glyphLine.Get(i).GetCode(), bytes, offset);
				}
				return bytes;
			}

			return null;
		}

		public override byte[] ConvertToBytes(Glyph glyph)
		{
			longTag.Add(glyph.GetCode());
			return cmapEncoding.GetCmapBytes(glyph.GetCode());
		}

		public override void WriteText(GlyphLine text, int from, int to, PdfOutputStream stream)
		{
			var len = to - from + 1;
			if (len > 0)
			{
				var bytes = ConvertToBytes(new GlyphLine(text, from, to + 1));
				StreamUtil.WriteHexedString(stream, bytes);
			}
		}

		public override void WriteText(string text, PdfOutputStream stream)
		{
			StreamUtil.WriteHexedString(stream, ConvertToBytes(text));
		}

		public override GlyphLine CreateGlyphLine(string content)
		{
			IList<Glyph> glyphs = new List<Glyph>();
			if (cidFontType == CID_FONT_TYPE_0)
			{
				var len = content.Length;
				if (cmapEncoding.IsDirect())
				{
					for (var k = 0; k < len; ++k)
					{
						var glyph = fontProgram.GetGlyphByCode(content[k]);
						if (glyph != null)
						{
							glyphs.Add(glyph);
						}
					}
				}
				else
				{
					for (var k = 0; k < len; ++k)
					{
						int ch;
						if (TextUtil.IsSurrogatePair(content, k))
						{
							ch = TextUtil.ConvertToUtf32(content, k);
							k++;
						}
						else
						{
							ch = content[k];
						}
						glyphs.Add(GetGlyph(ch));
					}
				}
			}
			else
			{
				if (cidFontType == CID_FONT_TYPE_2)
				{
					var len = content.Length;
					if (fontProgram.IsFontSpecific())
					{
						var b = PdfEncodings.ConvertToBytes(content, "symboltt");
						len = b.Length;
						for (var k = 0; k < len; ++k)
						{
							var glyph = fontProgram.GetGlyph(b[k] & 0xff);
							if (glyph != null)
							{
								glyphs.Add(glyph);
							}
						}
					}
					else
					{
						for (var k = 0; k < len; ++k)
						{
							int val;
							if (TextUtil.IsSurrogatePair(content, k))
							{
								val = TextUtil.ConvertToUtf32(content, k);
								k++;
							}
							else
							{
								val = content[k];
							}
							glyphs.Add(GetGlyph(val));
						}
					}
				}
				else
				{
					throw new PdfException("Font has no suitable cmap.");
				}
			}
			return new GlyphLine(glyphs);
		}

		public override int AppendGlyphs(string text, int from, int to, IList<Glyph> glyphs)
		{
			if (cidFontType == CID_FONT_TYPE_0)
			{
				if (cmapEncoding.IsDirect())
				{
					var processed = 0;
					for (var k = from; k <= to; k++)
					{
						var glyph = fontProgram.GetGlyphByCode(text[k]);
						if (glyph != null && (IsAppendableGlyph(glyph)))
						{
							glyphs.Add(glyph);
							processed++;
						}
						else
						{
							break;
						}
					}
					return processed;
				}

				return AppendUniGlyphs(text, from, to, glyphs);
			}

			if (cidFontType == CID_FONT_TYPE_2)
			{
				if (fontProgram.IsFontSpecific())
				{
					var processed = 0;
					for (var k = from; k <= to; k++)
					{
						var glyph = fontProgram.GetGlyph(text[k] & 0xff);
						if (glyph != null && (IsAppendableGlyph(glyph)))
						{
							glyphs.Add(glyph);
							processed++;
						}
						else
						{
							break;
						}
					}
					return processed;
				}

				return AppendUniGlyphs(text, from, to, glyphs);
			}

			throw new PdfException("Font has no suitable cmap.");
		}

		private int AppendUniGlyphs(string text, int from, int to, IList<Glyph> glyphs)
		{
			var processed = 0;
			for (var k = from; k <= to; ++k)
			{
				int val;
				var currentlyProcessed = processed;
				if (TextUtil.IsSurrogatePair(text, k))
				{
					val = TextUtil.ConvertToUtf32(text, k);
					processed += 2;
				}
				else
				{
					val = text[k];
					processed++;
				}
				var glyph = GetGlyph(val);
				if (IsAppendableGlyph(glyph))
				{
					glyphs.Add(glyph);
				}
				else
				{
					processed = currentlyProcessed;
					break;
				}
			}
			return processed;
		}

		public override int AppendAnyGlyph(string text, int from, IList<Glyph> glyphs)
		{
			var process = 1;
			if (cidFontType == CID_FONT_TYPE_0)
			{
				if (cmapEncoding.IsDirect())
				{
					var glyph = fontProgram.GetGlyphByCode(text[from]);
					if (glyph != null)
					{
						glyphs.Add(glyph);
					}
				}
				else
				{
					int ch;
					if (TextUtil.IsSurrogatePair(text, from))
					{
						ch = TextUtil.ConvertToUtf32(text, from);
						process = 2;
					}
					else
					{
						ch = text[from];
					}
					glyphs.Add(GetGlyph(ch));
				}
			}
			else
			{
				if (cidFontType == CID_FONT_TYPE_2)
				{
					var ttf = (TrueTypeFont)fontProgram;
					if (ttf.IsFontSpecific())
					{
						var b = PdfEncodings.ConvertToBytes(text, "symboltt");
						if (b.Length > 0)
						{
							var glyph = fontProgram.GetGlyph(b[0] & 0xff);
							if (glyph != null)
							{
								glyphs.Add(glyph);
							}
						}
					}
					else
					{
						int ch;
						if (TextUtil.IsSurrogatePair(text, from))
						{
							ch = TextUtil.ConvertToUtf32(text, from);
							process = 2;
						}
						else
						{
							ch = text[from];
						}
						glyphs.Add(GetGlyph(ch));
					}
				}
				else
				{
					throw new PdfException("Font has no suitable cmap.");
				}
			}
			return process;
		}

		//TODO what if Glyphs contains only whitespaces and ignorable identifiers?
		private bool IsAppendableGlyph(Glyph glyph)
		{
			// If font is specific and glyph.getCode() = 0, unicode value will be also 0.
			// Character.isIdentifierIgnorable(0) gets true.
			return glyph.GetCode() > 0 || TextUtil.IsWhitespaceOrNonPrintable(glyph.GetUnicode());
		}

		public override string Decode(PdfString content)
		{
			return DecodeIntoGlyphLine(content).ToString();
		}

		/// <summary><inheritDoc/></summary>
		public override GlyphLine DecodeIntoGlyphLine(PdfString characterCodes)
		{
			IList<Glyph> glyphs = new List<Glyph>();
			AppendDecodedCodesToGlyphsList(glyphs, characterCodes);
			return new GlyphLine(glyphs);
		}

		/// <summary><inheritDoc/></summary>
		public override bool AppendDecodedCodesToGlyphsList(IList<Glyph> list, PdfString characterCodes)
		{
			var allCodesDecoded = true;
			var charCodesSequence = characterCodes.GetValue();
			// A sequence of one or more bytes shall be extracted from the string and matched against the codespace
			// ranges in the CMap. That is, the first byte shall be matched against 1-byte codespace ranges; if no match is
			// found, a second byte shall be extracted, and the 2-byte code shall be matched against 2-byte codespace
			// ranges. This process continues for successively longer codes until a match is found or all codespace ranges
			// have been tested. There will be at most one match because codespace ranges shall not overlap.
			for (var i = 0; i < charCodesSequence.Length; i++)
			{
				var code = 0;
				Glyph glyph = null;
				var codeSpaceMatchedLength = 1;
				for (var codeLength = 1; codeLength <= MAX_CID_CODE_LENGTH && i + codeLength <= charCodesSequence.Length;
					codeLength++)
				{
					code = (code << 8) + charCodesSequence[i + codeLength - 1];
					if (!GetCmap().ContainsCodeInCodeSpaceRange(code, codeLength))
					{
						continue;
					}

					codeSpaceMatchedLength = codeLength;
					var glyphCode = GetCmap().GetCidCode(code);
					glyph = GetFontProgram().GetGlyphByCode(glyphCode);
					if (glyph != null)
					{
						i += codeLength - 1;
						break;
					}
				}
				if (glyph == null)
				{
					var logger = LogManager.GetLogger(typeof(PdfType0Font));
					if (logger.IsWarnEnabled)
					{
						var failedCodes = new StringBuilder();
						for (var codeLength = 1; codeLength <= MAX_CID_CODE_LENGTH && i + codeLength <= charCodesSequence.Length;
							codeLength++)
						{
							failedCodes.Append((int)charCodesSequence[i + codeLength - 1]).Append(" ");
						}
						logger.Warn(MessageFormatUtil.Format(LogMessageConstant.COULD_NOT_FIND_GLYPH_WITH_CODE, failedCodes
							.ToString()));
					}
					i += codeSpaceMatchedLength - 1;
				}
				if (glyph != null && glyph.GetChars() != null)
				{
					list.Add(glyph);
				}
				else
				{
					list.Add(new Glyph(0, GetFontProgram().GetGlyphByCode(0).GetWidth(), -1));
					allCodesDecoded = false;
				}
			}
			return allCodesDecoded;
		}

		public override float GetContentWidth(PdfString content)
		{
			float width = 0;
			var glyphLine = DecodeIntoGlyphLine(content);
			for (var i = glyphLine.start; i < glyphLine.end; i++)
			{
				width += glyphLine.Get(i).GetWidth();
			}
			return width;
		}

		public override bool IsBuiltWith(string fontProgram, string encoding)
		{
			return GetFontProgram().IsBuiltWith(fontProgram) && cmapEncoding.IsBuiltWith(encoding);
		}

		public override void Flush()
		{
			if (IsFlushed())
			{
				return;
			}
			EnsureUnderlyingObjectHasIndirectReference();
			if (newFont)
			{
				FlushFontData();
			}
			base.Flush();
		}

		/// <summary>Gets CMAP associated with the Pdf Font.</summary>
		/// <returns>CMAP</returns>
		/// <seealso cref="CMapEncoding"/>
		public virtual CMapEncoding GetCmap()
		{
			return cmapEncoding;
		}

		/// <summary>Creates a ToUnicode CMap to allow copy and paste from Acrobat.</summary>
		/// <param name="metrics">
		/// metrics[0] contains the glyph index and metrics[2]
		/// contains the Unicode code
		/// </param>
		/// <returns>the stream representing this CMap or <c>null</c></returns>
		[Obsolete(@"will be removed in 7.2. Use GetToUnicode() instead")]
		public virtual PdfStream GetToUnicode(object[] metrics)
		{
			return GetToUnicode();
		}

		protected internal override PdfDictionary GetFontDescriptor(string fontName)
		{
			var fontDescriptor = new PdfDictionary();
			MakeObjectIndirect(fontDescriptor);
			fontDescriptor.Put(PdfName.Type, PdfName.FontDescriptor);
			fontDescriptor.Put(PdfName.FontName, new PdfName(fontName));
			fontDescriptor.Put(PdfName.FontBBox, new PdfArray(GetFontProgram().GetFontMetrics().GetBbox()));
			fontDescriptor.Put(PdfName.Ascent, new PdfNumber(GetFontProgram().GetFontMetrics().GetTypoAscender()));
			fontDescriptor.Put(PdfName.Descent, new PdfNumber(GetFontProgram().GetFontMetrics().GetTypoDescender()));
			fontDescriptor.Put(PdfName.CapHeight, new PdfNumber(GetFontProgram().GetFontMetrics().GetCapHeight()));
			fontDescriptor.Put(PdfName.ItalicAngle, new PdfNumber(GetFontProgram().GetFontMetrics().GetItalicAngle()));
			fontDescriptor.Put(PdfName.StemV, new PdfNumber(GetFontProgram().GetFontMetrics().GetStemV()));
			fontDescriptor.Put(PdfName.Flags, new PdfNumber(GetFontProgram().GetPdfFontFlags()));
			if (fontProgram.GetFontIdentification().GetPanose() != null)
			{
				var styleDictionary = new PdfDictionary();
				styleDictionary.Put(PdfName.Panose, new PdfString(fontProgram.GetFontIdentification().GetPanose()).SetHexWriting
					(true));
				fontDescriptor.Put(PdfName.Style, styleDictionary);
			}
			return fontDescriptor;
		}

		/// <summary>Generates the CIDFontType2 dictionary.</summary>
		/// <param name="ttf">a font program of this font instance</param>
		/// <param name="fontDescriptor">the font descriptor dictionary</param>
		/// <param name="fontName">a name of the font</param>
		/// <param name="metrics">the horizontal width metrics</param>
		/// <returns>fully initialized CIDFont</returns>
		[Obsolete(@"will be removed in 7.2")]
		protected internal virtual PdfDictionary GetCidFontType2(TrueTypeFont ttf, PdfDictionary fontDescriptor, string
			 fontName, int[][] metrics)
		{
			return GetCidFont(fontDescriptor, fontName, ttf != null && !ttf.IsCff());
		}

		/// <summary>The method will update set of used glyphs with range used in subset or with all glyphs if there is no subset.
		///     </summary>
		/// <remarks>
		/// The method will update set of used glyphs with range used in subset or with all glyphs if there is no subset.
		/// This set of used glyphs is required for building width array and ToUnicode CMAP.
		/// </remarks>
		/// <param name="ttf">a font program of this font instance.</param>
		/// <param name="longTag">
		/// a set of integers, which are glyph ids that denote used glyphs.
		/// This set is updated inside of the method if needed.
		/// </param>
		/// <param name="includeMetrics">
		/// used to define whether longTag map is populated with glyph metrics.
		/// Deprecated and is not used right now.
		/// </param>
		[Obsolete(@"will be removed in 7.2")]
		protected internal virtual void AddRangeUni(TrueTypeFont ttf, IDictionary<int, int[]> longTag, bool includeMetrics
			)
		{
			AddRangeUni(ttf, longTag.Keys);
		}

		private void ConvertToBytes(Glyph glyph, ByteBuffer result)
		{
			var code = glyph.GetCode();
			longTag.Add(code);
			cmapEncoding.FillCmapBytes(code, result);
		}

		private static string GetOrdering(PdfDictionary cidFont)
		{
			var cidinfo = cidFont.GetAsDictionary(PdfName.CIDSystemInfo);
			if (cidinfo == null)
			{
				return null;
			}
			return cidinfo.ContainsKey(PdfName.Ordering) ? cidinfo.Get(PdfName.Ordering).ToString() : null;
		}

		private void FlushFontData()
		{
			if (cidFontType == CID_FONT_TYPE_0)
			{
				GetPdfObject().Put(PdfName.Type, PdfName.Font);
				GetPdfObject().Put(PdfName.Subtype, PdfName.Type0);
				var name = fontProgram.GetFontNames().GetFontName();
				var style = fontProgram.GetFontNames().GetStyle();
				if (style.Length > 0)
				{
					name += "-" + style;
				}
				GetPdfObject().Put(PdfName.BaseFont, new PdfName(MessageFormatUtil.Format("{0}-{1}", name, cmapEncoding.GetCmapName
					())));
				GetPdfObject().Put(PdfName.Encoding, new PdfName(cmapEncoding.GetCmapName()));
				var fontDescriptor = GetFontDescriptor(name);
				var cidFont = GetCidFont(fontDescriptor, fontProgram.GetFontNames().GetFontName(), false);
				GetPdfObject().Put(PdfName.DescendantFonts, new PdfArray(cidFont));
				fontDescriptor.Flush();
				cidFont.Flush();
			}
			else
			{
				if (cidFontType == CID_FONT_TYPE_2)
				{
					var ttf = (TrueTypeFont)GetFontProgram();
					var fontName = UpdateSubsetPrefix(ttf.GetFontNames().GetFontName(), subset, embedded);
					var fontDescriptor = GetFontDescriptor(fontName);
					PdfStream fontStream;
					ttf.UpdateUsedGlyphs((SortedSet<int>)longTag, subset, subsetRanges);
					if (ttf.IsCff())
					{
						byte[] cffBytes;
						if (subset)
						{
							cffBytes = new CFFFontSubset(ttf.GetFontStreamBytes(), longTag).Process();
						}
						else
						{
							cffBytes = ttf.GetFontStreamBytes();
						}
						fontStream = GetPdfFontStream(cffBytes, new[] { cffBytes.Length });
						fontStream.Put(PdfName.Subtype, new PdfName("CIDFontType0C"));
						// The PDF Reference manual advises to add -cmap in case CIDFontType0
						GetPdfObject().Put(PdfName.BaseFont, new PdfName(MessageFormatUtil.Format("{0}-{1}", fontName, cmapEncoding
							.GetCmapName())));
						fontDescriptor.Put(PdfName.FontFile3, fontStream);
					}
					else
					{
						byte[] ttfBytes = null;
						//getDirectoryOffset() > 0 means ttc, which shall be subsetted anyway.
						if (subset || ttf.GetDirectoryOffset() > 0)
						{
							try
							{
								ttfBytes = ttf.GetSubset(longTag, subset);
							}
							catch (IO.IOException)
							{
								var logger = LogManager.GetLogger(typeof(PdfType0Font));
								logger.Warn(LogMessageConstant.FONT_SUBSET_ISSUE);
								ttfBytes = null;
							}
						}
						if (ttfBytes == null)
						{
							ttfBytes = ttf.GetFontStreamBytes();
						}
						fontStream = GetPdfFontStream(ttfBytes, new[] { ttfBytes.Length });
						GetPdfObject().Put(PdfName.BaseFont, new PdfName(fontName));
						fontDescriptor.Put(PdfName.FontFile2, fontStream);
					}
					// CIDSet shall be based on font.numberOfGlyphs property of the font, it is maxp.numGlyphs for ttf,
					// because technically we convert all unused glyphs to space, e.g. just remove outlines.
					var numOfGlyphs = ttf.GetFontMetrics().GetNumberOfGlyphs();
					var cidSetBytes = new byte[ttf.GetFontMetrics().GetNumberOfGlyphs() / 8 + 1];
					for (var i = 0; i < numOfGlyphs / 8; i++)
					{
						cidSetBytes[i] |= 0xff;
					}
					for (var i = 0; i < numOfGlyphs % 8; i++)
					{
						cidSetBytes[cidSetBytes.Length - 1] |= rotbits[i];
					}
					fontDescriptor.Put(PdfName.CIDSet, new PdfStream(cidSetBytes));
					var cidFont = GetCidFont(fontDescriptor, fontName, !ttf.IsCff());
					GetPdfObject().Put(PdfName.Type, PdfName.Font);
					GetPdfObject().Put(PdfName.Subtype, PdfName.Type0);
					GetPdfObject().Put(PdfName.Encoding, new PdfName(cmapEncoding.GetCmapName()));
					GetPdfObject().Put(PdfName.DescendantFonts, new PdfArray(cidFont));
					var toUnicode = GetToUnicode();
					if (toUnicode != null)
					{
						GetPdfObject().Put(PdfName.ToUnicode, toUnicode);
						if (toUnicode.GetIndirectReference() != null)
						{
							toUnicode.Flush();
						}
					}
					// getPdfObject().getIndirectReference() != null by assertion of PdfType0Font#flush()
					// This means, that fontDescriptor, cidFont and fontStream already are indirects
					if (GetPdfObject().GetIndirectReference().GetDocument().GetPdfVersion().CompareTo(PdfVersion.PDF_2_0) >= 0
						)
					{
						// CIDSet is deprecated in PDF 2.0
						fontDescriptor.Remove(PdfName.CIDSet);
					}
					fontDescriptor.Flush();
					cidFont.Flush();
					fontStream.Flush();
				}
				else
				{
					throw new InvalidOperationException("Unsupported CID Font");
				}
			}
		}

		/// <summary>Generates the CIDFontType2 dictionary.</summary>
		/// <param name="ttf">a font program of this font instance</param>
		/// <param name="fontDescriptor">the indirect reference to the font descriptor</param>
		/// <param name="fontName">a name of the font</param>
		/// <param name="glyphIds">glyph ids used in from the font</param>
		/// <returns>fully initialized CIDFont</returns>
		[Obsolete(@"use GetCidFont(iText.Kernel.Pdf.PdfDictionary, System.String, bool) instead.")]
		protected internal virtual PdfDictionary GetCidFontType2(TrueTypeFont ttf, PdfDictionary fontDescriptor, string
			 fontName, int[] glyphIds)
		{
			return GetCidFont(fontDescriptor, fontName, ttf != null && !ttf.IsCff());
		}

		/// <summary>Generates the CIDFontType2 dictionary.</summary>
		/// <param name="fontDescriptor">the font descriptor dictionary</param>
		/// <param name="fontName">a name of the font</param>
		/// <param name="isType2">
		/// true, if the font is CIDFontType2 (TrueType glyphs),
		/// otherwise false, i.e. CIDFontType0 (Type1/CFF glyphs)
		/// </param>
		/// <returns>fully initialized CIDFont</returns>
		protected internal virtual PdfDictionary GetCidFont(PdfDictionary fontDescriptor, string fontName, bool isType2
			)
		{
			var cidFont = new PdfDictionary();
			MarkObjectAsIndirect(cidFont);
			cidFont.Put(PdfName.Type, PdfName.Font);
			// sivan; cff
			cidFont.Put(PdfName.FontDescriptor, fontDescriptor);
			if (isType2)
			{
				cidFont.Put(PdfName.Subtype, PdfName.CIDFontType2);
				cidFont.Put(PdfName.CIDToGIDMap, PdfName.Identity);
			}
			else
			{
				cidFont.Put(PdfName.Subtype, PdfName.CIDFontType0);
			}
			cidFont.Put(PdfName.BaseFont, new PdfName(fontName));
			var cidInfo = new PdfDictionary();
			cidInfo.Put(PdfName.Registry, new PdfString(cmapEncoding.GetRegistry()));
			cidInfo.Put(PdfName.Ordering, new PdfString(cmapEncoding.GetOrdering()));
			cidInfo.Put(PdfName.Supplement, new PdfNumber(cmapEncoding.GetSupplement()));
			cidFont.Put(PdfName.CIDSystemInfo, cidInfo);
			if (!vertical)
			{
				cidFont.Put(PdfName.DW, new PdfNumber(FontProgram.DEFAULT_WIDTH));
				var widthsArray = GenerateWidthsArray();
				if (widthsArray != null)
				{
					cidFont.Put(PdfName.W, widthsArray);
				}
			}
			else
			{
				// TODO DEVSIX-31
				var logger = LogManager.GetLogger(typeof(PdfType0Font));
				logger.Warn("Vertical writing has not been implemented yet.");
			}
			return cidFont;
		}

		private PdfObject GenerateWidthsArray()
		{
			var bytes = new ByteArrayOutputStream();
			var stream = new OutputStream<ByteArrayOutputStream>(bytes);
			stream.WriteByte('[');
			var lastNumber = -10;
			var firstTime = true;
			foreach (var code in longTag)
			{
				var glyph = fontProgram.GetGlyphByCode(code);
				if (glyph.GetWidth() == FontProgram.DEFAULT_WIDTH)
				{
					continue;
				}
				if (glyph.GetCode() == lastNumber + 1)
				{
					stream.WriteByte(' ');
				}
				else
				{
					if (!firstTime)
					{
						stream.WriteByte(']');
					}
					firstTime = false;
					stream.WriteInteger(glyph.GetCode());
					stream.WriteByte('[');
				}
				stream.WriteInteger(glyph.GetWidth());
				lastNumber = glyph.GetCode();
			}
			if (stream.GetCurrentPos() > 1)
			{
				stream.WriteString("]]");
				return new PdfLiteral(bytes.ToArray());
			}
			return null;
		}

		/// <summary>Creates a ToUnicode CMap to allow copy and paste from Acrobat.</summary>
		/// <returns>the stream representing this CMap or <c>null</c></returns>
		public virtual PdfStream GetToUnicode()
		{
			var stream = new OutputStream<ByteArrayOutputStream>(new ByteArrayOutputStream
				());
			stream.WriteString("/CIDInit /ProcSet findresource begin\n" + "12 dict begin\n" + "begincmap\n" + "/CIDSystemInfo\n"
				 + "<< /Registry (Adobe)\n" + "/Ordering (UCS)\n" + "/Supplement 0\n" + ">> def\n" + "/CMapName /Adobe-Identity-UCS def\n"
				 + "/CMapType 2 def\n" + "1 begincodespacerange\n" + "<0000><FFFF>\n" + "endcodespacerange\n");
			//accumulate long tag into a subset and write it.
			var glyphGroup = new List<Glyph>(100);
			var bfranges = 0;
			foreach (int? glyphId in longTag)
			{
				var glyph = fontProgram.GetGlyphByCode((int)glyphId);
				if (glyph.GetChars() != null)
				{
					glyphGroup.Add(glyph);
					if (glyphGroup.Count == 100)
					{
						bfranges += WriteBfrange(stream, glyphGroup);
					}
				}
			}
			//flush leftovers
			bfranges += WriteBfrange(stream, glyphGroup);
			if (bfranges == 0)
			{
				return null;
			}
			stream.WriteString("endcmap\n" + "CMapName currentdict /CMap defineresource pop\n" + "end end\n");
			return new PdfStream(((ByteArrayOutputStream)stream.GetOutputStream()).ToArray());
		}

		private int WriteBfrange(OutputStream<ByteArrayOutputStream> stream, IList<Glyph> range)
		{
			if (range.IsEmpty())
			{
				return 0;
			}
			stream.WriteInteger(range.Count);
			stream.WriteString(" beginbfrange\n");
			foreach (var glyph in range)
			{
				var fromTo = CMapContentParser.ToHex(glyph.GetCode());
				stream.WriteString(fromTo);
				stream.WriteString(fromTo);
				stream.WriteByte('<');
				foreach (var ch in glyph.GetChars())
				{
					stream.WriteString(ToHex4(ch));
				}
				stream.WriteByte('>');
				stream.WriteByte('\n');
			}
			stream.WriteString("endbfrange\n");
			range.Clear();
			return 1;
		}

		private static string ToHex4(char ch)
		{
			var s = "0000" + JavaUtil.IntegerToHexString(ch);
			return s.Substring(s.Length - 4);
		}

		/// <summary>The method will update set of used glyphs with range used in subset or with all glyphs if there is no subset.
		///     </summary>
		/// <remarks>
		/// The method will update set of used glyphs with range used in subset or with all glyphs if there is no subset.
		/// This set of used glyphs is required for building width array and ToUnicode CMAP.
		/// </remarks>
		/// <param name="ttf">a font program of this font instance.</param>
		/// <param name="longTag">
		/// a set of integers, which are glyph ids that denote used glyphs.
		/// This set is updated inside of the method if needed.
		/// </param>
		[Obsolete(@"use iText.IO.Font.TrueTypeFont.UpdateUsedGlyphs(Java.Util.SortedSet{E}, bool, System.Collections.Generic.IList{E}) instead."
			)]
		protected internal virtual void AddRangeUni(TrueTypeFont ttf, ICollection<int> longTag)
		{
			ttf.UpdateUsedGlyphs((SortedSet<int>)longTag, subset, subsetRanges);
		}

		private string GetCompatibleUniMap(string registry)
		{
			var uniMap = "";
			foreach (var name in CidFontProperties.GetRegistryNames().Get(registry + "_Uni"))
			{
				uniMap = name;
				if (name.EndsWith("V") && vertical)
				{
					break;
				}

				if (!name.EndsWith("V") && !vertical)
				{
					break;
				}
			}
			return uniMap;
		}

		private static CMapEncoding CreateCMap(PdfObject cmap, string uniMap)
		{
			if (cmap.IsStream())
			{
				var cmapStream = (PdfStream)cmap;
				var cmapBytes = cmapStream.GetBytes();
				return new CMapEncoding(cmapStream.GetAsName(PdfName.CMapName).GetValue(), cmapBytes);
			}

			var cmapName = ((PdfName)cmap).GetValue();
			if (PdfEncodings.IDENTITY_H.Equals(cmapName) || PdfEncodings.IDENTITY_V.Equals(cmapName))
			{
				return new CMapEncoding(cmapName);
			}

			return new CMapEncoding(cmapName, uniMap);
		}
	}
}
