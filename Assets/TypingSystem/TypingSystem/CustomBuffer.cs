namespace Typing
{
    using System;

    /// <summary>
    /// 上限に達したら古い要素を捨てるキュー
    /// </summary>
    public sealed class LimitedStructQueue<T> where T : struct
    {
        /// <summary>
        /// <see cref="Span"/>, <see cref="Memory"/>で代用できるなら使わない
        /// </summary>
        public ArraySegment<T> Segment => new(m_Array, 0, m_EndIndex);
        public ReadOnlySpan<T> Span => m_Array.AsSpan(0, m_EndIndex);
        public ReadOnlyMemory<T> Memory => m_Array.AsMemory(0, m_EndIndex);
        public int Length => m_EndIndex;

        private readonly T[] m_Array;
        private int m_EndIndex = 0;

        public LimitedStructQueue(int capacity)
        {
            m_Array = new T[capacity];
        }

        public void Enqueue(T element)
        {
            if (m_EndIndex == m_Array.Length)
            {
                for (int i = 0; i < m_Array.Length - 1; i++)
                    m_Array[i] = m_Array[i + 1];
                m_Array[^1] = element;
            }
            else
            {
                m_Array[m_EndIndex++] = element;
            }

        }

        public void Clear()
        {
            m_EndIndex = 0;
        }
    }

    public sealed class StructBuffer<T> where T : struct
    {
        /// <summary>
        /// <see cref="Span"/>, <see cref="Memory"/>で代用できるなら使わない
        /// </summary>
        public ArraySegment<T> Segment => new(m_Array, 0, m_EndIndex);
        public ReadOnlySpan<T> Span => m_Array.AsSpan(0, m_EndIndex);
        public ReadOnlyMemory<T> Memory => m_Array.AsMemory(0, m_EndIndex);
        public int Length => m_EndIndex;

        private readonly T[] m_Array;
        private int m_EndIndex = 0;

        public StructBuffer(int capacity)
        {
            m_Array = new T[capacity];
        }

        public void Add(T element)
        {
            m_Array[m_EndIndex++] = element;
        }

        public void Add(ReadOnlySpan<T> elements)
        {
            elements.CopyTo(m_Array.AsSpan(m_EndIndex));
            m_EndIndex += elements.Length;
        }

        public void Clear()
        {
            m_EndIndex = 0;
        }
    }
}