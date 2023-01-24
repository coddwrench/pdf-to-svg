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

using System.IO;

namespace  IText.IO.Codec {
    /// <summary>
    /// Modified from original LZWCompressor to change interface to passing a
    /// buffer of data to be compressed.
    /// </summary>
    public class LzwCompressor {
        /// <summary>base underlying code size of data being compressed 8 for TIFF, 1 to 8 for GIF</summary>
        internal int CodeSize;

        /// <summary>reserved clear code based on code size</summary>
        internal int ClearCode;

        /// <summary>reserved end of data code based on code size</summary>
        internal int EndOfInfo;

        /// <summary>current number bits output for each code</summary>
        internal int NumBits;

        /// <summary>limit at which current number of bits code size has to be increased</summary>
        internal int Limit;

        /// <summary>the prefix code which represents the predecessor string to current input point</summary>
        internal short Prefix;

        /// <summary>output destination for bit codes</summary>
        internal BitFile Bf;

        /// <summary>general purpose LZW string table</summary>
        internal LzwStringTable Lzss;

        /// <summary>modify the limits of the code values in LZW encoding due to TIFF bug / feature</summary>
        internal bool TiffFudge;

        /// <param name="outputStream">destination for compressed data</param>
        /// <param name="codeSize">the initial code size for the LZW compressor</param>
        /// <param name="tiff">flag indicating that TIFF lzw fudge needs to be applied</param>
        public LzwCompressor(Stream outputStream, int codeSize, bool tiff) {
            // set flag for GIF as NOT tiff
            Bf = new BitFile(outputStream, !tiff);
            CodeSize = codeSize;
            TiffFudge = tiff;
            ClearCode = 1 << CodeSize;
            EndOfInfo = ClearCode + 1;
            NumBits = CodeSize + 1;
            Limit = (1 << NumBits) - 1;
            if (TiffFudge) {
                --Limit;
            }
            //0xFFFF
            Prefix = -1;
            Lzss = new LzwStringTable();
            Lzss.ClearTable(CodeSize);
            Bf.WriteBits(ClearCode, NumBits);
        }

        /// <param name="buf">The data to be compressed to output stream</param>
        /// <param name="offset">The offset at which the data starts</param>
        /// <param name="length">The length of the data being compressed</param>
        public virtual void Compress(byte[] buf, int offset, int length) {
            int idx;
            byte c;
            short index;
            var maxOffset = offset + length;
            for (idx = offset; idx < maxOffset; ++idx) {
                c = buf[idx];
                if ((index = Lzss.FindCharString(Prefix, c)) != -1) {
                    Prefix = index;
                }
                else {
                    Bf.WriteBits(Prefix, NumBits);
                    if (Lzss.AddCharString(Prefix, c) > Limit) {
                        if (NumBits == 12) {
                            Bf.WriteBits(ClearCode, NumBits);
                            Lzss.ClearTable(CodeSize);
                            NumBits = CodeSize + 1;
                        }
                        else {
                            ++NumBits;
                        }
                        Limit = (1 << NumBits) - 1;
                        if (TiffFudge) {
                            --Limit;
                        }
                    }
                    Prefix = (short)(c & 0xFF);
                }
            }
        }

        /// <summary>
        /// Indicate to compressor that no more data to go so write out
        /// any remaining buffered data.
        /// </summary>
        public virtual void Flush() {
            if (Prefix != -1) {
                Bf.WriteBits(Prefix, NumBits);
            }
            Bf.WriteBits(EndOfInfo, NumBits);
            Bf.Flush();
        }
    }
}
