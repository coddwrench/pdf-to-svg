
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

    internal sealed class Tree
    {
        private const int MaxBits = 15;
        private const int BlCodes = 19;
        private const int DCodes = 30;
        private const int Literals = 256;
        private const int LengthCodes = 29;
        private const int LCodes = (Literals + 1 + LengthCodes);
        private const int HeapSize = (2 * LCodes + 1);

        // Bit length codes must not exceed MAX_BL_BITS bits
        internal const int MaxBlBits = 7;

        // end of block literal code
        internal const int EndBlock = 256;

        // repeat previous bit length 3-6 times (2 bits of repeat count)
        internal const int Rep36 = 16;

        // repeat a zero length 3-10 times  (3 bits of repeat count)
        internal const int Repz310 = 17;

        // repeat a zero length 11-138 times  (7 bits of repeat count)
        internal const int Repz11138 = 18;

        // extra bits for each length code
        internal static readonly int[] ExtraLBits =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
        };

        // extra bits for each distance code
        internal static readonly int[] ExtraDBits =
        {
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13
        };

        // extra bits for each bit length code
        internal static readonly int[] ExtraBlBits =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 7
        };

        internal static readonly byte[] BlOrder =
        {
            16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15
        };


        // The lengths of the bit length codes are sent in order of decreasing
        // probability, to avoid transmitting the lengths for unused bit
        // length codes.

        internal const int BufSize = 8 * 2;

        // see definition of array dist_code below
        internal const int DistCodeLen = 512;

        internal static readonly byte[] DistCode =
        {
            0, 1, 2, 3, 4, 4, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8,
            8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
            12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13,
            13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
            13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 0, 0, 16, 17,
            18, 18, 19, 19, 20, 20, 20, 20, 21, 21, 21, 21, 22, 22, 22, 22, 22, 22, 22, 22,
            23, 23, 23, 23, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
            24, 24, 24, 24, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
            26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
            26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 27, 27, 27, 27, 27, 27, 27, 27,
            27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
            27, 27, 27, 27, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
            28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
            28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
            28, 28, 28, 28, 28, 28, 28, 28, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
            29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
            29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
            29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29
        };

        internal static readonly byte[] LengthCode =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 12, 12,
            13, 13, 13, 13, 14, 14, 14, 14, 15, 15, 15, 15, 16, 16, 16, 16, 16, 16, 16, 16,
            17, 17, 17, 17, 17, 17, 17, 17, 18, 18, 18, 18, 18, 18, 18, 18, 19, 19, 19, 19,
            19, 19, 19, 19, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20,
            21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23,
            23, 23, 23, 23, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
            24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
            25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
            25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26,
            26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
            26, 26, 26, 26, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
            27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 28
        };

        internal static readonly int[] BaseLength =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56,
            64, 80, 96, 112, 128, 160, 192, 224, 0
        };

        internal static readonly int[] BaseDist =
        {
            0, 1, 2, 3, 4, 6, 8, 12, 16, 24,
            32, 48, 64, 96, 128, 192, 256, 384, 512, 768,
            1024, 1536, 2048, 3072, 4096, 6144, 8192, 12288, 16384, 24576
        };

        // Mapping from a distance to a distance code. dist is the distance - 1 and
        // must not have side effects. _dist_code[256] and _dist_code[257] are never
        // used.
        internal static int d_code(int dist)
        {
            return ((dist) < 256 ? DistCode[dist] : DistCode[256 + ((dist) >> 7)]);
        }

        internal short[] DynTree; // the dynamic tree
        internal int MaxCode; // largest code with non zero frequency
        internal StaticTree StatDesc; // the corresponding static tree

        // Compute the optimal bit lengths for a tree and update the total bit length
        // for the current block.
        // IN assertion: the fields freq and dad are set, heap[heap_max] and
        //    above are the tree nodes sorted by increasing frequency.
        // OUT assertions: the field len is set to the optimal bit length, the
        //     array bl_count contains the frequencies for each bit length.
        //     The length opt_len is updated; static_len is also updated if stree is
        //     not null.
        internal void gen_bitlen(Deflate s)
        {
            var tree = DynTree;
            var stree = StatDesc.Tree;
            var extra = StatDesc.ExtraBits;
            var based = StatDesc.ExtraBase;
            var maxLength = StatDesc.MaxLength;
            int h; // heap index
            int n, m; // iterate over the tree elements
            int bits; // bit length
            int xbits; // extra bits
            short f; // frequency
            var overflow = 0; // number of elements with bit length too large

            for (bits = 0; bits <= MaxBits; bits++) s.BlCount[bits] = 0;

            // In a first pass, compute the optimal bit lengths (which may
            // overflow in the case of the bit length tree).
            tree[s.Heap[s.HeapMax] * 2 + 1] = 0; // root of the heap

            for (h = s.HeapMax + 1; h < HeapSize; h++)
            {
                n = s.Heap[h];
                bits = tree[tree[n * 2 + 1] * 2 + 1] + 1;
                if (bits > maxLength)
                {
                    bits = maxLength;
                    overflow++;
                }

                tree[n * 2 + 1] = (short) bits;
                // We overwrite tree[n*2+1] which is no longer needed

                if (n > MaxCode) continue; // not a leaf node

                s.BlCount[bits]++;
                xbits = 0;
                if (n >= based) xbits = extra[n - based];
                f = tree[n * 2];
                s.OptLen += f * (bits + xbits);
                if (stree != null) s.StaticLen += f * (stree[n * 2 + 1] + xbits);
            }

            if (overflow == 0) return;

            // This happens for example on obj2 and pic of the Calgary corpus
            // Find the first bit length which could increase:
            do
            {
                bits = maxLength - 1;
                while (s.BlCount[bits] == 0) bits--;
                s.BlCount[bits]--; // move one leaf down the tree
                s.BlCount[bits + 1] += 2; // move one overflow item as its brother
                s.BlCount[maxLength]--;
                // The brother of the overflow item also moves one step up,
                // but this does not affect bl_count[max_length]
                overflow -= 2;
            } while (overflow > 0);

            for (bits = maxLength; bits != 0; bits--)
            {
                n = s.BlCount[bits];
                while (n != 0)
                {
                    m = s.Heap[--h];
                    if (m > MaxCode) continue;
                    if (tree[m * 2 + 1] != bits)
                    {
                        s.OptLen += (int) ((bits - (long) tree[m * 2 + 1]) * tree[m * 2]);
                        tree[m * 2 + 1] = (short) bits;
                    }

                    n--;
                }
            }
        }

        // Construct one Huffman tree and assigns the code bit strings and lengths.
        // Update the total bit length for the current block.
        // IN assertion: the field freq is set for all tree elements.
        // OUT assertions: the fields len and code are set to the optimal bit length
        //     and corresponding code. The length opt_len is updated; static_len is
        //     also updated if stree is not null. The field max_code is set.
        internal void build_tree(Deflate s)
        {
            var tree = DynTree;
            var stree = StatDesc.Tree;
            var elems = StatDesc.Elems;
            int n, m; // iterate over heap elements
            var maxCode = -1; // largest code with non zero frequency
            int node; // new node being created

            // Construct the initial heap, with least frequent element in
            // heap[1]. The sons of heap[n] are heap[2*n] and heap[2*n+1].
            // heap[0] is not used.
            s.HeapLen = 0;
            s.HeapMax = HeapSize;

            for (n = 0; n < elems; n++)
            {
                if (tree[n * 2] != 0)
                {
                    s.Heap[++s.HeapLen] = maxCode = n;
                    s.Depth[n] = 0;
                }
                else
                {
                    tree[n * 2 + 1] = 0;
                }
            }

            // The pkzip format requires that at least one distance code exists,
            // and that at least one bit should be sent even if there is only one
            // possible code. So to avoid special checks later on we force at least
            // two codes of non zero frequency.
            while (s.HeapLen < 2)
            {
                node = s.Heap[++s.HeapLen] = (maxCode < 2 ? ++maxCode : 0);
                tree[node * 2] = 1;
                s.Depth[node] = 0;
                s.OptLen--;
                if (stree != null) s.StaticLen -= stree[node * 2 + 1];
                // node is 0 or 1 so it does not have extra bits
            }

            MaxCode = maxCode;

            // The elements heap[heap_len/2+1 .. heap_len] are leaves of the tree,
            // establish sub-heaps of increasing lengths:

            for (n = s.HeapLen / 2; n >= 1; n--)
                s.Pqdownheap(tree, n);

            // Construct the Huffman tree by repeatedly combining the least two
            // frequent nodes.

            node = elems; // next internal node of the tree
            do
            {
                // n = node of least frequency
                n = s.Heap[1];
                s.Heap[1] = s.Heap[s.HeapLen--];
                s.Pqdownheap(tree, 1);
                m = s.Heap[1]; // m = node of next least frequency

                s.Heap[--s.HeapMax] = n; // keep the nodes sorted by frequency
                s.Heap[--s.HeapMax] = m;

                // Create a new node father of n and m
                tree[node * 2] = (short) (tree[n * 2] + tree[m * 2]);
                s.Depth[node] = (byte) (Math.Max(s.Depth[n], s.Depth[m]) + 1);
                tree[n * 2 + 1] = tree[m * 2 + 1] = (short) node;

                // and insert the new node in the heap
                s.Heap[1] = node++;
                s.Pqdownheap(tree, 1);
            } while (s.HeapLen >= 2);

            s.Heap[--s.HeapMax] = s.Heap[1];

            // At this point, the fields freq and dad are set. We can now
            // generate the bit lengths.

            gen_bitlen(s);

            // The field len is now set, we can generate the bit codes
            gen_codes(tree, maxCode, s.BlCount);
        }

        // Generate the codes for a given tree and bit counts (which need not be
        // optimal).
        // IN assertion: the array bl_count contains the bit length statistics for
        // the given tree and the field len is set for all tree elements.
        // OUT assertion: the field code is set for all tree elements of non
        //     zero code length.
        internal static void gen_codes(short[] tree, // the tree to decorate
            int maxCode, // largest code with non zero frequency
            short[] blCount // number of codes at each bit length
        )
        {
            var nextCode = new short[MaxBits + 1]; // next code value for each bit length
            short code = 0; // running code value
            int bits; // bit index
            int n; // code index

            // The distribution counts are first used to generate the code values
            // without bit reversal.
            for (bits = 1; bits <= MaxBits; bits++)
            {
                nextCode[bits] = code = (short) ((code + blCount[bits - 1]) << 1);
            }

            // Check that the bit counts in bl_count are consistent. The last code
            // must be all ones.
            //Assert (code + bl_count[MAX_BITS]-1 == (1<<MAX_BITS)-1,
            //        "inconsistent bit counts");
            //Tracev((stderr,"\ngen_codes: max_code %d ", max_code));

            for (n = 0; n <= maxCode; n++)
            {
                int len = tree[n * 2 + 1];
                if (len == 0) continue;
                // Now reverse the bits
                tree[n * 2] = (short) (bi_reverse(nextCode[len]++, len));
            }
        }

        // Reverse the first len bits of a code, using straightforward code (a faster
        // method would use a table)
        // IN assertion: 1 <= len <= 15
        internal static int bi_reverse(int code, // the value to invert
            int len // its bit length
        )
        {
            var res = 0;
            do
            {
                res |= code & 1;
                code >>= 1;
                res <<= 1;
            } while (--len > 0);

            return res >> 1;
        }
    }
}
