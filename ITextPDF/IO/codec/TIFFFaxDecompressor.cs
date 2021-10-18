/*
* Copyright 2003-2012 by Paulo Soares.
*
* This code was originally released in 2001 by SUN (see class
* com.sun.media.imageioimpl.plugins.tiff.TIFFFaxDecompressor.java)
* using the BSD license in a specific wording. In a mail dating from
* January 23, 2008, Brian Burkhalter (@sun.com) gave us permission
* to use the code under the following version of the BSD license:
*
* Copyright (c) 2005 Sun Microsystems, Inc. All  Rights Reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions
* are met:
*
* - Redistribution of source code must retain the above copyright
*   notice, this  list of conditions and the following disclaimer.
*
* - Redistribution in binary form must reproduce the above copyright
*   notice, this list of conditions and the following disclaimer in
*   the documentation and/or other materials provided with the
*   distribution.
*
* Neither the name of Sun Microsystems, Inc. or the names of
* contributors may be used to endorse or promote products derived
* from this software without specific prior written permission.
*
* This software is provided "AS IS," without a warranty of any
* kind. ALL EXPRESS OR IMPLIED CONDITIONS, REPRESENTATIONS AND
* WARRANTIES, INCLUDING ANY IMPLIED WARRANTY OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE OR NON-INFRINGEMENT, ARE HEREBY
* EXCLUDED. SUN MIDROSYSTEMS, INC. ("SUN") AND ITS LICENSORS SHALL
* NOT BE LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE AS A RESULT OF
* USING, MODIFYING OR DISTRIBUTING THIS SOFTWARE OR ITS
* DERIVATIVES. IN NO EVENT WILL SUN OR ITS LICENSORS BE LIABLE FOR
* ANY LOST REVENUE, PROFIT OR DATA, OR FOR DIRECT, INDIRECT, SPECIAL,
* CONSEQUENTIAL, INCIDENTAL OR PUNITIVE DAMAGES, HOWEVER CAUSED AND
* REGARDLESS OF THE THEORY OF LIABILITY, ARISING OUT OF THE USE OF OR
* INABILITY TO USE THIS SOFTWARE, EVEN IF SUN HAS BEEN ADVISED OF THE
* POSSIBILITY OF SUCH DAMAGES.
*
* You acknowledge that this software is not designed or intended for
* use in the design, construction, operation or maintenance of any
* nuclear facility.
*/

using System;

namespace  IText.IO.Codec {
    /// <summary>Class that can decompress TIFF files.</summary>
    public class TIFFFaxDecompressor {
        /// <summary>The logical order of bits within a byte.</summary>
        /// <remarks>
        /// The logical order of bits within a byte.
        /// <pre>
        /// 1 = MSB-to-LSB
        /// 2 = LSB-to-MSB (flipped)
        /// </pre>
        /// </remarks>
        protected internal int fillOrder;

        protected internal int compression;

        private int t4Options;

        private int t6Options;

        public int fails;

        // Variables set by T4Options
        /// <summary>Uncompressed mode flag: 1 if uncompressed, 0 if not.</summary>
        protected internal int uncompressedMode;

        /// <summary>
        /// EOL padding flag: 1 if fill bits have been added before an EOL such
        /// that the EOL ends on a byte boundary, 0 otherwise.
        /// </summary>
        protected internal int fillBits;

        /// <summary>Coding dimensionality: 1 for 2-dimensional, 0 for 1-dimensional.</summary>
        protected internal int oneD;

        private byte[] data;

        private int bitPointer;

        private int bytePointer;

        // Output image buffer
        private byte[] buffer;

        private int w;

        private int h;

        private int bitsPerScanline;

        private int lineBitNum;

        // Data structures needed to store changing elements for the previous
        // and the current scanline
        private int changingElemSize;

        private int[] prevChangingElems;

        private int[] currChangingElems;

        // Element at which to start search in getNextChangingElement
        private int lastChangingElement;

        private readonly object Lock = new object();

        internal static int[] table1 = { 0x00, 
                // 0 bits are left in first byte - SHOULD NOT HAPPEN
                0x01, 
                // 1 bits are left in first byte
                0x03, 
                // 2 bits are left in first byte
                0x07, 
                // 3 bits are left in first byte
                0x0f, 
                // 4 bits are left in first byte
                0x1f, 
                // 5 bits are left in first byte
                0x3f, 
                // 6 bits are left in first byte
                0x7f, 
                // 7 bits are left in first byte
                0xff };

        // 8 bits are left in first byte
        internal static int[] table2 = { 0x00, 
                // 0
                0x80, 
                // 1
                0xc0, 
                // 2
                0xe0, 
                // 3
                0xf0, 
                // 4
                0xf8, 
                // 5
                0xfc, 
                // 6
                0xfe, 
                // 7
                0xff };

        // 8
        // Table to be used when fillOrder = 2, for flipping bytes.
        internal static byte[] flipTable = { 0x00, 0x80, 0x40, 0xc0, 0x20
            , 0xa0, 0x60, 0xe0, 0x10, 0x90, 0x50, 0xd0, 0x30, 0xb0, 0x70, 0xf0, 0x08, 0x88, 0x48, 0xc8, 0x28, 0xa8, 
            0x68, 0xe8, 0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8, 0x04, 0x84, 0x44, 0xc4, 0x24, 0xa4, 0x64, 
            0xe4, 0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4, 0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac, 0x6c, 0xec, 
            0x1c, 0x9c, 0x5c, 0xdc, 0x3c, 0xbc, 0x7c, 0xfc, 0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2, 0x12, 
            0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2, 0x0a, 0x8a, 0x4a, 0xca, 0x2a, 0xaa, 0x6a, 0xea, 0x1a, 0x9a, 
            0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa, 0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6, 0x16, 0x96, 0x56, 
            0xd6, 0x36, 0xb6, 0x76, 0xf6, 0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee, 0x1e, 0x9e, 0x5e, 0xde, 
            0x3e, 0xbe, 0x7e, 0xfe, 0x01, 0x81, 0x41, 0xc1, 0x21, 0xa1, 0x61, 0xe1, 0x11, 0x91, 0x51, 0xd1, 0x31, 
            0xb1, 0x71, 0xf1, 0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9, 0x19, 0x99, 0x59, 0xd9, 0x39, 0xb9, 
            0x79, 0xf9, 0x05, 0x85, 0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5, 0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 
            0xf5, 0x0d, 0x8d, 0x4d, 0xcd, 0x2d, 0xad, 0x6d, 0xed, 0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd, 
            0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3, 0x63, 0xe3, 0x13, 0x93, 0x53, 0xd3, 0x33, 0xb3, 0x73, 0xf3, 0x0b, 
            0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb, 0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb, 0x07, 0x87, 
            0x47, 0xc7, 0x27, 0xa7, 0x67, 0xe7, 0x17, 0x97, 0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7, 0x0f, 0x8f, 0x4f, 
            0xcf, 0x2f, 0xaf, 0x6f, 0xef, 0x1f, 0x9f, 0x5f, 0xdf, 0x3f, 0xbf, 0x7f, 0xff };

