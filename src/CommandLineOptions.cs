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

        [Option('r', "recursive")]
        public bool Recursive { get; set; }

        [Value(1, Required = true)]
        public string SecondDirectory { get; set; } = null!;

        [Option('s', "simulate")]
        public bool Simulate { get; set; }
    }
}
