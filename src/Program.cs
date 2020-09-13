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

        logger.Log(severity, "[{Type}] {OldFile} --> {NewFile}",
            fileDifference.DifferenceType, fileDifference.TargetFile?.FullName, fileDifference.SourceFile?.FullName);

        if (!options.Simulate)
        {
            Apply(fileCopyQueue, fileDifference, options);
        }
    }
}

static void Apply(FileCopyQueue fileCopyQueue, FileDifference fileDifference, CommandLineOptions options)
{
    if (fileDifference.DifferenceType is FileDifferenceType.Created or FileDifferenceType.Modified)
    {
        fileCopyQueue.Enqueue(fileDifference.SourceFile, fileDifference.TargetFile);
    }
}
