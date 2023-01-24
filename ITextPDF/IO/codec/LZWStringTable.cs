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

namespace  IText.IO.Codec {
    /// <summary>General purpose LZW String Table.</summary>
    /// <remarks>
    /// General purpose LZW String Table.
    /// Extracted from GIFEncoder by Adam Doppelt
    /// Comments added by Robin Luiten
    /// <c>expandCode</c> added by Robin Luiten
    /// The strLen_ table to give quick access to the lenght of an expanded
    /// code for use by the <c>expandCode</c> method added by Robin.
    /// </remarks>
    public class LzwStringTable {
        /// <summary>codesize + Reserved Codes</summary>
        private const int ResCodes = 2;

        //0xFFFF
        private const short HashFree = -1;

        //0xFFFF
        private const short NextFirst = -1;

        private const int Maxbits = 12;

        private const int Maxstr = (1 << Maxbits);

        private const short Hashsize = 9973;

        private const short Hashstep = 2039;

        // after predecessor character
        internal byte[] StrChr;

        // predecessor string
        internal short[] StrNxt;

        // hash table to find  predecessor + char pairs
        internal short[] StrHsh;

        // next code if adding new prestring + char
        internal short NumStrings;

        /// <summary>
        /// each entry corresponds to a code and contains the length of data
        /// that the code expands to when decoded.
        /// </summary>
        internal int[] StrLen;

        /// <summary>Constructor allocate memory for string store data</summary>
        public LzwStringTable() {
            StrChr = new byte[Maxstr];
            StrNxt = new short[Maxstr];
            StrLen = new int[Maxstr];
            StrHsh = new short[Hashsize];
        }

        /// <param name="index">value of -1 indicates no predecessor [used in initialization]</param>
        /// <param name="b">
        /// the byte [character] to add to the string store which follows
        /// the predecessor string specified the index.
        /// </param>
        /// <returns>
        /// 0xFFFF if no space in table left for addition of predecessor
        /// index and byte b. Else return the code allocated for combination index + b.
        /// </returns>
        public virtual int AddCharString(short index, byte b) {
            int hshidx;
            // if used up all codes
            if (NumStrings >= Maxstr) {
                return 0xFFFF;
            }
            hshidx = Hash(index, b);
            while (StrHsh[hshidx] != HashFree) {
                hshidx = (hshidx + Hashstep) % Hashsize;
            }
            StrHsh[hshidx] = NumStrings;
            StrChr[NumStrings] = b;
            if (index == HashFree) {
                StrNxt[NumStrings] = NextFirst;
                StrLen[NumStrings] = 1;
            }
            else {
                StrNxt[NumStrings] = index;
                StrLen[NumStrings] = StrLen[index] + 1;
            }
            // return the code and inc for next code
            return NumStrings++;
        }

        /// <param name="index">index to prefix string</param>
        /// <param name="b">the character that follws the index prefix</param>
        /// <returns>
        /// b if param index is HASH_FREE. Else return the code
        /// for this prefix and byte successor
        /// </returns>
        public virtual short FindCharString(short index, byte b) {
            int hshidx;
            int nxtidx;
            if (index == HashFree) {
                // Rob fixed used to sign extend
                return (short)(b & 0xFF);
            }
            hshidx = Hash(index, b);
            // search
            while ((nxtidx = StrHsh[hshidx]) != HashFree) {
                if (StrNxt[nxtidx] == index && StrChr[nxtidx] == b) {
                    return (short)nxtidx;
                }
                hshidx = (hshidx + Hashstep) % Hashsize;
            }
            //return (short) 0xFFFF;
            return -1;
        }

        /// <param name="codesize">
        /// the size of code to be preallocated for the
        /// string store.
        /// </param>
        public virtual void ClearTable(int codesize) {
            NumStrings = 0;
            for (var q = 0; q < Hashsize; q++) {
                StrHsh[q] = HashFree;
            }
            var w = (1 << codesize) + ResCodes;
            for (var q = 0; q < w; q++) {
                // init with no prefix
                AddCharString(-1, (byte)q);
            }
        }

        public static int Hash(short index, byte lastbyte) {
            return (((short)(lastbyte << 8) ^ index) & 0xFFFF) % Hashsize;
        }

        /// <summary>
        /// If expanded data doesn't fit into array only what will fit is written
        /// to buf and the return value indicates how much of the expanded code has
        /// been written to the buf.
        /// </summary>
        /// <remarks>
        /// If expanded data doesn't fit into array only what will fit is written
        /// to buf and the return value indicates how much of the expanded code has
        /// been written to the buf. The next call to expandCode() should be with
        /// the same code and have the skip parameter set the negated value of the
        /// previous return. Successive negative return values should be negated and
        /// added together for next skip parameter value with same code.
        /// </remarks>
        /// <param name="buf">buffer to place expanded data into</param>
        /// <param name="offset">offset to place expanded data</param>
        /// <param name="code">
        /// the code to expand to the byte array it represents.
        /// PRECONDITION This code must already be in the LZSS
        /// </param>
        /// <param name="skipHead">
        /// is the number of bytes at the start of the expanded code to
        /// be skipped before data is written to buf. It is possible that skipHead is
        /// equal to codeLen.
        /// </param>
        /// <returns>
        /// the length of data expanded into buf. If the expanded code is longer
        /// than space left in buf then the value returned is a negative number which when
        /// negated is equal to the number of bytes that were used of the code being expanded.
        /// This negative value also indicates the buffer is full.
        /// </returns>
        public virtual int ExpandCode(byte[] buf, int offset, short code, int skipHead) {
            if (offset == -2) {
                if (skipHead == 1) {
                    skipHead = 0;
                }
            }
            // code == -1 is checked just in case.
            //-1 ~ 0xFFFF
            if (code == -1 || 
                        // DONE no more unpacked
                        skipHead == StrLen[code]) {
                return 0;
            }
            // how much data we are actually expanding
            int expandLen;
            // length of expanded code left
            var codeLen = StrLen[code] - skipHead;
            // how much space left
            var bufSpace = buf.Length - offset;
            if (bufSpace > codeLen) {
                // only got this many to unpack
                expandLen = codeLen;
            }
            else {
                expandLen = bufSpace;
            }
            // only > 0 if codeLen > bufSpace [left overs]
            var skipTail = codeLen - expandLen;
            // initialise to exclusive end address of buffer area
            var idx = offset + expandLen;
            // NOTE: data unpacks in reverse direction and we are placing the
            // unpacked data directly into the array in the correct location.
            while ((idx > offset) && (code != -1)) {
                // skip required of expanded data
                if (--skipTail < 0) {
                    buf[--idx] = StrChr[code];
                }
                // to predecessor code
                code = StrNxt[code];
            }
            if (codeLen > expandLen) {
                // indicate what part of codeLen used
                return -expandLen;
            }

            // indicate length of dat unpacked
            return expandLen;
        }

        public virtual void Dump(FormattingStreamWriter output) {
            int i;
            for (i = 258; i < NumStrings; ++i) {
                output.WriteLine(" strNxt_[" + i + "] = " + StrNxt[i] + " strChr_ " + JavaUtil.IntegerToHexString(StrChr
                    [i] & 0xFF) + " strLen_ " + JavaUtil.IntegerToHexString(StrLen[i]));
            }
        }
    }
}
