
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
    internal sealed class Inflate
    {
        // preset dictionary flag in zlib header
        private const int PresetDict = 0x20;

        private const int ZDeflated = 8;

        private const int METHOD = 0; // waiting for method byte
        private const int FLAG = 1; // waiting for flag byte
        private const int Dict4 = 2; // four dictionary check bytes to go
        private const int Dict3 = 3; // three dictionary check bytes to go
        private const int Dict2 = 4; // two dictionary check bytes to go
        private const int Dict1 = 5; // one dictionary check byte to go
        private const int Dict0 = 6; // waiting for inflateSetDictionary
        private const int BLOCKS = 7; // decompressing blocks
        private const int Check4 = 8; // four check bytes to go
        private const int CHECK3 = 9; // three check bytes to go
        private const int CHECK2 = 10; // two check bytes to go
        private const int CHECK1 = 11; // one check byte to go
        private const int Done = 12; // finished check, done
        private const int BAD = 13; // got an error--stay here

        internal int Mode; // current inflate mode

        // mode dependent information
        internal int method; // if FLAGS, method byte

        // if CHECK, check values to compare
        internal long[] was = new long[1]; // computed check value
        internal long need; // stream check value

        // if BAD, inflateSync's marker bytes count
        internal int marker;

        // mode independent information
        internal int nowrap; // flag for no wrapper
        internal int wbits; // log2(window size)  (8..15, defaults to 15)

        internal InfBlocks blocks; // current inflate_blocks state

        internal int InflateReset(ZStream z)
        {
            if (z == null || z.InflateState == null) return (int) ZStreamState.StreamError;

            z.TotalIn = z.TotalOut = 0;
            z.Msg = null;
            z.InflateState.Mode = z.InflateState.nowrap != 0 ? BLOCKS : METHOD;
            z.InflateState.blocks.reset(z, null);
            return (int) ZStreamState.Ok;
        }

        internal int InflateEnd(ZStream z)
        {
            if (blocks != null)
                blocks.free(z);
            blocks = null;
            //    ZFREE(z, z->state);
            return (int) ZStreamState.Ok;
        }

        internal int InflateInit(ZStream zStream, int w)
        {
            zStream.Msg = null;
            blocks = null;

            // handle undocumented nowrap option (no zlib header or check)
            nowrap = 0;
            if (w < 0)
            {
                w = -w;
                nowrap = 1;
            }

            // set window size
            if (w < 8 || w > 15)
            {
                InflateEnd(zStream);
                return (int) ZStreamState.StreamError;
            }

            wbits = w;

            zStream.InflateState.blocks = new InfBlocks(zStream,
                zStream.InflateState.nowrap != 0 ? null : this,
                1 << w);

            // reset state
            InflateReset(zStream);
            return (int) ZStreamState.Ok;
        }

        internal ZStreamState Inflated(ZStream zStream, FlushLevel f)
        {
            ZStreamState r;
            int b;

            if (zStream == null || zStream.InflateState == null || zStream.NextIn == null)
                return  ZStreamState.StreamError;
            f = (FlushLevel)(f == FlushLevel.Finish ? (int) ZStreamState.BufError : (int) ZStreamState.Ok);
            r = ZStreamState.BufError;
            while (true)
            {
                //System.out.println("mode: "+z.istate.mode);
                switch (zStream.InflateState.Mode)
                {
                    case METHOD:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        if (((zStream.InflateState.method = zStream.NextIn[zStream.NextInIndex++]) & 0xf) != ZDeflated)
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.Msg = "unknown compression method";
                            zStream.InflateState.marker = 5; // can't try inflateSync
                            break;
                        }

                        if ((zStream.InflateState.method >> 4) + 8 > zStream.InflateState.wbits)
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.Msg = "invalid window size";
                            zStream.InflateState.marker = 5; // can't try inflateSync
                            break;
                        }

                        zStream.InflateState.Mode = FLAG;
                        goto case FLAG;
                    case FLAG:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        b = (zStream.NextIn[zStream.NextInIndex++]) & 0xff;

                        if ((((zStream.InflateState.method << 8) + b) % 31) != 0)
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.Msg = "incorrect header check";
                            zStream.InflateState.marker = 5; // can't try inflateSync
                            break;
                        }

                        if ((b & PresetDict) == 0)
                        {
                            zStream.InflateState.Mode = BLOCKS;
                            break;
                        }

                        zStream.InflateState.Mode = Dict4;
                        goto case Dict4;
                    case Dict4:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.need = ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 24) & 0xff000000L;
                        zStream.InflateState.Mode = Dict3;
                        goto case Dict3;
                    case Dict3:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.need += ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 16) & 0xff0000L;
                        zStream.InflateState.Mode = Dict2;
                        goto case Dict2;
                    case Dict2:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.need += ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 8) & 0xff00L;
                        zStream.InflateState.Mode = Dict1;
                        goto case Dict1;
                    case Dict1:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.need += (zStream.NextIn[zStream.NextInIndex++] & 0xffL);
                        zStream.Adler = zStream.InflateState.need;
                        zStream.InflateState.Mode = Dict0;
                        return  ZStreamState.NeedDict;
                    case Dict0:
                        zStream.InflateState.Mode = BAD;
                        zStream.Msg = "need dictionary";
                        zStream.InflateState.marker = 0; // can try inflateSync
                        return  ZStreamState.StreamError;
                    case BLOCKS:

                        r = zStream.InflateState.blocks.proc(zStream, r);
                        if (r == ZStreamState.DataError)
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.InflateState.marker = 0; // can try inflateSync
                            break;
                        }

                        if (r == ZStreamState.Ok)
                        {
                            r = (ZStreamState)f;
                        }

                        if (r != ZStreamState.StreamEnd)
                        {
                            return r;
                        }

                        r = (ZStreamState)f;
                        zStream.InflateState.blocks.reset(zStream, zStream.InflateState.was);
                        if (zStream.InflateState.nowrap != 0)
                        {
                            zStream.InflateState.Mode = Done;
                            break;
                        }

                        zStream.InflateState.Mode = Check4;
                        goto case Check4;
                    case Check4:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.need = ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 24) & 0xff000000L;
                        zStream.InflateState.Mode = CHECK3;
                        goto case CHECK3;
                    case CHECK3:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.need += ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 16) & 0xff0000L;
                        zStream.InflateState.Mode = CHECK2;
                        goto case CHECK2;
                    case CHECK2:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.need += ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 8) & 0xff00L;
                        zStream.InflateState.Mode = CHECK1;
                        goto case CHECK1;
                    case CHECK1:

                        if (zStream.AvailIn == 0) return r;
                        r = (ZStreamState)f;

                        zStream.AvailIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.need += (zStream.NextIn[zStream.NextInIndex++] & 0xffL);

                        if (((int) (zStream.InflateState.was[0])) != ((int) (zStream.InflateState.need)))
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.Msg = "incorrect data check";
                            zStream.InflateState.marker = 5; // can't try inflateSync
                            break;
                        }

                        zStream.InflateState.Mode = Done;
                        goto case Done;
                    case Done:
                        return ZStreamState.StreamEnd;
                    case BAD:
                        return  ZStreamState.DataError;
                    default:
                        return ZStreamState.StreamError;
                }
            }
        }


        internal int InflateSetDictionary(ZStream z, byte[] dictionary, int dictLength)
        {
            var index = 0;
            var length = dictLength;
            if (z == null || z.InflateState == null || z.InflateState.Mode != Dict0)
                return (int) ZStreamState.StreamError;

            if (Utils.Adler32(1L, dictionary, 0, dictLength) != z.Adler)
            {
                return (int) ZStreamState.DataError;
            }

            z.Adler = Utils.Adler32(0, null, 0, 0);

            if (length >= (1 << z.InflateState.wbits))
            {
                length = (1 << z.InflateState.wbits) - 1;
                index = dictLength - length;
            }

            z.InflateState.blocks.set_dictionary(dictionary, index, length);
            z.InflateState.Mode = BLOCKS;
            return (int) ZStreamState.Ok;
        }

        private static readonly byte[] mark = {0, 0, 0xff, 0xff};

        internal int InflateSync(ZStream z)
        {
            int n; // number of bytes to look at
            int p; // pointer to bytes
            int m; // number of marker bytes found in a row
            long r, w; // temporaries to save total_in and total_out

            // set up
            if (z == null || z.InflateState == null)
                return (int) ZStreamState.StreamError;
            if (z.InflateState.Mode != BAD)
            {
                z.InflateState.Mode = BAD;
                z.InflateState.marker = 0;
            }

            if ((n = z.AvailIn) == 0)
                return (int) ZStreamState.BufError;
            p = z.NextInIndex;
            m = z.InflateState.marker;

            // search
            while (n != 0 && m < 4)
            {
                if (z.NextIn[p] == mark[m])
                {
                    m++;
                }
                else if (z.NextIn[p] != 0)
                {
                    m = 0;
                }
                else
                {
                    m = 4 - m;
                }

                p++;
                n--;
            }

            // restore
            z.TotalIn += p - z.NextInIndex;
            z.NextInIndex = p;
            z.AvailIn = n;
            z.InflateState.marker = m;

            // return no joy or set up to restart on a new block
            if (m != 4)
            {
                return (int) ZStreamState.DataError;
            }

            r = z.TotalIn;
            w = z.TotalOut;
            InflateReset(z);
            z.TotalIn = r;
            z.TotalOut = w;
            z.InflateState.Mode = BLOCKS;
            return (int) ZStreamState.Ok;
        }

        // Returns true if inflate is currently at the end of a block generated
        // by Z_SYNC_FLUSH or Z_FULL_FLUSH. This function is used by one PPP
        // implementation to provide an additional safety check. PPP uses Z_SYNC_FLUSH
        // but removes the length bytes of the resulting empty stored block. When
        // decompressing, PPP checks that at the end of input packet, inflate is
        // waiting for these length bytes.
        internal int InflateSyncPoint(ZStream z)
        {
            if (z?.InflateState?.blocks == null)
                return (int) ZStreamState.StreamError;
            return z.InflateState.blocks.sync_point();
        }
    }
}
