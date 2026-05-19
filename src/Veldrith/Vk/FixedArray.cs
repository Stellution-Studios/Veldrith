namespace Veldrith.Vk
{
    // Fixed-size "array" types, useful for constructing inputs
    // to some Vulkan functions without allocating and pinning a real array.

    internal struct FixedArray2<T> where T : struct
    {
        public T First;
        public T Second;

        public FixedArray2(T first, T second)
        {
            this.First = first;
            this.Second = second;
        }

        public uint Count => 2;
    }

    internal struct FixedArray3<T> where T : struct
    {
        public T First;
        public T Second;
        public T Third;

        public FixedArray3(T first, T second, T third)
        {
            this.First = first;
            this.Second = second;
            this.Third = third;
        }

        public uint Count => 3;
    }

    internal struct FixedArray4<T> where T : struct
    {
        public T First;
        public T Second;
        public T Third;
        public T Fourth;

        public FixedArray4(T first, T second, T third, T fourth)
        {
            this.First = first;
            this.Second = second;
            this.Third = third;
            this.Fourth = fourth;
        }

        public uint Count => 4;
    }

    internal struct FixedArray5<T> where T : struct
    {
        public T First;
        public T Second;
        public T Third;
        public T Fourth;
        public T Fifth;

        public FixedArray5(T first, T second, T third, T fourth, T fifth)
        {
            this.First = first;
            this.Second = second;
            this.Third = third;
            this.Fourth = fourth;
            this.Fifth = fifth;
        }

        public uint Count => 5;
    }

    internal struct FixedArray6<T> where T : struct
    {
        public T First;
        public T Second;
        public T Third;
        public T Fourth;
        public T Fifth;
        public T Sixth;

        public FixedArray6(T first, T second, T third, T fourth, T fifth, T sixth)
        {
            this.First = first;
            this.Second = second;
            this.Third = third;
            this.Fourth = fourth;
            this.Fifth = fifth;
            this.Sixth = sixth;
        }

        public uint Count => 6;
    }
}
