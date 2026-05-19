namespace Veldrith.Vk;
// Fixed-size "array" types, useful for constructing inputs
// to some Vulkan functions without allocating and pinning a real array.

/// <summary>
/// Defines the data layout and behavior of the FixedArray2 struct.
/// </summary>
internal struct FixedArray2<T> where T : struct {

    /// <summary>
    /// Stores the value associated with <c>First</c>.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the value associated with <c>Second</c>.
    /// </summary>
    public T Second;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray2" /> type.
    /// </summary>
    /// <param name="first">Specifies the value of <paramref name="first" />.</param>
    /// <param name="second">Specifies the value of <paramref name="second" />.</param>
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
/// Defines the data layout and behavior of the FixedArray3 struct.
/// </summary>
internal struct FixedArray3<T> where T : struct {

    /// <summary>
    /// Stores the value associated with <c>First</c>.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the value associated with <c>Second</c>.
    /// </summary>
    public T Second;

    /// <summary>
    /// Stores the value associated with <c>Third</c>.
    /// </summary>
    public T Third;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray3" /> type.
    /// </summary>
    /// <param name="first">Specifies the value of <paramref name="first" />.</param>
    /// <param name="second">Specifies the value of <paramref name="second" />.</param>
    /// <param name="third">Specifies the value of <paramref name="third" />.</param>
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
/// Defines the data layout and behavior of the FixedArray4 struct.
/// </summary>
internal struct FixedArray4<T> where T : struct {

    /// <summary>
    /// Stores the value associated with <c>First</c>.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the value associated with <c>Second</c>.
    /// </summary>
    public T Second;

    /// <summary>
    /// Stores the value associated with <c>Third</c>.
    /// </summary>
    public T Third;

    /// <summary>
    /// Stores the value associated with <c>Fourth</c>.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray4" /> type.
    /// </summary>
    /// <param name="first">Specifies the value of <paramref name="first" />.</param>
    /// <param name="second">Specifies the value of <paramref name="second" />.</param>
    /// <param name="third">Specifies the value of <paramref name="third" />.</param>
    /// <param name="fourth">Specifies the value of <paramref name="fourth" />.</param>
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
/// Defines the data layout and behavior of the FixedArray5 struct.
/// </summary>
internal struct FixedArray5<T> where T : struct {

    /// <summary>
    /// Stores the value associated with <c>First</c>.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the value associated with <c>Second</c>.
    /// </summary>
    public T Second;

    /// <summary>
    /// Stores the value associated with <c>Third</c>.
    /// </summary>
    public T Third;

    /// <summary>
    /// Stores the value associated with <c>Fourth</c>.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Stores the value associated with <c>Fifth</c>.
    /// </summary>
    public T Fifth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray5" /> type.
    /// </summary>
    /// <param name="first">Specifies the value of <paramref name="first" />.</param>
    /// <param name="second">Specifies the value of <paramref name="second" />.</param>
    /// <param name="third">Specifies the value of <paramref name="third" />.</param>
    /// <param name="fourth">Specifies the value of <paramref name="fourth" />.</param>
    /// <param name="fifth">Specifies the value of <paramref name="fifth" />.</param>
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
/// Defines the data layout and behavior of the FixedArray6 struct.
/// </summary>
internal struct FixedArray6<T> where T : struct {

    /// <summary>
    /// Stores the value associated with <c>First</c>.
    /// </summary>
    public T First;

    /// <summary>
    /// Stores the value associated with <c>Second</c>.
    /// </summary>
    public T Second;

    /// <summary>
    /// Stores the value associated with <c>Third</c>.
    /// </summary>
    public T Third;

    /// <summary>
    /// Stores the value associated with <c>Fourth</c>.
    /// </summary>
    public T Fourth;

    /// <summary>
    /// Stores the value associated with <c>Fifth</c>.
    /// </summary>
    public T Fifth;

    /// <summary>
    /// Stores the value associated with <c>Sixth</c>.
    /// </summary>
    public T Sixth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedArray6" /> type.
    /// </summary>
    /// <param name="first">Specifies the value of <paramref name="first" />.</param>
    /// <param name="second">Specifies the value of <paramref name="second" />.</param>
    /// <param name="third">Specifies the value of <paramref name="third" />.</param>
    /// <param name="fourth">Specifies the value of <paramref name="fourth" />.</param>
    /// <param name="fifth">Specifies the value of <paramref name="fifth" />.</param>
    /// <param name="sixth">Specifies the value of <paramref name="sixth" />.</param>
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