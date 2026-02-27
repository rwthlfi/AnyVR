using System;

namespace AnyVR.TextChat
{
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;

        public readonly int Capacity;
        private int _head;
        private int _tail;

        public CircularBuffer(int capacity)
        {
            Capacity = capacity;
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
        }

        public int Count
        {
            get
            {
                if (_head >= _tail)
                {
                    return _head - _tail;
                }

                return Capacity - (_tail - _head) + 1;
            }
        }

        public T this[int index]
        {
            get
            {
                int c = Count;
                if (index >= c || index < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                int actualIndex = (_head - index + Capacity) % Capacity;
                return _buffer[actualIndex];
            }
        }

        public void Push(T item)
        {
            _head = (_head + 1) % Capacity;
            if (_head == _tail)
            {
                _tail = (_tail + 1) % Capacity;
            }

            _buffer[_head] = item;
        }

        public T[] GetAll()
        {
            T[] res = new T[Count];
            for (int i = 0; i < Count; i++)
            {
                res[i] = this[i];
            }

            return res;
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
        }

        public bool IsEmpty()
        {
            return Count == 0;
        }
    }
}
