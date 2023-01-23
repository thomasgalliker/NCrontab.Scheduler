
using System;
using System.Collections.Generic;
using System.Threading;

namespace NCrontab.Scheduler.Internals
{
    internal sealed class FreeList<T> : IDisposable
    {
        private const int InitialCapacity = 4;
        private const int MinShrinkStart = 8;
        private T[] values;
        private int count;
        private FastQueue<int> freeIndex;
        private bool isDisposed;
        private readonly object lockObj = new object();

        public FreeList()
        {
            this.Initialize();
        }

        public T[] GetValues() => this.values; // no lock, safe for iterate

        public int GetCount()
        {
            lock (this.lockObj)
            {
                return this.count;
            }
        }

        public int Add(T value)
        {
            lock (this.lockObj)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FreeList<T>));
                }

                if (this.freeIndex.Count != 0)
                {
                    var index = this.freeIndex.Dequeue();
                    this.values[index] = value;
                    this.count++;
                    return index;
                }
                else
                {
                    // resize
                    var newValues = new T[this.values.Length * 2];
                    Array.Copy(this.values, 0, newValues, 0, this.values.Length);
                    this.freeIndex.EnsureNewCapacity(newValues.Length);
                    for (var i = this.values.Length; i < newValues.Length; i++)
                    {
                        this.freeIndex.Enqueue(i);
                    }

                    var index = this.freeIndex.Dequeue();
                    newValues[this.values.Length] = value;
                    this.count++;
                    Volatile.Write(ref this.values, newValues);
                    return index;
                }
            }
        }

        public void Remove(int index, bool shrinkWhenEmpty)
        {
            lock (this.lockObj)
            {
                if (this.isDisposed)
                {
                    return; // do nothing
                }

                ref var v = ref this.values[index];
                if (v == null)
                {
                    throw new KeyNotFoundException($"key index {index} is not found.");
                }

                v = default;
                this.freeIndex.Enqueue(index);
                this.count--;

                if (shrinkWhenEmpty && this.count == 0 && this.values.Length > MinShrinkStart)
                {
                    this.Initialize(); // re-init.
                }
            }
        }

        /// <summary>
        /// Dispose and get cleared count.
        /// </summary>
        public bool TryDispose(out int clearedCount)
        {
            lock (this.lockObj)
            {
                if (this.isDisposed)
                {
                    clearedCount = 0;
                    return false;
                }

                clearedCount = this.count;
                this.Dispose();
                return true;
            }
        }

        public void Dispose()
        {
            lock (this.lockObj)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.isDisposed = true;

                this.freeIndex = null;
                this.values = Array.Empty<T>();
                this.count = 0;
            }
        }

        // [MemberNotNull(nameof(freeIndex), nameof(values))]
        private void Initialize()
        {
            this.freeIndex = new FastQueue<int>(InitialCapacity);
            for (var i = 0; i < InitialCapacity; i++)
            {
                this.freeIndex.Enqueue(i);
            }
            this.count = 0;

            var v = new T[InitialCapacity];
            Volatile.Write(ref this.values, v);
        }
    }
}