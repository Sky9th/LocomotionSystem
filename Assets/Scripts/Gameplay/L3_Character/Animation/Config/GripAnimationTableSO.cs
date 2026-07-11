using RedDust.Core;
using UnityEngine;

namespace RedDust.Character.Animation
{
    [System.Serializable]
    public struct GripAnimationEntry
    {
        [Tooltip("握持姿态标签（Grip.*），HasTagExact 精确匹配。必填。")]
        public RdTagDefSO gripTag;

        [Tooltip("武器类型标签（Entity.Weapon.*），HasTag 前缀匹配。null=不限武器。")]
        public RdTagDefSO weaponTypeTag;

        [Tooltip("默认（Relax）使用的 Locomotion 动画集")]
        public LocomotionAnimationSetSO animationSet;

        [Tooltip("战斗（Combat）状态下使用的动画集。可选，为 null 时回退到 animationSet")]
        public LocomotionAnimationSetSO combatSet;
    }

    /// <summary>
    /// 握持姿态 × 武器类型 → LocomotionAnimationSetSO 查表。
    /// 装备系统换武器时，根据 OwnedTags 中 Grip.* + Entity.Weapon.* 双维度匹配解析动画集。
    /// </summary>
    [CreateAssetMenu(
        fileName = "GripAnimationTableSO",
        menuName = "RedDust/Animation/Grip/Grip Animation Table")]
    public sealed class GripAnimationTableSO : ScriptableObject
    {
        [Tooltip("无 Tag 匹配时使用的动画集。必填。")]
        public LocomotionAnimationSetSO defaultSet;

        [Tooltip("Grip × WeaponType → 动画集映射。按数组顺序，首个命中即返回。")]
        public GripAnimationEntry[] entries;

        public LocomotionAnimationSetSO Resolve(RdTagContainer ownedTags, EBodyForm bodyForm)
        {
            bool inCombat = bodyForm == EBodyForm.Combat;

            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.gripTag == null || e.animationSet == null) continue;

                // Grip 精确匹配
                if (!ownedTags.HasTagExact(e.gripTag.FullTag)) continue;

                // WeaponType 前缀匹配（null=不限）
                if (e.weaponTypeTag != null && !ownedTags.HasTag(e.weaponTypeTag.FullTag)) continue;

                if (inCombat)
                {
                    if (e.combatSet != null)
                        return e.combatSet;
                }
                return e.animationSet;
            }
            return defaultSet;
        }
    }
}
