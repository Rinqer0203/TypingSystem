namespace Typing.Extensions
{
    using System;
    using TMPro;
    using Typing;
    using UnityEngine;

    public static class TypingSystemExtensions
    {
        public readonly struct RichColorTag
        {
            private readonly string m_TagPrefix;
            private const string TAG_SUFFIX = "</color>";

            public RichColorTag(Color color)
            {
                //TODO:•‰‰×Œv‘ª
                m_TagPrefix = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>";
            }

            public void WriteBufferToTaggedText(in ReadOnlySpan<char> text, StructBuffer<char> buffer)
            {
                buffer.Add(m_TagPrefix);
                buffer.Add(text);
                buffer.Add(TAG_SUFFIX);
            }
        }

        public static void SetCharSpan(this TMP_Text tmp, in ReadOnlySpan<char> text, StructBuffer<char> buffer)
        {
            buffer.Clear();
            buffer.Add(text);
            tmp.SetCharArraySegment(buffer.Segment);
            buffer.Clear();
        }

        public static void SetCharSpanWithColorTag(this TMP_Text tmp, in ReadOnlySpan<char> text, StructBuffer<char> buffer, RichColorTag colorTag, int charLength)
        {
            buffer.Clear();
            colorTag.WriteBufferToTaggedText(text[..charLength], buffer);
            buffer.Add(text.Slice(charLength));
            tmp.SetCharArraySegment(buffer.Segment);
            buffer.Clear();
        }

        public static void SetCharArraySegment(this TMP_Text tmp, in ArraySegment<char> segment)
        {
            tmp.SetCharArray(segment.Array, segment.Offset, segment.Count);
        }

        public static void ChangePrefixColor(this TMP_Text tmp, Color color, int charLength)
        {
            tmp.ForceMeshUpdate();
            Color[] colors = tmp.mesh.colors;

            for (int i = 0; i < charLength; i++)
                for (int j = 0; j < 4; j++)
                    colors[4 * i + j] = color;

            tmp.mesh.colors = colors;
            tmp.UpdateGeometry(tmp.mesh, 0);
        }
    }
}