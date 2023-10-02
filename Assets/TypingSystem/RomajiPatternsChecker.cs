namespace Typing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// <see cref="RomajiPatternsChecker"/>が管理している有効なローマ字パターンのイテレータ
    /// </summary>
    public readonly struct PatternEnumerable
    {
        public struct PatternEnumerator
        {
            private List<(ReadOnlyMemory<char> pattern, int length)>.Enumerator m_InnerEnumerator;

            public PatternEnumerator(List<(ReadOnlyMemory<char> pattern, int length)>.Enumerator innerEnumerator)
            {
                m_InnerEnumerator = innerEnumerator;
            }

            public ReadOnlySpan<char> Current => m_InnerEnumerator.Current.pattern.Span;

            public bool MoveNext() => m_InnerEnumerator.MoveNext();
        }

        private readonly List<(ReadOnlyMemory<char> pattern, int length)> m_PatternViewList;

        public PatternEnumerable(List<(ReadOnlyMemory<char> pattern, int length)> patternViewList)
        {
            m_PatternViewList = patternViewList;
        }

        public PatternEnumerator GetEnumerator()
        {
            return new PatternEnumerator(m_PatternViewList.GetEnumerator());
        }
    }

    /// <summary>
    /// ローマ字パターンを管理して、入力された文字がパターンにマッチするかを判定する。
    /// Clear後、<see cref="AddPattern(char, int)"/>で追加された順番がパターンの優先度になる。
    /// </summary>
    internal sealed class RomajiPatternsChecker
    {
        public bool IsComplete { get; private set; } = false;

        public bool IsEmpty => m_PatternsBuffer.Length == 0;

        public int ValidInputCount => m_CurrentRomajiIndex;

        private readonly StructBuffer<char> m_PatternsBuffer
            = new(IKanaRomajiRegistry.MAX_PATTERN_CAPACITY * IKanaRomajiRegistry.MAX_ROMAJI_PATTERN_LENGTH);

        private readonly List<(ReadOnlyMemory<char> pattern, int length)> m_PatternViewList
            = new(IKanaRomajiRegistry.MAX_PATTERN_CAPACITY);

        private int m_CurrentRomajiIndex = 0;

        public void AddPattern(ReadOnlySpan<char> romaji, int kanaLength)
        {
            int startIndex = m_PatternsBuffer.Length;
            m_PatternsBuffer.Add(romaji);
            m_PatternViewList.Add((m_PatternsBuffer.Memory.Slice(startIndex, romaji.Length), kanaLength));
        }

        public void AddPattern(char romaji, int kanaLength)
        {
            int startIndex = m_PatternsBuffer.Length;
            m_PatternsBuffer.Add(romaji);
            m_PatternViewList.Add((m_PatternsBuffer.Memory.Slice(startIndex, 1), kanaLength));
        }

        public void Clear()
        {
            m_PatternsBuffer.Clear();
            m_PatternViewList.Clear();
            m_CurrentRomajiIndex = 0;
            IsComplete = false;
        }

        public PatternEnumerable GetPatterns()
        {
            return new PatternEnumerable(m_PatternViewList);
        }

        /// <summary>
        /// 候補の中で最も優先度の高いパターンを取得する
        /// </summary>
        public ReadOnlySpan<char> GetTopPriorityPattern(out int kanaLength)
        {
#if DEBUG
            Debug.Assert(IsComplete == false && m_PatternViewList.Count > 0);
#endif
            kanaLength = m_PatternViewList[0].length;
            return m_PatternViewList[0].pattern.Span;
        }

        /// <summary>
        /// ローマ字パターンに入力された文字がマッチするかを判定する
        /// </summary>
        /// <param name="hitPattern">入力が完了したパターン</param>
        /// <param name="completedKanaLength">入力が完了したかな文字列の長さ</param>
        public bool TryCheckInputChar(char inputChar, out ReadOnlySpan<char> hitPattern, out int completedKanaLength)
        {
            completedKanaLength = 0;
            hitPattern = ReadOnlySpan<char>.Empty;
            bool isMatched = false;

            foreach (var view in m_PatternViewList)
            {
                var pattern = view.pattern.Span;
                if (pattern[m_CurrentRomajiIndex] == inputChar)
                {
                    isMatched = true;

                    if (m_CurrentRomajiIndex + 1 == pattern.Length)
                    {
                        //パターン最後の入力だった場合
                        IsComplete = true;
                        hitPattern = pattern;
                        completedKanaLength = view.length;

                        return true;
                    }
                }
            }

            if (isMatched)
            {
                RemoveInvalidPatterns(inputChar);
                m_CurrentRomajiIndex++;
                return true;
            }

            return false;
        }

        /// <summary>    
        /// 候補から外れたパターンを削除
        /// </summary>
        private void RemoveInvalidPatterns(char inputChar)
        {
            //RemoveAllはGC Allocが発生する
            for (int i = 0; i < m_PatternViewList.Count; i++)
            {
                var pattern = m_PatternViewList[i].pattern.Span;

                if (pattern[m_CurrentRomajiIndex] != inputChar || m_CurrentRomajiIndex + 1 == pattern.Length)
                    m_PatternViewList.RemoveAt(i--);
            }
        }
    }
}