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
