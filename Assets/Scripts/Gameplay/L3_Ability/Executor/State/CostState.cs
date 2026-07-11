using System.Collections.Generic;
using RedDust.Gameplay.Properties;
using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// ④ 资源消耗。双阶段：预检（全部可负担?）→ 扣除。
    /// 正 amount=消耗（预检+扣除），负 amount=恢复（仅扣除，跳过预检）。
    ///
    /// 两条路径互斥（相位级排他，不混合）：
    ///   A. PropertyTable 存在 → 循环逐 Effect 内建查询/修改
    ///   B. PropertyTable 不存在 + PreviewCostCallback/ApplyCostCallback 有接线 → 整批 CostEffectSO[] 交给回调
    ///   C. 两条路都走不通 → RejectedState
    ///
    /// 预检失败 → RejectedState；通过 → WindupState。
    /// </summary>
    public class CostState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Cost;

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;

            if (a.selfEffects == null)
                return new WindupState();

            // 收集全部 CostEffectSO（正消耗 + 负恢复）
            var costs = new List<CostEffectSO>();
            foreach (var effect in a.selfEffects)
            {
                if (effect is CostEffectSO cost && cost.def != null)
                    costs.Add(cost);
            }

            if (costs.Count == 0)
                return new WindupState();

            var props = e.PropertyTable;

            if (props != null)
            {
                // ── 路径 A: PropertyTable 内建 ──
                if (!PeekViaTable(costs, props, a.internalName)) return new RejectedState();
                ModifyViaTable(costs, props);
            }
            else if (e.PreviewCostCallback != null || e.ApplyCostCallback != null)
            {
                // ── 路径 B: 回调相位级接管 ──
                var costArray = costs.ToArray();
                if (e.PreviewCostCallback != null)
                {
                    var reject = e.PreviewCostCallback(costArray);
                    if (reject != null)
                    {
                        Debug.LogWarning($"[Cost] Rejected by PreviewCostCallback: {a.internalName} — {reject}");
                        return new RejectedState();
                    }
                }
                e.ApplyCostCallback?.Invoke(costArray);
            }
            else
            {
                // ── 路径 C: 无路可走 ──
                Debug.LogError($"[Cost] No PropertyTable and no callbacks wired — rejected.");
                return new RejectedState();
            }

            return new WindupState();
        }

        /// <summary>PropertyTable 路径: Phase 1 逐 Effect 预检。正消耗检查余额，负/零跳过。</summary>
        private static bool PeekViaTable(List<CostEffectSO> costs, PropertyTable props, string abilityName)
        {
            foreach (var cost in costs)
            {
                if (cost.amount <= 0f) continue;

                if (!props.TryGetPath(cost.def, out var path))
                {
                    Debug.LogError($"[Cost] Property '{cost.def.Id}' not in PropertyTable — rejected.");
                    return false;
                }

                var current = props.GetFloat(path);
                if (current < cost.amount)
                {
                    Debug.LogWarning($"[Cost] Rejected: {abilityName} — insufficient {cost.def.Id} (need {cost.amount}, have {current:F1})");
                    return false;
                }
            }
            return true;
        }

        /// <summary>PropertyTable 路径: Phase 2 逐 Effect 扣除。</summary>
        private static void ModifyViaTable(List<CostEffectSO> costs, PropertyTable props)
        {
            foreach (var cost in costs)
            {
                if (props.TryGetPath(cost.def, out var path))
                    props.Modify(path, -cost.amount);
                else
                    Debug.LogWarning($"[Cost] Property '{cost.def.Id}' not in PropertyTable — skipped.");
            }
        }
    }
}
