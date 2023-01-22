
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

    public enum CompressionMethod
    {
        Deflated = 8
    }

    internal sealed class Deflate
    {

        private const int MaxMemLevel = 9;

        private const int ZDefaultCompression = -1;
        private const int DefMemLevel = 8;

        private enum FunctionType
        {
            Stored = 0,
            Fast = 1,
            Slow = 2,
        }

        private class Config
        {
            public readonly int GoodLength; // reduce lazy search above this match length
            public readonly int MaxLazy; // do not perform lazy search above this match length
            public readonly int NiceLength; // quit search above this match length
            public readonly int MaxChain;
            public readonly FunctionType Func;

            internal Config(int goodLength, int maxLazy,
                int niceLength, int maxChain, FunctionType func)
            {
                GoodLength = goodLength;
                MaxLazy = maxLazy;
                NiceLength = niceLength;
                MaxChain = maxChain;
                Func = func;
            }
        }

        private static readonly Config[] ConfigTable;

        static Deflate()
        {
            ConfigTable = new Config[10];
            // good  lazy  nice  chain
            ConfigTable[0] = new Config(0, 0, 0, 0, FunctionType.Stored);
            ConfigTable[1] = new Config(4, 4, 8, 4, FunctionType.Fast);
            ConfigTable[2] = new Config(4, 5, 16, 8, FunctionType.Fast);
            ConfigTable[3] = new Config(4, 6, 32, 32, FunctionType.Fast);

            ConfigTable[4] = new Config(4, 4, 16, 16, FunctionType.Slow);
            ConfigTable[5] = new Config(8, 16, 32, 32, FunctionType.Slow);
            ConfigTable[6] = new Config(8, 16, 128, 128, FunctionType.Slow);
            ConfigTable[7] = new Config(8, 32, 128, 256, FunctionType.Slow);
            ConfigTable[8] = new Config(32, 128, 258, 1024, FunctionType.Slow);
            ConfigTable[9] = new Config(32, 258, 258, 4096, FunctionType.Slow);
        }


        // block not completed, need more input or more output
        private const int NeedMore = 0;

        // block flush performed
        private const int BlockDone = 1;

        // finish started, need only more output at next deflate
        private const int FinishStarted = 2;

        // finish done, accept no more input or output
        private const int FinishDone = 3;

        // preset dictionary flag in zlib header
        private const int PresetDict = 0x20;

        private const int ZFiltered = 1;
        private const int ZHuffmanOnly = 2;
        private const int ZDefaultStrategy = 0;

        private const int InitState = 42;
        private const int BusyState = 113;
        private const int FinishState = 666;

        private const int StoredBlock = 0;
        private const int StaticTrees = 1;
        private const int DynTrees = 2;

        // The three kinds of block type
        private const int ZBinary = 0;
        private const int ZAscii = 1;
        private const int ZUnknown = 2;

        private const int BufSize = 8 * 2;

        // repeat previous bit length 3-6 times (2 bits of repeat count)
        private const int Rep36 = 16;

        // repeat a zero length 3-10 times  (3 bits of repeat count)
        private const int Repz310 = 17;

        // repeat a zero length 11-138 times  (7 bits of repeat count)
        private const int Repz11138 = 18;

        private const int MinMatch = 3;
        private const int MaxMatch = 258;
        private const int MinLookahead = (MaxMatch + MinMatch + 1);

        private const int MaxBits = 15;
        private const int DCodes = 30;
        private const int BlCodes = 19;
        private const int LengthCodes = 29;
        private const int Literals = 256;
        private const int LCodes = (Literals + 1 + LengthCodes);
        private const int HeapSize = (2 * LCodes + 1);

        private const int EndBlock = 256;

        private ZStream _stream; // pointer back to this zlib stream
        private int _status; // as the name implies

        internal byte[] PendingBuf; // output still pending
        internal int PendingBufSize; // size of pending_buf
        internal int PendingOut; // next pending byte to output to the stream
        internal int Pending; // nb of bytes in the pending buffer
        internal int Noheader; // suppress zlib header and adler32
        internal byte DataType; // UNKNOWN, BINARY or ASCII
        internal byte Method; // STORED (for zip only) or DEFLATED
        internal FlushLevel LastFlush; // value of flush param for previous deflate call

        internal int WSize; // LZ77 window size (32K by default)
        internal int WBits; // log2(w_size)  (8..16)
        internal int WMask; // w_size - 1

        private byte[] _window;
        // Sliding window. Input bytes are read into the second half of the window,
        // and move to the first half later to keep a dictionary of at least wSize
        // bytes. With this organization, matches are limited to a distance of
        // wSize-MAX_MATCH bytes, but this ensures that IO is always
        // performed with a length multiple of the block size. Also, it limits
        // the window size to 64K, which is quite useful on MSDOS.
        // To do: use the user input buffer as sliding window.

        internal int WindowSize;
        // Actual size of window: 2*wSize, except when the user input buffer
        // is directly used as sliding window.

        internal short[] Prev;
        // Link to older string with same hash index. To limit the size of this
        // array to 64K, this link is maintained only for the last 32K strings.
        // An index in this array is thus a window index modulo 32K.

        internal short[] Head; // Heads of the hash chains or NIL.

        internal int InsH; // hash index of string to be inserted
        internal int HashSize; // number of elements in hash table
        internal int HashBits; // log2(hash_size)
        internal int HashMask; // hash_size-1

        // Number of bits by which ins_h must be shifted at each input
        // step. It must be such that after MIN_MATCH steps, the oldest
        // byte no longer takes part in the hash key, that is:
        // hash_shift * MIN_MATCH >= hash_bits
        internal int HashShift;

        // Window position at the beginning of the current output block. Gets
        // negative when the window is moved backwards.

        internal int BlockStart;

        internal int MatchLength; // length of best match
        internal int PrevMatch; // previous match
        internal int MatchAvailable; // set if previous match exists
        internal int Strstart; // start of string to insert
        internal int MatchStart; // start of matching string
        internal int Lookahead; // number of valid bytes ahead in window

        // Length of the best match at previous step. Matches not greater than this
        // are discarded. This is used in the lazy match evaluation.
        internal int PrevLength;

        // To speed up deflation, hash chains are never searched beyond this
        // length.  A higher limit improves compression ratio but degrades the speed.
        internal int MaxChainLength;

        // Attempt to find a better match only when the current match is strictly
        // smaller than this value. This mechanism is used only for compression
        // levels >= 4.
        internal int MaxLazyMatch;

        // Insert new strings in the hash table only if the match length is not
        // greater than this length. This saves time but degrades compression.
        // max_insert_length is used only for compression levels <= 3.

        internal int Level; // compression level (1..9)
        internal int Strategy; // favor or force Huffman coding

        // Use a faster search when the previous match is longer than this
        internal int GoodMatch;

        // Stop searching when current match exceeds this
        internal int NiceMatch;

        internal short[] DynLtree; // literal and length tree
        internal short[] DynDtree; // distance tree
        internal short[] BlTree; // Huffman tree for bit lengths

        internal Tree LDesc = new Tree(); // desc for literal tree
        internal Tree DDesc = new Tree(); // desc for distance tree
        internal Tree BlDesc = new Tree(); // desc for bit length tree

        // number of codes at each bit length for an optimal tree
        internal short[] BlCount = new short[MaxBits + 1];

        // heap used to build the Huffman trees
        internal int[] Heap = new int[2 * LCodes + 1];

        internal int HeapLen; // number of elements in the heap

        internal int HeapMax; // element of largest frequency
        // The sons of heap[n] are heap[2*n] and heap[2*n+1]. heap[0] is not used.
        // The same heap array is used to build all trees.

        // Depth of each subtree used as tie breaker for trees of equal frequency
        internal byte[] Depth = new byte[2 * LCodes + 1];

        internal int LBuf; // index for literals or lengths */

        // Size of match buffer for literals/lengths.  There are 4 reasons for
        // limiting lit_bufsize to 64K:
        //   - frequencies can be kept in 16 bit counters
        //   - if compression is not successful for the first block, all input
        //     data is still in the window so we can still emit a stored block even
        //     when input comes from standard input.  (This can also be done for
        //     all blocks if lit_bufsize is not greater than 32K.)
        //   - if compression is not successful for a file smaller than 64K, we can
        //     even emit a stored file instead of a stored block (saving 5 bytes).
        //     This is applicable only for zip (not gzip or zlib).
        //   - creating new Huffman trees less frequently may not provide fast
        //     adaptation to changes in the input data statistics. (Take for
        //     example a binary file with poorly compressible code followed by
        //     a highly compressible string table.) Smaller buffer sizes give
        //     fast adaptation but have of course the overhead of transmitting
        //     trees more frequently.
        //   - I can't count above 4
        internal int LitBufsize;

        internal int LastLit; // running index in l_buf

        // Buffer for distances. To simplify the code, d_buf and l_buf have
        // the same number of elements. To use different lengths, an extra flag
        // array would be necessary.

        internal int DBuf; // index of pendig_buf

        internal int OptLen; // bit length of current block with optimal trees
        internal int StaticLen; // bit length of current block with static trees
        internal int Matches; // number of string matches in current block
        internal int LastEobLen; // bit length of EOB code for last block

        // Output buffer. bits are inserted starting at the bottom (least
        // significant bits).
        internal uint BiBuf;

        // Number of valid bits in bi_buf.  All bits above the last valid bit
        // are always zero.
        internal int BiValid;

        internal Deflate()
        {
            DynLtree = new short[HeapSize * 2];
            DynDtree = new short[(2 * DCodes + 1) * 2]; // distance tree
            BlTree = new short[(2 * BlCodes + 1) * 2]; // Huffman tree for bit lengths
        }

        internal void lm_init()
        {
            WindowSize = 2 * WSize;

            Head[HashSize - 1] = 0;
            for (var i = 0; i < HashSize - 1; i++)
            {
                Head[i] = 0;
            }

            // Set the default configuration parameters:
            MaxLazyMatch = ConfigTable[Level].MaxLazy;
            GoodMatch = ConfigTable[Level].GoodLength;
            NiceMatch = ConfigTable[Level].NiceLength;
            MaxChainLength = ConfigTable[Level].MaxChain;

            Strstart = 0;
            BlockStart = 0;
            Lookahead = 0;
            MatchLength = PrevLength = MinMatch - 1;
            MatchAvailable = 0;
            InsH = 0;
        }

        // Initialize the tree data structures for a new zlib stream.
        internal void tr_init()
        {

            LDesc.DynTree = DynLtree;
            LDesc.StatDesc = StaticTree.StaticLDesc;

            DDesc.DynTree = DynDtree;
            DDesc.StatDesc = StaticTree.StaticDDesc;

            BlDesc.DynTree = BlTree;
            BlDesc.StatDesc = StaticTree.StaticBlDesc;

            BiBuf = 0;
            BiValid = 0;
            LastEobLen = 8; // enough lookahead for inflate

            // Initialize the first block of the first file:
            init_block();
        }

        internal void init_block()
        {
            // Initialize the trees.
            for (var i = 0; i < LCodes; i++) DynLtree[i * 2] = 0;
            for (var i = 0; i < DCodes; i++) DynDtree[i * 2] = 0;
            for (var i = 0; i < BlCodes; i++) BlTree[i * 2] = 0;

            DynLtree[EndBlock * 2] = 1;
            OptLen = StaticLen = 0;
            LastLit = Matches = 0;
        }

        // Restore the heap property by moving down the tree starting at node k,
        // exchanging a node with the smallest of its two sons if necessary, stopping
        // when the heap property is re-established (each father smaller than its
        // two sons).
        internal void Pqdownheap(short[] tree, // the tree to restore
            int k // node to move down
        )
        {
            var v = Heap[k];
            var j = k << 1; // left son of k
            while (j <= HeapLen)
            {
                // Set j to the smallest of the two sons:
                if (j < HeapLen &&
                    Smaller(tree, Heap[j + 1], Heap[j], Depth))
                {
                    j++;
                }

                // Exit if v is smaller than both sons
                if (Smaller(tree, v, Heap[j], Depth)) break;

                // Exchange v with the smallest son
                Heap[k] = Heap[j];
                k = j;
                // And continue down the tree, setting j to the left son of k
                j <<= 1;
            }

            Heap[k] = v;
        }

        internal static bool Smaller(short[] tree, int n, int m, byte[] depth)
        {
            var tn2 = tree[n * 2];
            var tm2 = tree[m * 2];
            return (tn2 < tm2 ||
                    (tn2 == tm2 && depth[n] <= depth[m]));
        }

        // Scan a literal or distance tree to determine the frequencies of the codes
        // in the bit length tree.
        internal void scan_tree(short[] tree, // the tree to be scanned
            int maxCode // and its largest code of non zero frequency
        )
        {
            int n; // iterates over all tree elements
            var prevlen = -1; // last emitted length
            int curlen; // length of current code
            int nextlen = tree[0 * 2 + 1]; // length of next code
            var count = 0; // repeat count of the current code
            var maxCount = 7; // max repeat count
            var minCount = 4; // min repeat count

            if (nextlen == 0)
            {
                maxCount = 138;
                minCount = 3;
            }

            tree[(maxCode + 1) * 2 + 1] = -1; // guard

            for (n = 0; n <= maxCode; n++)
            {
                curlen = nextlen;
                nextlen = tree[(n + 1) * 2 + 1];
                if (++count < maxCount && curlen == nextlen)
                {
                    continue;
                }

                if (count < minCount)
                {
                    BlTree[curlen * 2] += (short) count;
                }
                else if (curlen != 0)
                {
                    if (curlen != prevlen) BlTree[curlen * 2]++;
                    BlTree[Rep36 * 2]++;
                }
                else if (count <= 10)
                {
                    BlTree[Repz310 * 2]++;
                }
                else
                {
                    BlTree[Repz11138 * 2]++;
                }

                count = 0;
                prevlen = curlen;
                if (nextlen == 0)
                {
                    maxCount = 138;
                    minCount = 3;
                }
                else if (curlen == nextlen)
                {
                    maxCount = 6;
                    minCount = 3;
                }
                else
                {
                    maxCount = 7;
                    minCount = 4;
                }
            }
        }

        // Construct the Huffman tree for the bit lengths and return the index in
        // bl_order of the last bit length code to send.
        internal int build_bl_tree()
        {
            int maxBlindex; // index of last bit length code of non zero freq

            // Determine the bit length frequencies for literal and distance trees
            scan_tree(DynLtree, LDesc.MaxCode);
            scan_tree(DynDtree, DDesc.MaxCode);

            // Build the bit length tree:
            BlDesc.build_tree(this);
            // opt_len now includes the length of the tree representations, except
            // the lengths of the bit lengths codes and the 5+5+4 bits for the counts.

            // Determine the number of bit length codes to send. The pkzip format
            // requires that at least 4 bit length codes be sent. (appnote.txt says
            // 3 but the actual value used is 4.)
            for (maxBlindex = BlCodes - 1; maxBlindex >= 3; maxBlindex--)
            {
                if (BlTree[Tree.BlOrder[maxBlindex] * 2 + 1] != 0) break;
            }

            // Update opt_len to include the bit length tree and counts
            OptLen += 3 * (maxBlindex + 1) + 5 + 5 + 4;

            return maxBlindex;
        }


        // Send the header for a block using dynamic Huffman trees: the counts, the
        // lengths of the bit length codes, the literal tree and the distance tree.
        // IN assertion: lcodes >= 257, dcodes >= 1, blcodes >= 4.
        internal void send_all_trees(int lcodes, int dcodes, int blcodes)
        {
            int rank; // index in bl_order

            send_bits(lcodes - 257, 5); // not +255 as stated in appnote.txt
            send_bits(dcodes - 1, 5);
            send_bits(blcodes - 4, 4); // not -3 as stated in appnote.txt
            for (rank = 0; rank < blcodes; rank++)
            {
                send_bits(BlTree[Tree.BlOrder[rank] * 2 + 1], 3);
            }

            send_tree(DynLtree, lcodes - 1); // literal tree
            send_tree(DynDtree, dcodes - 1); // distance tree
        }

        // Send a literal or distance tree in compressed form, using the codes in
        // bl_tree.
        internal void send_tree(short[] tree, // the tree to be sent
            int maxCode // and its largest code of non zero frequency
        )
        {
            int n; // iterates over all tree elements
            var prevlen = -1; // last emitted length
            int curlen; // length of current code
            int nextlen = tree[0 * 2 + 1]; // length of next code
            var count = 0; // repeat count of the current code
            var maxCount = 7; // max repeat count
            var minCount = 4; // min repeat count

            if (nextlen == 0)
            {
                maxCount = 138;
                minCount = 3;
            }

            for (n = 0; n <= maxCode; n++)
            {
                curlen = nextlen;
                nextlen = tree[(n + 1) * 2 + 1];
                if (++count < maxCount && curlen == nextlen)
                {
                    continue;
                }

                if (count < minCount)
                {
                    do
                    {
                        send_code(curlen, BlTree);
                    } while (--count != 0);
                }
                else if (curlen != 0)
                {
                    if (curlen != prevlen)
                    {
                        send_code(curlen, BlTree);
                        count--;
                    }

                    send_code(Rep36, BlTree);
                    send_bits(count - 3, 2);
                }
                else if (count <= 10)
                {
                    send_code(Repz310, BlTree);
                    send_bits(count - 3, 3);
                }
                else
                {
                    send_code(Repz11138, BlTree);
                    send_bits(count - 11, 7);
                }

                count = 0;
                prevlen = curlen;
                if (nextlen == 0)
                {
                    maxCount = 138;
                    minCount = 3;
                }
                else if (curlen == nextlen)
                {
                    maxCount = 6;
                    minCount = 3;
                }
                else
                {
                    maxCount = 7;
                    minCount = 4;
                }
            }
        }

        // Output a byte on the stream.
        // IN assertion: there is enough room in pending_buf.
        internal void put_byte(byte[] p, int start, int len)
        {
            Array.Copy(p, start, PendingBuf, Pending, len);
            Pending += len;
        }

        internal void put_byte(byte c)
        {
            PendingBuf[Pending++] = c;
        }

        internal void put_short(int w)
        {
            PendingBuf[Pending++] = (byte) (w /*&0xff*/);
            PendingBuf[Pending++] = (byte) (w >> 8);
        }

        internal void PutShortMsb(int b)
        {
            PendingBuf[Pending++] = (byte) (b >> 8);
            PendingBuf[Pending++] = (byte) (b /*&0xff*/);
        }

        internal void send_code(int c, short[] tree)
        {
            var c2 = c * 2;
            send_bits((tree[c2] & 0xffff), (tree[c2 + 1] & 0xffff));
        }

        internal void send_bits(int val, int length)
        {
            if (BiValid > BufSize - length)
            {
                BiBuf |= (uint) (val << BiValid);
                PendingBuf[Pending++] = (byte) (BiBuf /*&0xff*/);
                PendingBuf[Pending++] = (byte) (BiBuf >> 8);
                BiBuf = ((uint) val) >> (BufSize - BiValid);
                BiValid += length - BufSize;
            }
            else
            {
                BiBuf |= (uint) (val << BiValid);
                BiValid += length;
            }
        }

        // Send one empty static block to give enough lookahead for inflate.
        // This takes 10 bits, of which 7 may remain in the bit buffer.
        // The current inflate code requires 9 bits of lookahead. If the
        // last two codes for the previous block (real code plus EOB) were coded
        // on 5 bits or less, inflate may have only 5+3 bits of lookahead to decode
        // the last real code. In this case we send two empty static blocks instead
        // of one. (There are no problems if the previous block is stored or fixed.)
        // To simplify the code, we assume the worst case of last real code encoded
        // on one bit only.
        internal void _tr_align()
        {
            send_bits(StaticTrees << 1, 3);
            send_code(EndBlock, StaticTree.StaticLTree);

            bi_flush();

            // Of the 10 bits for the empty block, we have already sent
            // (10 - bi_valid) bits. The lookahead for the last real code (before
            // the EOB of the previous block) was thus at least one plus the length
            // of the EOB plus what we have just sent of the empty static block.
            if (1 + LastEobLen + 10 - BiValid < 9)
            {
                send_bits(StaticTrees << 1, 3);
                send_code(EndBlock, StaticTree.StaticLTree);
                bi_flush();
            }

            LastEobLen = 7;
        }


        // Save the match info and tally the frequency counts. Return true if
        // the current block must be flushed.
        internal bool _tr_tally(int dist, // distance of matched string
            int lc // match length-MIN_MATCH or unmatched char (if dist==0)
        )
        {

            PendingBuf[DBuf + LastLit * 2] = (byte) (dist >> 8);
            PendingBuf[DBuf + LastLit * 2 + 1] = (byte) dist;

            PendingBuf[LBuf + LastLit] = (byte) lc;
            LastLit++;

            if (dist == 0)
            {
                // lc is the unmatched char
                DynLtree[lc * 2]++;
            }
            else
            {
                Matches++;
                // Here, lc is the match length - MIN_MATCH
                dist--; // dist = match distance - 1
                DynLtree[(Tree.LengthCode[lc] + Literals + 1) * 2]++;
                DynDtree[Tree.d_code(dist) * 2]++;
            }

            if ((LastLit & 0x1fff) == 0 && Level > 2)
            {
                // Compute an upper bound for the compressed length
                var outLength = LastLit * 8;
                var inLength = Strstart - BlockStart;
                int dcode;
                for (dcode = 0; dcode < DCodes; dcode++)
                {
                    outLength += (int) (DynDtree[dcode * 2] *
                                        (5L + Tree.ExtraDBits[dcode]));
                }

                outLength >>= 3;
                if ((Matches < (LastLit / 2)) && outLength < inLength / 2) return true;
            }

            return (LastLit == LitBufsize - 1);
            // We avoid equality with lit_bufsize because of wraparound at 64K
            // on 16 bit machines and because stored blocks are restricted to
            // 64K-1 bytes.
        }

        // Send the block data compressed using the given Huffman trees
        internal void compress_block(short[] ltree, short[] dtree)
        {
            int dist; // distance of matched string
            int lc; // match length or unmatched char (if dist == 0)
            var lx = 0; // running index in l_buf
            int code; // the code to send
            int extra; // number of extra bits to send

            if (LastLit != 0)
            {
                do
                {
                    dist = ((PendingBuf[DBuf + lx * 2] << 8) & 0xff00) |
                           (PendingBuf[DBuf + lx * 2 + 1] & 0xff);
                    lc = (PendingBuf[LBuf + lx]) & 0xff;
                    lx++;

                    if (dist == 0)
                    {
                        send_code(lc, ltree); // send a literal byte
                    }
                    else
                    {
                        // Here, lc is the match length - MIN_MATCH
                        code = Tree.LengthCode[lc];

                        send_code(code + Literals + 1, ltree); // send the length code
                        extra = Tree.ExtraLBits[code];
                        if (extra != 0)
                        {
                            lc -= Tree.BaseLength[code];
                            send_bits(lc, extra); // send the extra length bits
                        }

                        dist--; // dist is now the match distance - 1
                        code = Tree.d_code(dist);

                        send_code(code, dtree); // send the distance code
                        extra = Tree.ExtraDBits[code];
                        if (extra != 0)
                        {
                            dist -= Tree.BaseDist[code];
                            send_bits(dist, extra); // send the extra distance bits
                        }
                    } // literal or match pair ?

                    // Check that the overlay between pending_buf and d_buf+l_buf is ok:
                } while (lx < LastLit);
            }

            send_code(EndBlock, ltree);
            LastEobLen = ltree[EndBlock * 2 + 1];
        }

        // Set the data type to ASCII or BINARY, using a crude approximation:
        // binary if more than 20% of the bytes are <= 6 or >= 128, ascii otherwise.
        // IN assertion: the fields freq of dyn_ltree are set and the total of all
        // frequencies does not exceed 64K (to fit in an int on 16 bit machines).
        internal void set_data_type()
        {
            var n = 0;
            var asciiFreq = 0;
            var binFreq = 0;
            while (n < 7)
            {
                binFreq += DynLtree[n * 2];
                n++;
            }

            while (n < 128)
            {
                asciiFreq += DynLtree[n * 2];
                n++;
            }

            while (n < Literals)
            {
                binFreq += DynLtree[n * 2];
                n++;
            }

            DataType = (byte) (binFreq > (asciiFreq >> 2) ? ZBinary : ZAscii);
        }

        // Flush the bit buffer, keeping at most 7 bits in it.
        internal void bi_flush()
        {
            if (BiValid == 16)
            {
                PendingBuf[Pending++] = (byte) (BiBuf /*&0xff*/);
                PendingBuf[Pending++] = (byte) (BiBuf >> 8);
                BiBuf = 0;
                BiValid = 0;
            }
            else if (BiValid >= 8)
            {
                PendingBuf[Pending++] = (byte) (BiBuf);
                BiBuf >>= 8;
                BiBuf &= 0x00ff;
                BiValid -= 8;
            }
        }

        // Flush the bit buffer and align the output on a byte boundary
        internal void bi_windup()
        {
            if (BiValid > 8)
            {
                PendingBuf[Pending++] = (byte) (BiBuf);
                PendingBuf[Pending++] = (byte) (BiBuf >> 8);
            }
            else if (BiValid > 0)
            {
                PendingBuf[Pending++] = (byte) (BiBuf);
            }

            BiBuf = 0;
            BiValid = 0;
        }

        // Copy a stored block, storing first the length and its
        // one's complement if requested.
        internal void copy_block(int buf, // the input data
            int len, // its length
            bool header // true if block header must be written
        )
        {
            //int index=0;
            bi_windup(); // align on byte boundary
            LastEobLen = 8; // enough lookahead for inflate

            if (header)
            {
                put_short((short) len);
                put_short((short) ~len);
            }

            //  while(len--!=0) {
            //    put_byte(window[buf+index]);
            //    index++;
            //  }
            put_byte(_window, buf, len);
        }

        internal void flush_block_only(bool eof)
        {
            _tr_flush_block(BlockStart >= 0 ? BlockStart : -1,
                Strstart - BlockStart,
                eof);
            BlockStart = Strstart;
            _stream.flush_pending();
        }

        // Copy without compression as much as possible from the input stream, return
        // the current block state.
        // This function does not insert new strings in the dictionary since
        // uncompressible data is probably not useful. This function is used
        // only for the level=0 compression option.
        // NOTE: this function should be optimized to avoid extra copying from
        // window to pending_buf.
        internal int deflate_stored(FlushLevel flush)
        {
            // Stored blocks are limited to 0xffff bytes, pending_buf is limited
            // to pending_buf_size, and each stored block has a 5 byte header:

            var maxBlockSize = 0xffff;
            int maxStart;

            if (maxBlockSize > PendingBufSize - 5)
            {
                maxBlockSize = PendingBufSize - 5;
            }

            // Copy as much as possible from input to output:
            while (true)
            {
                // Fill the window as much as possible:
                if (Lookahead <= 1)
                {
                    fill_window();
                    if (Lookahead == 0 && flush == FlushLevel.NoFlush) return NeedMore;
                    if (Lookahead == 0) break; // flush the current block
                }

                Strstart += Lookahead;
                Lookahead = 0;

                // Emit a stored block if pending_buf will be full:
                maxStart = BlockStart + maxBlockSize;
                if (Strstart == 0 || Strstart >= maxStart)
                {
                    // strstart == 0 is possible when wraparound on 16-bit machine
                    Lookahead = Strstart - maxStart;
                    Strstart = maxStart;

                    flush_block_only(false);
                    if (_stream.AvailOut == 0) return NeedMore;

                }

                // Flush if we may have to slide, otherwise block_start may become
                // negative and the data will be gone:
                if (Strstart - BlockStart >= WSize - MinLookahead)
                {
                    flush_block_only(false);
                    if (_stream.AvailOut == 0) return NeedMore;
                }
            }

            flush_block_only(flush == FlushLevel.Finish);
            if (_stream.AvailOut == 0)
                return (flush == FlushLevel.Finish) ? FinishStarted : NeedMore;

            return flush == FlushLevel.Finish ? FinishDone : BlockDone;
        }

        // Send a stored block
        internal void _tr_stored_block(int buf, // input block
            int storedLen, // length of input block
            bool eof // true if this is the last block for a file
        )
        {
            send_bits((StoredBlock << 1) + (eof ? 1 : 0), 3); // send block type
            copy_block(buf, storedLen, true); // with header
        }

        // Determine the best encoding for the current block: dynamic trees, static
        // trees or store, and output the encoded block to the zip file.
        internal void _tr_flush_block(int buf, // input block, or NULL if too old
            int storedLen, // length of input block
            bool eof // true if this is the last block for a file
        )
        {
            int optLenb, staticLenb; // opt_len and static_len in bytes
            var maxBlindex = 0; // index of last bit length code of non zero freq

            // Build the Huffman trees unless a stored block is forced
            if (Level > 0)
            {
                // Check if the file is ascii or binary
                if (DataType == ZUnknown) set_data_type();

                // Construct the literal and distance trees
                LDesc.build_tree(this);

                DDesc.build_tree(this);

                // At this point, opt_len and static_len are the total bit lengths of
                // the compressed block data, excluding the tree representations.

                // Build the bit length tree for the above two trees, and get the index
                // in bl_order of the last bit length code to send.
                maxBlindex = build_bl_tree();

                // Determine the best encoding. Compute first the block length in bytes
                optLenb = (OptLen + 3 + 7) >> 3;
                staticLenb = (StaticLen + 3 + 7) >> 3;

                if (staticLenb <= optLenb) optLenb = staticLenb;
            }
            else
            {
                optLenb = staticLenb = storedLen + 5; // force a stored block
            }

            if (storedLen + 4 <= optLenb && buf != -1)
            {
                // 4: two words for the lengths
                // The test buf != NULL is only necessary if LIT_BUFSIZE > WSIZE.
                // Otherwise we can't have processed more than WSIZE input bytes since
                // the last block flush, because compression would have been
                // successful. If LIT_BUFSIZE <= WSIZE, it is never too late to
                // transform a block into a stored block.
                _tr_stored_block(buf, storedLen, eof);
            }
            else if (staticLenb == optLenb)
            {
                send_bits((StaticTrees << 1) + (eof ? 1 : 0), 3);
                compress_block(StaticTree.StaticLTree, StaticTree.StaticDTree);
            }
            else
            {
                send_bits((DynTrees << 1) + (eof ? 1 : 0), 3);
                send_all_trees(LDesc.MaxCode + 1, DDesc.MaxCode + 1, maxBlindex + 1);
                compress_block(DynLtree, DynDtree);
            }

            // The above check is made mod 2^32, for files larger than 512 MB
            // and uLong implemented on 32 bits.

            init_block();

            if (eof)
            {
                bi_windup();
            }
        }

        // Fill the window when the lookahead becomes insufficient.
        // Updates strstart and lookahead.
        //
        // IN assertion: lookahead < MIN_LOOKAHEAD
        // OUT assertions: strstart <= window_size-MIN_LOOKAHEAD
        //    At least one byte has been read, or avail_in == 0; reads are
        //    performed for at least two bytes (required for the zip translate_eol
        //    option -- not supported here).
        internal void fill_window()
        {
            int n, m;
            int p;
            int more; // Amount of free space at the end of the window.

            do
            {
                more = (WindowSize - Lookahead - Strstart);

                // Deal with !@#$% 64K limit:
                if (more == 0 && Strstart == 0 && Lookahead == 0)
                {
                    more = WSize;
                }
                else if (more == -1)
                {
                    // Very unlikely, but possible on 16 bit machine if strstart == 0
                    // and lookahead == 1 (input done one byte at time)
                    more--;

                    // If the window is almost full and there is insufficient lookahead,
                    // move the upper half to the lower one to make room in the upper half.
                }
                else if (Strstart >= WSize + WSize - MinLookahead)
                {
                    Array.Copy(_window, WSize, _window, 0, WSize);
                    MatchStart -= WSize;
                    Strstart -= WSize; // we now have strstart >= MAX_DIST
                    BlockStart -= WSize;

                    // Slide the hash table (could be avoided with 32 bit values
                    // at the expense of memory usage). We slide even when level == 0
                    // to keep the hash table consistent if we switch back to level > 0
                    // later. (Using level 0 permanently is not an optimal usage of
                    // zlib, so we don't care about this pathological case.)

                    n = HashSize;
                    p = n;
                    do
                    {
                        m = (Head[--p] & 0xffff);
                        Head[p] = (short) (m >= WSize ? (m - WSize) : 0);
                    } while (--n != 0);

                    n = WSize;
                    p = n;
                    do
                    {
                        m = (Prev[--p] & 0xffff);
                        Prev[p] = (short) (m >= WSize ? (m - WSize) : 0);
                        // If n is not on any hash chain, prev[n] is garbage but
                        // its value will never be used.
                    } while (--n != 0);

                    more += WSize;
                }

                if (_stream.AvailIn == 0) return;

                // If there was no sliding:
                //    strstart <= WSIZE+MAX_DIST-1 && lookahead <= MIN_LOOKAHEAD - 1 &&
                //    more == window_size - lookahead - strstart
                // => more >= window_size - (MIN_LOOKAHEAD-1 + WSIZE + MAX_DIST-1)
                // => more >= window_size - 2*WSIZE + 2
                // In the BIG_MEM or MMAP case (not yet supported),
                //   window_size == input_size + MIN_LOOKAHEAD  &&
                //   strstart + s->lookahead <= input_size => more >= MIN_LOOKAHEAD.
                // Otherwise, window_size == 2*WSIZE so more >= 2.
                // If there was sliding, more >= WSIZE. So in all cases, more >= 2.

                n = _stream.read_buf(_window, Strstart + Lookahead, more);
                Lookahead += n;

                // Initialize the hash value now that we have some input:
                if (Lookahead >= MinMatch)
                {
                    InsH = _window[Strstart] & 0xff;
                    InsH = (((InsH) << HashShift) ^ (_window[Strstart + 1] & 0xff)) & HashMask;
                }
                // If the whole input has less than MIN_MATCH bytes, ins_h is garbage,
                // but this is not important since only literal bytes will be emitted.
            } while (Lookahead < MinLookahead && _stream.AvailIn != 0);
        }

        // Compress as much as possible from the input stream, return the current
        // block state.
        // This function does not perform lazy evaluation of matches and inserts
        // new strings in the dictionary only for unmatched strings or for short
        // matches. It is used only for the fast compression options.
        internal int deflate_fast(FlushLevel flush)
        {
            //    short hash_head = 0; // head of the hash chain
            var hashHead = 0; // head of the hash chain
            bool bflush; // set if current block must be flushed

            while (true)
            {
                // Make sure that we always have enough lookahead, except
                // at the end of the input file. We need MAX_MATCH bytes
                // for the next match, plus MIN_MATCH bytes to insert the
                // string following the next match.
                if (Lookahead < MinLookahead)
                {
                    fill_window();
                    if (Lookahead < MinLookahead && flush == FlushLevel.NoFlush)
                    {
                        return NeedMore;
                    }

                    if (Lookahead == 0) break; // flush the current block
                }

                // Insert the string window[strstart .. strstart+2] in the
                // dictionary, and set hash_head to the head of the hash chain:
                if (Lookahead >= MinMatch)
                {
                    InsH = (((InsH) << HashShift) ^ (_window[(Strstart) + (MinMatch - 1)] & 0xff)) & HashMask;

                    //  prev[strstart&w_mask]=hash_head=head[ins_h];
                    hashHead = (Head[InsH] & 0xffff);
                    Prev[Strstart & WMask] = Head[InsH];
                    Head[InsH] = (short) Strstart;
                }

                // Find the longest match, discarding those <= prev_length.
                // At this point we have always match_length < MIN_MATCH

                if (hashHead != 0L &&
                    ((Strstart - hashHead) & 0xffff) <= WSize - MinLookahead
                   )
                {
                    // To simplify the code, we prevent matches with the string
                    // of window index 0 (in particular we have to avoid a match
                    // of the string with itself at the start of the input file).
                    if (Strategy != ZHuffmanOnly)
                    {
                        MatchLength = longest_match(hashHead);
                    }
                    // longest_match() sets match_start
                }

                if (MatchLength >= MinMatch)
                {
                    //        check_match(strstart, match_start, match_length);

                    bflush = _tr_tally(Strstart - MatchStart, MatchLength - MinMatch);

                    Lookahead -= MatchLength;

                    // Insert new strings in the hash table only if the match length
                    // is not too large. This saves time but degrades compression.
                    if (MatchLength <= MaxLazyMatch &&
                        Lookahead >= MinMatch)
                    {
                        MatchLength--; // string at strstart already in hash table
                        do
                        {
                            Strstart++;

                            InsH = ((InsH << HashShift) ^ (_window[(Strstart) + (MinMatch - 1)] & 0xff)) & HashMask;
                            //      prev[strstart&w_mask]=hash_head=head[ins_h];
                            hashHead = (Head[InsH] & 0xffff);
                            Prev[Strstart & WMask] = Head[InsH];
                            Head[InsH] = (short) Strstart;

                            // strstart never exceeds WSIZE-MAX_MATCH, so there are
                            // always MIN_MATCH bytes ahead.
                        } while (--MatchLength != 0);

                        Strstart++;
                    }
                    else
                    {
                        Strstart += MatchLength;
                        MatchLength = 0;
                        InsH = _window[Strstart] & 0xff;

                        InsH = (((InsH) << HashShift) ^ (_window[Strstart + 1] & 0xff)) & HashMask;
                        // If lookahead < MIN_MATCH, ins_h is garbage, but it does not
                        // matter since it will be recomputed at next deflate call.
                    }
                }
                else
                {
                    // No match, output a literal byte

                    bflush = _tr_tally(0, _window[Strstart] & 0xff);
                    Lookahead--;
                    Strstart++;
                }

                if (bflush)
                {

                    flush_block_only(false);
                    if (_stream.AvailOut == 0) return NeedMore;
                }
            }

            flush_block_only(flush == FlushLevel.Finish);
            if (_stream.AvailOut == 0)
            {
                if (flush == FlushLevel.Finish) return FinishStarted;
                return NeedMore;
            }

            return flush == FlushLevel.Finish ? FinishDone : BlockDone;
        }

        // Same as above, but achieves better compression. We use a lazy
        // evaluation for matches: a match is finally adopted only if there is
        // no better match at the next window position.
        internal int deflate_slow(FlushLevel flush)
        {
            //    short hash_head = 0;    // head of hash chain
            var hashHead = 0; // head of hash chain
            bool bflush; // set if current block must be flushed

            // Process the input block.
            while (true)
            {
                // Make sure that we always have enough lookahead, except
                // at the end of the input file. We need MAX_MATCH bytes
                // for the next match, plus MIN_MATCH bytes to insert the
                // string following the next match.

                if (Lookahead < MinLookahead)
                {
                    fill_window();
                    if (Lookahead < MinLookahead && flush == FlushLevel.NoFlush)
                    {
                        return NeedMore;
                    }

                    if (Lookahead == 0) break; // flush the current block
                }

                // Insert the string window[strstart .. strstart+2] in the
                // dictionary, and set hash_head to the head of the hash chain:

                if (Lookahead >= MinMatch)
                {
                    InsH = (((InsH) << HashShift) ^ (_window[(Strstart) + (MinMatch - 1)] & 0xff)) & HashMask;
                    //  prev[strstart&w_mask]=hash_head=head[ins_h];
                    hashHead = (Head[InsH] & 0xffff);
                    Prev[Strstart & WMask] = Head[InsH];
                    Head[InsH] = (short) Strstart;
                }

                // Find the longest match, discarding those <= prev_length.
                PrevLength = MatchLength;
                PrevMatch = MatchStart;
                MatchLength = MinMatch - 1;

                if (hashHead != 0 && PrevLength < MaxLazyMatch &&
                    ((Strstart - hashHead) & 0xffff) <= WSize - MinLookahead
                   )
                {
                    // To simplify the code, we prevent matches with the string
                    // of window index 0 (in particular we have to avoid a match
                    // of the string with itself at the start of the input file).

                    if (Strategy != ZHuffmanOnly)
                    {
                        MatchLength = longest_match(hashHead);
                    }
                    // longest_match() sets match_start

                    if (MatchLength <= 5 && (Strategy == ZFiltered ||
                                             (MatchLength == MinMatch &&
                                              Strstart - MatchStart > 4096)))
                    {

                        // If prev_match is also MIN_MATCH, match_start is garbage
                        // but we will ignore the current match anyway.
                        MatchLength = MinMatch - 1;
                    }
                }

                // If there was a match at the previous step and the current
                // match is not better, output the previous match:
                if (PrevLength >= MinMatch && MatchLength <= PrevLength)
                {
                    var maxInsert = Strstart + Lookahead - MinMatch;
                    // Do not insert strings in hash table beyond this.

                    //          check_match(strstart-1, prev_match, prev_length);

                    bflush = _tr_tally(Strstart - 1 - PrevMatch, PrevLength - MinMatch);

                    // Insert in hash table all strings up to the end of the match.
                    // strstart-1 and strstart are already inserted. If there is not
                    // enough lookahead, the last two strings are not inserted in
                    // the hash table.
                    Lookahead -= PrevLength - 1;
                    PrevLength -= 2;
                    do
                    {
                        if (++Strstart <= maxInsert)
                        {
                            InsH = (((InsH) << HashShift) ^ (_window[(Strstart) + (MinMatch - 1)] & 0xff)) &
                                   HashMask;
                            //prev[strstart&w_mask]=hash_head=head[ins_h];
                            hashHead = (Head[InsH] & 0xffff);
                            Prev[Strstart & WMask] = Head[InsH];
                            Head[InsH] = (short) Strstart;
                        }
                    } while (--PrevLength != 0);

                    MatchAvailable = 0;
                    MatchLength = MinMatch - 1;
                    Strstart++;

                    if (bflush)
                    {
                        flush_block_only(false);
                        if (_stream.AvailOut == 0) return NeedMore;
                    }
                }
                else if (MatchAvailable != 0)
                {

                    // If there was no match at the previous position, output a
                    // single literal. If there was a match but the current match
                    // is longer, truncate the previous match to a single literal.

                    bflush = _tr_tally(0, _window[Strstart - 1] & 0xff);

                    if (bflush)
                    {
                        flush_block_only(false);
                    }

                    Strstart++;
                    Lookahead--;
                    if (_stream.AvailOut == 0) return NeedMore;
                }
                else
                {
                    // There is no previous match to compare with, wait for
                    // the next step to decide.

                    MatchAvailable = 1;
                    Strstart++;
                    Lookahead--;
                }
            }

            if (MatchAvailable != 0)
            {
                bflush = _tr_tally(0, _window[Strstart - 1] & 0xff);
                MatchAvailable = 0;
            }

            flush_block_only(flush == FlushLevel.Finish);

            if (_stream.AvailOut == 0)
            {
                if (flush == FlushLevel.Finish) return FinishStarted;
                return NeedMore;
            }

            return flush == FlushLevel.Finish ? FinishDone : BlockDone;
        }

        internal int longest_match(int curMatch)
        {
            var chainLength = MaxChainLength; // max hash chain length
            var scan = Strstart; // current string
            int match; // matched string
            int len; // length of current match
            var bestLen = PrevLength; // best match length so far
            var limit = Strstart > (WSize - MinLookahead) ? Strstart - (WSize - MinLookahead) : 0;
            var niceMatch = NiceMatch;

            // Stop when cur_match becomes <= limit. To simplify the code,
            // we prevent matches with the string of window index 0.

            var wmask = WMask;

            var strend = Strstart + MaxMatch;
            var scanEnd1 = _window[scan + bestLen - 1];
            var scanEnd = _window[scan + bestLen];

            // The code is optimized for HASH_BITS >= 8 and MAX_MATCH-2 multiple of 16.
            // It is easy to get rid of this optimization if necessary.

            // Do not waste too much time if we already have a good match:
            if (PrevLength >= GoodMatch)
            {
                chainLength >>= 2;
            }

            // Do not look for matches beyond the end of the input. This is necessary
            // to make deflate deterministic.
            if (niceMatch > Lookahead) niceMatch = Lookahead;

            do
            {
                match = curMatch;

                // Skip to next match if the match length cannot increase
                // or if the match length is less than 2:
                if (_window[match + bestLen] != scanEnd ||
                    _window[match + bestLen - 1] != scanEnd1 ||
                    _window[match] != _window[scan] ||
                    _window[++match] != _window[scan + 1]) continue;

                // The check at best_len-1 can be removed because it will be made
                // again later. (This heuristic is not always a win.)
                // It is not necessary to compare scan[2] and match[2] since they
                // are always equal when the other bytes match, given that
                // the hash keys are equal and that HASH_BITS >= 8.
                scan += 2;
                match++;

                // We check for insufficient lookahead only every 8th comparison;
                // the 256th check will be made at strstart+258.
                do
                {
                } while (_window[++scan] == _window[++match] &&
                         _window[++scan] == _window[++match] &&
                         _window[++scan] == _window[++match] &&
                         _window[++scan] == _window[++match] &&
                         _window[++scan] == _window[++match] &&
                         _window[++scan] == _window[++match] &&
                         _window[++scan] == _window[++match] &&
                         _window[++scan] == _window[++match] &&
                         scan < strend);

                len = MaxMatch - (strend - scan);
                scan = strend - MaxMatch;

                if (len > bestLen)
                {
                    MatchStart = curMatch;
                    bestLen = len;
                    if (len >= niceMatch) break;
                    scanEnd1 = _window[scan + bestLen - 1];
                    scanEnd = _window[scan + bestLen];
                }

            } while ((curMatch = (Prev[curMatch & wmask] & 0xffff)) > limit
                     && --chainLength != 0);

            if (bestLen <= Lookahead) return bestLen;
            return Lookahead;
        }

        internal ZStreamState DeflateInit(ZStream strm, int level, int bits)
        {
            return DeflateInit2(strm, level, CompressionMethod.Deflated, bits, DefMemLevel,
                ZDefaultStrategy);
        }

        internal ZStreamState DeflateInit(ZStream strm, int level)
        {
            return DeflateInit(strm, level, Utils.MaxWBits);
        }

        internal ZStreamState DeflateInit2(ZStream strm, int level, CompressionMethod method, int windowBits,
            int memLevel, int strategy)
        {
            var noheader = 0;

            strm.Msg = null;

            if (level == ZDefaultCompression) level = 6;

            if (windowBits < 0)
            {
                // undocumented feature: suppress zlib header
                noheader = 1;
                windowBits = -windowBits;
            }

            if (memLevel < 1 || memLevel > MaxMemLevel ||
                method != CompressionMethod.Deflated ||
                windowBits < 9 || windowBits > 15 || level < 0 || level > 9 ||
                strategy < 0 || strategy > ZHuffmanOnly)
            {
                return ZStreamState.StreamError;
            }

            strm.DeflateState = this;

            Noheader = noheader;
            WBits = windowBits;
            WSize = 1 << WBits;
            WMask = WSize - 1;

            HashBits = memLevel + 7;
            HashSize = 1 << HashBits;
            HashMask = HashSize - 1;
            HashShift = ((HashBits + MinMatch - 1) / MinMatch);

            _window = new byte[WSize * 2];
            Prev = new short[WSize];
            Head = new short[HashSize];

            LitBufsize = 1 << (memLevel + 6); // 16K elements by default

            // We overlay pending_buf and d_buf+l_buf. This works since the average
            // output size for (length,distance) codes is <= 24 bits.
            PendingBuf = new byte[LitBufsize * 4];
            PendingBufSize = LitBufsize * 4;

            DBuf = LitBufsize / 2;
            LBuf = (1 + 2) * LitBufsize;

            Level = level;

            //System.out.println("level="+level);

            Strategy = strategy;
            Method = (byte) method;

            return DeflateReset(strm);
        }

        internal ZStreamState DeflateReset(ZStream strm)
        {
            strm.TotalIn = strm.TotalOut = 0;
            strm.Msg = null; //
            strm.DataType = ZUnknown;

            Pending = 0;
            PendingOut = 0;

            if (Noheader < 0)
            {
                Noheader = 0; // was set to -1 by deflate(..., Z_FINISH);
            }

            _status = (Noheader != 0) ? BusyState : InitState;
            strm.Adler = Utils.Adler32(0, null, 0, 0);

            LastFlush = FlushLevel.NoFlush;

            tr_init();
            lm_init();
            return ZStreamState.Ok;
        }

        internal ZStreamState DeflateEnd()
        {
            if (_status != InitState && _status != BusyState && _status != FinishState)
            {
                return ZStreamState.StreamError;
            }

            // Deallocate in reverse order of allocations:
            PendingBuf = null;
            Head = null;
            Prev = null;
            _window = null;
            // free
            // dstate=null;
            return _status == BusyState ? ZStreamState.DataError : ZStreamState.Ok;
        }

        internal ZStreamState DeflateParams(ZStream strm, int level, int strategy)
        {
            var err = ZStreamState.Ok;

            if (level == ZDefaultCompression)
            {
                level = 6;
            }

            if (level < 0 || level > 9 ||
                strategy < 0 || strategy > ZHuffmanOnly)
            {
                return ZStreamState.StreamError;
            }

            if (ConfigTable[Level].Func != ConfigTable[level].Func &&
                strm.TotalIn != 0)
            {
                // Flush the last buffer:
                err = strm.Deflate(FlushLevel.PartialFlush);
            }

            if (Level != level)
            {
                Level = level;
                MaxLazyMatch = ConfigTable[Level].MaxLazy;
                GoodMatch = ConfigTable[Level].GoodLength;
                NiceMatch = ConfigTable[Level].NiceLength;
                MaxChainLength = ConfigTable[Level].MaxChain;
            }

            Strategy = strategy;
            return err;
        }

        internal ZStreamState DeflateSetDictionary(ZStream strm, byte[] dictionary, int dictLength)
        {
            var length = dictLength;
            var index = 0;

            if (dictionary == null || _status != InitState)
                return ZStreamState.StreamError;

            strm.Adler = Utils.Adler32(strm.Adler, dictionary, 0, dictLength);

            if (length < MinMatch) return ZStreamState.Ok;
            if (length > WSize - MinLookahead)
            {
                length = WSize - MinLookahead;
                index = dictLength - length; // use the tail of the dictionary
            }

            Array.Copy(dictionary, index, _window, 0, length);
            Strstart = length;
            BlockStart = length;

            // Insert all strings in the hash table (except for the last two bytes).
            // s->lookahead stays null, so s->ins_h will be recomputed at the next
            // call of fill_window.

            InsH = _window[0] & 0xff;
            InsH = (((InsH) << HashShift) ^ (_window[1] & 0xff)) & HashMask;

            for (var n = 0; n <= length - MinMatch; n++)
            {
                InsH = (((InsH) << HashShift) ^ (_window[(n) + (MinMatch - 1)] & 0xff)) & HashMask;
                Prev[n & WMask] = Head[InsH];
                Head[InsH] = (short) n;
            }

            return ZStreamState.Ok;
        }

        internal ZStreamState Deflated(ZStream strm, FlushLevel flush)
        {
            if (flush > FlushLevel.Finish || flush < 0)
            {
                return ZStreamState.StreamError;
            }

            if (strm.NextOut == null ||
                (strm.NextIn == null && strm.AvailIn != 0) ||
                (_status == FinishState && flush != FlushLevel.Finish))
            {
                strm.Msg = Utils.ErrMsg[ZStreamState.NeedDict - (ZStreamState.StreamError)];
                return ZStreamState.StreamError;
            }

            if (strm.AvailOut == 0)
            {
                strm.Msg = Utils.ErrMsg[ZStreamState.NeedDict - (ZStreamState.BufError)];
                return ZStreamState.BufError;
            }

            this._stream = strm; // just in case
            var oldFlush = LastFlush;
            LastFlush = flush;

            // Write the zlib header
            if (_status == InitState)
            {
                var header = ((int)CompressionMethod.Deflated + ((WBits - 8) << 4)) << 8;
                var levelFlags = ((Level - 1) & 0xff) >> 1;

                if (levelFlags > 3) levelFlags = 3;
                header |= (levelFlags << 6);
                if (Strstart != 0) header |= PresetDict;
                header += 31 - (header % 31);

                _status = BusyState;
                PutShortMsb(header);


                // Save the adler32 of the preset dictionary:
                if (Strstart != 0)
                {
                    PutShortMsb((int) (strm.Adler >> 16));
                    PutShortMsb((int) (strm.Adler & 0xffff));
                }

                strm.Adler = Utils.Adler32(0, null, 0, 0);
            }

            // Flush as much pending output as possible
            if (Pending != 0)
            {
                strm.flush_pending();
                if (strm.AvailOut == 0)
                {
                    //System.out.println("  avail_out==0");
                    // Since avail_out is 0, deflate will be called again with
                    // more output space, but possibly with both pending and
                    // avail_in equal to zero. There won't be anything to do,
                    // but this is not an error situation so make sure we
                    // return OK instead of BUF_ERROR at next call of deflate:
                    LastFlush = FlushLevel.BufError;
                    return ZStreamState.Ok;
                }

                // Make sure there is something to do and avoid duplicate consecutive
                // flushes. For repeated and useless calls with Z_FINISH, we keep
                // returning Z_STREAM_END instead of Z_BUFF_ERROR.
            }
            else if (strm.AvailIn == 0 && flush <= oldFlush &&
                     flush != FlushLevel.Finish)
            {
                strm.Msg = Utils.ErrMsg[ZStreamState.NeedDict - (ZStreamState.BufError)];
                return ZStreamState.BufError;
            }

            // User must not provide more input after the first FINISH:
            if (_status == FinishState && strm.AvailIn != 0)
            {
                strm.Msg = Utils.ErrMsg[ZStreamState.NeedDict - (ZStreamState.BufError)];
                return ZStreamState.BufError;
            }

            // Start a new block or continue the current one.
            if (strm.AvailIn != 0 || Lookahead != 0 ||
                (flush != FlushLevel.NoFlush && _status != FinishState))
            {
                var bstate = -1;
                switch (ConfigTable[Level].Func)
                {
                    case FunctionType.Stored:
                        bstate = deflate_stored(flush);
                        break;
                    case FunctionType.Fast:
                        bstate = deflate_fast(flush);
                        break;
                    case FunctionType.Slow:
                        bstate = deflate_slow(flush);
                        break;
                }

                if (bstate == FinishStarted || bstate == FinishDone)
                {
                    _status = FinishState;
                }

                if (bstate == NeedMore || bstate == FinishStarted)
                {
                    if (strm.AvailOut == 0)
                    {
                        LastFlush = FlushLevel.BufError; // avoid BUF_ERROR next call, see above
                    }

                    return ZStreamState.Ok;
                    // If flush != Z_NO_FLUSH && avail_out == 0, the next call
                    // of deflate should use the same flush parameter to make sure
                    // that the flush is complete. So we don't have to output an
                    // empty block here, this will be done at next call. This also
                    // ensures that for a very small output buffer, we emit at most
                    // one empty block.
                }

                if (bstate == BlockDone)
                {
                    if (flush == FlushLevel.PartialFlush)
                    {
                        _tr_align();
                    }
                    else
                    {
                        // FULL_FLUSH or SYNC_FLUSH
                        _tr_stored_block(0, 0, false);
                        // For a full flush, this empty block will be recognized
                        // as a special marker by inflate_sync().
                        if (flush == FlushLevel.FullFlush)
                        {
                            //state.head[s.hash_size-1]=0;
                            for (var i = 0; i < HashSize /*-1*/; i++) // forget history
                                Head[i] = 0;
                        }
                    }

                    strm.flush_pending();
                    if (strm.AvailOut == 0)
                    {
                        LastFlush = FlushLevel.BufError; // avoid BUF_ERROR at next call, see above
                        return ZStreamState.Ok;
                    }
                }
            }

            if (flush != FlushLevel.Finish) return ZStreamState.Ok;
            if (Noheader != 0) return ZStreamState.StreamEnd;

            // Write the zlib trailer (adler32)
            PutShortMsb((int) (strm.Adler >> 16));
            PutShortMsb((int) (strm.Adler & 0xffff));
            strm.flush_pending();

            // If avail_out is zero, the application will call deflate again
            // to flush the rest.
            Noheader = -1; // write the trailer only once!
            return Pending != 0 ? ZStreamState.Ok : ZStreamState.StreamEnd;
        }
    }
}