        // The main 10 bit white runs lookup table
        internal static short[] white = { 
                // 0 - 7
                6430, 6400, 6400, 6400, 3225, 3225, 3225, 3225, 
                // 8 - 15
                944, 944, 944, 944, 976, 976, 976, 976, 
                // 16 - 23
                1456, 1456, 1456, 1456, 1488, 1488, 1488, 1488, 
                // 24 - 31
                718, 718, 718, 718, 718, 718, 718, 718, 
                // 32 - 39
                750, 750, 750, 750, 750, 750, 750, 750, 
                // 40 - 47
                1520, 1520, 1520, 1520, 1552, 1552, 1552, 1552, 
                // 48 - 55
                428, 428, 428, 428, 428, 428, 428, 428, 
                // 56 - 63
                428, 428, 428, 428, 428, 428, 428, 428, 
                // 64 - 71
                654, 654, 654, 654, 654, 654, 654, 654, 
                // 72 - 79
                1072, 1072, 1072, 1072, 1104, 1104, 1104, 1104, 
                // 80 - 87
                1136, 1136, 1136, 1136, 1168, 1168, 1168, 1168, 
                // 88 - 95
                1200, 1200, 1200, 1200, 1232, 1232, 1232, 1232, 
                // 96 - 103
                622, 622, 622, 622, 622, 622, 622, 622, 
                // 104 - 111
                1008, 1008, 1008, 1008, 1040, 1040, 1040, 1040, 
                // 112 - 119
                44, 44, 44, 44, 44, 44, 44, 44, 
                // 120 - 127
                44, 44, 44, 44, 44, 44, 44, 44, 
                // 128 - 135
                396, 396, 396, 396, 396, 396, 396, 396, 
                // 136 - 143
                396, 396, 396, 396, 396, 396, 396, 396, 
                // 144 - 151
                1712, 1712, 1712, 1712, 1744, 1744, 1744, 1744, 
                // 152 - 159
                846, 846, 846, 846, 846, 846, 846, 846, 
                // 160 - 167
                1264, 1264, 1264, 1264, 1296, 1296, 1296, 1296, 
                // 168 - 175
                1328, 1328, 1328, 1328, 1360, 1360, 1360, 1360, 
                // 176 - 183
                1392, 1392, 1392, 1392, 1424, 1424, 1424, 1424, 
                // 184 - 191
                686, 686, 686, 686, 686, 686, 686, 686, 
                // 192 - 199
                910, 910, 910, 910, 910, 910, 910, 910, 
                // 200 - 207
                1968, 1968, 1968, 1968, 2000, 2000, 2000, 2000, 
                // 208 - 215
                2032, 2032, 2032, 2032, 16, 16, 16, 16, 
                // 216 - 223
                10257, 10257, 10257, 10257, 12305, 12305, 12305, 12305, 
                // 224 - 231
                330, 330, 330, 330, 330, 330, 330, 330, 
                // 232 - 239
                330, 330, 330, 330, 330, 330, 330, 330, 
                // 240 - 247
                330, 330, 330, 330, 330, 330, 330, 330, 
                // 248 - 255
                330, 330, 330, 330, 330, 330, 330, 330, 
                // 256 - 263
                362, 362, 362, 362, 362, 362, 362, 362, 
                // 264 - 271
                362, 362, 362, 362, 362, 362, 362, 362, 
                // 272 - 279
                362, 362, 362, 362, 362, 362, 362, 362, 
                // 280 - 287
                362, 362, 362, 362, 362, 362, 362, 362, 
                // 288 - 295
                878, 878, 878, 878, 878, 878, 878, 878, 
                // 296 - 303
                1904, 1904, 1904, 1904, 1936, 1936, 1936, 1936, 
                // 304 - 311
                -18413, -18413, -16365, -16365, -14317, -14317, -10221, -10221, 
                // 312 - 319
                590, 590, 590, 590, 590, 590, 590, 590, 
                // 320 - 327
                782, 782, 782, 782, 782, 782, 782, 782, 
                // 328 - 335
                1584, 1584, 1584, 1584, 1616, 1616, 1616, 1616, 
                // 336 - 343
                1648, 1648, 1648, 1648, 1680, 1680, 1680, 1680, 
                // 344 - 351
                814, 814, 814, 814, 814, 814, 814, 814, 
                // 352 - 359
                1776, 1776, 1776, 1776, 1808, 1808, 1808, 1808, 
                // 360 - 367
                1840, 1840, 1840, 1840, 1872, 1872, 1872, 1872, 
                // 368 - 375
                6157, 6157, 6157, 6157, 6157, 6157, 6157, 6157, 
                // 376 - 383
                6157, 6157, 6157, 6157, 6157, 6157, 6157, 6157, 
                // 384 - 391
                -12275, -12275, -12275, -12275, -12275, -12275, -12275, -12275, 
                // 392 - 399
                -12275, -12275, -12275, -12275, -12275, -12275, -12275, -12275, 
                // 400 - 407
                14353, 14353, 14353, 14353, 16401, 16401, 16401, 16401, 
                // 408 - 415
                22547, 22547, 24595, 24595, 20497, 20497, 20497, 20497, 
                // 416 - 423
                18449, 18449, 18449, 18449, 26643, 26643, 28691, 28691, 
                // 424 - 431
                30739, 30739, -32749, -32749, -30701, -30701, -28653, -28653, 
                // 432 - 439
                -26605, -26605, -24557, -24557, -22509, -22509, -20461, -20461, 
                // 440 - 447
                8207, 8207, 8207, 8207, 8207, 8207, 8207, 8207, 
                // 448 - 455
                72, 72, 72, 72, 72, 72, 72, 72, 
                // 456 - 463
                72, 72, 72, 72, 72, 72, 72, 72, 
                // 464 - 471
                72, 72, 72, 72, 72, 72, 72, 72, 
                // 472 - 479
                72, 72, 72, 72, 72, 72, 72, 72, 
                // 480 - 487
                72, 72, 72, 72, 72, 72, 72, 72, 
                // 488 - 495
                72, 72, 72, 72, 72, 72, 72, 72, 
                // 496 - 503
                72, 72, 72, 72, 72, 72, 72, 72, 
                // 504 - 511
                72, 72, 72, 72, 72, 72, 72, 72, 
                // 512 - 519
                104, 104, 104, 104, 104, 104, 104, 104, 
                // 520 - 527
                104, 104, 104, 104, 104, 104, 104, 104, 
                // 528 - 535
                104, 104, 104, 104, 104, 104, 104, 104, 
                // 536 - 543
                104, 104, 104, 104, 104, 104, 104, 104, 
                // 544 - 551
                104, 104, 104, 104, 104, 104, 104, 104, 
                // 552 - 559
                104, 104, 104, 104, 104, 104, 104, 104, 
                // 560 - 567
                104, 104, 104, 104, 104, 104, 104, 104, 
                // 568 - 575
                104, 104, 104, 104, 104, 104, 104, 104, 
                // 576 - 583
                4107, 4107, 4107, 4107, 4107, 4107, 4107, 4107, 
                // 584 - 591
                4107, 4107, 4107, 4107, 4107, 4107, 4107, 4107, 
                // 592 - 599
                4107, 4107, 4107, 4107, 4107, 4107, 4107, 4107, 
                // 600 - 607
                4107, 4107, 4107, 4107, 4107, 4107, 4107, 4107, 
                // 608 - 615
                266, 266, 266, 266, 266, 266, 266, 266, 
                // 616 - 623
                266, 266, 266, 266, 266, 266, 266, 266, 
                // 624 - 631
                266, 266, 266, 266, 266, 266, 266, 266, 
                // 632 - 639
                266, 266, 266, 266, 266, 266, 266, 266, 
                // 640 - 647
                298, 298, 298, 298, 298, 298, 298, 298, 
                // 648 - 655
                298, 298, 298, 298, 298, 298, 298, 298, 
                // 656 - 663
                298, 298, 298, 298, 298, 298, 298, 298, 
                // 664 - 671
                298, 298, 298, 298, 298, 298, 298, 298, 
                // 672 - 679
                524, 524, 524, 524, 524, 524, 524, 524, 
                // 680 - 687
                524, 524, 524, 524, 524, 524, 524, 524, 
                // 688 - 695
                556, 556, 556, 556, 556, 556, 556, 556, 
                // 696 - 703
                556, 556, 556, 556, 556, 556, 556, 556, 
                // 704 - 711
                136, 136, 136, 136, 136, 136, 136, 136, 
                // 712 - 719
                136, 136, 136, 136, 136, 136, 136, 136, 
                // 720 - 727
                136, 136, 136, 136, 136, 136, 136, 136, 
                // 728 - 735
                136, 136, 136, 136, 136, 136, 136, 136, 
                // 736 - 743
                136, 136, 136, 136, 136, 136, 136, 136, 
                // 744 - 751
                136, 136, 136, 136, 136, 136, 136, 136, 
                // 752 - 759
                136, 136, 136, 136, 136, 136, 136, 136, 
                // 760 - 767
                136, 136, 136, 136, 136, 136, 136, 136, 
                // 768 - 775
                168, 168, 168, 168, 168, 168, 168, 168, 
                // 776 - 783
                168, 168, 168, 168, 168, 168, 168, 168, 
                // 784 - 791
                168, 168, 168, 168, 168, 168, 168, 168, 
                // 792 - 799
                168, 168, 168, 168, 168, 168, 168, 168, 
                // 800 - 807
                168, 168, 168, 168, 168, 168, 168, 168, 
                // 808 - 815
                168, 168, 168, 168, 168, 168, 168, 168, 
                // 816 - 823
                168, 168, 168, 168, 168, 168, 168, 168, 
                // 824 - 831
                168, 168, 168, 168, 168, 168, 168, 168, 
                // 832 - 839
                460, 460, 460, 460, 460, 460, 460, 460, 
                // 840 - 847
                460, 460, 460, 460, 460, 460, 460, 460, 
                // 848 - 855
                492, 492, 492, 492, 492, 492, 492, 492, 
                // 856 - 863
                492, 492, 492, 492, 492, 492, 492, 492, 
                // 864 - 871
                2059, 2059, 2059, 2059, 2059, 2059, 2059, 2059, 
                // 872 - 879
                2059, 2059, 2059, 2059, 2059, 2059, 2059, 2059, 
                // 880 - 887
                2059, 2059, 2059, 2059, 2059, 2059, 2059, 2059, 
                // 888 - 895
                2059, 2059, 2059, 2059, 2059, 2059, 2059, 2059, 
                // 896 - 903
                200, 200, 200, 200, 200, 200, 200, 200, 
                // 904 - 911
                200, 200, 200, 200, 200, 200, 200, 200, 
                // 912 - 919
                200, 200, 200, 200, 200, 200, 200, 200, 
                // 920 - 927
                200, 200, 200, 200, 200, 200, 200, 200, 
                // 928 - 935
                200, 200, 200, 200, 200, 200, 200, 200, 
                // 936 - 943
                200, 200, 200, 200, 200, 200, 200, 200, 
                // 944 - 951
                200, 200, 200, 200, 200, 200, 200, 200, 
                // 952 - 959
                200, 200, 200, 200, 200, 200, 200, 200, 
                // 960 - 967
                232, 232, 232, 232, 232, 232, 232, 232, 
                // 968 - 975
                232, 232, 232, 232, 232, 232, 232, 232, 
                // 976 - 983
                232, 232, 232, 232, 232, 232, 232, 232, 
                // 984 - 991
                232, 232, 232, 232, 232, 232, 232, 232, 
                // 992 - 999
                232, 232, 232, 232, 232, 232, 232, 232, 
                // 1000 - 1007
                232, 232, 232, 232, 232, 232, 232, 232, 
                // 1008 - 1015
                232, 232, 232, 232, 232, 232, 232, 232, 
                // 1016 - 1023
                232, 232, 232, 232, 232, 232, 232, 232 };

