namespace Zlib
{
    public enum FlushLevel
    {
        BufError = -1,
        NoFlush = 0,
        PartialFlush = 1,
        SyncFlush = 2,
        FullFlush = 3,
        Finish = 4
    }
}