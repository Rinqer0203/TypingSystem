using System;
using TMPro;
using Typing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SampleTypingSystem
{
    public sealed class SampleTypingSystemManager : MonoBehaviour
    {
        [Serializable]
        private struct TypingText
        {
            [SerializeField]
            private string m_Text;
            [SerializeField]
            private string m_KanaText;

            public string Text => m_Text;
            public string KanaText => m_KanaText;
        }

        [SerializeField]
        private TextMeshProUGUI m_TypingTextTMPro;

        [SerializeField]
        private TextMeshProUGUI m_TypingKanaTextTMPro;

        [SerializeField]
        private TextMeshProUGUI m_TypingPatternTMPro;

        [SerializeField]
        private TextMeshProUGUI m_ValidInputTMPro;

        [SerializeField]
        private TextMeshProUGUI m_KanaPatternsTMPro;

        [SerializeField]
        private TextMeshProUGUI m_InputQueueTMPro;

        [SerializeField]
        private int m_ValidInputQueueCapacity = 20;

        [SerializeField]
        private TypingText[] m_TypingTexts;

        const int TEXT_BUFFER_CAPACITY = 64;

        private readonly TypingSystem m_TypingSystem = new();
        private readonly StructBuffer<char> m_TMProBuffer = new(TEXT_BUFFER_CAPACITY);
        private LimitedStructQueue<char> m_InputQueue;

        private int m_TypingTextIndex = 0;
        private bool m_IsDurty = false;

        private void Awake()
        {
            Keyboard.current.onTextInput += OnTextInput;
            m_InputQueue = new LimitedStructQueue<char>(m_ValidInputQueueCapacity);

            m_TypingSystem.SetTypingKanaText(m_TypingTexts[m_TypingTextIndex].KanaText);
            m_IsDurty = true;
        }

        private void Update()
        {
            if (m_IsDurty == false)
                return;
            m_IsDurty = false;

            UpdateTMProTexts();
        }

        private void UpdateTMProTexts()
        {
            static void SetCharArraySegment(TextMeshProUGUI text, ArraySegment<char> charArraySegment)
            {
                text.SetCharArray(charArraySegment.Array, charArraySegment.Offset, charArraySegment.Count);
            }

            m_TypingTextTMPro.SetText(m_TypingTexts[m_TypingTextIndex].Text);
            m_TypingKanaTextTMPro.SetText(m_TypingTexts[m_TypingTextIndex].KanaText);

            //タイピング中の文字列の有効な入力
            m_TMProBuffer.Add(m_TypingSystem.GetValidInputs());
            SetCharArraySegment(m_ValidInputTMPro, m_TMProBuffer.Segment);
            m_TMProBuffer.Clear();

            //タイピングテキストのローマ字パターン
            m_TMProBuffer.Add(m_TypingSystem.GetFullRomajiPattern());
            SetCharArraySegment(m_TypingPatternTMPro, m_TMProBuffer.Segment);
            m_TMProBuffer.Clear();

            //入力中のかなのローマ字入力パターン
            var romajiPatterns = m_TypingSystem.GetRomajiPatterns();
            foreach (var pattern in romajiPatterns)
            {
                m_TMProBuffer.Add(pattern.Span);
                m_TMProBuffer.Add(' ');
            }
            SetCharArraySegment(m_KanaPatternsTMPro, m_TMProBuffer.Segment);
            m_TMProBuffer.Clear();

            //入力キュー
            SetCharArraySegment(m_InputQueueTMPro, m_InputQueue.Segment);
        }

        private void OnTextInput(char inputChar)
        {
            m_IsDurty = true;
            m_InputQueue.Enqueue(inputChar);

            if (m_TypingSystem.OnInput(inputChar) && m_TypingSystem.IsComplete)
            {
                m_TypingTextIndex = (m_TypingTextIndex + 1) % m_TypingTexts.Length;
                m_TypingSystem.SetTypingKanaText(m_TypingTexts[m_TypingTextIndex].KanaText);
            }
        }
    }
}
