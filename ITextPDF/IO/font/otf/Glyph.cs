/*
*
* This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
* Authors: Bruno Lowagie, Paulo Soares, et al.
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License version 3
* as published by the Free Software Foundation with the addition of the
* following permission added to Section 15 as permitted in Section 7(a):
* FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
* ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
* OF THIRD PARTY RIGHTS
*
* This program is distributed in the hope that it will be useful, but
* WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
* or FITNESS FOR A PARTICULAR PURPOSE.
* See the GNU Affero General Public License for more details.
* You should have received a copy of the GNU Affero General Public License
* along with this program; if not, see http://www.gnu.org/licenses or write to
* the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
* Boston, MA, 02110-1301 USA, or download the license from the following URL:
* http://itextpdf.com/terms-of-use/
*
* The interactive user interfaces in modified source and object code versions
* of this program must display Appropriate Legal Notices, as required under
* Section 5 of the GNU Affero General Public License.
*
* In accordance with Section 7(b) of the GNU Affero General Public License,
* a covered work must retain the producer line in every PDF that is created
* or manipulated using iText.
*
* You can be released from the requirements of the license by purchasing
* a commercial license. Buying such a license is mandatory as soon as you
* develop commercial activities involving the iText software without
* disclosing the source code of your own applications.
* These activities include: offering paid services to customers as an ASP,
* serving PDFs on the fly in a web application, shipping iText with a closed
* source product.
*
* For more information, please contact iText Software Corp. at this
* address: sales@itextpdf.com
*/

using System;
using IText.IO.Util;

namespace  IText.IO.Font.Otf {
    public class Glyph {
        private const char REPLACEMENT_CHARACTER = '\ufffd';

        private static readonly char[] REPLACEMENT_CHARACTERS = { REPLACEMENT_CHARACTER };

        private static readonly string REPLACEMENT_CHARACTER_STRING = REPLACEMENT_CHARACTER.ToString();

        // The <i>code</i> or <i>id</i> by which this is represented in the Font File.
        private readonly int code;

        // The normalized width of this Glyph.
        private readonly int width;

        // The normalized bbox of this Glyph.
        private int[] bbox;

        // utf-32 representation of glyph if appears. Correct value is > -1
        private int unicode;

        // The Unicode text represented by this Glyph
        private char[] chars;

        // true, if this Glyph is Mark
        private readonly bool isMark;

        // placement offset
        internal short xPlacement;

        internal short yPlacement;

        // advance offset
        internal short xAdvance;

        internal short yAdvance;

        // Index delta to base glyph. If after a glyph there are several anchored glyphs we should know we to find base glyph.
        internal short anchorDelta;

        /// <summary>Construct a non-mark Glyph, retrieving characters from unicode.</summary>
        /// <param name="code">code representation of the glyph in the font file</param>
        /// <param name="width">normalized width of the glyph</param>
        /// <param name="unicode">utf-32 representation of glyph if appears. Correct value is &gt; -1</param>
        public Glyph(int code, int width, int unicode)
            : this(code, width, unicode, null, false) {
        }

        /// <summary>Construct a non-mark Glyph, using the codepoint of the characters as unicode point.</summary>
        /// <param name="code">code representation of the glyph in the font file</param>
        /// <param name="width">normalized width of the glyph</param>
        /// <param name="chars">The Unicode text represented by this Glyph.</param>
        public Glyph(int code, int width, char[] chars)
            : this(code, width, CodePoint(chars), chars, false) {
        }

        /// <summary>Construct a non-mark Glyph, retrieving characters from unicode.</summary>
        /// <param name="code">code representation of the glyph in the font file</param>
        /// <param name="width">normalized width of the glyph</param>
        /// <param name="unicode">utf-32 representation of glyph if appears. Correct value is &gt; -1</param>
        /// <param name="bbox">The normalized bounding box of this Glyph.</param>
        public Glyph(int code, int width, int unicode, int[] bbox)
            : this(code, width, unicode, null, false) {
            this.bbox = bbox;
        }