        // Additional make up codes for both White and Black runs
        //    static short[] additionalMakeup = {
        //        28679,  28679,  31752,  (short)32777,
        //        (short)33801,  (short)34825,  (short)35849,  (short)36873,
        //        (short)29703,  (short)29703,  (short)30727,  (short)30727,
        //        (short)37897,  (short)38921,  (short)39945,  (short)40969
        //    };
        //replace with constants without overload
        public static short[] additionalMakeup = { 28679, 28679, 31752, -32759, -31735, -30711, -29687
            , -28663, 29703, 29703, 30727, 30727, -27639, -26615, -25591, -24567 };

        // Initial black run look up table, uses the first 4 bits of a code
        internal static short[] initBlack = { 
                // 0 - 7
                3226, 6412, 200, 168, 38, 38, 134, 134, 
                // 8 - 15
                100, 100, 100, 100, 68, 68, 68, 68 };

        //
        internal static short[] twoBitBlack = { 292, 260, 226, 226 };

        // 0 - 3
        // Main black run table, using the last 9 bits of possible 13 bit code
        internal static short[] black = { 
                // 0 - 7
                62, 62, 30, 30, 0, 0, 0, 0, 
                // 8 - 15
                0, 0, 0, 0, 0, 0, 0, 0, 
                // 16 - 23
                0, 0, 0, 0, 0, 0, 0, 0, 
                // 24 - 31
                0, 0, 0, 0, 0, 0, 0, 0, 
                // 32 - 39
                3225, 3225, 3225, 3225, 3225, 3225, 3225, 3225, 
                // 40 - 47
                3225, 3225, 3225, 3225, 3225, 3225, 3225, 3225, 
                // 48 - 55
                3225, 3225, 3225, 3225, 3225, 3225, 3225, 3225, 
                // 56 - 63
                3225, 3225, 3225, 3225, 3225, 3225, 3225, 3225, 
                // 64 - 71
                588, 588, 588, 588, 588, 588, 588, 588, 
                // 72 - 79
                1680, 1680, 20499, 22547, 24595, 26643, 1776, 1776, 
                // 80 - 87
                1808, 1808, -24557, -22509, -20461, -18413, 1904, 1904, 
                // 88 - 95
                1936, 1936, -16365, -14317, 782, 782, 782, 782, 
                // 96 - 103
                814, 814, 814, 814, -12269, -10221, 10257, 10257, 
                // 104 - 111
                12305, 12305, 14353, 14353, 16403, 18451, 1712, 1712, 
                // 112 - 119
                1744, 1744, 28691, 30739, -32749, -30701, -28653, -26605, 
                // 120 - 127
                2061, 2061, 2061, 2061, 2061, 2061, 2061, 2061, 
                // 128 - 135
                424, 424, 424, 424, 424, 424, 424, 424, 
                // 136 - 143
                424, 424, 424, 424, 424, 424, 424, 424, 
                // 144 - 151
                424, 424, 424, 424, 424, 424, 424, 424, 
                // 152 - 159
                424, 424, 424, 424, 424, 424, 424, 424, 
                // 160 - 167
                750, 750, 750, 750, 1616, 1616, 1648, 1648, 
                // 168 - 175
                1424, 1424, 1456, 1456, 1488, 1488, 1520, 1520, 
                // 176 - 183
                1840, 1840, 1872, 1872, 1968, 1968, 8209, 8209, 
                // 184 - 191
                524, 524, 524, 524, 524, 524, 524, 524, 
                // 192 - 199
                556, 556, 556, 556, 556, 556, 556, 556, 
                // 200 - 207
                1552, 1552, 1584, 1584, 2000, 2000, 2032, 2032, 
                // 208 - 215
                976, 976, 1008, 1008, 1040, 1040, 1072, 1072, 
                // 216 - 223
                1296, 1296, 1328, 1328, 718, 718, 718, 718, 
                // 224 - 231
                456, 456, 456, 456, 456, 456, 456, 456, 
                // 232 - 239
                456, 456, 456, 456, 456, 456, 456, 456, 
                // 240 - 247
                456, 456, 456, 456, 456, 456, 456, 456, 
                // 248 - 255
                456, 456, 456, 456, 456, 456, 456, 456, 
                // 256 - 263
                326, 326, 326, 326, 326, 326, 326, 326, 
                // 264 - 271
                326, 326, 326, 326, 326, 326, 326, 326, 
                // 272 - 279
                326, 326, 326, 326, 326, 326, 326, 326, 
                // 280 - 287
                326, 326, 326, 326, 326, 326, 326, 326, 
                // 288 - 295
                326, 326, 326, 326, 326, 326, 326, 326, 
                // 296 - 303
                326, 326, 326, 326, 326, 326, 326, 326, 
                // 304 - 311
                326, 326, 326, 326, 326, 326, 326, 326, 
                // 312 - 319
                326, 326, 326, 326, 326, 326, 326, 326, 
                // 320 - 327
                358, 358, 358, 358, 358, 358, 358, 358, 
                // 328 - 335
                358, 358, 358, 358, 358, 358, 358, 358, 
                // 336 - 343
                358, 358, 358, 358, 358, 358, 358, 358, 
                // 344 - 351
                358, 358, 358, 358, 358, 358, 358, 358, 
                // 352 - 359
                358, 358, 358, 358, 358, 358, 358, 358, 
                // 360 - 367
                358, 358, 358, 358, 358, 358, 358, 358, 
                // 368 - 375
                358, 358, 358, 358, 358, 358, 358, 358, 
                // 376 - 383
                358, 358, 358, 358, 358, 358, 358, 358, 
                // 384 - 391
                490, 490, 490, 490, 490, 490, 490, 490, 
                // 392 - 399
                490, 490, 490, 490, 490, 490, 490, 490, 
                // 400 - 407
                4113, 4113, 6161, 6161, 848, 848, 880, 880, 
                // 408 - 415
                912, 912, 944, 944, 622, 622, 622, 622, 
                // 416 - 423
                654, 654, 654, 654, 1104, 1104, 1136, 1136, 
                // 424 - 431
                1168, 1168, 1200, 1200, 1232, 1232, 1264, 1264, 
                // 432 - 439
                686, 686, 686, 686, 1360, 1360, 1392, 1392, 
                // 440 - 447
                12, 12, 12, 12, 12, 12, 12, 12, 
                // 448 - 455
                390, 390, 390, 390, 390, 390, 390, 390, 
                // 456 - 463
                390, 390, 390, 390, 390, 390, 390, 390, 
                // 464 - 471
                390, 390, 390, 390, 390, 390, 390, 390, 
                // 472 - 479
                390, 390, 390, 390, 390, 390, 390, 390, 
                // 480 - 487
                390, 390, 390, 390, 390, 390, 390, 390, 
                // 488 - 495
                390, 390, 390, 390, 390, 390, 390, 390, 
                // 496 - 503
                390, 390, 390, 390, 390, 390, 390, 390, 
                // 504 - 511
                390, 390, 390, 390, 390, 390, 390, 390 };

