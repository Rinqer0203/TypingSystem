namespace Typing
{
    using System;

    /// <summary>
    /// かな2文字のペア構造体
    /// </summary>
    internal readonly struct KanaPair : IEquatable<KanaPair>
    {
        public readonly char FirstKana;
        public readonly char NextKana;

        public KanaPair(char firstKana, char nextKana = default)
        {
            FirstKana = firstKana;
            NextKana = nextKana;
        }

        public override bool Equals(object obj)
            => obj is KanaPair str && Equals(str);

        public bool Equals(KanaPair other)
            => FirstKana == other.FirstKana && NextKana == other.NextKana;

        public override int GetHashCode()
            => HashCode.Combine(FirstKana, NextKana);

        public static bool operator ==(KanaPair left, KanaPair right)
            => left.Equals(right);

        public static bool operator !=(KanaPair left, KanaPair right)
            => !(left == right);
    }
}
