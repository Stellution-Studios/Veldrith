namespace Veldrith.Vk;
// Fixed-size "array" types, useful for constructing inputs
// to some Vulkan functions without allocating and pinning a real array.

/// <summary>
/// Represents the FixedArray2 struct.
/// </summary>
internal struct FixedArray2<T> where T : struct {

    /// <summary>
    /// Represents the First field.
    /// </summary>
    public T First;

    /// <summary>
    /// Represents the Second field.
    /// </summary>
    public T Second;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray2" /> class.
    /// </summary>
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
/// Represents the FixedArray3 struct.
/// </summary>
internal struct FixedArray3<T> where T : struct {

    /// <summary>
    /// Represents the First field.
    /// </summary>
    public T First;

    /// <summary>
    /// Represents the Second field.
    /// </summary>
    public T Second;

    /// <summary>
    /// Represents the Third field.
    /// </summary>
    public T Third;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray3" /> class.
    /// </summary>
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
/// Represents the FixedArray4 struct.
/// </summary>
internal struct FixedArray4<T> where T : struct {

    /// <summary>
    /// Represents the First field.
    /// </summary>
    public T First;

    /// <summary>
    /// Represents the Second field.
    /// </summary>
    public T Second;

    /// <summary>
    /// Represents the Third field.
    /// </summary>
    public T Third;

    /// <summary>
    /// Represents the Fourth field.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray4" /> class.
    /// </summary>
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
/// Represents the FixedArray5 struct.
/// </summary>
internal struct FixedArray5<T> where T : struct {

    /// <summary>
    /// Represents the First field.
    /// </summary>
    public T First;

    /// <summary>
    /// Represents the Second field.
    /// </summary>
    public T Second;

    /// <summary>
    /// Represents the Third field.
    /// </summary>
    public T Third;

    /// <summary>
    /// Represents the Fourth field.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Represents the Fifth field.
    /// </summary>
    public T Fifth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray5" /> class.
    /// </summary>
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
/// Represents the FixedArray6 struct.
/// </summary>
internal struct FixedArray6<T> where T : struct {

    /// <summary>
    /// Represents the First field.
    /// </summary>
    public T First;

    /// <summary>
    /// Represents the Second field.
    /// </summary>
    public T Second;

    /// <summary>
    /// Represents the Third field.
    /// </summary>
    public T Third;

    /// <summary>
    /// Represents the Fourth field.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Represents the Fifth field.
    /// </summary>
    public T Fifth;

    /// <summary>
    /// Represents the Sixth field.
    /// </summary>
    public T Sixth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray6" /> class.
    /// </summary>
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