using System.IO;
using CommandLine;
using FSync;
using Microsoft.Extensions.Logging;

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
    var minimumLogLevel = options.Verbose ? LogLevel.Trace : LogLevel.Information;

    using var loggerFactory = LoggerFactory.Create(x => x.AddConsole().SetMinimumLevel(minimumLogLevel));
    var logger = loggerFactory.CreateLogger("Program");

    var finder = new FileDifferenceFinder(
        firstDirectory: options.FirstDirectory,
        secondDirectory: options.SecondDirectory,
        enumerationOptions: GetEnumerationOptions(options),
        comparisonTypes: comparisonTypes,
        hashAlgorithm: options.Algorithm);

    using var fileCopyQueue = new FileCopyQueue(loggerFactory.CreateLogger<FileCopyQueue>());

    foreach (var fileDifference in finder.FindDifferences(options.Wildcard))
    {
        var severity = fileDifference.DifferenceType is FileDifferenceType.None
            ? LogLevel.Debug
            : LogLevel.Information;

        var relativePathToOldFile = fileDifference.OldFile is null
            ? "(null)" : Path.GetRelativePath(options.SecondDirectory, fileDifference.OldFile.FullName);

        var relativePathToNewFile = fileDifference.NewFile is null
            ? "(null)" : Path.GetRelativePath(options.FirstDirectory, fileDifference.NewFile.FullName);

        logger.Log(severity, "[{Type}] {OldFile} --> {NewFile}",
            fileDifference.DifferenceType, relativePathToOldFile, relativePathToNewFile);

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
