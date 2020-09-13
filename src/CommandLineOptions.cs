namespace FSync
{
    using CommandLine;

    internal class CommandLineOptions
    {
        [Option('a', "algorithm", HelpText = "Algorithm name, e.g. MD5, SHA256")]
        public HashAlgorithmType? Algorithm { get; set; }

        [Option('H', "hash", HelpText = "Whether to compare files using the hash")]
        public bool CompareHash { get; set; }

        [Option('S', "size", HelpText = "Whether to compare files using the size.")]
        public bool CompareSize { get; set; }

        [Value(0, Required = true, HelpText = "The first directory to synchronize.")]
        public string FirstDirectory { get; set; } = null!;

        [Option('e', "include-encrypted", HelpText = "Whether to include encrypted files.")]
        public bool IncludeEncrypted { get; set; }

        [Option('j', "include-hidden", HelpText = "Whether to include hidden files.")]
        public bool IncludeHidden { get; set; }

        [Option('o', "include-sparse", HelpText = "Whether to include sparse files.")]
        public bool IncludeSparse { get; set; }

        [Option('d', "special-directories", HelpText = "Whether to include special directories.")]
        public bool IncludeSpecialDirectories { get; set; }

        [Option('m', "include-system", HelpText = "Whether to include system files.")]
        public bool IncludeSystem { get; set; }

        [Option('R', "recursive", HelpText = "Whether to synchronize files recursively.")]
        public bool Recursive { get; set; }

        [Value(1, Required = true, HelpText = "The second directory to synchronize.")]
        public string SecondDirectory { get; set; } = null!;

        [Option('s', "simulate", HelpText = "Whether to simulate synchronization.")]
        public bool Simulate { get; set; }

        [Option('v', "verbose", HelpText = "Whether to output detailed information.")]
        public bool Verbose { get; set; }

        [Option('w', "wildcard", Default = "*", HelpText = "The wildcard to match files against.")]
        public string Wildcard { get; set; } = "*";
    }
}
