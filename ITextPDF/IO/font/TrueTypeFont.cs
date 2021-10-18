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
using System.Collections;
using System.Collections.Generic;
using IText.IO.Font.Constants;
using IText.IO.Font.Otf;
using IText.IO.Util;
using IText.Logger;

namespace  IText.IO.Font {
    public class TrueTypeFont : FontProgram {
        private OpenTypeParser fontParser;

        protected internal int[][] bBoxes;

        protected internal bool isVertical;

        private GlyphSubstitutionTableReader gsubTable;

        private GlyphPositioningTableReader gposTable;

        private OpenTypeGdefTableReader gdefTable;

        /// <summary>The map containing the kerning information.</summary>
        /// <remarks>
        /// The map containing the kerning information. It represents the content of
        /// table 'kern'. The key is an <c>Integer</c> where the top 16 bits
        /// are the glyph number for the first character and the lower 16 bits are the
        /// glyph number for the second character. The value is the amount of kerning in
        /// normalized 1000 units as an <c>Integer</c>. This value is usually negative.
        /// </remarks>
        protected internal IntHashtable kerning = new IntHashtable();

        private byte[] fontStreamBytes;

        private TrueTypeFont(OpenTypeParser fontParser) {
            this.fontParser = fontParser;
            this.fontParser.LoadTables(true);
            InitializeFontProperties();
        }

        protected internal TrueTypeFont() {
            fontNames = new FontNames();
        }

        public TrueTypeFont(string path)
            : this(new OpenTypeParser(path)) {
        }

        public TrueTypeFont(byte[] ttf)
            : this(new OpenTypeParser(ttf)) {
        }

        internal TrueTypeFont(string ttcPath, int ttcIndex)
            : this(new OpenTypeParser(ttcPath, ttcIndex)) {
        }

        internal TrueTypeFont(byte[] ttc, int ttcIndex)
            : this(new OpenTypeParser(ttc, ttcIndex)) {
        }

        public override bool HasKernPairs() {
            return kerning.Size() > 0;
        }

        /// <summary>Gets the kerning between two glyphs.</summary>
        /// <param name="first">the first glyph</param>
        /// <param name="second">the second glyph</param>
        /// <returns>the kerning to be applied</returns>
        public override int GetKerning(Glyph first, Glyph second) {
            if (first == null || second == null) {
                return 0;
            }
            return kerning.Get((first.GetCode() << 16) + second.GetCode());
        }

        public virtual bool IsCff() {
            return fontParser.IsCff();
        }

        public virtual IDictionary<int, int[]> GetActiveCmap() {
            var cmaps = fontParser.GetCmapTable();
            if (cmaps.cmapExt != null) {
                return cmaps.cmapExt;
            }

            if (!cmaps.fontSpecific && cmaps.cmap31 != null) {
	            return cmaps.cmap31;
            }

            if (cmaps.fontSpecific && cmaps.cmap10 != null) {
	            return cmaps.cmap10;
            }

            if (cmaps.cmap31 != null) {
	            return cmaps.cmap31;
            }

            return cmaps.cmap10;
        }

        public virtual byte[] GetFontStreamBytes() {
            if (fontStreamBytes != null) {
                return fontStreamBytes;
            }
            try {
                if (fontParser.IsCff()) {
                    fontStreamBytes = fontParser.ReadCffFont();
                }
                else {
                    fontStreamBytes = fontParser.GetFullFont();
                }
            }
            catch (System.IO.IOException e) {
                fontStreamBytes = null;
                throw new IOException(IOException.IoException, e);
            }
            return fontStreamBytes;
        }

        public override int GetPdfFontFlags() {
            var flags = 0;
            if (fontMetrics.IsFixedPitch()) {
                flags |= 1;
            }
            flags |= IsFontSpecific() ? 4 : 32;
            if (fontNames.IsItalic()) {
                flags |= 64;
            }
            if (fontNames.IsBold() || fontNames.GetFontWeight() > 500) {
                flags |= 262144;
            }
            return flags;
        }

        /// <summary>The offset from the start of the file to the table directory.</summary>
        /// <remarks>
        /// The offset from the start of the file to the table directory.
        /// It is 0 for TTF and may vary for TTC depending on the chosen font.
        /// </remarks>
        /// <returns>directory Offset</returns>
        public virtual int GetDirectoryOffset() {
            return fontParser.directoryOffset;
        }

        public virtual GlyphSubstitutionTableReader GetGsubTable() {
            return gsubTable;
        }

        public virtual GlyphPositioningTableReader GetGposTable() {
            return gposTable;
        }

        public virtual OpenTypeGdefTableReader GetGdefTable() {
            return gdefTable;
        }

        public virtual byte[] GetSubset(ICollection<int> glyphs, bool subset) {
            try {
                return fontParser.GetSubset(glyphs, subset);
            }
            catch (System.IO.IOException e) {
                throw new IOException(IOException.IoException, e);
            }
        }

        protected internal virtual void ReadGdefTable() {
            var gdef = fontParser.tables.Get("GDEF");
            if (gdef != null) {
                gdefTable = new OpenTypeGdefTableReader(fontParser.raf, gdef[0]);
            }
            else {
                gdefTable = new OpenTypeGdefTableReader(fontParser.raf, 0);
            }
            gdefTable.ReadTable();
        }

