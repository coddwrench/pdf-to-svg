namespace Zlib
{
    public enum ZStreamState
    {
        Ok = 0,
        StreamEnd = 1,
        NeedDict = 2,
        Errno = -1,
        StreamError = -2,
        DataError = -3,
        MemError = -4,
        BufError = -5,
        VersionError = -6,
    }
}