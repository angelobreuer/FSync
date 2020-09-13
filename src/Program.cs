using System;
using System.IO;
using CommandLine;
using FSync;

var commandLineOptions = Parser.Default.ParseArguments<CommandLineOptions>(args);

commandLineOptions.WithParsed(Run);

static FileComparisonTypes GetFileComparisonTypes(CommandLineOptions options)
{
    var comparisonTypes = FileComparisonTypes.None;

    if (options.CompareHash)
    {
        comparisonTypes |= FileComparisonTypes.Hash;
    }

    if (options.CompareSize)
    {
        comparisonTypes |= FileComparisonTypes.Size;
    }

    return comparisonTypes;
}

static EnumerationOptions GetEnumerationOptions(CommandLineOptions options)
{
    var enumerationOptions = new EnumerationOptions();

    if (options.Recursive)
    {
        enumerationOptions.RecurseSubdirectories = true;
    }

    if (options.IncludeSpecialDirectories)
    {
        enumerationOptions.ReturnSpecialDirectories = true;
    }

    if (!options.IncludeEncrypted)
    {
        enumerationOptions.AttributesToSkip |= FileAttributes.Encrypted;
    }

    if (options.IncludeSystem)
    {
        enumerationOptions.AttributesToSkip &= ~FileAttributes.System;
    }

    if (!options.IncludeSparse)
    {
        enumerationOptions.AttributesToSkip |= FileAttributes.SparseFile;
    }

    return enumerationOptions;
}

static void Run(CommandLineOptions options)
{
    Directory.CreateDirectory(options.FirstDirectory);
    Directory.CreateDirectory(options.SecondDirectory);

    var comparisonTypes = GetFileComparisonTypes(options);
    var searchOption = GetEnumerationOptions(options);

    var finder = new FileDifferenceFinder(
        firstDirectory: options.FirstDirectory,
        secondDirectory: options.SecondDirectory,
        enumerationOptions: GetEnumerationOptions(options),
        comparisonTypes: comparisonTypes,
        hashAlgorithm: options.Algorithm);

    using var fileCopyQueue = new FileCopyQueue();

    foreach (var fileDifference in finder.FindDifferences(options.Wildcard))
    {
        Console.WriteLine($"{fileDifference.DifferenceType}  -  {fileDifference.OldFile}  -  {fileDifference.NewFile}");

        if (!options.Simulate)
        {
            Apply(fileCopyQueue, fileDifference, options);
        }
    }
}

static void Apply(FileCopyQueue fileCopyQueue, FileDifference fileDifference, CommandLineOptions options)
{
    switch (fileDifference.DifferenceType)
    {
        case FileDifferenceType.Created:
            var sourceFilePath = Path.Combine(
                options.SecondDirectory,
                Path.GetRelativePath(options.FirstDirectory, fileDifference.NewFile!.FullName));

            var targetFileInfo = new FileInfo(sourceFilePath);
            targetFileInfo.Directory!.Create();

            fileCopyQueue.Enqueue(fileDifference.NewFile, targetFileInfo);
            break;

        case FileDifferenceType.Deleted:
            fileDifference.OldFile!.Delete();
            break;

        case FileDifferenceType.Modified:
            fileCopyQueue.Enqueue(fileDifference.OldFile!, fileDifference.NewFile!);
            break;
    }
}