        internal static byte[] twoDCodes = { 
                // 0 - 7
                80, 88, 23, 71, 30, 30, 62, 62, 
                // 8 - 15
                4, 4, 4, 4, 4, 4, 4, 4, 
                // 16 - 23
                11, 11, 11, 11, 11, 11, 11, 11, 
                // 24 - 31
                11, 11, 11, 11, 11, 11, 11, 11, 
                // 32 - 39
                35, 35, 35, 35, 35, 35, 35, 35, 
                // 40 - 47
                35, 35, 35, 35, 35, 35, 35, 35, 
                // 48 - 55
                51, 51, 51, 51, 51, 51, 51, 51, 
                // 56 - 63
                51, 51, 51, 51, 51, 51, 51, 51, 
                // 64 - 71
                41, 41, 41, 41, 41, 41, 41, 41, 
                // 72 - 79
                41, 41, 41, 41, 41, 41, 41, 41, 
                // 80 - 87
                41, 41, 41, 41, 41, 41, 41, 41, 
                // 88 - 95
                41, 41, 41, 41, 41, 41, 41, 41, 
                // 96 - 103
                41, 41, 41, 41, 41, 41, 41, 41, 
                // 104 - 111
                41, 41, 41, 41, 41, 41, 41, 41, 
                // 112 - 119
                41, 41, 41, 41, 41, 41, 41, 41, 
                // 120 - 127
                41, 41, 41, 41, 41, 41, 41, 41 };

        /// <summary>
        /// Invokes the superclass method and then sets instance variables on
        /// the basis of the metadata set on this decompressor.
        /// </summary>
        /// <param name="fillOrder">The fill order</param>
        /// <param name="compression">The compression algorithm</param>
        /// <param name="t4Options">The T4 options</param>
        /// <param name="t6Options">The T6 options</param>
        public virtual void SetOptions(int fillOrder, int compression, int t4Options, int t6Options) {
            this.fillOrder = fillOrder;
            this.compression = compression;
            this.t4Options = t4Options;
            this.t6Options = t6Options;
            oneD = t4Options & 0x01;
            uncompressedMode = (t4Options & 0x02) >> 1;
            fillBits = (t4Options & 0x04) >> 2;
        }

