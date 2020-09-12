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

static SearchOption GetSearchOption(CommandLineOptions options)
{
    if (options.Recursive)
    {
        return SearchOption.AllDirectories;
    }
    else
    {
        return SearchOption.TopDirectoryOnly;
    }
}

static void Run(CommandLineOptions options)
{
    var comparisonTypes = GetFileComparisonTypes(options);
    var searchOption = GetSearchOption(options);

    var finder = new FileDifferenceFinder(
        firstDirectory: options.FirstDirectory,
        secondDirectory: options.SecondDirectory,
        searchOption: searchOption,
        comparisonTypes: comparisonTypes,
        hashAlgorithm: options.Algorithm);

    using var fileCopyQueue = new FileCopyQueue();

    foreach (var fileDifference in finder)
    {
        Console.WriteLine($"{fileDifference.DifferenceType}  -  {fileDifference.OldFile}  -  {fileDifference.NewFile}");

        if (!options.Simulate)
        {
            Apply(fileCopyQueue, fileDifference, options);
        }
    }

    fileCopyQueue.Drain();
}

static void Apply(FileCopyQueue fileCopyQueue, FileDifference fileDifference, CommandLineOptions options)
{
    switch (fileDifference.DifferenceType)
    {
        case FileDifferenceType.Created:
            var sourceFilePath = Path.Combine(
                options.FirstDirectory,
                Path.GetRelativePath(options.SecondDirectory, fileDifference.OldFile!.FullName));

            fileCopyQueue.Enqueue(new FileInfo(sourceFilePath), fileDifference.NewFile!);
            break;

        case FileDifferenceType.Deleted:
            fileDifference.OldFile!.Delete();
            break;

        case FileDifferenceType.Modified:
            fileCopyQueue.Enqueue(fileDifference.OldFile!, fileDifference.NewFile!);
            break;
    }
}
