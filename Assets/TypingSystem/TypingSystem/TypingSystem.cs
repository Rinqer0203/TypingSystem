namespace Typing
{
    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public interface ITypingSystem
    {
        bool IsComplete { get; }
        int InputedKanaLength { get; }
        ReadOnlySpan<char> ValidInputSpan { get; }
        ReadOnlySpan<char> FullRomajiPatternSpan { get; }
        PatternEnumerable GetPermittedRomajiPatterns();
    }

    public sealed class TypingSystem : ITypingSystem
    {
        private struct TypingKanaTextManager
        {
            private readonly string m_TypingKanaText;
            private ReadOnlyMemory<char> m_CurrentTypingKanaText;

            public TypingKanaTextManager(string typingKanaText)
            {
                m_TypingKanaText = typingKanaText;
                m_CurrentTypingKanaText = typingKanaText.AsMemory();
            }

            public bool IsComplete => m_CurrentTypingKanaText.IsEmpty;

            /// <summary>
            /// 入力していないかな文字列
            /// </summary>
            public ReadOnlySpan<char> CurrentTypingKanaSpan => m_CurrentTypingKanaText.Span;

            /// <summary>
            /// 入力済みのかな文字列
            /// </summary>
            public ReadOnlySpan<char> InputedKanaSpan => m_TypingKanaText.AsSpan(0, InputedKanaLength);

            /// <summary>
            /// 入力済みのかな文字列の長さ
            /// </summary>
            public int InputedKanaLength => m_TypingKanaText.Length - m_CurrentTypingKanaText.Length;

            public void AdvanceKana()
            {
                m_CurrentTypingKanaText = m_CurrentTypingKanaText.Slice(1);
            }

            /// <summary>
            /// タイプされていない文字列の先頭と次の文字のペアを取得する
            /// </summary>
            public KanaPair GetCurrentKanaPair()
            {
                return new KanaPair(GetKanaOrDefaultWithOffset(0), GetKanaOrDefaultWithOffset(1));
            }

            /// <summary>
            /// <see cref="GetCurrentKanaPair"にoffset指定できる/>
            /// </summary>
            public KanaPair GetCurrentKanaPairWithOffset(int offset)
            {
                return new KanaPair(GetKanaOrDefaultWithOffset(offset), GetKanaOrDefaultWithOffset(offset + 1));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private char GetKanaOrDefaultWithOffset(int offset)
            {
                //範囲チェック
                if ((uint)m_CurrentTypingKanaText.Length <= (uint)offset)
                    return default;
                return m_CurrentTypingKanaText.Span[offset];
            }
        }

        private const int MAX_ROMAJI_BUFFER_CAPACITY = 64;

        private TypingKanaTextManager m_TypingKanaText;

        private readonly IKanaRomajiRegistry m_IKanaRomajiRegistry = KanaRomajiRegistryHolder.Registry;
        private readonly RomajiPatternsChecker m_RomajiPatternsChecker = new();

        private readonly StructBuffer<char> m_FullRomajiPatternBuffer = new(MAX_ROMAJI_BUFFER_CAPACITY);
        private readonly StructBuffer<char> m_ValidInputsBuffer = new(MAX_ROMAJI_BUFFER_CAPACITY);

        /// <summary>
        /// 「ん」の冗長入力を許可するフラグ
        /// </summary>
        private bool m_CanInputRedundantN = false;

        public void SetTypingKanaText(string typingKanaText)
        {
            m_FullRomajiPatternBuffer.Clear();
            m_ValidInputsBuffer.Clear();
            m_RomajiPatternsChecker.Clear();
            m_TypingKanaText = new TypingKanaTextManager(typingKanaText);
            PrepareRomajiPatternChecker(m_TypingKanaText.GetCurrentKanaPair());
            GenerateFullRomajiPattern();
        }

        public bool IsComplete => m_TypingKanaText.IsComplete;

        public int InputedKanaLength => m_TypingKanaText.InputedKanaLength;

        /// <summary>
        /// 有効な入力文字を取得
        /// </summary>
        public ReadOnlySpan<char> ValidInputSpan => m_ValidInputsBuffer.Span;

        /// <summary>
        /// 現在の状態のフルサイズローマ字パターンを取得
        /// </summary>
        public ReadOnlySpan<char> FullRomajiPatternSpan => m_FullRomajiPatternBuffer.Span;

        /// <summary>
        /// 入力判定中のパターンのイテレータを取得する(foreachで回す想定)
        /// </summary>
        public PatternEnumerable GetPermittedRomajiPatterns() => m_RomajiPatternsChecker.GetPatterns();

        public bool CheckInputChar(char inputChar)
        {
            if (UpdateState(inputChar))
            {
                GenerateFullRomajiPattern();
                return true;
            }
            return false;
        }

        private bool IsInAsciiRange(char target)
        {
            return 0x00 <= target && target <= 0x7F;
        }

        private bool CanNPattern(char nextKana)
        {
            if (m_IKanaRomajiRegistry.TryGetRomajiPatterns(new KanaPair(nextKana), out var nextKanaPatterns))
            {
                //次の文字がかな文字の範囲内
                foreach (var romaji in nextKanaPatterns)
                    if (romaji[0] is not ('n' or 'a' or 'i' or 'u' or 'e' or 'o'))
                        return true;
            }
            return false;
        }

        private bool UpdateState(char inputChar)
        {
            //「ん」の冗長入力の判定
            if (m_CanInputRedundantN && inputChar == 'n')
            {
                m_CanInputRedundantN = false;
                m_ValidInputsBuffer.Add(inputChar);
                m_TypingKanaText.AdvanceKana();
                return true;
            }

            if (m_RomajiPatternsChecker.TryCheckInputChar(inputChar, out var hitPattern, out var kanaLength))
            {
                m_ValidInputsBuffer.Add(inputChar);
                var currentKanaPair = m_TypingKanaText.GetCurrentKanaPair();

                //「っ」(省略入力)の場合は次のローマ字パターンにフィルターを掛けて生成する
                if (currentKanaPair.FirstKana == 'っ' && hitPattern.IsEmpty == false &&
                    m_IKanaRomajiRegistry.ContainsPattern(new KanaPair('っ'), hitPattern) == false)
                {
                    m_TypingKanaText.AdvanceKana();
                    PrepareRomajiPatternChecker(m_TypingKanaText.GetCurrentKanaPair(), inputChar);
                    return true;
                }

                //「ん」(n)の場合は冗長入力を許可して次のローマ字パターンを生成
                if (currentKanaPair.FirstKana == 'ん' && hitPattern.IsEmpty == false && hitPattern.Length == 1 && hitPattern[0] == 'n')
                {
                    m_CanInputRedundantN = true;
                    PrepareRomajiPatternChecker(m_TypingKanaText.GetCurrentKanaPairWithOffset(1));
                    return true;
                }

                //「ん」の冗長入力を受け付けていた場合は「ん」を確定させて冗長入力を許可しないようにする
                if (m_CanInputRedundantN)
                {
                    m_TypingKanaText.AdvanceKana();
                    m_CanInputRedundantN = false;
                }

                if (m_RomajiPatternsChecker.IsComplete)
                {
                    for (int i = 0; i < kanaLength; i++)
                        m_TypingKanaText.AdvanceKana();
                    PrepareRomajiPatternChecker(m_TypingKanaText.GetCurrentKanaPair());
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// <see cref="m_RomajiPatternsChecker"/>にローマ字パターンを登録する
        /// </summary>
        private void PrepareRomajiPatternChecker(in KanaPair kanaPair, char patternInitialFilter = default)
        {
            m_RomajiPatternsChecker.Clear();

            if (IsInAsciiRange(kanaPair.FirstKana))
            {
                m_RomajiPatternsChecker.AddPattern(kanaPair.FirstKana, 1);
                return;
            }

            if (kanaPair.NextKana != default)
            {
                //「っ」「ん」のローマ字パターンをチェック
                if (kanaPair.FirstKana == 'っ' &&
                    m_IKanaRomajiRegistry.TryGetRomajiPatterns(new KanaPair(kanaPair.NextKana), out var nextKanaPatterns))
                {
                    static bool Contains(in ReadOnlySpan<char> chars, char target)
                    {
                        for (int i = 0; i < chars.Length; i++)
                            if (chars[i] == target)
                                return true;
                        return false;
                    }

                    Span<char> romajiInitials = stackalloc char[nextKanaPatterns.Length];
                    int index = 0;

                    foreach (var romaji in nextKanaPatterns)
                        if (romaji.Length > 1 && Contains(romajiInitials, romaji[0]) == false)
                        {
                            m_RomajiPatternsChecker.AddPattern(romaji[0], 1);
                            romajiInitials[index++] = romaji[0];
                        }
                }
                else if (kanaPair.FirstKana == 'ん' && CanNPattern(kanaPair.NextKana))
                {
                    m_RomajiPatternsChecker.AddPattern('n', 1);
                }
            }

            m_IKanaRomajiRegistry.GetRomajiPatternsOrEmpty(kanaPair
                , out var firstKanaRomajiPatterns, out var kanaPairRomajiPatterns);

            //1文字目と2文字目のペアのローマ字パターンをチェック対象に追加
            foreach (var romaji in kanaPairRomajiPatterns)
                if (patternInitialFilter == default || patternInitialFilter == romaji[0])
                    m_RomajiPatternsChecker.AddPattern(romaji, 2);

            //1文字目のローマ字パターンをチェック対象に追加
            foreach (var romaji in firstKanaRomajiPatterns)
                if (patternInitialFilter == default || patternInitialFilter == romaji[0])
                    m_RomajiPatternsChecker.AddPattern(romaji, 1);

#if DEBUG
            Debug.Assert(m_RomajiPatternsChecker.IsEmpty == false, "ローマ字パターンが見つからない");
#endif
        }

        /// <summary>
        /// 現在の入力を考慮した<see cref="m_TypingKanaText"/>のローマ字パターンを生成する
        /// </summary>
        private void GenerateFullRomajiPattern()
        {
            m_FullRomajiPatternBuffer.Clear();
            int kanaIndex = 0;

            //入力済みの文字をパターンに追加
            m_FullRomajiPatternBuffer.Add(m_ValidInputsBuffer.Span[..^m_RomajiPatternsChecker.ValidInputCount]);

            //ローマ字パターン入力途中か「っ」を最後に入力した状態なら優先度の高いパターンを追加
            if (m_RomajiPatternsChecker.IsComplete == false && m_RomajiPatternsChecker.ValidInputCount > 0 ||
                m_TypingKanaText.InputedKanaSpan.Length > 0 && m_TypingKanaText.InputedKanaSpan[^1] == 'っ')
            {
                m_FullRomajiPatternBuffer.Add(m_RomajiPatternsChecker.GetTopPriorityPattern(out var kanaLength));
                kanaIndex += kanaLength;
            }

            //「ん」の冗長入力を許可している場合は次の文字を対象にする
            if (m_CanInputRedundantN)
                kanaIndex++;

            var currentKanaSpan = m_TypingKanaText.CurrentTypingKanaSpan;

            //残りのパターンを追加
            for (int i = kanaIndex; i < currentKanaSpan.Length; i++)
            {
                var currentKana = currentKanaSpan[i];
                var nextKana = currentKanaSpan.Length > i + 1 ? currentKanaSpan[i + 1] : default;

                if (IsInAsciiRange(currentKana))
                {
                    m_FullRomajiPatternBuffer.Add(currentKana);
                    continue;
                }

                m_IKanaRomajiRegistry.GetRomajiPatternsOrEmpty(new KanaPair(currentKana, nextKana)
                    , out var currentPattern, out var pairPattern);

                //「っ」「ん」のローマ字パターンをチェック
                if (nextKana != default)
                {
                    if (currentKana == 'っ' && m_IKanaRomajiRegistry.TryGetRomajiPatterns(new KanaPair(nextKana), out var nextKanaPatterns))
                    {
                        bool isAdded = false;
                        foreach (var romaji in nextKanaPatterns)
                            if (romaji.Length > 1)
                            {
                                m_FullRomajiPatternBuffer.Add(romaji[0]);
                                isAdded = true;
                                break;
                            }

                        if (isAdded)
                            continue;
                    }
                    else if (currentKana == 'ん' && CanNPattern(nextKana))
                    {
                        m_FullRomajiPatternBuffer.Add('n');
                        continue;
                    }
                }

                if (pairPattern.IsEmpty == false)
                {
                    m_FullRomajiPatternBuffer.Add(pairPattern[0]);
                    i++;
                    continue;
                }

                if (currentPattern.IsEmpty == false)
                {
                    m_FullRomajiPatternBuffer.Add(currentPattern[0]);
                    continue;
                }

#if DEBUG
                Debug.LogWarning(currentKana + " : " + nextKana + " のローマ字パターンが見つからない");
#endif
            }
        }
    }
}