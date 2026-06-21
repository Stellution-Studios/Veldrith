using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the CommandBufferUsageList type used by the graphics runtime.
/// </summary>
internal class CommandBufferUsageList<T> {

    /// <summary>
    /// Executes the list logic for this backend.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    private readonly List<(MTLCommandBuffer buffer, T value)> _items = new List<(MTLCommandBuffer buffer, T item)>();

    /// <summary>
    /// Executes the add logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    /// <param name="value">The value used by this operation.</param>
    public void Add(MTLCommandBuffer cb, T value) {
        this._items.Add((cb, value));
    }

    /// <summary>
    /// Executes the enumerate items logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public ItemsEnumerator EnumerateItems() {
        return new ItemsEnumerator(this._items);
    }

    /// <summary>
    /// Executes the enumerate and remove logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public RemovalEnumerator EnumerateAndRemove(MTLCommandBuffer cb) {
        return new RemovalEnumerator(this._items, cb);
    }

    /// <summary>
    /// Executes the contains logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Contains(MTLCommandBuffer cb) {
        foreach ((MTLCommandBuffer buffer, T _) in this._items) {
            if (buffer.Equals(cb)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Executes the clear logic for this backend.
    /// </summary>
    public void Clear() {
        this._items.Clear();
    }

    /// <summary>
    /// This is a basic enumerator for the list.
    /// </summary>
    public struct ItemsEnumerator : IEnumerator<T>, IEnumerable {

        /// <summary>
        /// Stores the list collection used by this instance.
        /// </summary>
        /// <param name="buffer">The buffer resource involved in this operation.</param>
        /// <param name="value">The value used by this operation.</param>
        private readonly List<(MTLCommandBuffer buffer, T value)> list;

        /// <summary>
        /// Stores the index value used during command execution.
        /// </summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsEnumerator" /> type.
        /// </summary>
        /// <param name="list">The list value used by this operation.</param>
        public ItemsEnumerator(List<(MTLCommandBuffer buffer, T value)> list) {
            this.list = list;
        }

        /// <summary>
        /// Executes the move next logic for this backend.
        /// </summary>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
        public bool MoveNext() {
            if (this._index == this.list.Count) {
                return false;
            }

            this.Current = this.list[this._index].value;
            this._index++;

            return true;
        }

        /// <summary>
        /// Resets this instance to its initial state.
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
        /// Releases resources held by this instance.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Gets the enumerator value.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        public ItemsEnumerator GetEnumerator() {
            return this;
        }

        /// <summary>
        /// Gets the enumerator value.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    /// This is a combined enumerate + remove enumerator for the list.
    /// </summary>
    public struct RemovalEnumerator : IEnumerator<T>, IEnumerable {

        /// <summary>
        /// Stores the list collection used by this instance.
        /// </summary>
        /// <param name="buffer">The buffer resource involved in this operation.</param>
        /// <param name="value">The value used by this operation.</param>
        private readonly List<(MTLCommandBuffer buffer, T value)> list;

        /// <summary>
        /// Stores the cb state used by this instance.
        /// </summary>
        private readonly MTLCommandBuffer cb;

        /// <summary>
        /// Stores the count value used during command execution.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// Stores the index value used during command execution.
        /// </summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemovalEnumerator" /> type.
        /// </summary>
        /// <param name="list">The list value used by this operation.</param>
        /// <param name="cb">The cb value used by this operation.</param>
        public RemovalEnumerator(List<(MTLCommandBuffer buffer, T value)> list, MTLCommandBuffer cb) {
            this.list = list;
            this.cb = cb;

            this._count = list.Count;
            list.EnsureCapacity(this._count * 2);
        }

        /// <summary>
        /// Executes the move next logic for this backend.
        /// </summary>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
        /// Resets this instance to its initial state.
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
        /// Releases resources held by this instance.
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
        /// Gets the enumerator value.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        public RemovalEnumerator GetEnumerator() {
            return this;
        }

        /// <summary>
        /// Gets the enumerator value.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}