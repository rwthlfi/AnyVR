using NUnit.Framework;
using UnityEngine;

namespace TextChat.Tests
{
    public class BufferTest
    {
        [Test]
        public void WhereInputNotExceedsCapacity()
        {
            CircularBuffer<int> buffer = new(3);
            Assert.IsTrue(buffer.IsEmpty());
            
            buffer.Push(1);
            buffer.Push(2);

            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(1, buffer[1]);
        }
        
        [Test]
        public void WhereInputEqualsCapacity()
        {
            CircularBuffer<int> buffer = new(3);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            Assert.AreEqual(3, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(1, buffer[2]);
        }
        
        [Test]
        public void WhereInputExceedsCapacity()
        {
            CircularBuffer<int> buffer = new(3);
            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);
            buffer.Push(4);

            Assert.AreEqual(4, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
            Assert.AreEqual(2, buffer[2]);
        }
        
        [Test]
        public void WhereInputExceedsCapacityByLot()
        {
            const byte capacity = 10;
            CircularBuffer<int> buffer = new(capacity);

            byte count = (byte)Random.Range(capacity + 1, byte.MaxValue);

            for (byte i = 0; i < count; i++)
            {
                buffer.Push(i);
            }
            
            for (byte i = 0; i < capacity; i++)
            {
                Assert.AreEqual(count - i - 1, buffer[i]);
            }
        }
    }
}
