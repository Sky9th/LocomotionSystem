using System.Collections.Generic;
using System.Linq;

namespace RedDust.Ability
{
    /// <summary>
    /// 技能实例生命周期管理器。
    ///
    /// 副作用采用 Pull 模型：副作用携带 Owner=AbilityInstance，
    /// FloatState.Tick / CleanupExpiredCooldowns 发现 Owner.IsActive=false 时自动清理。
    /// InstanceManager 不追踪"影响了谁"。
    /// </summary>
    public sealed class InstanceManager
    {
        private readonly Dictionary<AbilityInstance, AbilityInstance> _instances = new();
        private readonly Dictionary<(AbilitySO def, object source), AbilityInstance> _logicalIndex = new();
        private readonly Dictionary<object, List<AbilityInstance>> _sourceIndex = new();
        private readonly Dictionary<ETriggerEvent, List<AbilityInstance>> _triggerIndex = new();

        public int Count => _instances.Count;
        public IReadOnlyList<AbilityInstance> All => _instances.Values.ToList();

        // ═══════════════════════════════════════════════════════════════
        //  Activate
        // ═══════════════════════════════════════════════════════════════

        public AbilityInstance Activate(
            AbilitySO definition, object source,
            ELifecycle lifecycle, ERefreshPolicy refresh = ERefreshPolicy.Refresh)
        {
            var key = (definition, source);

            if (_logicalIndex.TryGetValue(key, out var existing))
            {
                switch (refresh)
                {
                    case ERefreshPolicy.Refresh:
                        // 返回已有实例。调用方负责 RemoveAdjuncts(self) + 重新跑 FSM。
                        return existing;

                    case ERefreshPolicy.Stack:
                        return CreateInstance(definition, source, lifecycle, refresh, writeLogicalIndex: false);

                    case ERefreshPolicy.Replace:
                        Deactivate(existing);
                        return CreateInstance(definition, source, lifecycle, refresh, writeLogicalIndex: true);
                }
            }

            return CreateInstance(definition, source, lifecycle, refresh, writeLogicalIndex: true);
        }

        private AbilityInstance CreateInstance(
            AbilitySO definition, object source,
            ELifecycle lifecycle, ERefreshPolicy refresh, bool writeLogicalIndex)
        {
            var instance = new AbilityInstance(definition, source, lifecycle, refresh);

            _instances[instance] = instance;

            if (writeLogicalIndex)
                _logicalIndex[(definition, source)] = instance;

            if (!_sourceIndex.TryGetValue(source, out var list))
                _sourceIndex[source] = list = new List<AbilityInstance>();
            list.Add(instance);

            if (definition is PassiveAbilitySO passive && passive.trigger != ETriggerEvent.None)
                IndexTrigger(passive.trigger, instance);

            return instance;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Deactivate
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 标记实例失效。副作用由目标侧 Tick（FloatState / CleanupExpiredCooldowns）自动清理。
        /// </summary>
        public void Deactivate(AbilityInstance instance)
        {
            if (instance == null) return;
            if (!_instances.ContainsKey(instance)) return;

            instance.IsActive = false;

            _instances.Remove(instance);

            var key = (instance.Definition, instance.Source);
            if (_logicalIndex.TryGetValue(key, out var indexed) && ReferenceEquals(indexed, instance))
                _logicalIndex.Remove(key);

            if (_sourceIndex.TryGetValue(instance.Source, out var sourceList))
            {
                sourceList.Remove(instance);
                if (sourceList.Count == 0)
                    _sourceIndex.Remove(instance.Source);
            }

            UnindexAllTriggers(instance);
        }

        public void DeactivateBySource(object source)
        {
            if (source == null) return;
            if (!_sourceIndex.TryGetValue(source, out var instances)) return;

            var copy = instances.ToArray();
            foreach (var inst in copy)
                Deactivate(inst);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Query
        // ═══════════════════════════════════════════════════════════════

        public IReadOnlyList<AbilityInstance> GetByTrigger(ETriggerEvent trigger)
        {
            if (!_triggerIndex.TryGetValue(trigger, out var instances))
                return System.Array.Empty<AbilityInstance>();

            var result = new List<AbilityInstance>(instances.Count);
            foreach (var inst in instances)
                if (_instances.ContainsKey(inst))
                    result.Add(inst);

            return result;
        }

        public AbilityInstance Find(AbilityInstance instance)
        {
            if (instance == null) return null;
            _instances.TryGetValue(instance, out var existing);
            return existing;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Internal
        // ═══════════════════════════════════════════════════════════════

        private void IndexTrigger(ETriggerEvent trigger, AbilityInstance instance)
        {
            if (!_triggerIndex.TryGetValue(trigger, out var list))
                _triggerIndex[trigger] = list = new List<AbilityInstance>();
            list.Add(instance);
        }

        private void UnindexAllTriggers(AbilityInstance instance)
        {
            foreach (var kvp in _triggerIndex)
                kvp.Value.Remove(instance);
        }
    }
}
