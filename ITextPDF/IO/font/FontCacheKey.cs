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

using IText.IO.Util;

namespace  IText.IO.Font {
    public abstract class FontCacheKey {
        public static FontCacheKey Create(string fontName) {
            return new FontCacheStringKey(fontName);
        }

        public static FontCacheKey Create(string fontName, int ttcIndex) {
            return new FontCacheTtcKey(fontName, ttcIndex);
        }

        public static FontCacheKey Create(byte[] fontProgram) {
            return new FontCacheBytesKey(fontProgram);
        }

        public static FontCacheKey Create(byte[] fontProgram, int ttcIndex) {
            return new FontCacheTtcKey(fontProgram, ttcIndex);
        }

        private class FontCacheStringKey : FontCacheKey {
            private string fontName;

            internal FontCacheStringKey(string fontName) {
                this.fontName = fontName;
            }

            public override bool Equals(object o) {
                if (this == o) {
                    return true;
                }
                if (o == null || GetType() != o.GetType()) {
                    return false;
                }
                var that = (FontCacheStringKey)o;
                return fontName != null ? fontName.Equals(that.fontName) : that.fontName == null;
            }

            public override int GetHashCode() {
                return fontName != null ? fontName.GetHashCode() : 0;
            }
        }

        private class FontCacheBytesKey : FontCacheKey {
            private byte[] firstFontBytes;

            private int fontLength;

            private int hashcode;

            internal FontCacheBytesKey(byte[] fontBytes) {
                if (fontBytes != null) {
                    var maxBytesNum = 10000;
                    firstFontBytes = fontBytes.Length > maxBytesNum ? JavaUtil.ArraysCopyOf(fontBytes, maxBytesNum) : fontBytes;
                    fontLength = fontBytes.Length;
                }
                hashcode = CalcHashCode();
            }

            public override bool Equals(object o) {
                if (this == o) {
                    return true;
                }
                if (o == null || GetType() != o.GetType()) {
                    return false;
                }
                var that = (FontCacheBytesKey)o;
                if (fontLength != that.fontLength) {
                    return false;
                }
                return JavaUtil.ArraysEquals(firstFontBytes, that.firstFontBytes);
            }

            public override int GetHashCode() {
                return hashcode;
            }

            private int CalcHashCode() {
                var result = JavaUtil.ArraysHashCode(firstFontBytes);
                result = 31 * result + fontLength;
                return result;
            }
        }

        private class FontCacheTtcKey : FontCacheKey {
            private FontCacheKey ttcKey;

            private int ttcIndex;

            internal FontCacheTtcKey(string fontName, int ttcIndex) {
                ttcKey = new FontCacheStringKey(fontName);
                this.ttcIndex = ttcIndex;
            }

            internal FontCacheTtcKey(byte[] fontBytes, int ttcIndex) {
                ttcKey = new FontCacheBytesKey(fontBytes);
                this.ttcIndex = ttcIndex;
            }

            public override bool Equals(object o) {
                if (this == o) {
                    return true;
                }
                if (o == null || GetType() != o.GetType()) {
                    return false;
                }
                var that = (FontCacheTtcKey)o;
                if (ttcIndex != that.ttcIndex) {
                    return false;
                }
                return ttcKey.Equals(that.ttcKey);
            }

            public override int GetHashCode() {
                var result = ttcKey.GetHashCode();
                result = 31 * result + ttcIndex;
                return result;
            }
        }
    }
}
