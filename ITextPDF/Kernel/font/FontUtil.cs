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
using System.Text;
using IText.IO;
using IText.IO.Font;
using IText.IO.Font.Cmap;
using IText.IO.Util;
using IText.Kernel.Pdf;
using IText.Logger;

namespace IText.Kernel.Font {
    public class FontUtil {
        private static readonly Dictionary<string, CMapToUnicode> uniMaps = new Dictionary<string, CMapToUnicode>(
            );

        public static string AddRandomSubsetPrefixForFontName(string fontName) {
            var newFontName = new StringBuilder(fontName.Length + 7);
            for (var k = 0; k < 6; ++k) {
                newFontName.Append((char)(JavaUtil.Random() * 26 + 'A'));
            }
            newFontName.Append('+').Append(fontName);
            return newFontName.ToString();
        }

        internal static CMapToUnicode ProcessToUnicode(PdfObject toUnicode) {
            CMapToUnicode cMapToUnicode = null;
            if (toUnicode is PdfStream) {
                try {
                    var uniBytes = ((PdfStream)toUnicode).GetBytes();
                    ICMapLocation lb = new CMapLocationFromBytes(uniBytes);
                    cMapToUnicode = new CMapToUnicode();
                    CMapParser.ParseCid("", cMapToUnicode, lb);
                }
                catch (Exception) {
                    var logger = LogManager.GetLogger(typeof(CMapToUnicode));
                    logger.Error(LogMessageConstant.UNKNOWN_ERROR_WHILE_PROCESSING_CMAP);
                    cMapToUnicode = CMapToUnicode.EmptyCMapToUnicodeMap;
                }
            }
            else {
                if (PdfName.IdentityH.Equals(toUnicode)) {
                    cMapToUnicode = CMapToUnicode.GetIdentity();
                }
            }
            return cMapToUnicode;
        }

        internal static CMapToUnicode GetToUnicodeFromUniMap(string uniMap) {
            if (uniMap == null) {
                return null;
            }
            lock (uniMaps) {
                if (uniMaps.Contains(uniMap)) {
                    return uniMaps.Get(uniMap);
                }
                CMapToUnicode toUnicode;
                if (PdfEncodings.IDENTITY_H.Equals(uniMap)) {
                    toUnicode = CMapToUnicode.GetIdentity();
                }
                else {
                    var uni = FontCache.GetUni2CidCmap(uniMap);
                    if (uni == null) {
                        return null;
                    }
                    toUnicode = uni.ExportToUnicode();
                }
                uniMaps.Put(uniMap, toUnicode);
                return toUnicode;
            }
        }

        internal static string CreateRandomFontName() {
            var s = new StringBuilder("");
            for (var k = 0; k < 7; ++k) {
                s.Append((char)(JavaUtil.Random() * 26 + 'A'));
            }
            return s.ToString();
        }

        internal static int[] ConvertSimpleWidthsArray(PdfArray widthsArray, int first, int missingWidth) {
            var res = new int[256];
            JavaUtil.Fill(res, missingWidth);
            if (widthsArray == null) {
                var logger = LogManager.GetLogger(typeof(FontUtil));
                logger.Warn(LogMessageConstant.FONT_DICTIONARY_WITH_NO_WIDTHS);
                return res;
            }
            for (var i = 0; i < widthsArray.Size() && first + i < 256; i++) {
                var number = widthsArray.GetAsNumber(i);
                res[first + i] = number != null ? number.IntValue() : missingWidth;
            }
            return res;
        }

        internal static IntHashtable ConvertCompositeWidthsArray(PdfArray widthsArray) {
            var res = new IntHashtable();
            if (widthsArray == null) {
                return res;
            }
            for (var k = 0; k < widthsArray.Size(); ++k) {
                var c1 = widthsArray.GetAsNumber(k).IntValue();
                var obj = widthsArray.Get(++k);
                if (obj.IsArray()) {
                    var subWidths = (PdfArray)obj;
                    for (var j = 0; j < subWidths.Size(); ++j) {
                        var c2 = subWidths.GetAsNumber(j).IntValue();
                        res.Put(c1++, c2);
                    }
                }
                else {
                    var c2 = ((PdfNumber)obj).IntValue();
                    var w = widthsArray.GetAsNumber(++k).IntValue();
                    for (; c1 <= c2; ++c1) {
                        res.Put(c1, w);
                    }
                }
            }
            return res;
        }
    }
}