        public virtual void DecodeRaw(byte[] buffer, byte[] compData, int w, int h) {
            this.buffer = buffer;
            data = compData;
            this.w = w;
            this.h = h;
            bitsPerScanline = w;
            lineBitNum = 0;
            bitPointer = 0;
            bytePointer = 0;
            prevChangingElems = new int[w + 1];
            currChangingElems = new int[w + 1];
            fails = 0;
            try {
                if (compression == TIFFConstants.COMPRESSION_CCITTRLE) {
                    DecodeRLE();
                }
                else {
                    if (compression == TIFFConstants.COMPRESSION_CCITTFAX3) {
                        DecodeT4();
                    }
                    else {
                        if (compression == TIFFConstants.COMPRESSION_CCITTFAX4) {
                            uncompressedMode = (t6Options & 0x02) >> 1;
                            DecodeT6();
                        }
                        else {
                            throw new IOException(IOException.UnknownCompressionType1).SetMessageParams(compression);
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException) {
            }
        }

        //ignore
        public virtual void DecodeRLE() {
            for (var i = 0; i < h; i++) {
                // Decode the line.
                DecodeNextScanline();
                // Advance to the next byte boundary if not already there.
                if (bitPointer != 0) {
                    bytePointer++;
                    bitPointer = 0;
                }
                // Update the total number of bits.
                lineBitNum += bitsPerScanline;
            }
        }

        public virtual void DecodeNextScanline() {
            var bits = 0;
            var code = 0;
            var isT = 0;
            int current;
            int entry;
            int twoBits;
            var isWhite = true;
            var bitOffset = 0;
            // Initialize starting of the changing elements array
            changingElemSize = 0;
            // While scanline not complete
            while (bitOffset < w) {
                // Mark start of white run.
                var runOffset = bitOffset;
                while (isWhite && bitOffset < w) {
                    // White run
                    current = NextNBits(10);
                    entry = white[current];
                    // Get the 3 fields from the entry
                    isT = entry & 0x0001;
                    bits = ((int)(((uint)entry) >> 1)) & 0x0f;
                    if (bits == 12) {
                        // Additional Make up code
                        // Get the next 2 bits
                        twoBits = NextLesserThan8Bits(2);
                        // Consolidate the 2 new bits and last 2 bits into 4 bits
                        current = ((current << 2) & 0x000c) | twoBits;
                        entry = additionalMakeup[current];
                        bits = ((int)(((uint)entry) >> 1)) & 0x07;
                        // 3 bits 0000 0111
                        code = ((int)(((uint)entry) >> 4)) & 0x0fff;
                        // 12 bits
                        bitOffset += code;
                        // Skip white run
                        UpdatePointer(4 - bits);
                    }
                    else {
                        if (bits == 0) {
                            // ERROR
                            ++fails;
                        }
                        else
                        {
	                        // XXX return?
                            if (bits == 15) {
                                // EOL
                                //
                                // Instead of throwing an exception, assume that the
                                // EOL was premature; emit a warning and return.
                                //
                                ++fails;
                                return;
                            }

                            // 11 bits - 0000 0111 1111 1111 = 0x07ff
                            code = ((int)(((uint)entry) >> 5)) & 0x07ff;
                            bitOffset += code;
                            UpdatePointer(10 - bits);
                            if (isT == 0) {
	                            isWhite = false;
	                            currChangingElems[changingElemSize++] = bitOffset;
                            }
                        }
                    }
                }
                // Check whether this run completed one width
                if (bitOffset == w) {
                    // If the white run has not been terminated then ensure that
                    // the next code word is a terminating code for a white run
                    // of length zero.
                    var runLength = bitOffset - runOffset;
                    if (isWhite && runLength != 0 && runLength % 64 == 0 && NextNBits(8) != 0x35) {
                        ++fails;
                        UpdatePointer(8);
                    }
                    break;
                }
                // Mark start of black run.
                runOffset = bitOffset;
                while (!isWhite && bitOffset < w) {
                    // Black run
                    current = NextLesserThan8Bits(4);
                    entry = initBlack[current];
                    // Get the 3 fields from the entry
                    isT = entry & 0x0001;
                    bits = ((int)(((uint)entry) >> 1)) & 0x000f;
                    code = ((int)(((uint)entry) >> 5)) & 0x07ff;
                    if (code == 100) {
                        current = NextNBits(9);
                        entry = black[current];
                        // Get the 3 fields from the entry
                        isT = entry & 0x0001;
                        bits = ((int)(((uint)entry) >> 1)) & 0x000f;
                        code = ((int)(((uint)entry) >> 5)) & 0x07ff;
                        if (bits == 12) {
                            // Additional makeup codes
                            UpdatePointer(5);
                            current = NextLesserThan8Bits(4);
                            entry = additionalMakeup[current];
                            bits = ((int)(((uint)entry) >> 1)) & 0x07;
                            // 3 bits 0000 0111
                            code = ((int)(((uint)entry) >> 4)) & 0x0fff;
                            // 12 bits
                            SetToBlack(bitOffset, code);
                            bitOffset += code;
                            UpdatePointer(4 - bits);
                        }
                        else
                        {
	                        if (bits == 15) {
                                //
                                // Instead of throwing an exception, assume that the
                                // EOL was premature; emit a warning and return.
                                //
                                ++fails;
                                return;
                            }

	                        SetToBlack(bitOffset, code);
	                        bitOffset += code;
	                        UpdatePointer(9 - bits);
	                        if (isT == 0) {
		                        isWhite = true;
		                        currChangingElems[changingElemSize++] = bitOffset;
	                        }
                        }
                    }
                    else {
                        if (code == 200) {
                            // Is a Terminating code
                            current = NextLesserThan8Bits(2);
                            entry = twoBitBlack[current];
                            code = ((int)(((uint)entry) >> 5)) & 0x07ff;
                            bits = ((int)(((uint)entry) >> 1)) & 0x0f;
                            SetToBlack(bitOffset, code);
                            bitOffset += code;
                            UpdatePointer(2 - bits);
                            isWhite = true;
                            currChangingElems[changingElemSize++] = bitOffset;
                        }
                        else {
                            // Is a Terminating code
                            SetToBlack(bitOffset, code);
                            bitOffset += code;
                            UpdatePointer(4 - bits);
                            isWhite = true;
                            currChangingElems[changingElemSize++] = bitOffset;
                        }
                    }
                }
                // Check whether this run completed one width
                if (bitOffset == w) {
                    // If the black run has not been terminated then ensure that
                    // the next code word is a terminating code for a black run
                    // of length zero.
                    var runLength = bitOffset - runOffset;
                    if (!isWhite && runLength != 0 && runLength % 64 == 0 && NextNBits(10) != 0x37) {
                        ++fails;
                        UpdatePointer(10);
                    }
                    break;
                }
            }
            currChangingElems[changingElemSize++] = bitOffset;
        }

        public virtual void DecodeT4() {
            var height = h;
            int a0;
            int a1;
            int b1;
            int b2;
            var b = new int[2];
            int entry;
            int code;
            int bits;
            int color;
            bool isWhite;
            var currIndex = 0;
            int[] temp;
            if (data.Length < 2) {
                throw new Exception("Insufficient data to read initial EOL.");
            }
            // The data should start with an EOL code
            var next12 = NextNBits(12);
            if (next12 != 1) {
                ++fails;
            }
            UpdatePointer(12);
            // Find the first one-dimensionally encoded line.
            var modeFlag = 0;
            var lines = -1;
            // indicates imaginary line before first actual line.
            while (modeFlag != 1) {
                try {
                    modeFlag = FindNextLine();
                    lines++;
                }
                catch (Exception) {
                    // Normally 'lines' will be 0 on exiting loop.
                    throw new Exception("No reference line present.");
                }
            }
            int bitOffset;
            // Then the 1D encoded scanline data will occur, changing elements
            // array gets set.
            DecodeNextScanline();
            lines++;
            lineBitNum += bitsPerScanline;
            while (lines < height) {
                // Every line must begin with an EOL followed by a bit which
                // indicates whether the following scanline is 1D or 2D encoded.
                try {
                    modeFlag = FindNextLine();
                }
                catch (Exception) {
                    ++fails;
                    break;
                }
                if (modeFlag == 0) {
                    // 2D encoded scanline follows
                    // Initialize previous scanlines changing elements, and
                    // initialize current scanline's changing elements array
                    temp = prevChangingElems;
                    prevChangingElems = currChangingElems;
                    currChangingElems = temp;
                    currIndex = 0;
                    // a0 has to be set just before the start of this scanline.
                    a0 = -1;
                    isWhite = true;
                    bitOffset = 0;
                    lastChangingElement = 0;
                    while (bitOffset < w) {
                        // Get the next changing element
                        GetNextChangingElement(a0, isWhite, b);
                        b1 = b[0];
                        b2 = b[1];
                        // Get the next seven bits
                        entry = NextLesserThan8Bits(7);
                        // Run these through the 2DCodes table
                        entry = twoDCodes[entry] & 0xff;
                        // Get the code and the number of bits used up
                        code = (int)(((uint)(entry & 0x78)) >> 3);
                        bits = entry & 0x07;
                        if (code == 0) {
                            if (!isWhite) {
                                SetToBlack(bitOffset, b2 - bitOffset);
                            }
                            bitOffset = a0 = b2;
                            // Set pointer to consume the correct number of bits.
                            UpdatePointer(7 - bits);
                        }
                        else {
                            if (code == 1) {
                                // Horizontal
                                UpdatePointer(7 - bits);
                                // identify the next 2 codes.
                                int number;
                                if (isWhite) {
                                    number = DecodeWhiteCodeWord();
                                    bitOffset += number;
                                    currChangingElems[currIndex++] = bitOffset;
                                    number = DecodeBlackCodeWord();
                                    SetToBlack(bitOffset, number);
                                    bitOffset += number;
                                    currChangingElems[currIndex++] = bitOffset;
                                }
                                else {
                                    number = DecodeBlackCodeWord();
                                    SetToBlack(bitOffset, number);
                                    bitOffset += number;
                                    currChangingElems[currIndex++] = bitOffset;
                                    number = DecodeWhiteCodeWord();
                                    bitOffset += number;
                                    currChangingElems[currIndex++] = bitOffset;
                                }
                                a0 = bitOffset;
                            }
                            else {
                                if (code <= 8) {
                                    // Vertical
                                    a1 = b1 + (code - 5);
                                    currChangingElems[currIndex++] = a1;
                                    // We write the current color till a1 - 1 pos,
                                    // since a1 is where the next color starts
                                    if (!isWhite) {
                                        SetToBlack(bitOffset, a1 - bitOffset);
                                    }
                                    bitOffset = a0 = a1;
                                    isWhite = !isWhite;
                                    UpdatePointer(7 - bits);
                                }
                                else {
                                    ++fails;
                                    // Find the next one-dimensionally encoded line.
                                    var numLinesTested = 0;
                                    while (modeFlag != 1) {
                                        try {
                                            modeFlag = FindNextLine();
                                            numLinesTested++;
                                        }
                                        catch (Exception) {
                                            return;
                                        }
                                    }
                                    lines += numLinesTested - 1;
                                    UpdatePointer(13);
                                    break;
                                }
                            }
                        }
                    }
                    // Add the changing element beyond the current scanline for the
                    // other color too
                    currChangingElems[currIndex++] = bitOffset;
                    changingElemSize = currIndex;
                }
                else {
                    // modeFlag == 1
                    // 1D encoded scanline follows
                    DecodeNextScanline();
                }
                lineBitNum += bitsPerScanline;
                lines++;
            }
        }

        // while(lines < height)
        public virtual void DecodeT6() {
            lock (Lock) {
                var height = h;
                int a0;
                int a1;
                int b1;
                int b2;
                int entry;
                int code;
                int bits;
                bool isWhite;
                int currIndex;
                int[] temp;
                // Return values from getNextChangingElement
                var b = new int[2];
                // uncompressedMode - have written some code for this, but this
                // has not been tested due to lack of test images using this optional
                // extension. This code is when code == 11. aastha 03/03/1999
                // Local cached reference
                var cce = currChangingElems;
                // Assume invisible preceding row of all white pixels and insert
                // both black and white changing elements beyond the end of this
                // imaginary scanline.
                changingElemSize = 0;
                cce[changingElemSize++] = w;
                cce[changingElemSize++] = w;
                int bitOffset;
                for (var lines = 0; lines < height; lines++) {
                    // a0 has to be set just before the start of the scanline.
                    a0 = -1;
                    isWhite = true;
                    // Assign the changing elements of the previous scanline to
                    // prevChangingElems and start putting this new scanline's
                    // changing elements into the currChangingElems.
                    temp = prevChangingElems;
                    prevChangingElems = currChangingElems;
                    cce = currChangingElems = temp;
                    currIndex = 0;
                    // Start decoding the scanline
                    bitOffset = 0;
                    // Reset search start position for getNextChangingElement
                    lastChangingElement = 0;
                    // Till one whole scanline is decoded
                    while (bitOffset < w) {
                        // Get the next changing element
                        GetNextChangingElement(a0, isWhite, b);
                        b1 = b[0];
                        b2 = b[1];
                        // Get the next seven bits
                        entry = NextLesserThan8Bits(7);
                        // Run these through the 2DCodes table
                        entry = twoDCodes[entry] & 0xff;
                        // Get the code and the number of bits used up
                        code = (int)(((uint)(entry & 0x78)) >> 3);
                        bits = entry & 0x07;
                        if (code == 0) {
                            // Pass
                            // We always assume WhiteIsZero format for fax.
                            if (!isWhite) {
                                if (b2 > w) {
                                    b2 = w;
                                }
                                SetToBlack(bitOffset, b2 - bitOffset);
                            }
                            bitOffset = a0 = b2;
                            // Set pointer to only consume the correct number of bits.
                            UpdatePointer(7 - bits);
                        }
                        else {
                            if (code == 1) {
                                // Horizontal
                                // Set pointer to only consume the correct number of bits.
                                UpdatePointer(7 - bits);
                                // identify the next 2 alternating color codes.
                                int number;
                                if (isWhite) {
                                    // Following are white and black runs
                                    number = DecodeWhiteCodeWord();
                                    bitOffset += number;
                                    cce[currIndex++] = bitOffset;
                                    number = DecodeBlackCodeWord();
                                    if (number > w - bitOffset) {
                                        number = w - bitOffset;
                                    }
                                    SetToBlack(bitOffset, number);
                                    bitOffset += number;
                                    cce[currIndex++] = bitOffset;
                                }
                                else {
                                    // First a black run and then a white run follows
                                    number = DecodeBlackCodeWord();
                                    if (number > w - bitOffset) {
                                        number = w - bitOffset;
                                    }
                                    SetToBlack(bitOffset, number);
                                    bitOffset += number;
                                    cce[currIndex++] = bitOffset;
                                    number = DecodeWhiteCodeWord();
                                    bitOffset += number;
                                    cce[currIndex++] = bitOffset;
                                }
                                a0 = bitOffset;
                            }
                            else {
                                if (code <= 8) {
                                    // Vertical
                                    a1 = b1 + (code - 5);
                                    cce[currIndex++] = a1;
                                    // We write the current color till a1 - 1 pos,
                                    // since a1 is where the next color starts
                                    if (!isWhite) {
                                        if (a1 > w) {
                                            a1 = w;
                                        }
                                        SetToBlack(bitOffset, a1 - bitOffset);
                                    }
                                    bitOffset = a0 = a1;
                                    isWhite = !isWhite;
                                    UpdatePointer(7 - bits);
                                }
                                else {
                                    if (code == 11) {
                                        var entranceCode = NextLesserThan8Bits(3);
                                        var zeros = 0;
                                        var exit = false;
                                        while (!exit) {
                                            while (NextLesserThan8Bits(1) != 1) {
                                                zeros++;
                                            }
                                            if (zeros > 5) {
                                                // Exit code
                                                // Zeros before exit code
                                                zeros = zeros - 6;
                                                if (!isWhite && (zeros > 0)) {
                                                    cce[currIndex++] = bitOffset;
                                                }
                                                // Zeros before the exit code
                                                bitOffset += zeros;
                                                if (zeros > 0) {
                                                    // Some zeros have been written
                                                    isWhite = true;
                                                }
                                                // Read in the bit which specifies the color of
                                                // the following run
                                                if (NextLesserThan8Bits(1) == 0) {
                                                    if (!isWhite) {
                                                        cce[currIndex++] = bitOffset;
                                                    }
                                                    isWhite = true;
                                                }
                                                else {
                                                    if (isWhite) {
                                                        cce[currIndex++] = bitOffset;
                                                    }
                                                    isWhite = false;
                                                }
                                                exit = true;
                                            }
                                            if (zeros == 5) {
                                                if (!isWhite) {
                                                    cce[currIndex++] = bitOffset;
                                                }
                                                bitOffset += zeros;
                                                // Last thing written was white
                                                isWhite = true;
                                            }
                                            else {
                                                bitOffset += zeros;
                                                cce[currIndex++] = bitOffset;
                                                SetToBlack(bitOffset, 1);
                                                ++bitOffset;
                                                // Last thing written was black
                                                isWhite = false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // while bitOffset < w
                    // Add the changing element beyond the current scanline for the
                    // other color too, if not already added previously
                    if (currIndex <= w) {
                        cce[currIndex++] = bitOffset;
                    }
                    // Number of changing elements in this scanline.
                    changingElemSize = currIndex;
                    lineBitNum += bitsPerScanline;
                }
            }
        }

        // for lines < height
        private void SetToBlack(int bitNum, int numBits) {
            // bitNum is relative to current scanline so bump it by lineBitNum
            bitNum += lineBitNum;
            var lastBit = bitNum + numBits;
            var byteNum = bitNum >> 3;
            // Handle bits in first byte
            var shift = bitNum & 0x7;
            if (shift > 0) {
                var maskVal = 1 << (7 - shift);
                var val = buffer[byteNum];
                while (maskVal > 0 && bitNum < lastBit) {
                    val |= (byte)maskVal;
                    maskVal >>= 1;
                    ++bitNum;
                }
                buffer[byteNum] = val;
            }
            // Fill in 8 bits at a time
            byteNum = bitNum >> 3;
            while (bitNum < lastBit - 7) {
                buffer[byteNum++] = 255;
                bitNum += 8;
            }
            // Fill in remaining bits
            while (bitNum < lastBit) {
                byteNum = bitNum >> 3;
                buffer[byteNum] |= (byte)(1 << (7 - (bitNum & 0x7)));
                ++bitNum;
            }
        }

        // Returns run length
        private int DecodeWhiteCodeWord() {
            int current;
            int entry;
            int bits;
            int isT;
            int twoBits;
            var code = -1;
            var runLength = 0;
            var isWhite = true;
            while (isWhite) {
                current = NextNBits(10);
                entry = white[current];
                // Get the 3 fields from the entry
                isT = entry & 0x0001;
                bits = ((int)(((uint)entry) >> 1)) & 0x0f;
                if (bits == 12) {
                    // Additional Make up code
                    // Get the next 2 bits
                    twoBits = NextLesserThan8Bits(2);
                    // Consolidate the 2 new bits and last 2 bits into 4 bits
                    current = ((current << 2) & 0x000c) | twoBits;
                    entry = additionalMakeup[current];
                    bits = ((int)(((uint)entry) >> 1)) & 0x07;
                    // 3 bits 0000 0111
                    code = ((int)(((uint)entry) >> 4)) & 0x0fff;
                    // 12 bits
                    runLength += code;
                    UpdatePointer(4 - bits);
                }
                else
                {
	                if (bits == 0) {
                        // ERROR
                        throw new Exception("Error 0");
                    }

	                if (bits == 15) {
		                // EOL
		                throw new Exception("Error 1");
	                }

	                // 11 bits - 0000 0111 1111 1111 = 0x07ff
	                code = ((int)(((uint)entry) >> 5)) & 0x07ff;
	                runLength += code;
	                UpdatePointer(10 - bits);
	                if (isT == 0) {
		                isWhite = false;
	                }
                }
            }
            return runLength;
        }

        // Returns run length
        private int DecodeBlackCodeWord() {
            int current;
            int entry;
            int bits;
            int isT;
            int twoBits;
            var code = -1;
            var runLength = 0;
            var isWhite = false;
            while (!isWhite) {
                current = NextLesserThan8Bits(4);
                entry = initBlack[current];
                // Get the 3 fields from the entry
                isT = entry & 0x0001;
                bits = ((int)(((uint)entry) >> 1)) & 0x000f;
                code = ((int)(((uint)entry) >> 5)) & 0x07ff;
                if (code == 100) {
                    current = NextNBits(9);
                    entry = black[current];
                    // Get the 3 fields from the entry
                    isT = entry & 0x0001;
                    bits = ((int)(((uint)entry) >> 1)) & 0x000f;
                    code = ((int)(((uint)entry) >> 5)) & 0x07ff;
                    if (bits == 12) {
                        // Additional makeup codes
                        UpdatePointer(5);
                        current = NextLesserThan8Bits(4);
                        entry = additionalMakeup[current];
                        bits = ((int)(((uint)entry) >> 1)) & 0x07;
                        // 3 bits 0000 0111
                        code = ((int)(((uint)entry) >> 4)) & 0x0fff;
                        // 12 bits
                        runLength += code;
                        UpdatePointer(4 - bits);
                    }
                    else
                    {
	                    if (bits == 15) {
                            // EOL code
                            throw new Exception("Error 2");
                        }

	                    runLength += code;
	                    UpdatePointer(9 - bits);
	                    if (isT == 0) {
		                    isWhite = true;
	                    }
                    }
                }
                else {
                    if (code == 200) {
                        // Is a Terminating code
                        current = NextLesserThan8Bits(2);
                        entry = twoBitBlack[current];
                        code = ((int)(((uint)entry) >> 5)) & 0x07ff;
                        runLength += code;
                        bits = ((int)(((uint)entry) >> 1)) & 0x0f;
                        UpdatePointer(2 - bits);
                        isWhite = true;
                    }
                    else {
                        // Is a Terminating code
                        runLength += code;
                        UpdatePointer(4 - bits);
                        isWhite = true;
                    }
                }
            }
            return runLength;
        }

        private int FindNextLine() {
            // Set maximum and current bit index into the compressed data.
            var bitIndexMax = data.Length * 8 - 1;
            var bitIndexMax12 = bitIndexMax - 12;
            var bitIndex = bytePointer * 8 + bitPointer;
            // Loop while at least 12 bits are available.
            while (bitIndex <= bitIndexMax12) {
                // Get the next 12 bits.
                var next12Bits = NextNBits(12);
                bitIndex += 12;
                // Loop while the 12 bits are not unity, i.e., while the EOL
                // has not been reached, and there is at least one bit left.
                while (next12Bits != 1 && bitIndex < bitIndexMax) {
                    next12Bits = ((next12Bits & 0x000007ff) << 1) | (NextLesserThan8Bits(1) & 0x00000001);
                    bitIndex++;
                }
                if (next12Bits == 1) {
                    // now positioned just after EOL
                    if (oneD == 1) {
                        // two-dimensional coding
                        if (bitIndex < bitIndexMax) {
                            // check next bit against type of line being sought
                            return NextLesserThan8Bits(1);
                        }
                    }
                    else {
                        return 1;
                    }
                }
            }
            // EOL not found.
            throw new Exception();
        }

        private void GetNextChangingElement(int a0, bool isWhite, int[] ret) {
            // Local copies of instance variables
            var pce = prevChangingElems;
            var ces = changingElemSize;
            // If the previous match was at an odd element, we still
            // have to search the preceeding element.
            // int start = lastChangingElement & ~0x1;
            var start = lastChangingElement > 0 ? lastChangingElement - 1 : 0;
            if (isWhite) {
                start &= ~0x1;
            }
            else {
                // Search even numbered elements
                start |= 0x1;
            }
            // Search odd numbered elements
            var i = start;
            for (; i < ces; i += 2) {
                var temp = pce[i];
                if (temp > a0) {
                    lastChangingElement = i;
                    ret[0] = temp;
                    break;
                }
            }
            if (i + 1 < ces) {
                ret[1] = pce[i + 1];
            }
        }

        private int NextNBits(int bitsToGet) {
            byte b;
            byte next;
            byte next2next;
            var l = data.Length - 1;
            var bp = bytePointer;
            if (fillOrder == 1) {
                b = data[bp];
                if (bp == l) {
                    next = 0x00;
                    next2next = 0x00;
                }
                else {
                    if ((bp + 1) == l) {
                        next = data[bp + 1];
                        next2next = 0x00;
                    }
                    else {
                        next = data[bp + 1];
                        next2next = data[bp + 2];
                    }
                }
            }
            else {
                if (fillOrder == 2) {
                    b = flipTable[data[bp] & 0xff];
                    if (bp == l) {
                        next = 0x00;
                        next2next = 0x00;
                    }
                    else {
                        if ((bp + 1) == l) {
                            next = flipTable[data[bp + 1] & 0xff];
                            next2next = 0x00;
                        }
                        else {
                            next = flipTable[data[bp + 1] & 0xff];
                            next2next = flipTable[data[bp + 2] & 0xff];
                        }
                    }
                }
                else {
                    throw new Exception("Invalid FillOrder");
                }
            }
            var bitsLeft = 8 - bitPointer;
            var bitsFromNextByte = bitsToGet - bitsLeft;
            var bitsFromNext2NextByte = 0;
            if (bitsFromNextByte > 8) {
                bitsFromNext2NextByte = bitsFromNextByte - 8;
                bitsFromNextByte = 8;
            }
            bytePointer++;
            var i1 = (b & table1[bitsLeft]) << (bitsToGet - bitsLeft);
            var i2 = (int)(((uint)(next & table2[bitsFromNextByte])) >> (8 - bitsFromNextByte));
            var i3 = 0;
            if (bitsFromNext2NextByte != 0) {
                i2 <<= bitsFromNext2NextByte;
                i3 = (int)(((uint)(next2next & table2[bitsFromNext2NextByte])) >> (8 - bitsFromNext2NextByte));
                i2 |= i3;
                bytePointer++;
                bitPointer = bitsFromNext2NextByte;
            }
            else {
                if (bitsFromNextByte == 8) {
                    bitPointer = 0;
                    bytePointer++;
                }
                else {
                    bitPointer = bitsFromNextByte;
                }
            }
            return i1 | i2;
        }

        private int NextLesserThan8Bits(int bitsToGet) {
            byte b;
            byte next;
            var l = data.Length - 1;
            var bp = bytePointer;
            if (fillOrder == 1) {
                b = data[bp];
                if (bp == l) {
                    next = 0x00;
                }
                else {
                    next = data[bp + 1];
                }
            }
            else {
                if (fillOrder == 2) {
                    b = flipTable[data[bp] & 0xff];
                    if (bp == l) {
                        next = 0x00;
                    }
                    else {
                        next = flipTable[data[bp + 1] & 0xff];
                    }
                }
                else {
                    throw new Exception("Invalid FillOrder");
                }
            }
            var bitsLeft = 8 - bitPointer;
            var bitsFromNextByte = bitsToGet - bitsLeft;
            var shift = bitsLeft - bitsToGet;
            int i1;
            int i2;
            if (shift >= 0) {
                i1 = (int)(((uint)(b & table1[bitsLeft])) >> shift);
                bitPointer += bitsToGet;
                if (bitPointer == 8) {
                    bitPointer = 0;
                    bytePointer++;
                }
            }
            else {
                i1 = (b & table1[bitsLeft]) << (-shift);
                i2 = (int)(((uint)(next & table2[bitsFromNextByte])) >> (8 - bitsFromNextByte));
                i1 |= i2;
                bytePointer++;
                bitPointer = bitsFromNextByte;
            }
            return i1;
        }

        // Move pointer backwards by given amount of bits
        private void UpdatePointer(int bitsToMoveBack) {
            if (bitsToMoveBack > 8) {
                bytePointer -= bitsToMoveBack / 8;
                bitsToMoveBack %= 8;
            }
            var i = bitPointer - bitsToMoveBack;
            if (i < 0) {
                bytePointer--;
                bitPointer = 8 + i;
            }
            else {
                bitPointer = i;
            }
        }
    }
}
