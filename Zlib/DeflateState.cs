namespace Zlib
{
    public enum DeflateState
    {
        Unknown = -1,

        // block not completed, need more input or more output
        NeedMore = 0,

        // block flush performed
        BlockDone = 1,

        // finish started, need only more output at next deflate
        FinishStarted = 2,

        // finish done, accept no more input or output
        FinishDone = 3,
    }
}