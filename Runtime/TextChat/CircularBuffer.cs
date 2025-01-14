// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2024 Engineering Hydrology, RWTH Aachen University.
// 
// AnyVR is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// AnyVR is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AnyVR.
// If not, see <https://www.gnu.org/licenses/>.

using System;

namespace AnyVr.TextChat
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