        /// <summary>Construct a non-mark Glyph object with id -1 and characters retrieved from unicode.</summary>
        /// <param name="width">normalized width of the glyph</param>
        /// <param name="unicode">utf-32 representation of glyph if appears. Correct value is &gt; -1</param>
        public Glyph(int width, int unicode)
            : this(-1, width, unicode, GetChars(unicode), false) {
        }

        /// <summary>Construct a glyph object form the passed arguments.</summary>
        /// <param name="code">code representation of the glyph in the font file</param>
        /// <param name="width">normalized width of the glyph</param>
        /// <param name="unicode">utf-32 representation of glyph if appears. Correct value is &gt; -1</param>
        /// <param name="chars">
        /// The Unicode text represented by this Glyph.
        /// if null is passed, the unicode value is used to retrieve the chars.
        /// </param>
        /// <param name="IsMark">True if the glyph is a Mark</param>
        public Glyph(int code, int width, int unicode, char[] chars, bool IsMark) {
            this.code = code;
            this.width = width;
            this.unicode = unicode;
            isMark = IsMark;
            this.chars = chars != null ? chars : GetChars(unicode);
        }

        /// <summary>Copy a Glyph.</summary>
        /// <param name="glyph">Glyph to copy</param>
        public Glyph(Glyph glyph) {
            code = glyph.code;
            width = glyph.width;
            chars = glyph.chars;
            unicode = glyph.unicode;
            isMark = glyph.isMark;
            bbox = glyph.bbox;
            xPlacement = glyph.xPlacement;
            yPlacement = glyph.yPlacement;
            xAdvance = glyph.xAdvance;
            yAdvance = glyph.yAdvance;
            anchorDelta = glyph.anchorDelta;
        }

        /// <summary>Copy a Glyph and assign new placement and advance offsets and a new index delta to base glyph</summary>
        /// <param name="glyph">Glyph to copy</param>
        /// <param name="xPlacement">x - placement offset</param>
        /// <param name="yPlacement">y - placement offset</param>
        /// <param name="xAdvance">x - advance offset</param>
        /// <param name="yAdvance">y - advance offset</param>
        /// <param name="anchorDelta">Index delta to base glyph. If after a glyph there are several anchored glyphs we should know we to find base glyph.
        ///     </param>
        public Glyph(Glyph glyph, int xPlacement, int yPlacement, int xAdvance, int yAdvance, int
             anchorDelta)
            : this(glyph) {
            this.xPlacement = (short)xPlacement;
            this.yPlacement = (short)yPlacement;
            this.xAdvance = (short)xAdvance;
            this.yAdvance = (short)yAdvance;
            this.anchorDelta = (short)anchorDelta;
        }

        /// <summary>Copy a glyph and assign the copied glyph a new unicode point and characters</summary>
        /// <param name="glyph">glyph to copy</param>
        /// <param name="unicode">new unicode point</param>
        public Glyph(Glyph glyph, int unicode)
            : this(glyph.code, glyph.width, unicode, GetChars(unicode), glyph.IsMark()) {
        }

        public virtual int GetCode() {
            return code;
        }

        public virtual int GetWidth() {
            return width;
        }

        public virtual int[] GetBbox() {
            return bbox;
        }

        public virtual bool HasValidUnicode() {
            return unicode > -1;
        }

        public virtual int GetUnicode() {
            return unicode;
        }

        public virtual void SetUnicode(int unicode) {
            this.unicode = unicode;
            chars = GetChars(unicode);
        }

        public virtual char[] GetChars() {
            return chars;
        }

        public virtual void SetChars(char[] chars) {
            this.chars = chars;
        }

        public virtual bool IsMark() {
            return isMark;
        }

        public virtual short GetXPlacement() {
            return xPlacement;
        }

        public virtual void SetXPlacement(short xPlacement) {
            this.xPlacement = xPlacement;
        }

        public virtual short GetYPlacement() {
            return yPlacement;
        }

