using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the CommandBufferUsageList class.
/// </summary>
internal class CommandBufferUsageList<T> {

    /// <summary>
    /// Stores the value associated with <c>_items</c>.
    /// </summary>
    private readonly List<(MTLCommandBuffer buffer, T value)> _items = new List<(MTLCommandBuffer buffer, T item)>();

    /// <summary>
    /// Executes the Add operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public void Add(MTLCommandBuffer cb, T value) {
        this._items.Add((cb, value));
    }

    /// <summary>
    /// Executes the EnumerateItems operation.
    /// </summary>
    /// <returns>Returns the result produced by the EnumerateItems operation.</returns>
    public ItemsEnumerator EnumerateItems() {
        return new ItemsEnumerator(this._items);
    }

    /// <summary>
    /// Executes the EnumerateAndRemove operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    /// <returns>Returns the result produced by the EnumerateAndRemove operation.</returns>
    public RemovalEnumerator EnumerateAndRemove(MTLCommandBuffer cb) {
        return new RemovalEnumerator(this._items, cb);
    }

    /// <summary>
    /// Executes the Contains operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    /// <returns>Returns the result produced by the Contains operation.</returns>
    public bool Contains(MTLCommandBuffer cb) {
        foreach ((MTLCommandBuffer buffer, T _) in this._items) {
            if (buffer.Equals(cb)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Executes the Clear operation.
    /// </summary>
    public void Clear() {
        this._items.Clear();
    }

    /// <summary>
    /// This is a basic enumerator for the list.
    /// </summary>
    public struct ItemsEnumerator : IEnumerator<T>, IEnumerable {

        /// <summary>
        /// Stores the value associated with <c>list</c>.
        /// </summary>
        private readonly List<(MTLCommandBuffer buffer, T value)> list;

        /// <summary>
        /// Stores the value associated with <c>_index</c>.
        /// </summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsEnumerator" /> type.
        /// </summary>
        /// <param name="value">Specifies the value of <paramref name="value" />.</param>
        public ItemsEnumerator(List<(MTLCommandBuffer buffer, T value)> list) {
            this.list = list;
        }

        /// <summary>
        /// Executes the MoveNext operation.
        /// </summary>
        /// <returns>Returns the result produced by the MoveNext operation.</returns>
        public bool MoveNext() {
            if (this._index == this.list.Count) {
                return false;
            }

            this.Current = this.list[this._index].value;
            this._index++;

            return true;
        }

        /// <summary>
        /// Executes the Reset operation.
        /// </summary>
        public void Reset() {
            this._index = 0;
        }

        /// <summary>
        /// Gets or sets Current.
        /// </summary>
        public T Current { get; private set; }

        /// <summary>
        /// Gets the current item as an <see cref="object" />.
        /// </summary>
        object IEnumerator.Current => this.Current;

        /// <summary>
        /// Executes the Dispose operation.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Executes the GetEnumerator operation.
        /// </summary>
        /// <returns>Returns the result produced by the GetEnumerator operation.</returns>
        public ItemsEnumerator GetEnumerator() {
            return this;
        }

        /// <summary>
        /// Executes the GetEnumerator operation.
        /// </summary>
        /// <returns>Returns the result produced by the GetEnumerator operation.</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    /// This is a combined enumerate + remove enumerator for the list.
    /// It works by duplicating the items that shall be retained to the end of the list
    /// and then moving them in-place to the front of the list upon disposal.
    /// The combined operation has therefore O(n) time complexity.
    /// </summary>
    public struct RemovalEnumerator : IEnumerator<T>, IEnumerable {

        /// <summary>
        /// Stores the value associated with <c>list</c>.
        /// </summary>
        private readonly List<(MTLCommandBuffer buffer, T value)> list;

        /// <summary>
        /// Stores the value associated with <c>cb</c>.
        /// </summary>
        private readonly MTLCommandBuffer cb;

        /// <summary>
        /// Stores the value associated with <c>_count</c>.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// Stores the value associated with <c>_index</c>.
        /// </summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemovalEnumerator" /> type.
        /// </summary>
        /// <param name="value">Specifies the value of <paramref name="value" />.</param>
        /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
        public RemovalEnumerator(List<(MTLCommandBuffer buffer, T value)> list, MTLCommandBuffer cb) {
            this.list = list;
            this.cb = cb;

            this._count = list.Count;
            list.EnsureCapacity(this._count * 2);
        }

        /// <summary>
        /// Executes the MoveNext operation.
        /// </summary>
        /// <returns>Returns the result produced by the MoveNext operation.</returns>
        public bool MoveNext() {
            while (true) {
                if (this._index == this._count) {
                    return false;
                }

                if (this.list[this._index].buffer.Equals(this.cb)) {
                    break;
                }

                // Track the item to be kept.
                this.list.Add(this.list[this._index]);
                this._index++;
            }

            this.Current = this.list[this._index].value;
            this._index++;

            return true;
        }

        /// <summary>
        /// Executes the Reset operation.
        /// </summary>
        public void Reset() {
            this._index = 0;
        }

        /// <summary>
        /// Gets or sets Current.
        /// </summary>
        public T Current { get; private set; }

        /// <summary>
        /// Gets the current item as an <see cref="object" />.
        /// </summary>
        object IEnumerator.Current => this.Current;

        /// <summary>
        /// Executes the Dispose operation.
        /// </summary>
        public void Dispose() {
            if (this.list.Count == 0) {
                return;
            }

            int toKeepItemCount = this.list.Count - this._count;
            Span<(MTLCommandBuffer buffer, T value)> listSpan = CollectionsMarshal.AsSpan(this.list);

            listSpan.Slice(this._count, toKeepItemCount).CopyTo(listSpan);
            this.list.RemoveRange(toKeepItemCount, this.list.Count - toKeepItemCount);
        }

        /// <summary>
        /// Executes the GetEnumerator operation.
        /// </summary>
        /// <returns>Returns the result produced by the GetEnumerator operation.</returns>
        public RemovalEnumerator GetEnumerator() {
            return this;
        }

        /// <summary>
        /// Executes the GetEnumerator operation.
        /// </summary>
        /// <returns>Returns the result produced by the GetEnumerator operation.</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}