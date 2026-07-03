using System.Collections.Generic;
using RedDust.Container;
using RedDust.Core;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Character
{
    /// <summary>
    /// L4 装备子模块 — 临时方案。
    ///
    /// 每帧以 CharacterContainer.BodyContainer 为数据源，
    /// diff 上一帧快照 → 管理武器 GO 创建/销毁 + 同步 GripTag。
    ///
    /// 职责边界：
    /// - 读 Container 数据 → 派生 GO 和 GripTag（单向，不写回 Container）
    /// - 不管 BodyForm（那是 Director 的事）
    /// - 不管技能激活（那是 Ability 的事）
    ///
    /// TODO Phase 3 (ReplaceModel): 模型替换时需要 Despawn 全部视图 + 刷新 _animator + Respawn。
    /// </summary>
    internal sealed class CharacterEquipment : ModuleChild
    {
        private readonly CharacterBuildContext ctx;

        /// <summary>上一帧的槽位快照：SlotKey → EntityId。用 GoF 做 diff 避免每帧 Instantiate/Destroy。</summary>
        private readonly Dictionary<string, string> _slotSnapshot = new();

        /// <summary>已生成的武器 GO：SlotKey → GameObject。槽位移除时用于精准 Destroy。</summary>
        private readonly Dictionary<string, GameObject> _spawnedViews = new();

        private Animator _animator;

        internal CharacterEquipment(CharacterBuildContext ctx, ModuleRegistry registry) : base(registry)
        {
            this.ctx = ctx;
        }

        public override void OnWire()
        {
            _animator = ctx.ModelRoot?.GetComponent<Animator>();
        }

        /// <summary>
        /// 每帧由 CharacterActor.Update 调用，在 anim set 解析之前。
        /// </summary>
        public void SyncEquipment()
        {
            var bodyContainer = ctx.Container;
            if (bodyContainer == null) return;

            // 1. 读当前 Container 状态
            var next = ReadSlotState(bodyContainer);

            // 2. Diff — 只在变化时操作 GO
            bool changed = false;

            // Removed / changed: 上一帧的 slot 在当前帧消失或 entityId 不同
            foreach (var kv in _slotSnapshot)
            {
                if (!next.TryGetValue(kv.Key, out var newId) || newId != kv.Value)
                {
                    DespawnView(kv.Key);
                    changed = true;
                }
            }

            // Added / changed: 当前帧新出现或 entityId 不同的 slot
            foreach (var kv in next)
            {
                if (!_slotSnapshot.TryGetValue(kv.Key, out var oldId) || oldId != kv.Value)
                {
                    var entity = FindInSlot(bodyContainer, kv.Key, kv.Value);
                    if (entity != null)
                        SpawnView(kv.Key, entity);
                    changed = true;
                }
            }

            if (changed)
            {
                _slotSnapshot.Clear();
                foreach (var kv in next)
                    _slotSnapshot[kv.Key] = kv.Value;
            }

            // 3. 同步 GripTag
            SyncGripTags(bodyContainer);
        }

        // ── Slot State ──

        private static Dictionary<string, string> ReadSlotState(Container.RdContainer container)
        {
            var state = new Dictionary<string, string>();
            foreach (var slot in container.SlotsOrdered)
            {
                if (!slot.IsEmpty && slot.Items.Count > 0)
                    state[slot.Def.SlotId] = slot.Items[0].Id;
            }
            return state;
        }

        private static Entity FindInSlot(Container.RdContainer container, string slotKey, string entityId)
        {
            var slot = container.GetSlot(slotKey);
            if (slot == null) return null;
            foreach (var item in slot.Items)
            {
                if (item.Id == entityId)
                    return item;
            }
            return null;
        }

        // ── GO Lifecycle ──

        private void SpawnView(string slotKey, Entity entity)
        {
            if (entity.Preset?.Prefab == null) return;

            var bone = SlotBoneMapper.GetBoneForSlot(_animator, slotKey);
            if (bone == null)
            {
                if (_animator == null || !_animator.isHuman)
                    Debug.LogWarning($"[CharacterEquipment] {ctx.Root.name}: non-humanoid rig — cannot attach '{slotKey}' to bone.");
                else
                    Debug.LogWarning($"[CharacterEquipment] {ctx.Root.name}: no bone mapping for slot '{slotKey}'.");
                return;
            }

            var socket = WeaponAttachPoint.GetOrCreateSocket(bone, slotKey, entity.Properties?.GetTagList(CharacterConst.PropertyPath.CommonTags));
            var go = Object.Instantiate(entity.Preset.Prefab, socket, worldPositionStays: false);
            go.name = $"{entity.Preset.name}_{entity.Id}";
            _spawnedViews[slotKey] = go;

            Debug.Log($"[CharacterEquipment] {ctx.Root.name}: spawned {go.name} → {slotKey}");
        }

        private void DespawnView(string slotKey)
        {
            if (!_spawnedViews.TryGetValue(slotKey, out var go)) return;
            _spawnedViews.Remove(slotKey);

            if (go != null)
            {
                Debug.Log($"[CharacterEquipment] {ctx.Root.name}: destroying {go.name} from {slotKey}");
                Object.Destroy(go);
            }
        }

        // ── Grip Tags ──

        /// <summary>
        /// 从所有装备 Entity 的 Common/Tags 同步全部标签到 ctx.OwnedGripTags。
        /// 动画系统用 Equip.Grip.* 选动画集，AbilityForest 用 Weapon.* 过滤技能树。
        /// Container 全空时跳过 —— 保留 PlayerDirector hack 可用。
        /// </summary>
        private void SyncGripTags(Container.RdContainer container)
        {
            bool hasEquipment = false;
            foreach (var slot in container.SlotsOrdered)
            {
                if (!slot.IsEmpty) { hasEquipment = true; break; }
            }
            if (!hasEquipment) return;

            ctx.OwnedGripTags.Clear();
            foreach (var entity in container.AllItems())
            {
                var tags = entity.Properties?.GetTagList(CharacterConst.PropertyPath.CommonTags);
                if (tags == null) continue;

                foreach (var tag in tags)
                {
                    if (!string.IsNullOrEmpty(tag))
                        ctx.OwnedGripTags.AddTag(tag);
                }
            }
        }
    }
}
