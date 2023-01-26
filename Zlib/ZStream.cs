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

        internal DataType DataType; // best guess about the data type: ascii or binary

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
            var windowBits = nowrap ? -bits : bits;
            try
            {
                DeflateState = new Deflate(level, windowBits);
                return DeflateState.DeflateReset(this);
            }
            catch (Exception e)
            {
                return ZStreamState.StreamError;
            }  
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

        public ZStreamState DeflateParams(int level, ZlibStrategy strategy)
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

            if (DeflateState.Noheader == false)
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