        protected internal virtual void ReadGsubTable() {
            var gsub = fontParser.tables.Get("GSUB");
            if (gsub != null) {
                gsubTable = new GlyphSubstitutionTableReader(fontParser.raf, gsub[0], gdefTable, ÑodeToGlyph, fontMetrics.
                    GetUnitsPerEm());
            }
        }

        protected internal virtual void ReadGposTable() {
            var gpos = fontParser.tables.Get("GPOS");
            if (gpos != null) {
                gposTable = new GlyphPositioningTableReader(fontParser.raf, gpos[0], gdefTable, ÑodeToGlyph, fontMetrics.GetUnitsPerEm
                    ());
            }
        }

        private void InitializeFontProperties() {
            // initialize sfnt tables
            var head = fontParser.GetHeadTable();
            var hhea = fontParser.GetHheaTable();
            var os_2 = fontParser.GetOs_2Table();
            var post = fontParser.GetPostTable();
            isFontSpecific = fontParser.GetCmapTable().fontSpecific;
            kerning = fontParser.ReadKerning(head.unitsPerEm);
            bBoxes = fontParser.ReadBbox(head.unitsPerEm);
            // font names group
            fontNames = fontParser.GetFontNames();
            // font metrics group
            fontMetrics.SetUnitsPerEm(head.unitsPerEm);
            fontMetrics.UpdateBbox(head.xMin, head.yMin, head.xMax, head.yMax);
            fontMetrics.SetNumberOfGlyphs(fontParser.ReadNumGlyphs());
            fontMetrics.SetGlyphWidths(fontParser.GetGlyphWidthsByIndex());
            fontMetrics.SetTypoAscender(os_2.sTypoAscender);
            fontMetrics.SetTypoDescender(os_2.sTypoDescender);
            fontMetrics.SetCapHeight(os_2.sCapHeight);
            fontMetrics.SetXHeight(os_2.sxHeight);
            fontMetrics.SetItalicAngle(post.italicAngle);
            fontMetrics.SetAscender(hhea.Ascender);
            fontMetrics.SetDescender(hhea.Descender);
            fontMetrics.SetLineGap(hhea.LineGap);
            fontMetrics.SetWinAscender(os_2.usWinAscent);
            fontMetrics.SetWinDescender(os_2.usWinDescent);
            fontMetrics.SetAdvanceWidthMax(hhea.advanceWidthMax);
            fontMetrics.SetUnderlinePosition((post.underlinePosition - post.underlineThickness) / 2);
            fontMetrics.SetUnderlineThickness(post.underlineThickness);
            fontMetrics.SetStrikeoutPosition(os_2.yStrikeoutPosition);
            fontMetrics.SetStrikeoutSize(os_2.yStrikeoutSize);
            fontMetrics.SetSubscriptOffset(-os_2.ySubscriptYOffset);
            fontMetrics.SetSubscriptSize(os_2.ySubscriptYSize);
            fontMetrics.SetSuperscriptOffset(os_2.ySuperscriptYOffset);
            fontMetrics.SetSuperscriptSize(os_2.ySuperscriptYSize);
            fontMetrics.SetIsFixedPitch(post.isFixedPitch);
            // font identification group
            var ttfVersion = fontNames.GetNames(5);
            if (ttfVersion != null) {
                fontIdentification.SetTtfVersion(ttfVersion[0][3]);
            }
            var ttfUniqueId = fontNames.GetNames(3);
            if (ttfUniqueId != null) {
                fontIdentification.SetTtfVersion(ttfUniqueId[0][3]);
            }
            var pdfPanose = new byte[12];
            pdfPanose[1] = (byte)(os_2.sFamilyClass);
            pdfPanose[0] = (byte)(os_2.sFamilyClass >> 8);
            Array.Copy(os_2.panose, 0, pdfPanose, 2, 10);
            fontIdentification.SetPanose(pdfPanose);
            var cmap = GetActiveCmap();
            var glyphWidths = fontParser.GetGlyphWidthsByIndex();
            var numOfGlyphs = fontMetrics.GetNumberOfGlyphs();
            UnicodeToGlyph = new LinkedDictionary<int, Glyph>(cmap.Count);
            ÑodeToGlyph = new LinkedDictionary<int, Glyph>(numOfGlyphs);
            avgWidth = 0;
            foreach (var charCode in cmap.Keys) {
                var index = cmap.Get(charCode)[0];
                if (index >= numOfGlyphs) {
                    var LOGGER = LogManager.GetLogger(typeof(TrueTypeFont));
                    LOGGER.Warn(MessageFormatUtil.Format(LogMessageConstant.FONT_HAS_INVALID_GLYPH, GetFontNames().GetFontName
                        (), index));
                    continue;
                }
                var glyph = new Glyph(index, glyphWidths[index], charCode, bBoxes != null ? bBoxes[index] : null);
                UnicodeToGlyph.Put(charCode, glyph);
                // This is done on purpose to keep the mapping to glyphs with smaller unicode values, in contrast with
                // larger values which often represent different forms of other characters.
                if (!ÑodeToGlyph.ContainsKey(index)) {
                    ÑodeToGlyph.Put(index, glyph);
                }
                avgWidth += glyph.GetWidth();
            }
            FixSpaceIssue();
            for (var index = 0; index < glyphWidths.Length; index++) {
                if (ÑodeToGlyph.ContainsKey(index)) {
                    continue;
                }
                var glyph = new Glyph(index, glyphWidths[index], -1);
                ÑodeToGlyph.Put(index, glyph);
                avgWidth += glyph.GetWidth();
            }
            if (ÑodeToGlyph.Count != 0) {
                avgWidth /= ÑodeToGlyph.Count;
            }
            ReadGdefTable();
            ReadGsubTable();
            ReadGposTable();
            isVertical = false;
        }

