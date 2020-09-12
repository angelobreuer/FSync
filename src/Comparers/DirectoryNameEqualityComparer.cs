namespace FSync.Comparers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    internal sealed class DirectoryNameEqualityComparer : IEqualityComparer<string>
    {
        private static DirectoryNameEqualityComparer? _instance;

        /// <summary>
        ///     Gets a shared instance of the <see cref="DirectoryNameEqualityComparer"/> class.
        /// </summary>
        /// <value>a shared instance of the <see cref="DirectoryNameEqualityComparer"/> class</value>
        public static DirectoryNameEqualityComparer Instance => _instance ??= new DirectoryNameEqualityComparer();

        /// <inheritdoc/>
        public bool Equals([AllowNull] string x, [AllowNull] string y)
        {
            if (x is null || y is null)
            {
                return (x is null) == (y is null);
            }

            var xName = Path.GetDirectoryName(x)!;
            var yName = Path.GetDirectoryName(y)!;

            return xName.Equals(yName, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public int GetHashCode([DisallowNull] string obj)
        {
            return Path.GetDirectoryName(obj)!.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }
}
