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
using System.Globalization;
using IText.IO.Util;

namespace  IText.IO.Font {
    public class FontEncoding {
        private static readonly byte[] emptyBytes = new byte[0];

        public const string FONT_SPECIFIC = "FontSpecific";

        /// <summary>A not defined character in a custom PDF encoding.</summary>
        public const string NOTDEF = ".notdef";

        /// <summary>Base font encoding.</summary>
        protected internal string baseEncoding;

        /// <summary>
        /// <see langword="true"/>
        /// if the font must use its built in encoding.
        /// </summary>
        /// <remarks>
        /// <see langword="true"/>
        /// if the font must use its built in encoding. In that case
        /// the
        /// <c>encoding</c>
        /// is only used to map a char to the position inside the font, not to the expected char name.
        /// </remarks>
        protected internal bool fontSpecific;

        /// <summary>Mapping map from unicode to simple code according to the encoding.</summary>
        protected internal IntHashtable unicodeToCode;

        protected internal int[] codeToUnicode;

        /// <summary>Encoding names.</summary>
        protected internal string[] differences;

        /// <summary>Encodings unicode differences</summary>
        protected internal IntHashtable unicodeDifferences;

        protected internal FontEncoding() {
            unicodeToCode = new IntHashtable(256);
            codeToUnicode = ArrayUtil.FillWithValue(new int[256], -1);
            unicodeDifferences = new IntHashtable(256);
            fontSpecific = false;
        }

        public static FontEncoding CreateFontEncoding(string baseEncoding) {
            var encoding = new FontEncoding();
            encoding.baseEncoding = NormalizeEncoding(baseEncoding);
            if (encoding.baseEncoding.StartsWith("#")) {
                encoding.FillCustomEncoding();
            }
            else {
                encoding.FillNamedEncoding();
            }
            return encoding;
        }

        public static FontEncoding CreateEmptyFontEncoding() {
            var encoding = new FontEncoding();
            encoding.baseEncoding = null;
            encoding.fontSpecific = false;
            encoding.differences = new string[256];
            for (var ch = 0; ch < 256; ch++) {
                encoding.unicodeDifferences.Put(ch, ch);
            }
            return encoding;
        }

        /// <summary>This encoding will base on font encoding (FontSpecific encoding in Type 1 terminology)</summary>
        /// <returns>created font specific encoding</returns>
        public static FontEncoding CreateFontSpecificEncoding() {
            var encoding = new FontEncoding();
            encoding.fontSpecific = true;
            for (var ch = 0; ch < 256; ch++) {
                encoding.unicodeToCode.Put(ch, ch);
                encoding.codeToUnicode[ch] = ch;
                encoding.unicodeDifferences.Put(ch, ch);
            }
            return encoding;
        }

        public virtual string GetBaseEncoding() {
            return baseEncoding;
        }

        public virtual bool IsFontSpecific() {
            return fontSpecific;
        }

        public virtual bool AddSymbol(int code, int unicode) {
            if (code < 0 || code > 255) {
                return false;
            }
            var glyphName = AdobeGlyphList.UnicodeToName(unicode);
            if (glyphName != null) {
                unicodeToCode.Put(unicode, code);
                codeToUnicode[code] = unicode;
                differences[code] = glyphName;
                unicodeDifferences.Put(unicode, unicode);
                return true;
            }

            return false;
        }

        /// <summary>Gets unicode value for corresponding font's char code.</summary>
        /// <param name="index">font's char code</param>
        /// <returns>-1, if the char code unsupported or valid unicode.</returns>
        public virtual int GetUnicode(int index) {
            return codeToUnicode[index];
        }

        public virtual int GetUnicodeDifference(int index) {
            return unicodeDifferences.Get(index);
        }

        public virtual bool HasDifferences() {
            return differences != null;
        }

        public virtual string GetDifference(int index) {
            return differences != null ? differences[index] : null;
        }

        /// <summary>Sets a new value in the differences array.</summary>
        /// <remarks>
        /// Sets a new value in the differences array.
        /// See
        /// <see cref="differences"/>.
        /// </remarks>
        /// <param name="index">position to replace</param>
        /// <param name="difference">new difference value</param>
        public virtual void SetDifference(int index, string difference) {
            if (index >= 0 && differences != null && index < differences.Length) {
                differences[index] = difference;
            }
        }

        /// <summary>
        /// Converts a
        /// <c>String</c>
        /// to a
        /// <c>byte</c>
        /// array according to the encoding.
        /// </summary>
        /// <remarks>
        /// Converts a
        /// <c>String</c>
        /// to a
        /// <c>byte</c>
        /// array according to the encoding.
        /// String could contain a unicode symbols or font specific codes.
        /// </remarks>
        /// <param name="text">
        /// the
        /// <c>String</c>
        /// to be converted.
        /// </param>
        /// <returns>
        /// an array of
        /// <c>byte</c>
        /// representing the conversion according to the encoding
        /// </returns>
        public virtual byte[] ConvertToBytes(string text) {
            if (text == null || text.Length == 0) {
                return emptyBytes;
            }
            var ptr = 0;
            var bytes = new byte[text.Length];
            for (var i = 0; i < text.Length; i++) {
                if (unicodeToCode.ContainsKey(text[i])) {
                    bytes[ptr++] = (byte)ConvertToByte(text[i]);
                }
            }
            return ArrayUtil.ShortenArray(bytes, ptr);
        }

        /// <summary>
        /// Converts a unicode symbol or font specific code
        /// to
        /// <c>byte</c>
        /// according to the encoding.
        /// </summary>
        /// <param name="unicode">a unicode symbol or FontSpecif code to be converted.</param>
        /// <returns>
        /// a
        /// <c>byte</c>
        /// representing the conversion according to the encoding
        /// </returns>
        public virtual int ConvertToByte(int unicode) {
            return unicodeToCode.Get(unicode);
        }