        /// <summary>Gets the code pages supported by the font.</summary>
        /// <returns>the code pages supported by the font</returns>
        public virtual string[] GetCodePagesSupported() {
            var cp = ((long)fontParser.GetOs_2Table().ulCodePageRange2 << 32) + (fontParser.GetOs_2Table().ulCodePageRange1
                 & 0xffffffffL);
            var count = 0;
            long bit = 1;
            for (var k = 0; k < 64; ++k) {
                if ((cp & bit) != 0 && TrueTypeCodePages.Get(k) != null) {
                    ++count;
                }
                bit <<= 1;
            }
            var ret = new string[count];
            count = 0;
            bit = 1;
            for (var k = 0; k < 64; ++k) {
                if ((cp & bit) != 0 && TrueTypeCodePages.Get(k) != null) {
                    ret[count++] = TrueTypeCodePages.Get(k);
                }
                bit <<= 1;
            }
            return ret;
        }

        public override bool IsBuiltWith(string fontProgram) {
            return Equals(fontParser.fileName, fontProgram);
        }

        public virtual void Close() {
            if (fontParser != null) {
                fontParser.Close();
            }
            fontParser = null;
        }

        /// <summary>The method will update usedGlyphs with additional range or with all glyphs if there is no subset.
        ///     </summary>
        /// <remarks>
        /// The method will update usedGlyphs with additional range or with all glyphs if there is no subset.
        /// This set of used glyphs can be used for building width array and ToUnicode CMAP.
        /// </remarks>
        /// <param name="usedGlyphs">
        /// a set of integers, which are glyph ids that denote used glyphs.
        /// This set is updated inside of the method if needed.
        /// </param>
        /// <param name="subset">subset status</param>
        /// <param name="subsetRanges">additional subset ranges</param>
        public virtual void UpdateUsedGlyphs(SortedSet<int> usedGlyphs, bool subset, IList<int[]> subsetRanges) {
            int[] compactRange;
            if (subsetRanges != null) {
                compactRange = ToCompactRange(subsetRanges);
            }
            else {
                if (!subset) {
                    compactRange = new[] { 0, 0xFFFF };
                }
                else {
                    compactRange = new int[] {  };
                }
            }
            for (var k = 0; k < compactRange.Length; k += 2) {
                var from = compactRange[k];
                var to = compactRange[k + 1];
                for (var glyphId = from; glyphId <= to; glyphId++) {
                    if (GetGlyphByCode(glyphId) != null) {
                        usedGlyphs.Add(glyphId);
                    }
                }
            }
        }

        /// <summary>
        /// Normalizes given ranges by making sure that first values in pairs are lower than second values and merges overlapping
        /// ranges in one.
        /// </summary>
        /// <param name="ranges">
        /// a
        /// <see cref="IList"/>
        /// of integer arrays, which are constituted by pairs of ints that denote
        /// each range limits. Each integer array size shall be a multiple of two.
        /// </param>
        /// <returns>single merged array consisting of pairs of integers, each of them denoting a range.</returns>
        private static int[] ToCompactRange(IList<int[]> ranges) {
            IList<int[]> simp = new List<int[]>();
            foreach (var range in ranges) {
                for (var j = 0; j < range.Length; j += 2) {
                    simp.Add(new[] { Math.Max(0, Math.Min(range[j], range[j + 1])), Math.Min(0xffff, Math.Max(range[j], range
                        [j + 1])) });
                }
            }
            for (var k1 = 0; k1 < simp.Count - 1; ++k1) {
                for (var k2 = k1 + 1; k2 < simp.Count; ++k2) {
                    var r1 = simp[k1];
                    var r2 = simp[k2];
                    if (r1[0] >= r2[0] && r1[0] <= r2[1] || r1[1] >= r2[0] && r1[0] <= r2[1]) {
                        r1[0] = Math.Min(r1[0], r2[0]);
                        r1[1] = Math.Max(r1[1], r2[1]);
                        simp.JRemoveAt(k2);
                        --k2;
                    }
                }
            }
            var s = new int[simp.Count * 2];
            for (var k = 0; k < simp.Count; ++k) {
                var r = simp[k];
                s[k * 2] = r[0];
                s[k * 2 + 1] = r[1];
            }
            return s;
        }
    }
}
