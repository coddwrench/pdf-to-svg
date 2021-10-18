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

namespace  IText.IO.Font.Cmap {
    /// <author>psoares</author>
    public abstract class AbstractCMap {
        private string cmapName;

        private string registry;

        private string ordering;

        private int supplement;

        public virtual string GetName() {
            return cmapName;
        }

        internal virtual void SetName(string cmapName) {
            this.cmapName = cmapName;
        }

        public virtual string GetOrdering() {
            return ordering;
        }

        internal virtual void SetOrdering(string ordering) {
            this.ordering = ordering;
        }

        public virtual string GetRegistry() {
            return registry;
        }

        internal virtual void SetRegistry(string registry) {
            this.registry = registry;
        }

        public virtual int GetSupplement() {
            return supplement;
        }

        internal virtual void SetSupplement(int supplement) {
            this.supplement = supplement;
        }

        internal abstract void AddChar(string mark, CMapObject code);

        internal virtual void AddCodeSpaceRange(byte[] low, byte[] high) {
        }

        internal virtual void AddRange(string from, string to, CMapObject code) {
            var a1 = DecodeStringToByte(from);
            var a2 = DecodeStringToByte(to);
            if (a1.Length != a2.Length || a1.Length == 0) {
                throw new ArgumentException("Invalid map.");
            }
            byte[] sout = null;
            if (code.IsString()) {
                sout = DecodeStringToByte(code.ToString());
            }
            var start = ByteArrayToInt(a1);
            var end = ByteArrayToInt(a2);
            for (var k = start; k <= end; ++k) {
                IntToByteArray(k, a1);
                var mark = PdfEncodings.ConvertToString(a1, null);
                if (code.IsArray()) {
                    IList<CMapObject> codes = (List<CMapObject>)code.GetValue();
                    AddChar(mark, codes[k - start]);
                }
                else {
                    if (code.IsNumber()) {
                        var nn = (int)code.GetValue() + k - start;
                        AddChar(mark, new CMapObject(CMapObject.NUMBER, nn));
                    }
                    else {
                        if (code.IsString()) {
                            var s1 = new CMapObject(CMapObject.HEX_STRING, sout);
                            AddChar(mark, s1);
                            Debug.Assert(sout != null);
                            IntToByteArray(ByteArrayToInt(sout) + 1, sout);
                        }
                    }
                }
            }
        }

        //    protected static byte[] toByteArray(String value) {
        //        if (PdfEncodings.isPdfDocEncoding(value)) {
        //            return PdfEncodings.convertToBytes(value, PdfEncodings.PDF_DOC_ENCODING);
        //        } else {
        //            return PdfEncodings.convertToBytes(value, null);
        //        }
        //    }
        public static byte[] DecodeStringToByte(string range) {
            var bytes = new byte[range.Length];
            for (var i = 0; i < range.Length; i++) {
                bytes[i] = (byte)range[i];
            }
            return bytes;
        }

        protected internal virtual string ToUnicodeString(string value, bool isHexWriting) {
            var bytes = DecodeStringToByte(value);
            if (isHexWriting) {
                return PdfEncodings.ConvertToString(bytes, PdfEncodings.UNICODE_BIG_UNMARKED);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xfe && bytes[1] == 0xff) {
	            return PdfEncodings.ConvertToString(bytes, PdfEncodings.UNICODE_BIG);
            }

            return PdfEncodings.ConvertToString(bytes, PdfEncodings.PDF_DOC_ENCODING);
        }

        private static void IntToByteArray(int n, byte[] b) {
            for (var k = b.Length - 1; k >= 0; --k) {
                b[k] = (byte)n;
                n = (int)(((uint)n) >> 8);
            }
        }

        private static int ByteArrayToInt(byte[] b) {
            var n = 0;
            for (var k = 0; k < b.Length; ++k) {
                n = n << 8;
                n |= b[k] & 0xff;
            }
            return n;
        }
    }
}
