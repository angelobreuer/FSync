namespace FSync
{
    using System;

    [Flags]
    public enum FileComparisonTypes : byte
    {
        None = 0,
        Size = 1 << 0,
        Hash = 1 << 1,
    }
}
