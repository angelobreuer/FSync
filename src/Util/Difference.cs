namespace FSync.Util
{
    public readonly struct Difference<T>
    {
        // 0 = left, 1 = intersect, 2 = right
        private readonly byte _type;

        private Difference(byte type, T left = default, T right = default)
        {
            _type = type;
            Left = left;
            Right = right;
        }

        public bool IsIntersecting => _type == 1;

        public bool IsLeft => _type == 0;

        public bool IsRight => _type == 2;

        public T Left { get; }

        public T Right { get; }

        public static Difference<T> CreateIntersecting(T left, T right)
            => new Difference<T>(type: 1, left, right);

        public static Difference<T> CreateLeft(T left)
            => new Difference<T>(type: 0, left: left);

        public static Difference<T> CreateRight(T right)
            => new Difference<T>(type: 2, right: right);
    }
}
