namespace FSync
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using FSync.Comparers;
    using FSync.Util;

    public sealed class FileDifferenceFinder : IEnumerable<FileDifference>
    {
        public const HashAlgorithmType DefaultHashAlgorithm = HashAlgorithmType.MD5;

        public FileDifferenceFinder(
            string firstDirectory, string secondDirectory,
            SearchOption searchOption = SearchOption.AllDirectories,
            FileComparisonTypes comparisonTypes = FileComparisonTypes.None,
            HashAlgorithmType? hashAlgorithm = null)
        {
            FirstDirectory = firstDirectory ?? throw new ArgumentNullException(nameof(firstDirectory));
            SecondDirectory = secondDirectory ?? throw new ArgumentNullException(nameof(secondDirectory));
            SearchOption = searchOption;
            ComparisonTypes = comparisonTypes;

            if (hashAlgorithm is null)
            {
                HashAlgorithm = HashAlgorithmType.None;
            }
            else if (comparisonTypes.HasFlag(FileComparisonTypes.Hash))
            {
                HashAlgorithm = hashAlgorithm ?? DefaultHashAlgorithm;
            }
            else
            {
                throw new InvalidOperationException("No hash algorithm should be explicitly specified if no hashing is used.");
            }
        }

        public FileComparisonTypes ComparisonTypes { get; }

        public string FirstDirectory { get; }

        public HashAlgorithmType HashAlgorithm { get; }

        public SearchOption SearchOption { get; }

        public string SecondDirectory { get; }

        /// <inheritdoc/>
        public IEnumerator<FileDifference> GetEnumerator() => FindDifferences().GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private bool CompareByHash(FileInfo firstFileInfo, FileInfo secondFileInfo)
        {
            using var hashAlgorithm = System.Security.Cryptography.HashAlgorithm.Create(HashAlgorithm.ToString())
                ?? throw new InvalidOperationException($"Invalid hash algorithm: {HashAlgorithm}.");

            byte[] firstFileHash;
            using (var firstFileInfoFileStream = firstFileInfo.OpenRead())
            {
                firstFileHash = hashAlgorithm.ComputeHash(firstFileInfoFileStream);
            }

            byte[] secondFileHash;
            using (var secondFileInfoFileStream = secondFileInfo.OpenRead())
            {
                secondFileHash = hashAlgorithm.ComputeHash(secondFileInfoFileStream);
            }

            return firstFileHash.SequenceEqual(secondFileHash);
        }

        private int CompareFile(FileInfo firstFileInfo, FileInfo secondFileInfo)
        {
            if (CompareFileCore(firstFileInfo, secondFileInfo))
            {
                // files are equal
                return 0;
            }

            return firstFileInfo.LastWriteTimeUtc.CompareTo(secondFileInfo.LastWriteTimeUtc);
        }

        private bool CompareFileCore(FileInfo firstFileInfo, FileInfo secondFileInfo)
        {
            if (ComparisonTypes.HasFlag(FileComparisonTypes.Hash))
            {
                if (!CompareByHash(firstFileInfo, secondFileInfo))
                {
                    return false;
                }
            }

            if (ComparisonTypes.HasFlag(FileComparisonTypes.Size))
            {
                if (firstFileInfo.Length != secondFileInfo.Length)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<FileDifference> FindDifferences()
        {
            var differenceChain = Enumerable.Empty<FileDifference>();
            FindDifferences(FirstDirectory, SecondDirectory, ref differenceChain);
            return differenceChain;
        }

        private void FindDifferences(string? firstDirectory, string? secondDirectory, ref IEnumerable<FileDifference> differenceChain)
        {
            if (firstDirectory is null && secondDirectory is null)
            {
                throw new InvalidOperationException("One of source directory or target directory must be not null.");
            }

            var firstDirectoryFiles = firstDirectory is null ? Enumerable.Empty<string>() : Directory.EnumerateFiles(firstDirectory);
            var secondDirectoryFiles = secondDirectory is null ? Enumerable.Empty<string>() : Directory.EnumerateFiles(secondDirectory);

            differenceChain = differenceChain.Concat(FindDifferencesCore(firstDirectoryFiles, secondDirectoryFiles));

            var firstDirectorySubDirectories = Directory.EnumerateDirectories(FirstDirectory).ToArray();
            var secondDirectorySubDirectories = Directory.EnumerateDirectories(SecondDirectory).ToArray();

            var diffDirectories = firstDirectorySubDirectories.Diff(secondDirectorySubDirectories, DirectoryNameEqualityComparer.Instance);

            foreach (var directoryDifference in diffDirectories)
            {
                FindDifferences(directoryDifference.Left, directoryDifference.Right, ref differenceChain);
            }
        }

        private IEnumerable<FileDifference> FindDifferencesCore(IEnumerable<string> firstDirectoryFileNames, IEnumerable<string> secondDirectoryFileNames)
        {
            var fileDifferences = firstDirectoryFileNames.Diff(secondDirectoryFileNames, FileNameEqualityComparer.Instance);

            foreach (var fileDifference in fileDifferences)
            {
                if (fileDifference.IsIntersecting)
                {
                    var left = new FileInfo(fileDifference.Left);
                    var right = new FileInfo(fileDifference.Right);

                    var comparisonResult = CompareFile(left, right);

                    if (comparisonResult < 0)
                    {
                        // left file is older
                        yield return new FileDifference(FileDifferenceType.Modified, newFile: left, oldFile: right);
                    }
                    else if (comparisonResult > 0)
                    {
                        // right file is older
                        yield return new FileDifference(FileDifferenceType.Modified, newFile: right, oldFile: left);
                    }
                    else
                    {
                        // files are equal
                        yield return new FileDifference(FileDifferenceType.None, newFile: left, oldFile: right);
                    }
                }
                else if (fileDifference.IsLeft)
                {
                    var left = new FileInfo(fileDifference.Left);

                    yield return new FileDifference(FileDifferenceType.Created, newFile: left, oldFile: null);
                }
                else
                {
                    var right = new FileInfo(fileDifference.Right);

                    yield return new FileDifference(FileDifferenceType.Deleted, newFile: null, oldFile: right);
                }
            }
        }
    }
}
