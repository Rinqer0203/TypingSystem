namespace Typing.Extensions
{
    using System;
    using TMPro;
    using Typing;
    using UnityEngine;

    public readonly struct RichColorTag
    {
        private readonly string m_TagPrefix;
        private const string TAG_SUFFIX = "</color>";

        public RichColorTag(Color color)
        {
            m_TagPrefix = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>";
        }

        /// <summary>
        /// ��������J���[�^�O�ň͂�Ńo�b�t�@�ɏ�������
        /// </summary>
        /// <param name="text">�^�O�ň͂ރe�L�X�g</param>
        /// <param name="charLength">�^�O�ň͂ސ擪����̕�����</param>
        /// <param name="buffer">�������ݐ�̃o�b�t�@</param>
        public void WriteTaggedTextToBuffer(in ReadOnlySpan<char> text, int charLength, StructBuffer<char> buffer)
        {
            buffer.Clear();
            buffer.Add(m_TagPrefix);
            buffer.Add(text[..charLength]);
            buffer.Add(TAG_SUFFIX);
            buffer.Add(text[charLength..]);
        }
    }

    public static class TextMeshProExtensions
    {
        public static void SetCharSpan(this TMP_Text tmp, in ReadOnlySpan<char> text, StructBuffer<char> buffer)
        {
            buffer.Clear();
            buffer.Add(text);
            tmp.SetCharArraySegment(buffer.Segment);
            buffer.Clear();
        }

        public static void SetCharArraySegment(this TMP_Text tmp, in ArraySegment<char> segment)
        {
            tmp.SetCharArray(segment.Array, segment.Offset, segment.Count);
        }

        /// <summary>
        /// <see cref="TMP_Text"/>���Ń������m�ۂ����Ă���̂�GC Alloc���C�ɂ���ꍇ�͒���
        /// </summary>
        public static void ChangePrefixColor(this TMP_Text tmp, Color color, int charLength)
        {
            tmp.ForceMeshUpdate();
            var colors = tmp.mesh.colors;

            for (int i = 0; i < charLength; i++)
                for (int j = 0; j < 4; j++)
                    colors[4 * i + j] = color;

            tmp.mesh.colors = colors;
            tmp.UpdateGeometry(tmp.mesh, 0);
        }
    }
}