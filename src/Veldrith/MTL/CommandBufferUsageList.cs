// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class CommandBufferUsageList<T> {
    private readonly List<(MTLCommandBuffer buffer, T value)> _items = new List<(MTLCommandBuffer buffer, T item)>();

    public void Add(MTLCommandBuffer cb, T value) {
        this._items.Add((cb, value));
    }

    public ItemsEnumerator EnumerateItems() {
        return new ItemsEnumerator(this._items);
    }

    public RemovalEnumerator EnumerateAndRemove(MTLCommandBuffer cb) {
        return new RemovalEnumerator(this._items, cb);
    }

    public bool Contains(MTLCommandBuffer cb) {
        foreach ((MTLCommandBuffer buffer, T _) in this._items) {
            if (buffer.Equals(cb)) {
                return true;
            }
        }

        return false;
    }

    public void Clear() {
        this._items.Clear();
    }

    /// <summary>
    ///     This is a basic enumerator for the list.
    /// </summary>
    public struct ItemsEnumerator : IEnumerator<T>, IEnumerable {
        private readonly List<(MTLCommandBuffer buffer, T value)> list;
        private int _index;

        public ItemsEnumerator(List<(MTLCommandBuffer buffer, T value)> list) {
            this.list = list;
        }

        public bool MoveNext() {
            if (this._index == this.list.Count) {
                return false;
            }

            this.Current = this.list[this._index].value;
            this._index++;

            return true;
        }

        public void Reset() {
            this._index = 0;
        }

        public T Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public void Dispose() { }

        public ItemsEnumerator GetEnumerator() {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    ///     This is a combined enumerate + remove enumerator for the list.
    ///     It works by duplicating the items that shall be retained to the end of the list
    ///     and then moving them in-place to the front of the list upon disposal.
    ///     The combined operation has therefore O(n) time complexity.
    /// </summary>
    public struct RemovalEnumerator : IEnumerator<T>, IEnumerable {
        private readonly List<(MTLCommandBuffer buffer, T value)> list;
        private readonly MTLCommandBuffer cb;
        private readonly int _count;
        private int _index;

        public RemovalEnumerator(List<(MTLCommandBuffer buffer, T value)> list, MTLCommandBuffer cb) {
            this.list = list;
            this.cb = cb;

            this._count = list.Count;
            list.EnsureCapacity(this._count * 2);
        }

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

        public void Reset() {
            this._index = 0;
        }

        public T Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public void Dispose() {
            if (this.list.Count == 0) {
                return;
            }

            int toKeepItemCount = this.list.Count - this._count;
            Span<(MTLCommandBuffer buffer, T value)> listSpan = CollectionsMarshal.AsSpan(this.list);

            listSpan.Slice(this._count, toKeepItemCount).CopyTo(listSpan);
            this.list.RemoveRange(toKeepItemCount, this.list.Count - toKeepItemCount);
        }

        public RemovalEnumerator GetEnumerator() {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}