using System.Collections.Generic;
using System.Linq;

namespace RedDust.Core
{
    /// <summary>
    /// RdTag 集合，管理实体当前持有的所有标签。
    /// _tagsByOwner(owner → List&lt;RdTag&gt;) 是主数据结构，_activeTags 是去重查询缓存。
    /// </summary>
    public sealed class RdTagContainer
    {
        private readonly Dictionary<object, List<RdTag>> _tagsByOwner = new();
        private readonly HashSet<RdTag> _activeTags = new();

        private static readonly object PermanentOwner = new();

        public int Count => _activeTags.Count;

        // ── 写入（无 owner，手动管理）──

        public void AddTag(string tag) => AddTag(tag, PermanentOwner);

        public void AddTag(RdTag tag) => AddTag(tag.Tag, PermanentOwner);

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            var rdTag = new RdTag(tag);
            _activeTags.Remove(rdTag);
            foreach (var list in _tagsByOwner.Values)
                list.Remove(rdTag);
        }

        public void RemoveTag(RdTag tag)
        {
            if (!tag.IsValid) return;
            _activeTags.Remove(tag);
            foreach (var list in _tagsByOwner.Values)
                list.Remove(tag);
        }

        // ── 写入（带 owner 追踪）──

        public void AddTag(string tag, object owner)
        {
            if (string.IsNullOrEmpty(tag) || owner == null) return;
            var rdTag = new RdTag(tag);
            if (!rdTag.IsValid) return;

            _activeTags.Add(rdTag);

            if (!_tagsByOwner.TryGetValue(owner, out var list))
                _tagsByOwner[owner] = list = new List<RdTag>();
            if (!list.Contains(rdTag))
                list.Add(rdTag);
        }

        public void RemoveTagsByOwner(object owner)
        {
            if (owner == null || !_tagsByOwner.TryGetValue(owner, out var tags)) return;

            // 检查每个 tag 是否还有其他 owner 持有
            foreach (var tag in tags)
            {
                bool stillActive = false;
                foreach (var kvp in _tagsByOwner)
                {
                    if (kvp.Key == owner) continue;
                    if (kvp.Value.Contains(tag)) { stillActive = true; break; }
                }
                if (!stillActive)
                    _activeTags.Remove(tag);
            }

            _tagsByOwner.Remove(owner);
        }

        /// <summary>清理 Owner 满足判定条件的标签。由 L3 层传入判定委托，避免 L1→L3 依赖。</summary>
        public void RemoveTagsWhere(System.Func<object, bool> predicate)
        {
            var ownersToRemove = new List<object>();
            foreach (var owner in _tagsByOwner.Keys)
                if (predicate(owner))
                    ownersToRemove.Add(owner);

            foreach (var owner in ownersToRemove)
                RemoveTagsByOwner(owner);
        }

        // ── 查询 ──

        public bool HasTag(string query)
        {
            if (string.IsNullOrEmpty(query)) return false;
            foreach (var tag in _activeTags)
                if (tag.Matches(query))
                    return true;
            return false;
        }

        public bool HasTag(RdTag query) => HasTag(query.Tag);

        public bool HasTagExact(string query)
        {
            if (string.IsNullOrEmpty(query)) return false;
            return _activeTags.Contains(new RdTag(query));
        }

        public bool HasTagExact(RdTag query) => HasTagExact(query.Tag);

        public bool HasTagAtDepth(int depth)
        {
            foreach (var tag in _activeTags)
                if (tag.Depth == depth)
                    return true;
            return false;
        }

        public int MaxDepthUnder(string ancestor)
        {
            int max = 0;
            foreach (var tag in _activeTags)
                if (tag.Matches(ancestor) && tag.Depth > max)
                    max = tag.Depth;
            return max;
        }

        public bool HasAny(params string[] queries)
        {
            if (queries == null || queries.Length == 0) return false;
            foreach (var q in queries)
                if (HasTag(q))
                    return true;
            return false;
        }

        public bool HasAll(params string[] queries)
        {
            if (queries == null || queries.Length == 0) return false;
            foreach (var q in queries)
                if (!HasTag(q))
                    return false;
            return true;
        }

        public string[] GetAll() => _activeTags.Select(t => t.Tag).ToArray();

        public void Clear()
        {
            _tagsByOwner.Clear();
            _activeTags.Clear();
        }
    }
}
