
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

using System;

namespace Zlib
{
    public sealed class ZStream
    {

        private const int DefWBits = Utils.MaxWBits;

        public byte[] NextIn; // next input byte
        public int NextInIndex;
        public int AvailIn; // number of bytes available at next_in
        public long TotalIn; // total nb of input bytes read so far

        public byte[] NextOut; // next output byte should be put there
        public int NextOutIndex;
        public int AvailOut; // remaining free space at next_out
        public long TotalOut; // total nb of bytes output so far

        public string Msg;

        internal Deflate DeflateState;
        internal Inflate InflateState;

        internal int DataType; // best guess about the data type: ascii or binary

        public long Adler;

        public int InflateInit()
        {
            return InflateInit(DefWBits);
        }

        public int InflateInit(bool nowrap)
        {
            return InflateInit(DefWBits, nowrap);
        }

        public int InflateInit(int w)
        {
            return InflateInit(w, false);
        }

        public int InflateInit(int w, bool nowrap)
        {
            InflateState = new Inflate();
            return InflateState.InflateInit(this, nowrap ? -w : w);
        }

        public ZStreamState Inflate(FlushLevel f)
        {
            if (InflateState == null) 
                return ZStreamState.StreamError;
            return InflateState.Inflated(this, f);
        }

        public int InflateEnd()
        {
            if (InflateState == null) return (int) ZStreamState.StreamError;
            var ret = InflateState.InflateEnd(this);
            InflateState = null;
            return ret;
        }

        public int InflateSync()
        {
            if (InflateState == null)
                return (int) ZStreamState.StreamError;
            return InflateState.InflateSync(this);
        }

        public int InflateSetDictionary(byte[] dictionary, int dictLength)
        {
            if (InflateState == null)
                return (int) ZStreamState.StreamError;
            return InflateState.InflateSetDictionary(this, dictionary, dictLength);
        }

        public ZStreamState DeflateInit(int level)
        {
            return DeflateInit(level, Utils.MaxWBits);
        }

        public ZStreamState DeflateInit(int level, bool nowrap)
        {
            return DeflateInit(level, Utils.MaxWBits, nowrap);
        }

        public ZStreamState DeflateInit(int level, int bits)
        {
            return DeflateInit(level, bits, false);
        }

        public ZStreamState DeflateInit(int level, int bits, bool nowrap)
        {
            DeflateState = new Deflate();
            return DeflateState.DeflateInit(this, level, nowrap ? -bits : bits);
        }

        public ZStreamState Deflate(FlushLevel flush)
        {
            if (DeflateState == null)
            {
                return ZStreamState.StreamError;
            }

            return DeflateState.Deflated(this, flush);
        }

        public ZStreamState DeflateEnd()
        {
            if (DeflateState == null) return  ZStreamState.StreamError;
            var ret = DeflateState.DeflateEnd();
            DeflateState = null;
            return ret;
        }

        public ZStreamState DeflateParams(int level, int strategy)
        {
            if (DeflateState == null) return  ZStreamState.StreamError;
            return DeflateState.DeflateParams(this, level, strategy);
        }

        public ZStreamState DeflateSetDictionary(byte[] dictionary, int dictLength)
        {
            if (DeflateState == null)
                return ZStreamState.StreamError;
            return DeflateState.DeflateSetDictionary(this, dictionary, dictLength);
        }

        // Flush as much pending output as possible. All deflate() output goes
        // through this function so some applications may wish to modify it
        // to avoid allocating a large strm->next_out buffer and copying into it.
        // (See also read_buf()).
        internal void flush_pending()
        {
            var len = DeflateState.Pending;

            if (len > AvailOut) len = AvailOut;
            if (len == 0) return;

            Array.Copy(DeflateState.PendingBuf, DeflateState.PendingOut,
                NextOut, NextOutIndex, len);

            NextOutIndex += len;
            DeflateState.PendingOut += len;
            TotalOut += len;
            AvailOut -= len;
            DeflateState.Pending -= len;
            if (DeflateState.Pending == 0)
            {
                DeflateState.PendingOut = 0;
            }
        }

        // Read a new buffer from the current input stream, update the adler32
        // and total number of bytes read.  All deflate() input goes through
        // this function so some applications may wish to modify it to avoid
        // allocating a large strm->next_in buffer and copying from it.
        // (See also flush_pending()).
        internal int read_buf(byte[] buf, int start, int size)
        {
            var len = AvailIn;

            if (len > size) len = size;
            if (len == 0) return 0;

            AvailIn -= len;

            if (DeflateState.Noheader == 0)
            {
                Adler = Utils.Adler32(Adler, NextIn, NextInIndex, len);
            }

            Array.Copy(NextIn, NextInIndex, buf, start, len);
            NextInIndex += len;
            TotalIn += len;
            return len;
        }

        public void Free()
        {
            NextIn = null;
            NextOut = null;
            Msg = null;
        }
    }
}
