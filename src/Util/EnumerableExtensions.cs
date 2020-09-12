namespace FSync.Util
{
    using System.Collections.Generic;
    using System.Linq;

    internal static class EnumerableExtensions
    {
        public static IEnumerable<Difference<T>> Diff<T>(this IEnumerable<T> enumerable, IEnumerable<T> second, IEqualityComparer<T> equalityComparer)
        {
            var leftList = enumerable.ToList();
            var rightList = new List<T>();

            foreach (var rightItem in second)
            {
                var leftItem = leftList.FirstOrDefault(x => equalityComparer.Equals(x, rightItem));

                if (leftItem is not null)
                {
                    yield return Difference<T>.CreateIntersecting(leftItem, rightItem);
                }
                else
                {
                    yield return Difference<T>.CreateRight(rightItem);
                }

                rightList.Add(rightItem);
            }

            foreach (var leftItem in leftList.Except(rightList, equalityComparer))
            {
                yield return Difference<T>.CreateLeft(leftItem);
            }
        }

        public static IEnumerable<Difference<T>> Diff<T>(this IEnumerable<T> enumerable, IEnumerable<T> second)
            => Diff(enumerable, second, EqualityComparer<T>.Default);
    }
}
