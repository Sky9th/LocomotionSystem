using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Gameplay.Character
{
    /// <summary>
    /// SlotId → HumanBodyBones 映射。
    /// 人形 Avatar 通过 Animator.GetBoneTransform 统一抽象，
    /// 不管底层骨骼命名差异，对人形怪通用。
    /// 非 humanoid rig 返回 null。
    /// </summary>
    internal static class SlotBoneMapper
    {
        private static readonly Dictionary<string, HumanBodyBones> _map = new()
        {
            { CharacterConst.Slot.RightHand, HumanBodyBones.RightHand },
            { CharacterConst.Slot.LeftHand,  HumanBodyBones.LeftHand },
            { CharacterConst.Slot.Head,      HumanBodyBones.Head },
            { CharacterConst.Slot.Chest,     HumanBodyBones.Chest },
            { CharacterConst.Slot.RightLeg,  HumanBodyBones.RightUpperLeg },
            { CharacterConst.Slot.LeftLeg,   HumanBodyBones.LeftUpperLeg },
            { CharacterConst.Slot.RightFoot, HumanBodyBones.RightFoot },
            { CharacterConst.Slot.LeftFoot,  HumanBodyBones.LeftFoot },
        };

        /// <summary>
        /// 返回指定槽位对应的骨骼 Transform。
        /// 非 humanoid animator 或未映射的 slotId → null。
        /// </summary>
        public static Transform GetBoneForSlot(Animator animator, string slotId)
        {
            if (animator == null || !animator.isHuman) return null;
            if (!_map.TryGetValue(slotId, out var bone)) return null;
            return animator.GetBoneTransform(bone);
        }

        /// <summary>检查 slotId 是否在映射表中。</summary>
        public static bool HasMapping(string slotId)
        {
            return _map.ContainsKey(slotId);
        }
    }
}
