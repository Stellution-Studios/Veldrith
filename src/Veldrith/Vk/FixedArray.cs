namespace Veldrith.Vk;
// Fixed-size "array" types, useful for constructing inputs
// to some Vulkan functions without allocating and pinning a real array.

/// <summary>
/// Represents the FixedArray2 data structure used by the graphics runtime.
/// </summary>
internal struct FixedArray2<T> where T : struct {

    /// <summary>
    /// Stores the first state used by this instance.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the second state used by this instance.
    /// </summary>
    public T Second;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray2" /> type.
    /// </summary>
    /// <param name="first">The first value used by this operation.</param>
    /// <param name="second">The second value used by this operation.</param>
    public FixedArray2(T first, T second) {
        this.First = first;
        this.Second = second;
    }

    /// <summary>
    /// Gets or sets Count.
    /// </summary>
    public uint Count => 2;
}

/// <summary>
/// Represents the FixedArray3 data structure used by the graphics runtime.
/// </summary>
internal struct FixedArray3<T> where T : struct {

    /// <summary>
    /// Stores the first state used by this instance.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the second state used by this instance.
    /// </summary>
    public T Second;

    /// <summary>
    /// Stores the third state used by this instance.
    /// </summary>
    public T Third;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray3" /> type.
    /// </summary>
    /// <param name="first">The first value used by this operation.</param>
    /// <param name="second">The second value used by this operation.</param>
    /// <param name="third">The third value used by this operation.</param>
    public FixedArray3(T first, T second, T third) {
        this.First = first;
        this.Second = second;
        this.Third = third;
    }

    /// <summary>
    /// Gets or sets Count.
    /// </summary>
    public uint Count => 3;
}

/// <summary>
/// Represents the FixedArray4 data structure used by the graphics runtime.
/// </summary>
internal struct FixedArray4<T> where T : struct {

    /// <summary>
    /// Stores the first state used by this instance.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the second state used by this instance.
    /// </summary>
    public T Second;

    /// <summary>
    /// Stores the third state used by this instance.
    /// </summary>
    public T Third;

    /// <summary>
    /// Stores the fourth state used by this instance.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray4" /> type.
    /// </summary>
    /// <param name="first">The first value used by this operation.</param>
    /// <param name="second">The second value used by this operation.</param>
    /// <param name="third">The third value used by this operation.</param>
    /// <param name="fourth">The fourth value used by this operation.</param>
    public FixedArray4(T first, T second, T third, T fourth) {
        this.First = first;
        this.Second = second;
        this.Third = third;
        this.Fourth = fourth;
    }

    /// <summary>
    /// Gets or sets Count.
    /// </summary>
    public uint Count => 4;
}

/// <summary>
/// Represents the FixedArray5 data structure used by the graphics runtime.
/// </summary>
internal struct FixedArray5<T> where T : struct {

    /// <summary>
    /// Stores the first state used by this instance.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the second state used by this instance.
    /// </summary>
    public T Second;

    /// <summary>
    /// Stores the third state used by this instance.
    /// </summary>
    public T Third;

    /// <summary>
    /// Stores the fourth state used by this instance.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Stores the fifth state used by this instance.
    /// </summary>
    public T Fifth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray5" /> type.
    /// </summary>
    /// <param name="first">The first value used by this operation.</param>
    /// <param name="second">The second value used by this operation.</param>
    /// <param name="third">The third value used by this operation.</param>
    /// <param name="fourth">The fourth value used by this operation.</param>
    /// <param name="fifth">The fifth value used by this operation.</param>
    public FixedArray5(T first, T second, T third, T fourth, T fifth) {
        this.First = first;
        this.Second = second;
        this.Third = third;
        this.Fourth = fourth;
        this.Fifth = fifth;
    }

    /// <summary>
    /// Gets or sets Count.
    /// </summary>
    public uint Count => 5;
}

/// <summary>
/// Represents the FixedArray6 data structure used by the graphics runtime.
/// </summary>
internal struct FixedArray6<T> where T : struct {

    /// <summary>
    /// Stores the first state used by this instance.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the second state used by this instance.
    /// </summary>
    public T Second;

    /// <summary>
    /// Stores the third state used by this instance.
    /// </summary>
    public T Third;

    /// <summary>
    /// Stores the fourth state used by this instance.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Stores the fifth state used by this instance.
    /// </summary>
    public T Fifth;

    /// <summary>
    /// Stores the sixth state used by this instance.
    /// </summary>
    public T Sixth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray6" /> type.
    /// </summary>
    /// <param name="first">The first value used by this operation.</param>
    /// <param name="second">The second value used by this operation.</param>
    /// <param name="third">The third value used by this operation.</param>
    /// <param name="fourth">The fourth value used by this operation.</param>
    /// <param name="fifth">The fifth value used by this operation.</param>
    /// <param name="sixth">The sixth value used by this operation.</param>
    public FixedArray6(T first, T second, T third, T fourth, T fifth, T sixth) {
        this.First = first;
        this.Second = second;
        this.Third = third;
        this.Fourth = fourth;
        this.Fifth = fifth;
        this.Sixth = sixth;
    }

    /// <summary>
    /// Gets or sets Count.
    /// </summary>
    public uint Count => 6;
}