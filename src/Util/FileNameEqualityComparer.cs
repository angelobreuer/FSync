namespace FSync.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    internal sealed class FileNameEqualityComparer : IEqualityComparer<string>
    {
        private static FileNameEqualityComparer? _instance;

        /// <summary>
        ///     Gets a shared instance of the <see cref="FileNameEqualityComparer"/> class.
        /// </summary>
        /// <value>a shared instance of the <see cref="FileNameEqualityComparer"/> class</value>
        public static FileNameEqualityComparer Instance => _instance ??= new FileNameEqualityComparer();

        /// <inheritdoc/>
        public bool Equals([AllowNull] string x, [AllowNull] string y)
        {
            if (x is null || y is null)
            {
                return (x is null) == (y is null);
            }

            var xName = Path.GetFileName(x)!;
            var yName = Path.GetFileName(y)!;

            return xName.Equals(yName, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public int GetHashCode([DisallowNull] string obj)
        {
            return Path.GetFileName(obj)!.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }
}
