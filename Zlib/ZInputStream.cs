/*
Copyright (c) 2001 Lapo Luchini.

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
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS
OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
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
/* This file is a port of jzlib v1.0.7, com.jcraft.jzlib.ZInputStream.java
 */

using System;
using System.Diagnostics;
using System.IO;

namespace Zlib
{
    internal class ZInputStream
        : Stream
    {
        private const int BufferSize = 512;

        protected ZStream z = new ZStream();

        protected FlushLevel flushLevel = FlushLevel.NoFlush;

        // TODO Allow custom buf
        protected byte[] buf = new byte[BufferSize];
        protected byte[] buf1 = new byte[1];
        protected bool compress;

        protected Stream input;
        protected bool closed;

        private bool nomoreinput;

        public ZInputStream(Stream input)
            : this(input, false)
        {
        }

        public ZInputStream(Stream input, bool nowrap)
        {
            Debug.Assert(input.CanRead);

            this.input = input;
            z.InflateInit(nowrap);
            compress = false;
            z.NextIn = buf;
            z.NextInIndex = 0;
            z.AvailIn = 0;
        }

        public ZInputStream(Stream input, int level)
        {
            Debug.Assert(input.CanRead);

            this.input = input;
            z.DeflateInit(level);
            compress = true;
            z.NextIn = buf;
            z.NextInIndex = 0;
            z.AvailIn = 0;
        }

        /*public int available() throws IOException {
        return inf.finished() ? 0 : 1;
        }*/

        public sealed override bool CanRead => !closed;

        public sealed override bool CanSeek => false;

        public sealed override bool CanWrite => false;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!closed)
                {
                    closed = true;
                    input.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public sealed override void Flush()
        {
        }

        public virtual FlushLevel FlushMode
        {
            get => flushLevel;
            set => flushLevel = value;
        }

        public sealed override long Length => throw new NotSupportedException();

        public sealed override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] b, int off, int len)
        {
            if (len == 0)
                return 0;

            z.NextOut = b;
            z.NextOutIndex = off;
            z.AvailOut = len;

            ZStreamState err;
            do
            {
                if (z.AvailIn == 0 && !nomoreinput)
                {
                    // if buffer is empty and more input is available, refill it
                    z.NextInIndex = 0;
                    z.AvailIn = input.Read(buf, 0, buf.Length); //(bufsize<z.avail_out ? bufsize : z.avail_out));

                    if (z.AvailIn <= 0)
                    {
                        z.AvailIn = 0;
                        nomoreinput = true;
                    }
                }

                err = compress
                    ? z.Deflate(flushLevel)
                    : z.Inflate(flushLevel);

                if (nomoreinput && err == ZStreamState.BufError)
                    return 0;
                if (err != ZStreamState.Ok &&  err != ZStreamState.StreamEnd)
                    // TODO
//					throw new ZStreamException((compress ? "de" : "in") + "flating: " + z.msg);
                    throw new IOException((compress ? "de" : "in") + "flating: " + z.Msg);
                if ((nomoreinput ||  err == ZStreamState.StreamEnd) && z.AvailOut == len)
                    return 0;
            } while (z.AvailOut == len && err == ZStreamState.Ok);

            //Console.Error.WriteLine("("+(len-z.avail_out)+")");
            return len - z.AvailOut;
        }

        public override int ReadByte()
        {
            if (Read(buf1, 0, 1) <= 0)
                return -1;
            return buf1[0];
        }


        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public sealed override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public virtual long TotalIn => z.TotalIn;

        public virtual long TotalOut => z.TotalOut;

        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
