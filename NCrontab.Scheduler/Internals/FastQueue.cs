
using System;
using System.Runtime.CompilerServices;

namespace NCrontab.Scheduler.Internals
{
    internal class FastQueue<T>
    {
        private T[] array;
        private int head;
        private int tail;

        public FastQueue(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }

            this.array = new T[capacity];
            this.head = this.tail = this.Count = 0;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get; private set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            if (this.Count == this.array.Length)
            {
                this.ThrowForFullQueue();
            }

            this.array[this.tail] = item;
            this.MoveNext(ref this.tail);
            this.Count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (this.Count == 0)
            {
                this.ThrowForEmptyQueue();
            }

            var head = this.head;
            var array = this.array;
            var removed = array[head];
            array[head] = default;
            this.MoveNext(ref this.head);
            this.Count--;
            return removed;
        }

        public void EnsureNewCapacity(int capacity)
        {
            var newarray = new T[capacity];
            if (this.Count > 0)
            {
                if (this.head < this.tail)
                {
                    Array.Copy(this.array, this.head, newarray, 0, this.Count);
                }
                else
                {
                    Array.Copy(this.array, this.head, newarray, 0, this.array.Length - this.head);
                    Array.Copy(this.array, 0, newarray, this.array.Length - this.head, this.tail);
                }
            }

            this.array = newarray;
            this.head = 0;
            this.tail = this.Count == capacity ? 0 : this.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveNext(ref int index)
        {
            var tmp = index + 1;
            if (tmp == this.array.Length)
            {
                tmp = 0;
            }
            index = tmp;
        }

        private void ThrowForEmptyQueue()
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        private void ThrowForFullQueue()
        {
            throw new InvalidOperationException("Queue is full.");
        }
    }
}