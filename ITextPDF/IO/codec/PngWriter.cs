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
using System.IO;
using IText.IO.Source;

namespace  IText.IO.Codec {
    /// <summary>Writes a PNG image.</summary>
    public class PngWriter {
        private static readonly byte[] PngSignture = { 137, 80, 78, 71, 13, 10, 26, 10 };


        // ReSharper disable once InconsistentNaming once IdentifierTypo
        private static readonly byte[] IHDR = ByteUtils.GetIsoBytes("IHDR");
        // ReSharper disable once InconsistentNaming once IdentifierTypo
        private static readonly byte[] PLTE = ByteUtils.GetIsoBytes("PLTE");
        // ReSharper disable once InconsistentNaming once IdentifierTypo
        private static readonly byte[] IDAT = ByteUtils.GetIsoBytes("IDAT");
        // ReSharper disable once InconsistentNaming once IdentifierTypo
        private static readonly byte[] IEND = ByteUtils.GetIsoBytes("IEND");
        // ReSharper disable once InconsistentNaming once IdentifierTypo
        private static readonly byte[] ICCP = ByteUtils.GetIsoBytes("iCCP");

        private static int[] _crcTable;

        private readonly Stream _out;

         static PngWriter()
         {
             _crcTable = GetCrcTable();
         }

        public PngWriter(Stream @out) {
            _out = @out;
            @out.Write(PngSignture);
        }

        public virtual void WriteHeader(int width, int height, int bitDepth, int colorType) {
            var ms = new MemoryStream();
            OutputInt(width, ms);
            OutputInt(height, ms);
            ms.Write(bitDepth);
            ms.Write(colorType);
            ms.Write(0);
            ms.Write(0);
            ms.Write(0);
            WriteChunk(IHDR, ms.ToArray());
        }

        public virtual void WriteEnd() {
            WriteChunk(IEND, Array.Empty<byte>());
        }

        public virtual void WriteData(byte[] data, int stride) {
            var stream = new MemoryStream();
            var zip = new DeflaterOutputStream(stream);
            int k;
            for (k = 0; k < data.Length - stride; k += stride) {
                zip.Write(0);
                zip.Write(data, k, stride);
            }
            var remaining = data.Length - k;
            if (remaining > 0) {
                zip.Write(0);
                zip.Write(data, k, remaining);
            }
            zip.Dispose();
            WriteChunk(IDAT, stream.ToArray());
        }

        public virtual void WritePalette(byte[] data) {
            WriteChunk(PLTE, data);
        }

        public virtual void WriteIccProfile(byte[] data) {
            var stream = new MemoryStream();
            stream.Write((byte)'I');
            stream.Write((byte)'C');
            stream.Write((byte)'C');
            stream.Write(0);
            stream.Write(0);
            var zip = new DeflaterOutputStream(stream);
            zip.Write(data);
            zip.Dispose();
            WriteChunk(ICCP, stream.ToArray());
        }

        private static int[] GetCrcTable() {
            var crc2 = new int[256];
            for (var n = 0; n < 256; n++) {
                var c = n;
                for (var k = 0; k < 8; k++) {
                    if ((c & 1) != 0) {
                        c = unchecked((int)(0xedb88320)) ^ ((int)(((uint)c) >> 1));
                    }
                    else {
                        c = (int)(((uint)c) >> 1);
                    }
                }
                crc2[n] = c;
            }
            return crc2;
        }

        private static int Update_crc(int crc, byte[] buf, int offset, int len)
        {
            var c = crc;
            for (var n = 0; n < len; n++)
            {
                c = _crcTable[(c ^ buf[n + offset]) & 0xff] ^ ((int) (((uint) c) >> 8));
            }

            return c;
        }

        private static int Crc(byte[] buf, int offset, int len) {
            return ~Update_crc(-1, buf, offset, len);
        }

        private static int Crc(byte[] buf) {
            return ~Update_crc(-1, buf, 0, buf.Length);
        }

        public virtual void OutputInt(int n) {
            OutputInt(n, _out);
        }

        public static void OutputInt(int n, Stream s) {
            s.Write((byte)(n >> 24));
            s.Write((byte)(n >> 16));
            s.Write((byte)(n >> 8));
            s.Write((byte)n);
        }

        public virtual void WriteChunk(byte[] chunkType, byte[] data) {
            OutputInt(data.Length);
            _out.Write(chunkType, 0, 4);
            _out.Write(data);
            var c = Update_crc(-1, chunkType, 0, chunkType.Length);
            c = ~Update_crc(c, data, 0, data.Length);
            OutputInt(c);
        }
    }
}
