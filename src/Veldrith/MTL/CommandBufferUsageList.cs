using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the CommandBufferUsageList class.
/// </summary>
internal class CommandBufferUsageList<T> {

    /// <summary>
    /// Represents the _items field.
    /// </summary>
    private readonly List<(MTLCommandBuffer buffer, T value)> _items = new List<(MTLCommandBuffer buffer, T item)>();

    /// <summary>
    /// Performs the Add operation.
    /// </summary>
    /// <param name="cb">The value of cb.</param>
    /// <param name="value">The value of value.</param>
    public void Add(MTLCommandBuffer cb, T value) {
        this._items.Add((cb, value));
    }

    /// <summary>
    /// Performs the EnumerateItems operation.
    /// </summary>
    /// <returns>The result of the EnumerateItems operation.</returns>
    public ItemsEnumerator EnumerateItems() {
        return new ItemsEnumerator(this._items);
    }

    /// <summary>
    /// Performs the EnumerateAndRemove operation.
    /// </summary>
    /// <param name="cb">The value of cb.</param>
    /// <returns>The result of the EnumerateAndRemove operation.</returns>
    public RemovalEnumerator EnumerateAndRemove(MTLCommandBuffer cb) {
        return new RemovalEnumerator(this._items, cb);
    }

    /// <summary>
    /// Performs the Contains operation.
    /// </summary>
    /// <param name="cb">The value of cb.</param>
    /// <returns>The result of the Contains operation.</returns>
    public bool Contains(MTLCommandBuffer cb) {
        foreach ((MTLCommandBuffer buffer, T _) in this._items) {
            if (buffer.Equals(cb)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Performs the Clear operation.
    /// </summary>
    public void Clear() {
        this._items.Clear();
    }

    /// <summary>
    /// This is a basic enumerator for the list.
    /// </summary>
    public struct ItemsEnumerator : IEnumerator<T>, IEnumerable {

        /// <summary>
        /// Represents the list field.
        /// </summary>
        private readonly List<(MTLCommandBuffer buffer, T value)> list;

        /// <summary>
        /// Represents the _index field.
        /// </summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsEnumerator" /> type.
        /// </summary>
        /// <param name="value">The value of value.</param>
        public ItemsEnumerator(List<(MTLCommandBuffer buffer, T value)> list) {
            this.list = list;
        }

        /// <summary>
        /// Performs the MoveNext operation.
        /// </summary>
        /// <returns>The result of the MoveNext operation.</returns>
        public bool MoveNext() {
            if (this._index == this.list.Count) {
                return false;
            }

            this.Current = this.list[this._index].value;
            this._index++;

            return true;
        }

        /// <summary>
        /// Performs the Reset operation.
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
        /// Performs the Dispose operation.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Performs the GetEnumerator operation.
        /// </summary>
        /// <returns>The result of the GetEnumerator operation.</returns>
        public ItemsEnumerator GetEnumerator() {
            return this;
        }

        /// <summary>
        /// Performs the GetEnumerator operation.
        /// </summary>
        /// <returns>The result of the GetEnumerator operation.</returns>
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
        /// Represents the list field.
        /// </summary>
        private readonly List<(MTLCommandBuffer buffer, T value)> list;

        /// <summary>
        /// Represents the cb field.
        /// </summary>
        private readonly MTLCommandBuffer cb;

        /// <summary>
        /// Represents the _count field.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// Represents the _index field.
        /// </summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemovalEnumerator" /> type.
        /// </summary>
        /// <param name="value">The value of value.</param>
        /// <param name="cb">The value of cb.</param>
        public RemovalEnumerator(List<(MTLCommandBuffer buffer, T value)> list, MTLCommandBuffer cb) {
            this.list = list;
            this.cb = cb;

            this._count = list.Count;
            list.EnsureCapacity(this._count * 2);
        }

        /// <summary>
        /// Performs the MoveNext operation.
        /// </summary>
        /// <returns>The result of the MoveNext operation.</returns>
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
        /// Performs the Reset operation.
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
        /// Performs the Dispose operation.
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
        /// Performs the GetEnumerator operation.
        /// </summary>
        /// <returns>The result of the GetEnumerator operation.</returns>
        public RemovalEnumerator GetEnumerator() {
            return this;
        }

        /// <summary>
        /// Performs the GetEnumerator operation.
        /// </summary>
        /// <returns>The result of the GetEnumerator operation.</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}