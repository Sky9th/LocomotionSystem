using System.Collections.Generic;
using System.Linq;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// GameplayTag 集合，管理角色当前持有的所有标签。
    /// 用于 CombatComponent 的门控、冷却、状态查询。
    /// </summary>
    public sealed class GameplayTagContainer
    {
        private readonly HashSet<GameplayTag> _tags = new();

        public int Count => _tags.Count;

        /// <summary>添加标签。已存在则无操作。</summary>
        public void AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            _tags.Add(new GameplayTag(tag));
        }

        /// <summary>移除标签。不存在则无操作。</summary>
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            _tags.Remove(new GameplayTag(tag));
        }

        /// <summary>是否有匹配 query 层级的标签。query "State" 匹配 "State.Attacking"。</summary>
        public bool HasTag(string query)
        {
            if (string.IsNullOrEmpty(query)) return false;
            foreach (var tag in _tags)
                if (tag.Matches(query))
                    return true;
            return false;
        }

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

        /// <summary>获取所有标签（调试用）。</summary>
        public string[] GetAll()
        {
            return _tags.Select(t => t.Tag).ToArray();
        }

        public void Clear() => _tags.Clear();
    }
}
