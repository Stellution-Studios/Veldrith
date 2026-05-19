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
    /// Executes Add.
    /// </summary>
    public void Add(MTLCommandBuffer cb, T value) {
        this._items.Add((cb, value));
    }

    /// <summary>
    /// Executes EnumerateItems.
    /// </summary>
    public ItemsEnumerator EnumerateItems() {
        return new ItemsEnumerator(this._items);
    }

    /// <summary>
    /// Executes EnumerateAndRemove.
    /// </summary>
    public RemovalEnumerator EnumerateAndRemove(MTLCommandBuffer cb) {
        return new RemovalEnumerator(this._items, cb);
    }

    /// <summary>
    /// Executes Contains.
    /// </summary>
    public bool Contains(MTLCommandBuffer cb) {
        foreach ((MTLCommandBuffer buffer, T _) in this._items) {
            if (buffer.Equals(cb)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Executes Clear.
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
        /// Initializes a new instance of the <see cref="ItemsEnumerator" /> class.
        /// </summary>
        public ItemsEnumerator(List<(MTLCommandBuffer buffer, T value)> list) {
            this.list = list;
        }

        /// <summary>
        /// Executes MoveNext.
        /// </summary>
        public bool MoveNext() {
            if (this._index == this.list.Count) {
                return false;
            }

            this.Current = this.list[this._index].value;
            this._index++;

            return true;
        }

        /// <summary>
        /// Executes Reset.
        /// </summary>
        public void Reset() {
            this._index = 0;
        }

        /// <summary>
        /// Gets or sets Current.
        /// </summary>
        public T Current { get; private set; }

        /// <summary>
        /// Gets or sets IEnumerator.Current.
        /// </summary>
        object IEnumerator.Current => this.Current;

        /// <summary>
        /// Executes Dispose.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Executes GetEnumerator.
        /// </summary>
        public ItemsEnumerator GetEnumerator() {
            return this;
        }

        /// <summary>
        /// Executes IEnumerable.GetEnumerator.
        /// </summary>
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
        /// Initializes a new instance of the <see cref="RemovalEnumerator" /> class.
        /// </summary>
        public RemovalEnumerator(List<(MTLCommandBuffer buffer, T value)> list, MTLCommandBuffer cb) {
            this.list = list;
            this.cb = cb;

            this._count = list.Count;
            list.EnsureCapacity(this._count * 2);
        }

        /// <summary>
        /// Executes MoveNext.
        /// </summary>
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
        /// Executes Reset.
        /// </summary>
        public void Reset() {
            this._index = 0;
        }

        /// <summary>
        /// Gets or sets Current.
        /// </summary>
        public T Current { get; private set; }

        /// <summary>
        /// Gets or sets IEnumerator.Current.
        /// </summary>
        object IEnumerator.Current => this.Current;

        /// <summary>
        /// Executes Dispose.
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
        /// Executes GetEnumerator.
        /// </summary>
        public RemovalEnumerator GetEnumerator() {
            return this;
        }

        /// <summary>
        /// Executes IEnumerable.GetEnumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}