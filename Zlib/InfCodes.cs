
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



namespace Zlib {

    internal sealed class InfCodes
    {

        private static readonly int[] InflateMask =
        {
            0x00000000, 0x00000001, 0x00000003, 0x00000007, 0x0000000f,
            0x0000001f, 0x0000003f, 0x0000007f, 0x000000ff, 0x000001ff,
            0x000003ff, 0x000007ff, 0x00000fff, 0x00001fff, 0x00003fff,
            0x00007fff, 0x0000ffff
        };

        //private const int ZOk=0;
        //private const int ZStreamEnd=1;
        //private const int ZNeedDict=2;
        //private const int ZErrno=-1;
        //private const int ZStreamError=-2;
        //private const int ZDataError=-3;
        //private const int ZMemError=-4;
        //private const int ZBufError=-5;
        //private const int ZVersionError=-6;

        // waiting for "i:"=input,
        //             "o:"=output,
        //             "x:"=nothing
        private const int Start = 0; // x: set up for LEN
        private const int Len = 1; // i: get length/literal/eob next
        private const int Lenext = 2; // i: getting length extra (have base)
        private const int Dist = 3; // i: get distance next
        private const int Distext = 4; // i: getting distance extra
        private const int Copy = 5; // o: copying bytes in window, waiting for space
        private const int Lit = 6; // o: got literal, waiting for output space
        private const int Wash = 7; // o: got eob, possibly still output waiting
        private const int End = 8; // x: got eob and all data flushed
        private const int Badcode = 9; // x: got error

        int mode; // current inflate_codes mode

        // mode dependent information
        int len;

        int[] tree; // pointer into tree
        int treeIndex;
        int need; // bits needed

        int lit;

        // if EXT or COPY, where and how much
        int get; // bits to get for extra
        int dist; // distance back to copy from

        byte lbits; // ltree bits decoded per branch
        byte dbits; // dtree bits decoder per branch
        int[] ltree; // literal/length/eob tree
        int ltreeIndex; // literal/length/eob tree
        int[] dtree; // distance tree
        int dtreeIndex; // distance tree

        internal void Init(int bl, int bd,
            int[] tl, int tlIndex,
            int[] td, int tdIndex, ZStream z)
        {
            mode = Start;
            lbits = (byte) bl;
            dbits = (byte) bd;
            ltree = tl;
            ltreeIndex = tlIndex;
            dtree = td;
            dtreeIndex = tdIndex;
            tree = null;
        }

