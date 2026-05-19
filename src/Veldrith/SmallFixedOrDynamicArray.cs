using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// Defines the data layout and behavior of the SmallFixedOrDynamicArray struct.
/// </summary>
internal unsafe struct SmallFixedOrDynamicArray : IDisposable {

    /// <summary>
    /// Stores the value associated with <c>_max_fixed_values</c>.
    /// </summary>
    private const int _max_fixed_values = 5;

    /// <summary>
    /// Stores the value associated with <c>Count</c>.
    /// </summary>
    public readonly uint Count;

    private fixed uint _fixedData[_max_fixed_values];

    /// <summary>
    /// Stores the value associated with <c>Data</c>.
    /// </summary>
    public readonly uint[] Data;

    /// <summary>
    /// Executes the Get operation.
    /// </summary>
    /// <param name="i">Specifies the value of <paramref name="i" />.</param>
    /// <returns>Returns the result produced by the Get operation.</returns>
    public uint Get(uint i) {
        return this.Count > _max_fixed_values ? this.Data[i] : this._fixedData[i];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SmallFixedOrDynamicArray" /> type.
    /// </summary>
    /// <param name="count">Specifies the value of <paramref name="count" />.</param>
    /// <param name="data">Specifies the value of <paramref name="data" />.</param>
    public SmallFixedOrDynamicArray(uint count, ref uint data) {
        if (count > _max_fixed_values) {
            this.Data = ArrayPool<uint>.Shared.Rent((int)count);
            for (int i = 0; i < count; i++) {
                this.Data[i] = Unsafe.Add(ref data, i);
            }
        }
        else {
            for (int i = 0; i < count; i++) {
                this._fixedData[i] = Unsafe.Add(ref data, i);
            }

            this.Data = null;
        }

        this.Count = count;
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose() {
        if (this.Data != null) {
            ArrayPool<uint>.Shared.Return(this.Data);
        }
    }
}