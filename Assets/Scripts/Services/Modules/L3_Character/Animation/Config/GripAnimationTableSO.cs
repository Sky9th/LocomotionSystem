using RedDust.Core;
using UnityEngine;

namespace RedDust.Character.Animation
{
    [System.Serializable]
    public struct GripAnimationEntry
    {
        [Tooltip("握持姿态标签（Equip.Grip.*），精确匹配")]
        public GameplayTagDefinitionSO gripTag;

        [Tooltip("默认（Relax）使用的 Locomotion 动画集")]
        public LocomotionAnimationSetSO animationSet;

        [Tooltip("战斗（Combat）状态下使用的动画集。可选，为 null 时回退到 animationSet")]
        public LocomotionAnimationSetSO combatSet;
    }

    /// <summary>
    /// 握持姿态 → LocomotionAnimationSetSO 查表。
    /// 装备系统换武器时，根据 OwnedTags 中 Equip.Grip.* 标签解析对应动画集。
    /// </summary>
    [CreateAssetMenu(
        fileName = "GripAnimationTableSO",
        menuName = "RedDust/Animation/Grip/Grip Animation Table")]
    public sealed class GripAnimationTableSO : ScriptableObject
    {
        [Tooltip("无 Tag 匹配时使用的动画集。必填。")]
        public LocomotionAnimationSetSO defaultSet;

        [Tooltip("Grip Tag → 动画集映射。按数组顺序，首个精确命中即返回。")]
        public GripAnimationEntry[] entries;

        public LocomotionAnimationSetSO Resolve(GameplayTagContainer ownedTags, EBodyForm bodyForm)
        {
            bool inCombat = bodyForm == EBodyForm.Combat;

            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.gripTag != null
                    && e.animationSet != null
                    && ownedTags.HasTagExact(e.gripTag.FullTag))
                {
                    if (inCombat)
                    {
                        if (e.combatSet != null)
                        {
                            Debug.Log($"[GripTable] Matched grip={e.gripTag.FullTag} BodyForm=Combat → {e.combatSet.name}");
                            return e.combatSet;
                        }
                        Debug.Log($"[GripTable] Matched grip={e.gripTag.FullTag} BodyForm=Combat → combatSet is null, fallback");
                    }
                    return e.animationSet;
                }
            }
            return defaultSet;
        }
    }
}
