using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 武器 → 技能组映射。4 槽对应 Q/E/R/F。
    /// 每把武器持有此资产引用，CombatComponent.SkillBar 据此初始化。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Combat/Weapon Skill Set", fileName = "WeaponSkillSet_")]
    public sealed class WeaponSkillSetSO : ScriptableObject
    {
        [Tooltip("技能数组。Index 0=Q, 1=E, 2=R, 3=F。未配置的槽位留 null。")]
        public SkillDefSO[] skills = new SkillDefSO[4];
    }
}
