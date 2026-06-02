using System;

namespace RedDust.Core
{
    /// <summary>
    /// 层级标签运行时值类型。轻量、HashSet 友好、零装箱，对标 UE FGameplayTag。
    /// 设计时使用 <see cref="GameplayTagDefinitionSO"/> 定义，运行时通过隐式转换获取。
    ///
    /// 层级匹配：查询 "State" 匹配 "State.Attacking"（前缀+点），
    /// 但不匹配 "StateAttacking"（缺少层级分隔符）。
    /// </summary>
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        /// <summary>完整层级标签字符串，如 "State.Attacking"。</summary>
        public readonly string Tag;

        /// <summary>层级深度，构造时预计算。根=1，"State.Attacking"=2。</summary>
        public readonly int Depth;

        /// <summary>Tag 字符串是否非空。用于配置校验和门控 guard。</summary>
        public bool IsValid => !string.IsNullOrEmpty(Tag);

        public GameplayTag(string tag)
        {
            Tag = tag;
            Depth = string.IsNullOrEmpty(tag) ? 0 : CountSeparators(tag) + 1;
        }

        // ── 层级匹配 ──

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

        /// <summary>类型安全重载。行为与 <see cref="Matches(string)"/> 一致。</summary>
        public bool Matches(GameplayTag query) => Matches(query.Tag);

        /// <summary>this 是否是 other 的祖先层级（前缀）。</summary>
        public bool IsAncestorOf(GameplayTag other)
            => !string.IsNullOrEmpty(Tag)
               && other.Tag != null
               && other.Tag.StartsWith(Tag + ".", StringComparison.Ordinal);

        /// <summary>this 是否是 other 的后代层级。</summary>
        public bool IsDescendantOf(GameplayTag other) => other.IsAncestorOf(this);

        // ── Equality（HashSet 去重 + O(1) 查询）──

        public bool Equals(GameplayTag other) => Tag == other.Tag;
        public override bool Equals(object obj) => obj is GameplayTag other && Equals(other);
        public override int GetHashCode() => Tag?.GetHashCode() ?? 0;

        public static bool operator ==(GameplayTag a, GameplayTag b) => a.Tag == b.Tag;
        public static bool operator !=(GameplayTag a, GameplayTag b) => a.Tag != b.Tag;

        public override string ToString() => Tag ?? "<null>";

        // ── 内部 ──

        private static int CountSeparators(string tag)
        {
            int count = 0;
            for (int i = 0; i < tag.Length; i++)
                if (tag[i] == '.')
                    count++;
            return count;
        }
    }
}
