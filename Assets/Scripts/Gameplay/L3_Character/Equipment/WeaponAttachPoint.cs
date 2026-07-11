using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Gameplay.Character
{
    /// <summary>
    /// 武器附着点 — Soft Wave。
    ///
    /// 模型 pivot 和骨骼位置都准确，但骨骼中心点到握持面存在微小间隙。
    /// 此类按 (slotKey, gripTag) 提供统一的 local-space 偏移，
    /// 在骨骼下创建 _EquipSocket_{slotKey} 子对象承载 offset，
    /// 武器作为 Socket 子对象挂载。
    /// 同一种武器类型共享同一个 offset，无需逐武器或逐模型调整。
    ///
    /// 纯 C# 硬编码 — 很长时间内不需要 SO 的资产灵活性。
    /// </summary>
    internal static class WeaponAttachPoint
    {
        /// <summary>(slotKey, gripTag) → (pos, rot Euler)</summary>
        private static readonly Dictionary<(string, string), (Vector3 pos, Vector3 rot)> _table = new()
        {
            // RightHand — 单手剑
            { (CharacterConst.Slot.RightHand, CharacterConst.GripTag.OneHanded),
                (new Vector3(0.0865f, 0.0455f, -0.0335f),
                 new Vector3(353.8777f, 175.3269f, 273.2053f)) },

            // RightHand — 手枪
            { (CharacterConst.Slot.RightHand, CharacterConst.GripTag.Pistol2H),
                (new Vector3(0.0880f, 0.0245f, -0.0589f),
                 new Vector3(12.4864f, 75.1338f, 251.1423f)) },
        };

        /// <summary>
        /// 获取或创建骨骼下的装备挂点 Socket，并应用握持偏移。
        /// 武器应作为此 Socket 的子对象 Instantiate。
        /// </summary>
        public static Transform GetOrCreateSocket(Transform bone, string slotKey, string[] entityTags)
        {
            var socketName = $"_EquipSocket_{slotKey}";
            var socket = bone.Find(socketName);

            if (socket == null)
            {
                var go = new GameObject(socketName);
                socket = go.transform;
                socket.SetParent(bone, worldPositionStays: false);
            }

            var (pos, rot) = Resolve(slotKey, entityTags);
            socket.SetLocalPositionAndRotation(pos, Quaternion.Euler(rot));

            return socket;
        }

        /// <summary>
        /// 解析 (slotKey, entityTags) → (localPosition, localEuler) 偏移。
        /// 匹配优先级: (slot, tag) 精确 → (slot, null) 默认 → zero。
        /// </summary>
        private static (Vector3 pos, Vector3 rot) Resolve(string slotKey, string[] entityTags)
        {
            if (entityTags == null || entityTags.Length == 0)
                return (Vector3.zero, Vector3.zero);

            foreach (var tag in entityTags)
            {
                if (_table.TryGetValue((slotKey, tag), out var result))
                    return result;
            }

            if (_table.TryGetValue((slotKey, null), out var fallback))
                return fallback;

            return (Vector3.zero, Vector3.zero);
        }
    }
}
