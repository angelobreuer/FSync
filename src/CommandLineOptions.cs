namespace FSync
{
    using CommandLine;

    internal class CommandLineOptions
    {
        [Option('a', "algorithm")]
        public HashAlgorithmType? Algorithm { get; set; }

        [Option('h', "hash")]
        public bool CompareHash { get; set; }

        [Option('k', "size")]
        public bool CompareSize { get; set; }

        [Value(0, Required = true)]
        public string FirstDirectory { get; set; } = null!;

        [Option('e', "include-encrypted")]
        public bool IncludeEncrypted { get; set; }

        [Option('j', "include-hidden")]
        public bool IncludeHidden { get; set; }

        [Option('o', "include-sparse")]
        public bool IncludeSparse { get; set; }

        [Option('y', "special-directories")]
        public bool IncludeSpecialDirectories { get; set; }

        [Option('m', "include-system")]
        public bool IncludeSystem { get; set; }

        [Option('r', "recursive")]
        public bool Recursive { get; set; }

        [Value(1, Required = true)]
        public string SecondDirectory { get; set; } = null!;

        [Option('s', "simulate")]
        public bool Simulate { get; set; }

        [Option('v', "verbose")]
        public bool Verbose { get; set; }

        [Option('w', "wildcard", Default = "*")]
        public string Wildcard { get; set; } = "*";
    }
}
