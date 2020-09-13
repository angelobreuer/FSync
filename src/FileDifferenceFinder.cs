namespace FSync
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using FSync.Util;

    public sealed class FileDifferenceFinder : IEnumerable<FileDifference>
    {
        public const HashAlgorithmType DefaultHashAlgorithm = HashAlgorithmType.MD5;
        private const string FindAllPattern = "*";

        private readonly EnumerationOptions _enumerationOptions;
        private readonly bool _recurseSubdirectories;

        public FileDifferenceFinder(
            string firstDirectory, string secondDirectory,
            EnumerationOptions enumerationOptions,
            FileComparisonTypes comparisonTypes = FileComparisonTypes.None,
            HashAlgorithmType? hashAlgorithm = null)
        {
            if (enumerationOptions is null)
            {
                throw new ArgumentNullException(nameof(enumerationOptions));
            }

            FirstDirectory = firstDirectory ?? throw new ArgumentNullException(nameof(firstDirectory));
            SecondDirectory = secondDirectory ?? throw new ArgumentNullException(nameof(secondDirectory));
            ComparisonTypes = comparisonTypes;

            if (enumerationOptions.RecurseSubdirectories)
            {
                _recurseSubdirectories = true;
            }

            _enumerationOptions = CloneEnumerationOptions(enumerationOptions);
            _enumerationOptions.RecurseSubdirectories = false; // we handle recursively by our own

            if (comparisonTypes.HasFlag(FileComparisonTypes.Hash))
            {
                HashAlgorithm = hashAlgorithm ?? DefaultHashAlgorithm;
            }
            else if (hashAlgorithm != null)
            {
                throw new InvalidOperationException("No hash algorithm should be explicitly specified if no hashing is used.");
            }
        }

        public FileComparisonTypes ComparisonTypes { get; }

        public string FirstDirectory { get; }

        public HashAlgorithmType HashAlgorithm { get; }

        public string SecondDirectory { get; }

        public IEnumerable<FileDifference> FindDifferences(string wildcard = FindAllPattern)
        {
            var differenceChain = Enumerable.Empty<FileDifference>();
            FindDifferences(FirstDirectory, SecondDirectory, wildcard, ref differenceChain);
            return differenceChain;
        }

        /// <inheritdoc/>
        public IEnumerator<FileDifference> GetEnumerator() => FindDifferences().GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static EnumerationOptions CloneEnumerationOptions(EnumerationOptions enumerationOptions) => new EnumerationOptions
        {
            AttributesToSkip = enumerationOptions.AttributesToSkip,
            BufferSize = enumerationOptions.BufferSize,
            IgnoreInaccessible = enumerationOptions.IgnoreInaccessible,
            MatchCasing = enumerationOptions.MatchCasing,
            MatchType = enumerationOptions.MatchType,
            RecurseSubdirectories = enumerationOptions.RecurseSubdirectories,
            ReturnSpecialDirectories = enumerationOptions.ReturnSpecialDirectories
        };

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

        private void FindDifferences(string? firstDirectory, string? secondDirectory, string wildcard, ref IEnumerable<FileDifference> differenceChain)
        {
            if (firstDirectory is null && secondDirectory is null)
            {
                throw new InvalidOperationException("One of source directory or target directory must be not null.");
            }

            var firstDirectoryFiles = firstDirectory is null
                ? Enumerable.Empty<string>()
                : Directory.EnumerateFiles(firstDirectory, wildcard, _enumerationOptions);

            var secondDirectoryFiles = secondDirectory is null
                ? Enumerable.Empty<string>()
                : Directory.EnumerateFiles(secondDirectory, wildcard, _enumerationOptions);

            differenceChain = differenceChain.Concat(FindDifferencesCore(firstDirectoryFiles, secondDirectoryFiles));

            if (_recurseSubdirectories)
            {
                var firstDirectorySubDirectories = firstDirectory is null
                    ? Enumerable.Empty<string>()
                    : Directory.EnumerateDirectories(firstDirectory, FindAllPattern, _enumerationOptions).ToArray();

                var secondDirectorySubDirectories = secondDirectory is null
                    ? Enumerable.Empty<string>()
                    : Directory.EnumerateDirectories(secondDirectory, FindAllPattern, _enumerationOptions).ToArray();

                // NOTE: We treat the directory names as file names because they have similar
                // behavior when matching them
                var diffDirectories = firstDirectorySubDirectories.Diff(secondDirectorySubDirectories, FileNameEqualityComparer.Instance);

                foreach (var directoryDifference in diffDirectories)
                {
                    FindDifferences(directoryDifference.Left, directoryDifference.Right, wildcard, ref differenceChain);
                }
            }
        }

        private IEnumerable<FileDifference> FindDifferencesCore(IEnumerable<string> firstDirectoryFileNames, IEnumerable<string> secondDirectoryFileNames)
        {
            var fileDifferences = firstDirectoryFileNames.Diff(secondDirectoryFileNames, FileNameEqualityComparer.Instance).ToArray(); // TODO remove ToArray()

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
