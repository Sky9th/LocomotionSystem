using System;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 层级字符串标签，用于战斗状态门控、冷却、状态标记。
    /// 支持层级匹配：查询 "State" 匹配 "State.Attacking"、"State.Dead" 等子标签。
    /// </summary>
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        public readonly string Tag;

        public GameplayTag(string tag)
        {
            Tag = tag;
        }

        /// <summary>
        /// 层级匹配。query "State" 匹配 Tag "State.Attacking"（前缀+点），
        /// 但不匹配 "StateAttacking"（缺少层级分隔符）。
        /// </summary>
        public bool Matches(string query)
        {
            if (string.IsNullOrEmpty(query)) return false;
            if (string.IsNullOrEmpty(Tag)) return false;
            return Tag == query || Tag.StartsWith(query + ".", StringComparison.Ordinal);
        }

        public bool Equals(GameplayTag other) => Tag == other.Tag;
        public override bool Equals(object obj) => obj is GameplayTag other && Equals(other);
        public override int GetHashCode() => Tag?.GetHashCode() ?? 0;

        public static bool operator ==(GameplayTag a, GameplayTag b) => a.Tag == b.Tag;
        public static bool operator !=(GameplayTag a, GameplayTag b) => a.Tag != b.Tag;

        public override string ToString() => Tag ?? "<null>";
    }
}