        public virtual void SetYPlacement(short yPlacement) {
            this.yPlacement = yPlacement;
        }

        public virtual short GetXAdvance() {
            return xAdvance;
        }

        public virtual void SetXAdvance(short xAdvance) {
            this.xAdvance = xAdvance;
        }

        public virtual short GetYAdvance() {
            return yAdvance;
        }

        public virtual void SetYAdvance(short yAdvance) {
            this.yAdvance = yAdvance;
        }

        public virtual short GetAnchorDelta() {
            return anchorDelta;
        }

        public virtual void SetAnchorDelta(short anchorDelta) {
            this.anchorDelta = anchorDelta;
        }

        public virtual bool HasOffsets() {
            return HasAdvance() || HasPlacement();
        }

        // In case some of placement values are not zero we always expect anchorDelta to be non-zero
        public virtual bool HasPlacement() {
            return anchorDelta != 0;
        }

        public virtual bool HasAdvance() {
            return xAdvance != 0 || yAdvance != 0;
        }

        public override int GetHashCode() {
            var prime = 31;
            var result = 1;
            result = prime * result + ((chars == null) ? 0 : JavaUtil.ArraysHashCode(chars));
            result = prime * result + code;
            result = prime * result + width;
            return result;
        }

        /// <summary>Two Glyphs are equal if their unicode characters, code and normalized width are equal.</summary>
        /// <param name="obj">The object</param>
        /// <returns>True if this equals obj cast to Glyph, false otherwise.</returns>
        public override bool Equals(object obj) {
            if (this == obj) {
                return true;
            }
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            var other = (Glyph)obj;
            return JavaUtil.ArraysEquals(chars, other.chars) && code == other.code && width == other.width;
        }

        /// <summary>Gets a Unicode string corresponding to this glyph.</summary>
        /// <remarks>
        /// Gets a Unicode string corresponding to this glyph. In general case it might consist of many characters.
        /// If this glyph does not have a valid unicode (
        /// <see cref="HasValidUnicode()"/>
        /// ), then a string consisting of a special
        /// Unicode '\ufffd' character is returned.
        /// </remarks>
        /// <returns>the Unicode string that corresponds to this glyph</returns>
        public virtual string GetUnicodeString()
        {
	        if (chars != null) {
                return JavaUtil.GetStringForChars(chars);
            }

	        return REPLACEMENT_CHARACTER_STRING;
        }

        /// <summary>Gets Unicode char sequence corresponding to this glyph.</summary>
        /// <remarks>
        /// Gets Unicode char sequence corresponding to this glyph. In general case it might consist of many characters.
        /// If this glyph does not have a valid unicode (
        /// <see cref="HasValidUnicode()"/>
        /// ), then a special
        /// Unicode '\ufffd' character is returned.
        /// </remarks>
        /// <returns>the Unicode char sequence that corresponds to this glyph</returns>
        public virtual char[] GetUnicodeChars()
        {
	        if (chars != null) {
                return chars;
            }

	        return REPLACEMENT_CHARACTERS;
        }

        public override string ToString() {
            return MessageFormatUtil.Format("[id={0}, chars={1}, uni={2}, width={3}]", ToHex(code), chars != null ? JavaUtil.ArraysToString
                (chars) : "null", ToHex(unicode), width);
        }

        private static string ToHex(int ch) {
            var s = "0000" + JavaUtil.IntegerToHexString(ch);
            return s.Substring(Math.Min(4, s.Length - 4));
        }

        private static int CodePoint(char[] a) {
            if (a != null)
            {
	            if (a.Length == 1 && JavaUtil.IsValidCodePoint(a[0])) {
                    return a[0];
                }

	            if (a.Length == 2 && char.IsHighSurrogate(a[0]) && char.IsLowSurrogate(a[1])) {
		            return JavaUtil.ToCodePoint(a[0], a[1]);
	            }
            }
            return -1;
        }

        private static char[] GetChars(int unicode) {
            return unicode > -1 ? TextUtil.ConvertFromUtf32(unicode) : null;
        }
    }
}
