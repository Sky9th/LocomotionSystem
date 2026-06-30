using RedDust.Properties;

namespace RedDust.Ability
{
    /// <summary>
    /// ③ 资源消耗。双阶段不可分割：预检（全部可负担?）→ 扣除。
    /// 预检失败不扣任何资源，保证原子性。
    /// </summary>
    public class AbilityCost
    {
        /// <summary>
        /// 阶段一：预检。遍历 selfEffects 中的全部 CostEffectSO，确认资源充足。
        /// </summary>
        /// <param name="selfEffects">技能的 selfEffects 数组</param>
        /// <param name="peekStat">属性值查询回调。(def) → 当前值</param>
        /// <returns>null=全部可承担；非null=失败描述（首个不足的资源名+需求/持有）</returns>
        public string Peek(EffectSO[] selfEffects, System.Func<PropertyDefSO, float> peekStat)
        {
            if (selfEffects == null) return null;

            foreach (var effect in selfEffects)
            {
                if (effect is not CostEffectSO cost || cost.def == null) continue;

                if (peekStat == null) return "PeekStat callback is null";

                float current = peekStat(cost.def);
                if (current < cost.amount)
                    return $"insufficient {cost.def.Id} (need {cost.amount}, have {current:F1})";
            }

            return null;
        }

        /// <summary>
        /// 阶段二：扣除。预检通过后逐项执行。
        /// 调用方必须在 Peek 返回 null 之后才能调用此方法。
        /// </summary>
        /// <param name="selfEffects">技能的 selfEffects 数组</param>
        /// <param name="modifyStat">属性修改回调。(def, delta) → void。delta 为负值表示消耗。</param>
        public void Deduct(EffectSO[] selfEffects, System.Action<PropertyDefSO, float> modifyStat)
        {
            if (selfEffects == null) return;

            foreach (var effect in selfEffects)
            {
                if (effect is not CostEffectSO cost || cost.def == null) continue;
                modifyStat?.Invoke(cost.def, -cost.amount);
            }
        }
    }
}