        internal ZStreamState Proc(InfBlocks s, ZStream z, ZStreamState r)
        {
            int j; // temporary storage
            int tindex; // temporary pointer
            int e; // extra bits or operation
            var b = 0; // bit buffer
            var k = 0; // bits in bit buffer
            var p = 0; // input data pointer
            int n; // bytes available there
            int q; // output window write pointer
            int m; // bytes to end of window or read pointer
            int f; // pointer to copy strings from

            // copy input/output information to locals (UPDATE macro restores)
            p = z.NextInIndex;
            n = z.AvailIn;
            b = s.bitb;
            k = s.bitk;
            q = s.write;
            m = q < s.read ? s.read - q - 1 : s.end - q;

            // process input and output based on current state
            while (true)
            {
                switch (mode)
                {
                    // waiting for "i:"=input, "o:"=output, "x:"=nothing
                    case Start: // x: set up for LEN
                        if (m >= 258 && n >= 10)
                        {

                            s.bitb = b;
                            s.bitk = k;
                            z.AvailIn = n;
                            z.TotalIn += p - z.NextInIndex;
                            z.NextInIndex = p;
                            s.write = q;
                            r = inflate_fast(lbits, dbits,
                                ltree, ltreeIndex,
                                dtree, dtreeIndex,
                                s, z);

                            p = z.NextInIndex;
                            n = z.AvailIn;
                            b = s.bitb;
                            k = s.bitk;
                            q = s.write;
                            m = q < s.read ? s.read - q - 1 : s.end - q;

                            if (r != (int) ZStreamState.Ok)
                            {
                                mode = r == ZStreamState.StreamEnd ? Wash : Badcode;
                                break;
                            }
                        }

                        need = lbits;
                        tree = ltree;
                        treeIndex = ltreeIndex;

                        mode = Len;
                        goto case Len;
                    case Len: // i: get length/literal/eob next
                        j = need;

                        while (k < (j))
                        {
                            if (n != 0) r = (int) ZStreamState.Ok;
                            else
                            {

                                s.bitb = b;
                                s.bitk = k;
                                z.AvailIn = n;
                                z.TotalIn += p - z.NextInIndex;
                                z.NextInIndex = p;
                                s.write = q;
                                return s.inflate_flush(z, r);
                            }

                            n--;
                            b |= (z.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        tindex = (treeIndex + (b & InflateMask[j])) * 3;

                        b >>= (tree[tindex + 1]);
                        k -= (tree[tindex + 1]);

                        e = tree[tindex];

                        if (e == 0)
                        {
                            // literal
                            lit = tree[tindex + 2];
                            mode = Lit;
                            break;
                        }

                        if ((e & 16) != 0)
                        {
                            // length
                            get = e & 15;
                            len = tree[tindex + 2];
                            mode = Lenext;
                            break;
                        }

                        if ((e & 64) == 0)
                        {
                            // next table
                            need = e;
                            treeIndex = tindex / 3 + tree[tindex + 2];
                            break;
                        }

                        if ((e & 32) != 0)
                        {
                            // end of block
                            mode = Wash;
                            break;
                        }

                        mode = Badcode; // invalid code
                        z.Msg = "invalid literal/length code";
                        r = ZStreamState.DataError;

                        s.bitb = b;
                        s.bitk = k;
                        z.AvailIn = n;
                        z.TotalIn += p - z.NextInIndex;
                        z.NextInIndex = p;
                        s.write = q;
                        return s.inflate_flush(z, r);

                    case Lenext: // i: getting length extra (have base)
                        j = get;

                        while (k < (j))
                        {
                            if (n != 0) r = (int) ZStreamState.Ok;
                            else
                            {

                                s.bitb = b;
                                s.bitk = k;
                                z.AvailIn = n;
                                z.TotalIn += p - z.NextInIndex;
                                z.NextInIndex = p;
                                s.write = q;
                                return s.inflate_flush(z, r);
                            }

                            n--;
                            b |= (z.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        len += (b & InflateMask[j]);

                        b >>= j;
                        k -= j;

                        need = dbits;
                        tree = dtree;
                        treeIndex = dtreeIndex;
                        mode = Dist;
                        goto case Dist;
                    case Dist: // i: get distance next
                        j = need;

                        while (k < (j))
                        {
                            if (n != 0) r = (int) ZStreamState.Ok;
                            else
                            {

                                s.bitb = b;
                                s.bitk = k;
                                z.AvailIn = n;
                                z.TotalIn += p - z.NextInIndex;
                                z.NextInIndex = p;
                                s.write = q;
                                return s.inflate_flush(z, r);
                            }

                            n--;
                            b |= (z.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        tindex = (treeIndex + (b & InflateMask[j])) * 3;

                        b >>= tree[tindex + 1];
                        k -= tree[tindex + 1];

                        e = (tree[tindex]);
                        if ((e & 16) != 0)
                        {
                            // distance
                            get = e & 15;
                            dist = tree[tindex + 2];
                            mode = Distext;
                            break;
                        }

                        if ((e & 64) == 0)
                        {
                            // next table
                            need = e;
                            treeIndex = tindex / 3 + tree[tindex + 2];
                            break;
                        }

                        mode = Badcode; // invalid code
                        z.Msg = "invalid distance code";
                        r = ZStreamState.DataError;

                        s.bitb = b;
                        s.bitk = k;
                        z.AvailIn = n;
                        z.TotalIn += p - z.NextInIndex;
                        z.NextInIndex = p;
                        s.write = q;
                        return s.inflate_flush(z, r);

                    case Distext: // i: getting distance extra
                        j = get;

                        while (k < (j))
                        {
                            if (n != 0) r = (int) ZStreamState.Ok;
                            else
                            {

                                s.bitb = b;
                                s.bitk = k;
                                z.AvailIn = n;
                                z.TotalIn += p - z.NextInIndex;
                                z.NextInIndex = p;
                                s.write = q;
                                return s.inflate_flush(z, r);
                            }

                            n--;
                            b |= (z.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        dist += (b & InflateMask[j]);

                        b >>= j;
                        k -= j;

                        mode = Copy;
                        goto case Copy;
                    case Copy: // o: copying bytes in window, waiting for space
                        f = q - dist;
                        while (f < 0)
                        {
                            // modulo window size-"while" instead
                            f += s.end; // of "if" handles invalid distances
                        }

                        while (len != 0)
                        {

                            if (m == 0)
                            {
                                if (q == s.end && s.read != 0)
                                {
                                    q = 0;
                                    m = q < s.read ? s.read - q - 1 : s.end - q;
                                }

                                if (m == 0)
                                {
                                    s.write = q;
                                    r = s.inflate_flush(z, r);
                                    q = s.write;
                                    m = q < s.read ? s.read - q - 1 : s.end - q;

                                    if (q == s.end && s.read != 0)
                                    {
                                        q = 0;
                                        m = q < s.read ? s.read - q - 1 : s.end - q;
                                    }

                                    if (m == 0)
                                    {
                                        s.bitb = b;
                                        s.bitk = k;
                                        z.AvailIn = n;
                                        z.TotalIn += p - z.NextInIndex;
                                        z.NextInIndex = p;
                                        s.write = q;
                                        return s.inflate_flush(z, r);
                                    }
                                }
                            }

                            s.window[q++] = s.window[f++];
                            m--;

                            if (f == s.end)
                                f = 0;
                            len--;
                        }

                        mode = Start;
                        break;
                    case Lit: // o: got literal, waiting for output space
                        if (m == 0)
                        {
                            if (q == s.end && s.read != 0)
                            {
                                q = 0;
                                m = q < s.read ? s.read - q - 1 : s.end - q;
                            }

                            if (m == 0)
                            {
                                s.write = q;
                                r = s.inflate_flush(z, r);
                                q = s.write;
                                m = q < s.read ? s.read - q - 1 : s.end - q;

                                if (q == s.end && s.read != 0)
                                {
                                    q = 0;
                                    m = q < s.read ? s.read - q - 1 : s.end - q;
                                }

                                if (m == 0)
                                {
                                    s.bitb = b;
                                    s.bitk = k;
                                    z.AvailIn = n;
                                    z.TotalIn += p - z.NextInIndex;
                                    z.NextInIndex = p;
                                    s.write = q;
                                    return s.inflate_flush(z, r);
                                }
                            }
                        }

                        r = (int) ZStreamState.Ok;

                        s.window[q++] = (byte) lit;
                        m--;

                        mode = Start;
                        break;
                    case Wash: // o: got eob, possibly more output
                        if (k > 7)
                        {
                            // return unused byte, if any
                            k -= 8;
                            n++;
                            p--; // can always return one
                        }

                        s.write = q;
                        r = s.inflate_flush(z, r);
                        q = s.write;
                        m = q < s.read ? s.read - q - 1 : s.end - q;

                        if (s.read != s.write)
                        {
                            s.bitb = b;
                            s.bitk = k;
                            z.AvailIn = n;
                            z.TotalIn += p - z.NextInIndex;
                            z.NextInIndex = p;
                            s.write = q;
                            return s.inflate_flush(z, r);
                        }

                        mode = End;
                        goto case End;
                    case End:
                        r = ZStreamState.StreamEnd;
                        s.bitb = b;
                        s.bitk = k;
                        z.AvailIn = n;
                        z.TotalIn += p - z.NextInIndex;
                        z.NextInIndex = p;
                        s.write = q;
                        return s.inflate_flush(z, r);

                    case Badcode: // x: got error

                        r = ZStreamState.DataError;

                        s.bitb = b;
                        s.bitk = k;
                        z.AvailIn = n;
                        z.TotalIn += p - z.NextInIndex;
                        z.NextInIndex = p;
                        s.write = q;
                        return s.inflate_flush(z, r);

                    default:
                        r = ZStreamState.StreamError;

                        s.bitb = b;
                        s.bitk = k;
                        z.AvailIn = n;
                        z.TotalIn += p - z.NextInIndex;
                        z.NextInIndex = p;
                        s.write = q;
                        return s.inflate_flush(z, r);
                }
            }
        }

        internal void Free(ZStream z)
        {
            //  ZFREE(z, c);
        }

        // Called with number of bytes left to write in window at least 258
        // (the maximum string length) and number of input bytes available
        // at least ten.  The ten bytes are six bytes for the longest length/
        // distance pair plus four bytes for overloading the bit buffer.

        internal ZStreamState inflate_fast(int bl, int bd,
            int[] tl, int tlIndex,
            int[] td, int tdIndex,
            InfBlocks s, ZStream z)
        {
            int t; // temporary pointer
            int[] tp; // temporary pointer
            int tpIndex; // temporary pointer
            int e; // extra bits or operation
            int b; // bit buffer
            int k; // bits in bit buffer
            int p; // input data pointer
            int n; // bytes available there
            int q; // output window write pointer
            int m; // bytes to end of window or read pointer
            int ml; // mask for literal/length tree
            int md; // mask for distance tree
            int c; // bytes to copy
            int d; // distance back to copy from
            int r; // copy source pointer

            int tpIndexT3; // (tp_index+t)*3

            // load input, output, bit values
            p = z.NextInIndex;
            n = z.AvailIn;
            b = s.bitb;
            k = s.bitk;
            q = s.write;
            m = q < s.read ? s.read - q - 1 : s.end - q;

            // initialize masks
            ml = InflateMask[bl];
            md = InflateMask[bd];

            // do until not enough input or output space for fast loop
            do
            {
                // assume called with m >= 258 && n >= 10
                // get literal/length code
                while (k < (20))
                {
                    // max bits for literal/length code
                    n--;
                    b |= (z.NextIn[p++] & 0xff) << k;
                    k += 8;
                }

                t = b & ml;
                tp = tl;
                tpIndex = tlIndex;
                tpIndexT3 = (tpIndex + t) * 3;
                if ((e = tp[tpIndexT3]) == 0)
                {
                    b >>= (tp[tpIndexT3 + 1]);
                    k -= (tp[tpIndexT3 + 1]);

                    s.window[q++] = (byte) tp[tpIndexT3 + 2];
                    m--;
                    continue;
                }

                do
                {

                    b >>= (tp[tpIndexT3 + 1]);
                    k -= (tp[tpIndexT3 + 1]);

                    if ((e & 16) != 0)
                    {
                        e &= 15;
                        c = tp[tpIndexT3 + 2] + (b & InflateMask[e]);

                        b >>= e;
                        k -= e;

                        // decode distance base of block to copy
                        while (k < (15))
                        {
                            // max bits for distance code
                            n--;
                            b |= (z.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        t = b & md;
                        tp = td;
                        tpIndex = tdIndex;
                        tpIndexT3 = (tpIndex + t) * 3;
                        e = tp[tpIndexT3];

                        do
                        {

                            b >>= (tp[tpIndexT3 + 1]);
                            k -= (tp[tpIndexT3 + 1]);

                            if ((e & 16) != 0)
                            {
                                // get extra bits to add to distance base
                                e &= 15;
                                while (k < (e))
                                {
                                    // get extra bits (up to 13)
                                    n--;
                                    b |= (z.NextIn[p++] & 0xff) << k;
                                    k += 8;
                                }

                                d = tp[tpIndexT3 + 2] + (b & InflateMask[e]);

                                b >>= (e);
                                k -= (e);

                                // do the copy
                                m -= c;
                                if (q >= d)
                                {
                                    // offset before dest
                                    //  just copy
                                    r = q - d;
                                    if (q - r > 0 && 2 > (q - r))
                                    {
                                        s.window[q++] = s.window[r++]; // minimum count is three,
                                        s.window[q++] = s.window[r++]; // so unroll loop a little
                                        c -= 2;
                                    }
                                    else
                                    {
                                        Array.Copy(s.window, r, s.window, q, 2);
                                        q += 2;
                                        r += 2;
                                        c -= 2;
                                    }
                                }
                                else
                                {
                                    // else offset after destination
                                    r = q - d;
                                    do
                                    {
                                        r += s.end; // force pointer in window
                                    } while (r < 0); // covers invalid distances

                                    e = s.end - r;
                                    if (c > e)
                                    {
                                        // if source crosses,
                                        c -= e; // wrapped copy
                                        if (q - r > 0 && e > (q - r))
                                        {
                                            do
                                            {
                                                s.window[q++] = s.window[r++];
                                            } while (--e != 0);
                                        }
                                        else
                                        {
                                            Array.Copy(s.window, r, s.window, q, e);
                                            q += e;
                                            r += e;
                                            e = 0;
                                        }

                                        r = 0; // copy rest from start of window
                                    }

                                }

                                // copy all or what's left
                                if (q - r > 0 && c > (q - r))
                                {
                                    do
                                    {
                                        s.window[q++] = s.window[r++];
                                    } while (--c != 0);
                                }
                                else
                                {
                                    Array.Copy(s.window, r, s.window, q, c);
                                    q += c;
                                    r += c;
                                    c = 0;
                                }

                                break;
                            }

                            if ((e & 64) == 0)
                            {
                                t += tp[tpIndexT3 + 2];
                                t += (b & InflateMask[e]);
                                tpIndexT3 = (tpIndex + t) * 3;
                                e = tp[tpIndexT3];
                            }
                            else
                            {
                                z.Msg = "invalid distance code";

                                c = z.AvailIn - n;
                                c = (k >> 3) < c ? k >> 3 : c;
                                n += c;
                                p -= c;
                                k -= c << 3;

                                s.bitb = b;
                                s.bitk = k;
                                z.AvailIn = n;
                                z.TotalIn += p - z.NextInIndex;
                                z.NextInIndex = p;
                                s.write = q;

                                return ZStreamState.DataError;
                            }
                        } while (true);

                        break;
                    }

                    if ((e & 64) == 0)
                    {
                        t += tp[tpIndexT3 + 2];
                        t += (b & InflateMask[e]);
                        tpIndexT3 = (tpIndex + t) * 3;
                        if ((e = tp[tpIndexT3]) == 0)
                        {

                            b >>= (tp[tpIndexT3 + 1]);
                            k -= (tp[tpIndexT3 + 1]);

                            s.window[q++] = (byte) tp[tpIndexT3 + 2];
                            m--;
                            break;
                        }
                    }
                    else if ((e & 32) != 0)
                    {

                        c = z.AvailIn - n;
                        c = (k >> 3) < c ? k >> 3 : c;
                        n += c;
                        p -= c;
                        k -= c << 3;

                        s.bitb = b;
                        s.bitk = k;
                        z.AvailIn = n;
                        z.TotalIn += p - z.NextInIndex;
                        z.NextInIndex = p;
                        s.write = q;

                        return  ZStreamState.StreamEnd;
                    }
                    else
                    {
                        z.Msg = "invalid literal/length code";

                        c = z.AvailIn - n;
                        c = (k >> 3) < c ? k >> 3 : c;
                        n += c;
                        p -= c;
                        k -= c << 3;

                        s.bitb = b;
                        s.bitk = k;
                        z.AvailIn = n;
                        z.TotalIn += p - z.NextInIndex;
                        z.NextInIndex = p;
                        s.write = q;

                        return ZStreamState.DataError;
                    }
                } while (true);
            } while (m >= 258 && n >= 10);

            // not enough input or output--restore pointers and return
            c = z.AvailIn - n;
            c = (k >> 3) < c ? k >> 3 : c;
            n += c;
            p -= c;
            k -= c << 3;

            s.bitb = b;
            s.bitk = k;
            z.AvailIn = n;
            z.TotalIn += p - z.NextInIndex;
            z.NextInIndex = p;
            s.write = q;

            return (int) ZStreamState.Ok;
        }
    }
}
