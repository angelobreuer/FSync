namespace FSync
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public readonly struct FileDifference
    {
        public FileDifference(FileDifferenceType differenceType, FileInfo? newFile, FileInfo? oldFile)
        {
            if (newFile is null && oldFile is null)
            {
                throw new InvalidOperationException("One of new file or old file must be provided.");
            }

            DifferenceType = differenceType;
            NewFile = newFile;
            OldFile = oldFile;
        }

        public FileDifferenceType DifferenceType { get; }

        [NotNullIfNotNull(nameof(OldFile))]
        public FileInfo? NewFile { get; }

        [NotNullIfNotNull(nameof(NewFile))]
        public FileInfo? OldFile { get; }
    }
}
