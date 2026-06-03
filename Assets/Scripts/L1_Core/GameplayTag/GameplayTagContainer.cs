using System.Collections.Generic;
using System.Linq;

namespace RedDust.Core
{
    /// <summary>
    /// GameplayTag 集合，管理实体当前持有的所有标签。
    /// 全系统通用基础设施。用于门控、冷却、状态查询、跨系统通信。
    ///
    /// HashSet 底层：自动去重，O(1) 查询。
    /// </summary>
    public sealed class GameplayTagContainer
    {
        private readonly HashSet<GameplayTag> _tags = new();

        /// <summary>当前标签数量。</summary>
        public int Count => _tags.Count;

        // ── 写入 ──

        /// <summary>添加标签。已存在或空字符串则无操作。</summary>
        public void AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            _tags.Add(new GameplayTag(tag));
        }

        /// <summary>类型安全重载。无效标签或已存在则无操作。</summary>
        public void AddTag(GameplayTag tag)
        {
            if (!tag.IsValid) return;
            _tags.Add(tag);
        }

        /// <summary>移除标签。不存在或空字符串则无操作。</summary>
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            _tags.Remove(new GameplayTag(tag));
        }

        /// <summary>类型安全重载。无效标签或不存在则无操作。</summary>
        public void RemoveTag(GameplayTag tag)
        {
            if (!tag.IsValid) return;
            _tags.Remove(tag);
        }

        // ── 层级查询（前缀匹配）──

        /// <summary>是否有匹配 query 层级的标签。query "State" 匹配 "State.Attacking"。</summary>
        public bool HasTag(string query)
        {
            if (string.IsNullOrEmpty(query)) return false;
            foreach (var tag in _tags)
                if (tag.Matches(query))
                    return true;
            return false;
        }

        /// <summary>类型安全重载。</summary>
        public bool HasTag(GameplayTag query) => HasTag(query.Tag);

        // ── 精确查询（冷却管理专用）──

        /// <summary>精确匹配（不使用层级匹配）。
        /// "Skill.Cooldown.Slash" 不匹配 "Skill.Cooldown.Slash.Extra"。</summary>
        public bool HasTagExact(string query)
        {
            if (string.IsNullOrEmpty(query)) return false;
            return _tags.Contains(new GameplayTag(query));
        }

        /// <summary>类型安全重载。</summary>
        public bool HasTagExact(GameplayTag query) => HasTagExact(query.Tag);

        // ── 深度查询 ──

        /// <summary>是否有指定深度的标签。</summary>
        public bool HasTagAtDepth(int depth)
        {
            foreach (var tag in _tags)
                if (tag.Depth == depth)
                    return true;
            return false;
        }

        /// <summary>获取指定祖先层级下最深标签的深度。无匹配返回 0。</summary>
        public int MaxDepthUnder(string ancestor)
        {
            int max = 0;
            foreach (var tag in _tags)
                if (tag.Matches(ancestor) && tag.Depth > max)
                    max = tag.Depth;
            return max;
        }

        // ── 集合查询 ──

        /// <summary>是否有任意一个匹配的标签。</summary>
        public bool HasAny(params string[] queries)
        {
            if (queries == null || queries.Length == 0) return false;
            foreach (var q in queries)
                if (HasTag(q))
                    return true;
            return false;
        }

        /// <summary>是否全部匹配。</summary>
        public bool HasAll(params string[] queries)
        {
            if (queries == null || queries.Length == 0) return false;
            foreach (var q in queries)
                if (!HasTag(q))
                    return false;
            return true;
        }

        /// <summary>获取所有标签字符串数组（调试用）。</summary>
        public string[] GetAll()
        {
            return _tags.Select(t => t.Tag).ToArray();
        }

        /// <summary>清空所有标签。</summary>
        public void Clear() => _tags.Clear();
    }
}
