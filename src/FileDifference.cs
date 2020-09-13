namespace FSync
{
    using System;
    using System.IO;

    public readonly struct FileDifference
    {
        public FileDifference(FileDifferenceType differenceType, FileInfo sourceFile, FileInfo targetFile)
        {
            DifferenceType = differenceType;
            SourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
            TargetFile = targetFile ?? throw new ArgumentNullException(nameof(targetFile));
        }

        public FileDifferenceType DifferenceType { get; }

        public FileInfo SourceFile { get; }

        public FileInfo TargetFile { get; }
    }
}
