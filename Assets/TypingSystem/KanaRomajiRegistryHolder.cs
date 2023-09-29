namespace Typing
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    /// <summary>
    /// ひらがなに対応するローマ字パターンを管理
    /// </summary>
    internal interface IKanaRomajiRegistry
    {
        /// <summary>
        /// ローマ字パターンの最長文字数
        /// </summary>
        const int MAX_ROMAJI_LENGTH = 4;

        /// <summary>
        /// <see cref="KanaPair"/>が持ちうる最大のパターン数（余裕を持たせて実際より多い）
        /// </summary>
        const int MAX_PATTERN_CAPACITY = 16;

        /// <summary>
        /// かなに対応するローマ字パターンを取得する
        /// </summary>
        ReadOnlySpan<string> GetRomajiPatternsOrEmpty(KanaPair kanaPair);

        /// <summary>
        /// 最初の1文字のかなに対応するローマ字パターンと、2文字のかなに対応するローマ字パターンを取得する
        /// </summary>
        void GetRomajiPatternsOrEmpty(KanaPair kanaPair
            , out ReadOnlySpan<string> currentKanaRomajiPatterns, out ReadOnlySpan<string> KanaPairRomajiPatterns);

        bool ContainsKey(KanaPair kanaPair);
    }

    internal static class KanaRomajiRegistryHolder
    {
        public static IKanaRomajiRegistry Registry { get; } = new KanaRomajiRegistry();

        private sealed class KanaRomajiRegistry : IKanaRomajiRegistry
        {
            private readonly Dictionary<KanaPair, string[]> m_KanaRomajiDictionary;

            public KanaRomajiRegistry()
            {
                m_KanaRomajiDictionary = GenerateKanaRomajiDictionary();
            }

            void IKanaRomajiRegistry.GetRomajiPatternsOrEmpty(KanaPair kanaPair
                , out ReadOnlySpan<string> currentKanaRomajiPatterns, out ReadOnlySpan<string> KanaPairRomajiPatterns)
            {
                currentKanaRomajiPatterns = GetRomajiPatternsOrEmpty(kanaPair.FirstKana);
                if (kanaPair.NextKana == default)
                    KanaPairRomajiPatterns = ReadOnlySpan<string>.Empty;
                else
                    KanaPairRomajiPatterns = GetRomajiPatternsOrEmpty(kanaPair.FirstKana, kanaPair.NextKana);
            }

            ReadOnlySpan<string> IKanaRomajiRegistry.GetRomajiPatternsOrEmpty(KanaPair kanaPair)
            {
                return GetRomajiPatternsOrEmpty(kanaPair.FirstKana, kanaPair.NextKana);
            }

            bool IKanaRomajiRegistry.ContainsKey(KanaPair kanaPair)
            {
                return m_KanaRomajiDictionary.ContainsKey(kanaPair);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<string> GetRomajiPatternsOrEmpty(char currentKana, char nextKana = default)
            {
                if (m_KanaRomajiDictionary.TryGetValue(new(currentKana, nextKana), out var romajiPatterns))
                    return romajiPatterns.AsSpan();
                else
                    return ReadOnlySpan<string>.Empty;
            }
        }

        private static Dictionary<KanaPair, string[]> GenerateKanaRomajiDictionary()
        {
            const int DICTIONARY_CAPACITY = 220;
            var dic = new Dictionary<KanaPair, string[]>(DICTIONARY_CAPACITY);

            //ローマ字の優先順位はString配列のインデックス順
            dic.Add(new('あ'), new string[] { "a" });
            dic.Add(new('い'), new string[] { "i" });
            dic.Add(new('う'), new string[] { "u", "wu", "whu" });
            dic.Add(new('え'), new string[] { "e" });
            dic.Add(new('お'), new string[] { "o" });

            dic.Add(new('か'), new string[] { "ka", "ca" });
            dic.Add(new('き'), new string[] { "ki" });
            dic.Add(new('く'), new string[] { "ku", "cu", "qu" });
            dic.Add(new('け'), new string[] { "ke" });
            dic.Add(new('こ'), new string[] { "ko", "co" });

            dic.Add(new('さ'), new string[] { "sa" });
            dic.Add(new('し'), new string[] { "si", "ci", "shi" });
            dic.Add(new('す'), new string[] { "su" });
            dic.Add(new('せ'), new string[] { "se", "ce" });
            dic.Add(new('そ'), new string[] { "so" });

            dic.Add(new('た'), new string[] { "ta" });
            dic.Add(new('ち'), new string[] { "ti", "chi" });
            dic.Add(new('つ'), new string[] { "tu", "tsu" });
            dic.Add(new('て'), new string[] { "te" });
            dic.Add(new('と'), new string[] { "to" });

            dic.Add(new('な'), new string[] { "na" });
            dic.Add(new('に'), new string[] { "ni" });
            dic.Add(new('ぬ'), new string[] { "nu" });
            dic.Add(new('ね'), new string[] { "ne" });
            dic.Add(new('の'), new string[] { "no" });

            dic.Add(new('は'), new string[] { "ha" });
            dic.Add(new('ひ'), new string[] { "hi" });
            dic.Add(new('ふ'), new string[] { "fu", "hu" });
            dic.Add(new('へ'), new string[] { "he" });
            dic.Add(new('ほ'), new string[] { "ho" });

            dic.Add(new('ま'), new string[] { "ma" });
            dic.Add(new('み'), new string[] { "mi" });
            dic.Add(new('む'), new string[] { "mu" });
            dic.Add(new('め'), new string[] { "me" });
            dic.Add(new('も'), new string[] { "mo" });

            dic.Add(new('や'), new string[] { "ya" });
            dic.Add(new('ゆ'), new string[] { "yu" });
            dic.Add(new('よ'), new string[] { "yo" });

            dic.Add(new('ら'), new string[] { "ra" });
            dic.Add(new('り'), new string[] { "ri" });
            dic.Add(new('る'), new string[] { "ru" });
            dic.Add(new('れ'), new string[] { "re" });
            dic.Add(new('ろ'), new string[] { "ro" });

            dic.Add(new('わ'), new string[] { "wa" });
            dic.Add(new('を'), new string[] { "wo" });
            dic.Add(new('ん'), new string[] { "nn", "xn" });  // 「n」は別プロセスでパターン判定

            //--------------------------------------------------------------------------------

            dic.Add(new('が'), new string[] { "ga" });
            dic.Add(new('ぎ'), new string[] { "gi" });
            dic.Add(new('ぐ'), new string[] { "gu" });
            dic.Add(new('げ'), new string[] { "ge" });
            dic.Add(new('ご'), new string[] { "go" });

            dic.Add(new('ざ'), new string[] { "za" });
            dic.Add(new('じ'), new string[] { "ji", "zi" });
            dic.Add(new('ず'), new string[] { "zu" });
            dic.Add(new('ぜ'), new string[] { "ze" });
            dic.Add(new('ぞ'), new string[] { "zo" });

            dic.Add(new('だ'), new string[] { "da" });
            dic.Add(new('ぢ'), new string[] { "di" });
            dic.Add(new('づ'), new string[] { "zu", "du" });
            dic.Add(new('で'), new string[] { "de" });
            dic.Add(new('ど'), new string[] { "do" });

            dic.Add(new('ば'), new string[] { "ba" });
            dic.Add(new('び'), new string[] { "bi" });
            dic.Add(new('ぶ'), new string[] { "bu" });
            dic.Add(new('べ'), new string[] { "be" });
            dic.Add(new('ぼ'), new string[] { "bo" });

            dic.Add(new('ぱ'), new string[] { "pa" });
            dic.Add(new('ぴ'), new string[] { "pi" });
            dic.Add(new('ぷ'), new string[] { "pu" });
            dic.Add(new('ぺ'), new string[] { "pe" });
            dic.Add(new('ぽ'), new string[] { "po" });

            //--------------------------------------------------------------------------------

            dic.Add(new('い', 'ぇ'), new string[] { "ye" });

            dic.Add(new('う', 'ぁ'), new string[] { "wha" });
            dic.Add(new('う', 'ぃ'), new string[] { "wi", "whi" });
            dic.Add(new('う', 'ぇ'), new string[] { "we", "whe" });
            dic.Add(new('う', 'ぉ'), new string[] { "who" });

            dic.Add(new('ゔ', 'ぁ'), new string[] { "va" });
            dic.Add(new('ゔ', 'ぃ'), new string[] { "vi", "vyi" });
            dic.Add(new('ゔ'), new string[] { "vu" });
            dic.Add(new('ゔ', 'ぇ'), new string[] { "ve", "vye" });
            dic.Add(new('ゔ', 'ぉ'), new string[] { "vo" });

            dic.Add(new('ゔ', 'ゃ'), new string[] { "vya" });
            dic.Add(new('ゔ', 'ゅ'), new string[] { "vyu" });
            dic.Add(new('ゔ', 'ょ'), new string[] { "vyo" });

            dic.Add(new('き', 'ゃ'), new string[] { "kya" });
            dic.Add(new('き', 'ぃ'), new string[] { "kyi" });
            dic.Add(new('き', 'ゅ'), new string[] { "kyu" });
            dic.Add(new('き', 'ぇ'), new string[] { "kye" });
            dic.Add(new('き', 'ょ'), new string[] { "kyo" });

            dic.Add(new('く', 'ぁ'), new string[] { "qa", "kwa" });
            dic.Add(new('く', 'ぃ'), new string[] { "qi", "kwi" });
            dic.Add(new('く', 'ぅ'), new string[] { "kwu" });
            dic.Add(new('く', 'ぇ'), new string[] { "qe", "kwe" });
            dic.Add(new('く', 'ぉ'), new string[] { "qo", "kwo" });

            dic.Add(new('し', 'ゃ'), new string[] { "sya", "sha" });
            dic.Add(new('し', 'ぃ'), new string[] { "syi" });
            dic.Add(new('し', 'ゅ'), new string[] { "syu", "shu" });
            dic.Add(new('し', 'ぇ'), new string[] { "sye", "she" });
            dic.Add(new('し', 'ょ'), new string[] { "syo", "sho" });

            dic.Add(new('す', 'ぁ'), new string[] { "swa" });
            dic.Add(new('す', 'ぃ'), new string[] { "swi" });
            dic.Add(new('す', 'ぅ'), new string[] { "swu" });
            dic.Add(new('す', 'ぇ'), new string[] { "swe" });
            dic.Add(new('す', 'ぉ'), new string[] { "swo" });

            dic.Add(new('ち', 'ゃ'), new string[] { "tya", "cha", "cya" });
            dic.Add(new('ち', 'ぃ'), new string[] { "tyi", "cyi" });
            dic.Add(new('ち', 'ゅ'), new string[] { "tyu", "chu", "cyu" });
            dic.Add(new('ち', 'ぇ'), new string[] { "tye", "che", "cye" });
            dic.Add(new('ち', 'ょ'), new string[] { "tyo", "cho", "cyo" });

            dic.Add(new('つ', 'ぁ'), new string[] { "tsa" });
            dic.Add(new('つ', 'ぃ'), new string[] { "tsi" });
            dic.Add(new('つ', 'ぇ'), new string[] { "tse" });
            dic.Add(new('つ', 'ぉ'), new string[] { "tso" });

            dic.Add(new('て', 'ゃ'), new string[] { "tha" });
            dic.Add(new('て', 'ぃ'), new string[] { "thi" });
            dic.Add(new('て', 'ゅ'), new string[] { "thu" });
            dic.Add(new('て', 'ぇ'), new string[] { "the" });
            dic.Add(new('て', 'ょ'), new string[] { "tho" });

            dic.Add(new('と', 'ぁ'), new string[] { "twa" });
            dic.Add(new('と', 'ぃ'), new string[] { "twi" });
            dic.Add(new('と', 'ぅ'), new string[] { "twu" });
            dic.Add(new('と', 'ぇ'), new string[] { "twe" });
            dic.Add(new('と', 'ぉ'), new string[] { "two" });

            dic.Add(new('に', 'ゃ'), new string[] { "nya" });
            dic.Add(new('に', 'ぃ'), new string[] { "nyi" });
            dic.Add(new('に', 'ゅ'), new string[] { "nyu" });
            dic.Add(new('に', 'ぇ'), new string[] { "nye" });
            dic.Add(new('に', 'ょ'), new string[] { "nyo" });

            dic.Add(new('ひ', 'ゃ'), new string[] { "hya" });
            dic.Add(new('ひ', 'ぃ'), new string[] { "hyi" });
            dic.Add(new('ひ', 'ゅ'), new string[] { "hyu" });
            dic.Add(new('ひ', 'ぇ'), new string[] { "hye" });
            dic.Add(new('ひ', 'ょ'), new string[] { "hyo" });

            dic.Add(new('ふ', 'ぁ'), new string[] { "fa", "hwa" });
            dic.Add(new('ふ', 'ぃ'), new string[] { "fi", "hwi" });
            dic.Add(new('ふ', 'ぇ'), new string[] { "fe", "hwe" });
            dic.Add(new('ふ', 'ぉ'), new string[] { "fo", "hwo" });

            dic.Add(new('ふ', 'ゃ'), new string[] { "fya" });
            dic.Add(new('ふ', 'ゅ'), new string[] { "fyu", "hwyu" });
            dic.Add(new('ふ', 'ょ'), new string[] { "fyo" });

            dic.Add(new('み', 'ゃ'), new string[] { "mya" });
            dic.Add(new('み', 'ぃ'), new string[] { "myi" });
            dic.Add(new('み', 'ゅ'), new string[] { "myu" });
            dic.Add(new('み', 'ぇ'), new string[] { "mye" });
            dic.Add(new('み', 'ょ'), new string[] { "myo" });

            dic.Add(new('り', 'ゃ'), new string[] { "rya" });
            dic.Add(new('り', 'ぃ'), new string[] { "ryi" });
            dic.Add(new('り', 'ゅ'), new string[] { "ryu" });
            dic.Add(new('り', 'ぇ'), new string[] { "rye" });
            dic.Add(new('り', 'ょ'), new string[] { "ryo" });

            //--------------------------------------------------------------------------------

            dic.Add(new('ぎ', 'ゃ'), new string[] { "gya" });
            dic.Add(new('ぎ', 'ぃ'), new string[] { "gyi" });
            dic.Add(new('ぎ', 'ゅ'), new string[] { "gyu" });
            dic.Add(new('ぎ', 'ぇ'), new string[] { "gye" });
            dic.Add(new('ぎ', 'ょ'), new string[] { "gyo" });

            dic.Add(new('ぐ', 'ぁ'), new string[] { "gwa" });
            dic.Add(new('ぐ', 'ぃ'), new string[] { "gwi" });
            dic.Add(new('ぐ', 'ぅ'), new string[] { "gwu" });
            dic.Add(new('ぐ', 'ぇ'), new string[] { "gwe" });
            dic.Add(new('ぐ', 'ぉ'), new string[] { "gwo" });

            dic.Add(new('じ', 'ゃ'), new string[] { "ja", "jya", "zya" });
            dic.Add(new('じ', 'ぃ'), new string[] { "jyi", "zyi" });
            dic.Add(new('じ', 'ゅ'), new string[] { "ju", "jyu", "zyu" });
            dic.Add(new('じ', 'ぇ'), new string[] { "je", "jye", "zye" });
            dic.Add(new('じ', 'ょ'), new string[] { "jo", "jyo", "zyo" });

            dic.Add(new('ず', 'ぁ'), new string[] { "zwa" });
            dic.Add(new('ず', 'ぃ'), new string[] { "zwi" });
            dic.Add(new('ず', 'ぅ'), new string[] { "zwu" });
            dic.Add(new('ず', 'ぇ'), new string[] { "zwe" });
            dic.Add(new('ず', 'ぉ'), new string[] { "zwo" });

            dic.Add(new('ぢ', 'ゃ'), new string[] { "dya" });
            dic.Add(new('ぢ', 'ぃ'), new string[] { "dyi" });
            dic.Add(new('ぢ', 'ゅ'), new string[] { "dyu" });
            dic.Add(new('ぢ', 'ぇ'), new string[] { "dye" });
            dic.Add(new('ぢ', 'ょ'), new string[] { "dyo" });

            dic.Add(new('で', 'ゃ'), new string[] { "dha" });
            dic.Add(new('で', 'ぃ'), new string[] { "dhi" });
            dic.Add(new('で', 'ゅ'), new string[] { "dhu" });
            dic.Add(new('で', 'ぇ'), new string[] { "dhe" });
            dic.Add(new('で', 'ょ'), new string[] { "dho" });

            dic.Add(new('ど', 'ぁ'), new string[] { "dwa" });
            dic.Add(new('ど', 'ぃ'), new string[] { "dwi" });
            dic.Add(new('ど', 'ぅ'), new string[] { "dwu" });
            dic.Add(new('ど', 'ぇ'), new string[] { "dwe" });
            dic.Add(new('ど', 'ぉ'), new string[] { "dwo" });

            dic.Add(new('び', 'ゃ'), new string[] { "bya" });
            dic.Add(new('び', 'ぃ'), new string[] { "byi" });
            dic.Add(new('び', 'ゅ'), new string[] { "byu" });
            dic.Add(new('び', 'ぇ'), new string[] { "bye" });
            dic.Add(new('び', 'ょ'), new string[] { "byo" });

            dic.Add(new('ぴ', 'ゃ'), new string[] { "pya" });
            dic.Add(new('ぴ', 'ぃ'), new string[] { "pyi" });
            dic.Add(new('ぴ', 'ゅ'), new string[] { "pyu" });
            dic.Add(new('ぴ', 'ぇ'), new string[] { "pye" });
            dic.Add(new('ぴ', 'ょ'), new string[] { "pyo" });

            //--------------------------------------------------------------------------------

            dic.Add(new('ぁ'), new string[] { "la", "xa" });
            dic.Add(new('ぃ'), new string[] { "li", "xi", "lyi", "xyi" });
            dic.Add(new('ぅ'), new string[] { "lu", "xu" });
            dic.Add(new('ぇ'), new string[] { "le", "xe", "lye", "xye" });
            dic.Add(new('ぉ'), new string[] { "lo", "xo" });

            dic.Add(new('ゃ'), new string[] { "lya", "xya" });
            dic.Add(new('ゅ'), new string[] { "lyu", "xyu" });
            dic.Add(new('ょ'), new string[] { "lyo", "xyo" });

            dic.Add(new('ゎ'), new string[] { "lwa", "xwa" });
            dic.Add(new('っ'), new string[] { "xtu", "ltu", "xtsu", "ltsu" });

            dic.Add(new('ゐ'), new string[] { "wyi" });
            dic.Add(new('ゑ'), new string[] { "wye" });

            dic.Add(new('ー'), new string[] { "-" });
            dic.Add(new('「'), new string[] { "[" });
            dic.Add(new('」'), new string[] { "]" });
            dic.Add(new('、'), new string[] { "," });
            dic.Add(new('。'), new string[] { "." });

#if DEBUG
            Debug.Assert(dic.Count <= DICTIONARY_CAPACITY, "KanaRomajiDictionaryのキャパシティを超えた（GC Alloc)");
#endif
            return dic;
        }
    }
}