        /// <summary>
        /// Check whether a unicode symbol or font specific code can be converted
        /// to
        /// <c>byte</c>
        /// according to the encoding.
        /// </summary>
        /// <param name="unicode">a unicode symbol or font specific code to be checked.</param>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// if
        /// <c>ch</c>
        /// could be encoded.
        /// </returns>
        public virtual bool CanEncode(int unicode) {
            return unicodeToCode.ContainsKey(unicode) || TextUtil.IsNonPrintable(unicode) || TextUtil
                .IsNewLine(unicode);
        }

        /// <summary>
        /// Check whether a
        /// <c>byte</c>
        /// code can be converted
        /// to unicode symbol according to the encoding.
        /// </summary>
        /// <param name="code">a byte code to be checked.</param>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// if
        /// <paramref name="code"/>
        /// could be decoded.
        /// </returns>
        public virtual bool CanDecode(int code) {
            return codeToUnicode[code] > -1;
        }

        /// <summary>
        /// Checks whether the
        /// <see cref="FontEncoding"/>
        /// was built with corresponding encoding.
        /// </summary>
        /// <param name="encoding">an encoding</param>
        /// <returns>true, if the FontEncoding was built with the encoding. Otherwise false.</returns>
        public virtual bool IsBuiltWith(string encoding) {
            return Equals(NormalizeEncoding(encoding), baseEncoding);
        }

        protected internal virtual void FillCustomEncoding() {
            differences = new string[256];
            var tok = new StringTokenizer(baseEncoding.Substring(1), " ,\t\n\r\f");
            if (tok.NextToken().Equals("full")) {
                while (tok.HasMoreTokens()) {
                    var order = tok.NextToken();
                    var name = tok.NextToken();
                    var uni = (char)Convert.ToInt32(tok.NextToken(), 16);
                    var uniName = AdobeGlyphList.NameToUnicode(name);
                    int orderK;
                    if (order.StartsWith("'")) {
                        orderK = order[1];
                    }
                    else {
                        orderK = Convert.ToInt32(order, CultureInfo.InvariantCulture);
                    }
                    orderK %= 256;
                    unicodeToCode.Put(uni, orderK);
                    codeToUnicode[orderK] = uni;
                    differences[orderK] = name;
                    unicodeDifferences.Put(uni, uniName);
                }
            }
            else {
                var k = 0;
                if (tok.HasMoreTokens()) {
                    k = Convert.ToInt32(tok.NextToken(), CultureInfo.InvariantCulture);
                }
                while (tok.HasMoreTokens() && k < 256) {
                    var hex = tok.NextToken();
                    var uni = Convert.ToInt32(hex, 16) % 0x10000;
                    var name = AdobeGlyphList.UnicodeToName(uni);
                    if (name == null) {
                        name = "uni" + hex;
                    }
                    unicodeToCode.Put(uni, k);
                    codeToUnicode[k] = uni;
                    differences[k] = name;
                    unicodeDifferences.Put(uni, uni);
                    k++;
                }
            }
            for (var k = 0; k < 256; k++) {
                if (differences[k] == null) {
                    differences[k] = NOTDEF;
                }
            }
        }

        protected internal virtual void FillNamedEncoding() {
            // check if the encoding exists
            PdfEncodings.ConvertToBytes(" ", baseEncoding);
            var stdEncoding = PdfEncodings.WINANSI.Equals(baseEncoding) || PdfEncodings.MACROMAN.Equals(baseEncoding);
            if (!stdEncoding && differences == null) {
                differences = new string[256];
            }
            var b = new byte[256];
            for (var k = 0; k < 256; ++k) {
                b[k] = (byte)k;
            }
            var str = PdfEncodings.ConvertToString(b, baseEncoding);
            var encoded = str.ToCharArray();
            for (var ch = 0; ch < 256; ++ch) {
                var uni = encoded[ch];
                var name = AdobeGlyphList.UnicodeToName(uni);
                if (name == null) {
                    name = NOTDEF;
                }
                else {
                    unicodeToCode.Put(uni, ch);
                    codeToUnicode[ch] = uni;
                    unicodeDifferences.Put(uni, uni);
                }
                if (differences != null) {
                    differences[ch] = name;
                }
            }
        }

        protected internal virtual void FillStandardEncoding() {
            var encoded = PdfEncodings.standardEncoding;
            for (var ch = 0; ch < 256; ++ch) {
                var uni = encoded[ch];
                var name = AdobeGlyphList.UnicodeToName(uni);
                if (name == null) {
                    name = NOTDEF;
                }
                else {
                    unicodeToCode.Put(uni, ch);
                    codeToUnicode[ch] = uni;
                    unicodeDifferences.Put(uni, uni);
                }
                if (differences != null) {
                    differences[ch] = name;
                }
            }
        }

        /// <summary>Normalize the encoding names.</summary>
        /// <remarks>
        /// Normalize the encoding names. "winansi" is changed to "Cp1252" and
        /// "macroman" is changed to "MacRoman".
        /// </remarks>
        /// <param name="enc">the encoding to be normalized</param>
        /// <returns>the normalized encoding</returns>
        protected internal static string NormalizeEncoding(string enc) {
            var tmp = enc == null ? "" : enc.ToLowerInvariant();
            switch (tmp) {
                case "":
                case "winansi":
                case "winansiencoding": {
                    return PdfEncodings.WINANSI;
                }

                case "macroman":
                case "macromanencoding": {
                    return PdfEncodings.MACROMAN;
                }

                case "zapfdingbatsencoding": {
                    return PdfEncodings.ZAPFDINGBATS;
                }

                default: {
                    return enc;
                }
            }
        }
    }
}
