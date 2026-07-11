using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 技能定义抽象基类。ActiveAbilitySO（主动）和 PassiveAbilitySO（被动）的公共根。
    /// 提取共享的 Identity、Effects、Cooldown，SDamageInfo / HitReactionComponent 等
    /// 消费侧使用此类型，不区分主动被动。
    /// </summary>
    public abstract class AbilitySO : ScriptableObject
    {
        [Header("Identity")]
        public string internalName;
        public string displayName;
        public Sprite icon;
        [TextArea(2, 4)]
        public string description;

        [Tooltip("技能标签。激活时施加(冷却>0)，冷却结束移除。层级决定互斥粒度。必须是叶标签(无子节点)。")]
        public RdTagDefSO abilityTag;

        [Header("Effects")]
        [Tooltip("施加给目标的效果。")]
        public EffectSO[] targetEffects;

        [Tooltip("激活时对持有者自己的效果。")]
        public EffectSO[] selfEffects;

        [Header("Cooldown")]
        [Tooltip("冷却时长（秒）。0=无冷却。")]
        public float cooldownDuration;

        [Tooltip("联动冷却标签。与这些标签的其他技能共享冷却。")]
        public RdTagDefSO[] sharedCooldownTags;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (abilityTag == null) return;

            // 检查是否有任何 Tag 以此标签为 parent（即非叶标签）
            var allTags = UnityEditor.AssetDatabase.FindAssets("t:RdTagDefSO");
            foreach (var guid in allTags)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var tag = UnityEditor.AssetDatabase.LoadAssetAtPath<RdTagDefSO>(path);
                if (tag != null && tag.Parent == abilityTag)
                {
                    Debug.LogError($"[AbilitySO] {name}: abilityTag '{abilityTag.FullTag}' 有子标签 '{tag.FullTag}'，必须是叶标签！");
                    abilityTag = null;
                    UnityEditor.EditorUtility.SetDirty(this);
                    return;
                }
            }
        }
#endif
    }
}
