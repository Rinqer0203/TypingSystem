namespace Typing
{
    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public sealed class TypingSystem
    {
        private struct TypingKanaText
        {
            private ReadOnlyMemory<char> m_CurrentTypingText;

            public TypingKanaText(ReadOnlyMemory<char> typingText)
            {
                m_CurrentTypingText = typingText;
            }

            public bool IsComplete => m_CurrentTypingText.IsEmpty;

            public ReadOnlySpan<char> CurrentTypingTextSpan => m_CurrentTypingText.Span;

            public void AdvanceKana()
            {
                m_CurrentTypingText = m_CurrentTypingText.Slice(1);
            }

            /// <summary>
            /// タイプされていない文字列の先頭と次の文字のペアを取得する
            /// </summary>
            public KanaPair GetCurrentKanaPair()
            {
                return new(GetKanaOrDefaultWithOffset(0), GetKanaOrDefaultWithOffset(1));
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
                if ((uint)m_CurrentTypingText.Length <= (uint)offset)
                    return default;
                return m_CurrentTypingText.Span[offset];
            }
        }

        private const int MAX_ROMAJI_LENGTH = 128;

        /// <summary>
        /// 「ん(n)」の冗長入力のパターンを判定するための文字列の長さ
        /// </summary>
        private const int N_LENGTH = -1;

        private TypingKanaText m_TypingKanaText;

        private readonly IKanaRomajiRegistry m_IKanaRomajiRegistry = KanaRomajiRegistryHolder.Registry;
        private readonly RomajiPatternsChecker m_RomajiPatternsChecker = new();

        private readonly StructBuffer<char> m_FullRomajiPattern = new(MAX_ROMAJI_LENGTH);
        private readonly StructBuffer<char> m_ValidInputs = new(MAX_ROMAJI_LENGTH);

        /// <summary>
        /// 「ん」の冗長入力を許可するフラグ
        /// </summary>
        private bool m_CanInputRedundantN = false;

        public void SetTypingKanaText(string typingKanaText)
        {
            m_FullRomajiPattern.Clear();
            m_ValidInputs.Clear();
            m_RomajiPatternsChecker.Clear();
            m_TypingKanaText = new TypingKanaText(typingKanaText.AsMemory());
            PrepareRomajiPatternChecker(m_TypingKanaText.GetCurrentKanaPair());
            GenerateFullRomajiPattern();
        }

        public bool IsComplete => m_TypingKanaText.IsComplete;

        public ReadOnlySpan<char> GetValidInputs() => m_ValidInputs.Span;

        public ReadOnlySpan<char> GetFullRomajiPattern() => m_FullRomajiPattern.Span;

        public ReadOnlySpan<ReadOnlyMemory<char>> GetRomajiPatterns()
            => m_RomajiPatternsChecker.GetRomajiPatterns();

        public bool OnInput(char inputChar)
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

        private bool CanNPattern(char nextKana, IKanaRomajiRegistry registry)
        {
            if (registry.ContainsKey(new KanaPair(nextKana)))
            {
                //次の文字がかな文字の範囲内
                foreach (var romaji in registry.GetRomajiPatternsOrEmpty(new KanaPair(nextKana)))
                    if (romaji[0] != 'n' && romaji[0] != 'a' && romaji[0] != 'i' && romaji[0] != 'u' && romaji[0] != 'e' && romaji[0] != 'o')
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
                m_ValidInputs.Add(inputChar);
                m_TypingKanaText.AdvanceKana();
                return true;
            }

            if (m_RomajiPatternsChecker.TryCheckInputChar(inputChar, out var kanaLength))
            {
                m_ValidInputs.Add(inputChar);

                //ん(n)の場合は冗長入力を許可して次のローマ字パターンを生成
                if (kanaLength == N_LENGTH)
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
        private void PrepareRomajiPatternChecker(KanaPair kanaPair)
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
                if (kanaPair.FirstKana == 'っ')
                {
                    foreach (var romaji in m_IKanaRomajiRegistry.GetRomajiPatternsOrEmpty(new KanaPair(kanaPair.NextKana)))
                        m_RomajiPatternsChecker.AddPattern(romaji[0], 1);
                }
                else if (kanaPair.FirstKana == 'ん')
                {
                    if (CanNPattern(kanaPair.NextKana, m_IKanaRomajiRegistry))
                        m_RomajiPatternsChecker.AddPattern('n', N_LENGTH);
                }
            }

            m_IKanaRomajiRegistry.GetRomajiPatternsOrEmpty(kanaPair
                , out var firstKanaRomajiPatterns, out var kanaPairRomajiPatterns);

            //1文字目と2文字目のペアのローマ字パターンをチェック対象に追加
            foreach (var romaji in kanaPairRomajiPatterns)
                m_RomajiPatternsChecker.AddPattern(romaji, 2);

            //1文字目のローマ字パターンをチェック対象に追加
            foreach (var romaji in firstKanaRomajiPatterns)
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
            m_FullRomajiPattern.Clear();

            var kanaText = m_TypingKanaText.CurrentTypingTextSpan;
            int KanaTextIndex = 0;

            //入力済みの文字をパターンに追加
            m_FullRomajiPattern.Add(m_ValidInputs.Span[..^m_RomajiPatternsChecker.ValidInputCount]);

            //入力途中の優先度の最も高いパターンを追加
            if (m_RomajiPatternsChecker.IsComplete == false && m_RomajiPatternsChecker.ValidInputCount > 0)
            {
                var pattern = m_RomajiPatternsChecker.GetTopPriorityPattern(out var kanaLength);
                m_FullRomajiPattern.Add(pattern);
                KanaTextIndex += kanaLength;
            }

            //「ん」の冗長入力を許可している場合は次の文字を対象にする
            if (m_CanInputRedundantN)
                KanaTextIndex++;

            //残りのパターンを追加
            for (int i = KanaTextIndex; i < kanaText.Length; i++)
            {
                var currentKana = kanaText[i];
                var nextKana = i + 1 < kanaText.Length ? kanaText[i + 1] : default;

                if (IsInAsciiRange(currentKana))
                {
                    m_FullRomajiPattern.Add(currentKana);
                    continue;
                }

                m_IKanaRomajiRegistry.GetRomajiPatternsOrEmpty(new KanaPair(currentKana, nextKana)
                    , out var currentPattern, out var pairPattern);

                //「っ」「ん」のローマ字パターンをチェック
                if (nextKana != default)
                {
                    if (currentKana == 'っ')
                    {
                        var nextKanaPattern = m_IKanaRomajiRegistry.GetRomajiPatternsOrEmpty(new(nextKana));
                        if (nextKanaPattern.IsEmpty == false)
                        {
                            m_FullRomajiPattern.Add(nextKanaPattern[0][0]);
                            continue;
                        }
                    }
                    else if (currentKana == 'ん' && CanNPattern(nextKana, m_IKanaRomajiRegistry))
                    {
                        m_FullRomajiPattern.Add('n');
                        continue;
                    }
                }

                if (pairPattern.IsEmpty == false)
                {
                    m_FullRomajiPattern.Add(pairPattern[0]);
                    i++;
                    continue;
                }

                if (currentPattern.IsEmpty == false)
                {
                    m_FullRomajiPattern.Add(currentPattern[0]);
                    continue;
                }

#if DEBUG
                Debug.LogWarning(currentKana + " : " + nextKana + " のローマ字パターンが見つからない");
#endif
            }
        }
    }
}