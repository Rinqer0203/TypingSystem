using System;
using TMPro;
using Typing;
using Typing.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

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
        private int m_ValidInputQueueCapacity = 40;

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
            //タイピング対象の文字列を表示
            m_TypingTextTMPro.SetText(m_TypingTexts[m_TypingTextIndex].Text);

            //タイピング対象のかな文字列を表示（入力済みの文字は緑色）
            m_TypingKanaTextTMPro.SetText(m_TypingTexts[m_TypingTextIndex].KanaText);
            Profiler.BeginSample("あ");
            m_TypingKanaTextTMPro.ChangePrefixColor(Color.green, m_TypingSystem.InputedKanaLength);
            Profiler.EndSample();
            Debug.Break();

            //タイピング中の文字列の有効な入力
            m_ValidInputTMPro.SetCharSpan(m_TypingSystem.GetValidInputs(), m_TMProBuffer);

            //タイピングテキストのローマ字パターン
            m_TypingPatternTMPro.SetCharSpan(m_TypingSystem.GetFullRomajiPattern(), m_TMProBuffer);
            m_TypingPatternTMPro.ChangePrefixColor(Color.green, m_TypingSystem.GetValidInputs().Length);

            //入力中のかなのローマ字入力パターン
            foreach (var pattern in m_TypingSystem.GetCurrentRomajiPatterns())
            {
                m_TMProBuffer.Add(pattern);
                m_TMProBuffer.Add(' ');
            }
            m_KanaPatternsTMPro.SetCharArraySegment(m_TMProBuffer.Segment);
            m_TMProBuffer.Clear();

            //入力キュー
            m_InputQueueTMPro.SetCharArraySegment(m_InputQueue.Segment);
        }

        private void OnTextInput(char inputChar)
        {
            if (char.IsControl(inputChar))
                return;

            m_IsDurty = true;
            m_InputQueue.Enqueue(inputChar);

            if (m_TypingSystem.CheckInputChar(inputChar) && m_TypingSystem.IsComplete)
            {
                m_TypingTextIndex = (m_TypingTextIndex + 1) % m_TypingTexts.Length;
                m_TypingSystem.SetTypingKanaText(m_TypingTexts[m_TypingTextIndex].KanaText);
            }
        }
    }
}