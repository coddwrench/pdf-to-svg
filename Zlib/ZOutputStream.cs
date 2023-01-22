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
/* This file is a port of jzlib v1.0.7, com.jcraft.jzlib.ZOutputStream.java
 */

using System;
using System.Diagnostics;
using System.IO;

namespace Zlib
{
    internal class ZOutputStream : Stream
    {
        private const int BufferSize = 512;

        protected ZStream ZStream = new ZStream();

        protected int FlushLevel = JZlib.ZNoFlush;

        // TODO Allow custom buf
        protected byte[] Buf = new byte[BufferSize];
        protected byte[] Buf1 = new byte[1];
        protected bool Compress;

        protected Stream Output;
        protected bool Closed;

        public ZOutputStream(Stream output)
        {
            Debug.Assert(output.CanWrite);

            this.Output = output;
            ZStream.InflateInit();
            Compress = false;
        }

        public ZOutputStream(Stream output, int level)
            : this(output, level, false)
        {
        }

        public ZOutputStream(Stream output, int level, bool nowrap)
        {
            Debug.Assert(output.CanWrite);

            this.Output = output;
            ZStream.DeflateInit(level, nowrap);
            Compress = true;
        }

        public sealed override bool CanRead => false;
        public sealed override bool CanSeek => false;
        public sealed override bool CanWrite => !Closed;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Closed)
                    return;

                try
                {
                    try
                    {
                        Finish();
                    }
                    catch (IOException)
                    {
                        // Ignore
                    }
                }
                finally
                {
                    Closed = true;
                    End();
                    Output.Dispose();
                    Output = null;
                }
            }

            base.Dispose(disposing);
        }

        public virtual void End()
        {
            if (ZStream == null)
                return;
            if (Compress)
                ZStream.DeflateEnd();
            else
                ZStream.InflateEnd();
            ZStream.Free();
            ZStream = null;
        }

        public virtual void Finish()
        {
            do
            {
                ZStream.NextOut = Buf;
                ZStream.NextOutIndex = 0;
                ZStream.AvailOut = Buf.Length;

                var err = Compress
                    ? ZStream.Deflate(JZlib.ZFinish)
                    : ZStream.Inflate(JZlib.ZFinish);

                if (err != ZStreamState.StreamEnd && err != ZStreamState.Ok)
                    // TODO
//					throw new ZStreamException((compress?"de":"in")+"flating: "+z.msg);
                    throw new IOException((Compress ? "de" : "in") + "flating: " + ZStream.Msg);

                var count = Buf.Length - ZStream.AvailOut;
                if (count > 0)
                {
                    Output.Write(Buf, 0, count);
                }
            } while (ZStream.AvailIn > 0 || ZStream.AvailOut == 0);

            Flush();
        }

        public override void Flush()
        {
            Output.Flush();
        }

        public virtual int FlushMode
        {
            get => FlushLevel;
            set => FlushLevel = value;
        }

        public sealed override long Length => throw new NotSupportedException();

        public sealed override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public sealed override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public sealed override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public virtual long TotalIn => ZStream.TotalIn;

        public virtual long TotalOut => ZStream.TotalOut;

        public override void Write(byte[] b, int off, int len)
        {
            if (len == 0)
                return;

            ZStream.NextIn = b;
            ZStream.NextInIndex = off;
            ZStream.AvailIn = len;

            do
            {
                ZStream.NextOut = Buf;
                ZStream.NextOutIndex = 0;
                ZStream.AvailOut = Buf.Length;

                var err = Compress
                    ? ZStream.Deflate(FlushLevel)
                    : ZStream.Inflate(FlushLevel);

                if (err != ZStreamState.Ok)
                    // TODO
//					throw new ZStreamException((compress ? "de" : "in") + "flating: " + z.msg);
                    throw new IOException((Compress ? "de" : "in") + "flating: " + ZStream.Msg);

                Output.Write(Buf, 0, Buf.Length - ZStream.AvailOut);
            } while (ZStream.AvailIn > 0 || ZStream.AvailOut == 0);
        }

        public override void WriteByte(byte b)
        {
            Buf1[0] = b;
            Write(Buf1, 0, 1);
        }
    }
}
