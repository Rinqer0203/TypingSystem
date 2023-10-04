using System;
using TMPro;
using Typing;
using Typing.Extensions;
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
        private TextMeshProUGUI m_TypingFullPatternTMPro;

        [SerializeField]
        private TextMeshProUGUI m_ValidInputTMPro;

        [SerializeField]
        private TextMeshProUGUI m_PermittedRomajiPatternsTMPro;

        [SerializeField]
        private TextMeshProUGUI m_InputQueueTMPro;

        [SerializeField]
        private int m_ValidInputQueueCapacity = 40;

        [SerializeField]
        private Color m_InputedColor = Color.green;

        [SerializeField]
        private TypingText[] m_TypingTexts;

        const int TEXT_BUFFER_CAPACITY = 64;

        private readonly TypingSystem m_TypingSystem = new();
        private readonly StructBuffer<char> m_TMProBuffer = new(TEXT_BUFFER_CAPACITY);
        private LimitedStructQueue<char> m_InputQueue;
        private RichColorTag m_RichColorTag;

        private int m_TypingTextIndex = 0;
        private bool m_IsDurty = false;

        private void Awake()
        {
            Keyboard.current.onTextInput += OnTextInput;
            m_InputQueue = new LimitedStructQueue<char>(m_ValidInputQueueCapacity);
            m_RichColorTag = new RichColorTag(m_InputedColor);

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
            //�^�C�s���O�Ώۂ̕������\��
            m_TypingTextTMPro.SetText(m_TypingTexts[m_TypingTextIndex].Text);

            //�^�C�s���O�Ώۂ̂��ȕ������\���i���͍ς݂̕����͗ΐF�j
            m_RichColorTag.WriteTaggedTextToBuffer(m_TypingTexts[m_TypingTextIndex].KanaText, m_TypingSystem.InputedKanaLength, m_TMProBuffer);
            m_TypingKanaTextTMPro.SetCharArraySegment(m_TMProBuffer.Segment);
            m_TMProBuffer.Clear();

            //�^�C�s���O�e�L�X�g�̃��[�}���p�^�[���i���͍ς݂̕����͗ΐF�j
            m_RichColorTag.WriteTaggedTextToBuffer(m_TypingSystem.FullRomajiPatternSpan, m_TypingSystem.ValidInputSpan.Length, m_TMProBuffer);
            m_TypingFullPatternTMPro.SetCharArraySegment(m_TMProBuffer.Segment);
            m_TMProBuffer.Clear();

            //�^�C�s���O���̕�����̗L���ȓ���
            m_ValidInputTMPro.SetCharSpan(m_TypingSystem.ValidInputSpan, m_TMProBuffer);

            //���͉\�ȃ��[�}�����̓p�^�[��
            foreach (var pattern in m_TypingSystem.GetPermittedRomajiPatterns())
            {
                m_TMProBuffer.Add(pattern);
                m_TMProBuffer.Add(' ');
            }
            m_PermittedRomajiPatternsTMPro.SetCharArraySegment(m_TMProBuffer.Segment);
            m_TMProBuffer.Clear();

            //���̓L���[
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