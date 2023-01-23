
/*
 *
Copyright (c) 2000,2001,2002,2003 ymnk, JCraft,Inc. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  1. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.

  2. Redistributions in binary form must reproduce the above copyright 
     notice, this list of conditions and the following disclaimer in 
     the documentation and/or other materials provided with the distribution.

  3. The names of the authors may not be used to endorse or promote products
     derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
/*
 * This program is based on zlib-1.1.3, so all credit should go authors
 * Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
 * and contributors of zlib.
 */

namespace Zlib
{

    internal static class Utils
    {

        public const int MaxWBits = 15; // 32K LZ77 window

        public static readonly string[] ErrMsg =
        {
            "need dictionary", // Z_NEED_DICT       2
            "stream end", // Z_STREAM_END      1
            "", // Z_OK              0
            "file error", // Z_ERRNO         (-1)
            "stream error", // Z_STREAM_ERROR  (-2)
            "data error", // Z_DATA_ERROR    (-3)
            "insufficient memory", // Z_MEM_ERROR     (-4)
            "buffer error", // Z_BUF_ERROR     (-5)
            "incompatible version", // Z_VERSION_ERROR (-6)
            ""
        };


        // largest prime smaller than 65536
        private const int Base = 65521;

        // NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1
        private const int Max = 5552;

        internal static long Adler32(long adler, byte[] buf, int index, int len)
        {
            if (buf == null)
            {
                return 1L;
            }

            var s1 = adler & 0xffff;
            var s2 = (adler >> 16) & 0xffff;

            while (len > 0)
            {
                var k = len < Max ? len : Max;
                len -= k;
                while (k >= 16)
                {
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    s1 += buf[index++] & 0xff;
                    s2 += s1;
                    k -= 16;
                }
                if (k != 0)
                {
                    do
                    {
                        s1 += buf[index++] & 0xff;
                        s2 += s1;
                    } while (--k != 0);
                }

                s1 %= Base;
                s2 %= Base;
            }

            return (s2 << 16) | s1;
        }

    }
}