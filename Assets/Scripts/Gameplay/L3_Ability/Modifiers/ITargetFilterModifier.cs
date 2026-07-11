using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 目标过滤修改器。④ Search 之后、⑤ 效果构造之前执行。
    /// 短路模式：第一个返回非 null 的修改器生效，该目标被跳过。
    /// 与 IConditionModifier 的区别：Condition 检查 Caster（"我能放吗？"），
    /// TargetFilter 检查 Target（"该对他生效吗？"）。
    /// </summary>
    public interface ITargetFilterModifier
    {
        int Priority { get; }

        /// <summary>
        /// 过滤目标。
        /// </summary>
        /// <param name="caster">技能释放者</param>
        /// <param name="target">待检查的目标</param>
        /// <returns>null 表示目标有效；非 null 字符串为忽略原因</returns>
        string Filter(GameObject caster, GameObject target);
    }
}
