using System.Text;
using RedDust.Ability;
using UnityEngine;

namespace RedDust.UI
{
    /// <summary>
    /// 技能卡展示数据。从 ActiveAbilitySO 提取的纯展示层数据。
    /// 由调用方在 Update/事件中调用 ActiveAbilitySO.ToSkillCardData() 构造，传给 SkillCard.SetData()。
    /// </summary>
    public struct SkillCardData
    {
        // ── Identity ──
        public Sprite icon;
        public string displayName;
        public string description;

        // ── Cooldown ──
        public float cooldownDuration;

        // ── Activation ──
        public string activationTypeLabel;
        public string animationLayerLabel;
        public float windupDuration;
        public float fireWindowDuration;
        public float recoveryDuration;
        public float animationSpeed;
        public bool canCancelWindup;
        public bool canCancelRecovery;

        // ── Search ──
        public string searchTypeLabel;
        public float searchRange;

        // ── Effects (pre-formatted strings) ──
        /// <summary>伤害修正文本。格式: "Slash +50%", "Pierce +10 +20%"</summary>
        public string[] damageModifiers;
        /// <summary>硬直文本。格式: "硬直: Flinch  冲击值: 15  击退: 1"</summary>
        public string impactText;
        /// <summary>消耗文本。格式: "体力 -30", "弹药 -5"</summary>
        public string[] costs;
        /// <summary>Buff 文本。格式: "MaxHP +20% (永久)", "攻击力 +10 (5s)"</summary>
        public string[] buffs;

        // ── Combo ──
        /// <summary>连招文本。格式: "→ 刀·突刺 (窗口 0.4~0.9s)"</summary>
        public string[] comboLinks;

        // ── Noise ──
        public int noiseLevel;
        public float noiseDecayRadius;

        // ── Queries ──
        public bool HasEffects => (damageModifiers != null && damageModifiers.Length > 0)
                               || !string.IsNullOrEmpty(impactText)
                               || (costs != null && costs.Length > 0)
                               || (buffs != null && buffs.Length > 0);
        public bool HasCombo => comboLinks != null && comboLinks.Length > 0;
        public bool HasNoise => noiseLevel > 0;

        // ── Factory ──

        /// <summary>
        /// 从 ActiveAbilitySO 提取展示数据。
        /// 纯数据提取——不解析子资产引用（Icon、Activation、Search 等通过 .asset 直接读）。
        /// </summary>
        public static SkillCardData FromActiveAbility(ActiveAbilitySO def)
        {
            if (def == null)
                return default;

            var data = new SkillCardData
            {
                icon = def.icon,
                displayName = def.displayName ?? def.internalName,
                description = def.description,
                cooldownDuration = def.cooldownDuration,
            };

            // ── Activation ──
            if (def.activation != null)
            {
                var act = def.activation;
                data.activationTypeLabel = ActivationTypeLabel(act.activationType);
                data.animationLayerLabel = AnimationLayerLabel(act.animationLayer);
                data.windupDuration = act.windupDuration;
                data.fireWindowDuration = act.fireWindowDuration;
                data.recoveryDuration = act.recoveryDuration;
                data.animationSpeed = act.animationSpeed;
                data.canCancelWindup = act.canCancelWindup;
                data.canCancelRecovery = act.canCancelRecovery;
            }

            // ── Search ──
            if (def.search != null)
            {
                data.searchTypeLabel = SearchTypeLabel(def.search.searchType);
                data.searchRange = def.search.range;
            }

            // ── Effects ──
            ExtractEffects(def, ref data);

            // ── Combo ──
            if (def.comboLinks != null && def.comboLinks.Length > 0)
            {
                var combos = new string[def.comboLinks.Length];
                for (int i = 0; i < def.comboLinks.Length; i++)
                {
                    var link = def.comboLinks[i];
                    var nextName = link.NextSkill != null
                        ? (link.NextSkill.displayName ?? link.NextSkill.internalName)
                        : "???";
                    combos[i] = $"→ {nextName}\n  窗口 {link.WindowStart:F1}s ~ {link.WindowStart + link.WindowDuration:F1}s"
                              + (link.BypassCooldown ? " (跳过冷却)" : "");
                }
                data.comboLinks = combos;
            }

            // ── Noise ──
            if (def.noise != null)
            {
                data.noiseLevel = Mathf.RoundToInt(def.noise.level);
                data.noiseDecayRadius = def.noise.decayRadius;
            }

            return data;
        }

        // ── Label helpers ──

        private static string ActivationTypeLabel(EActivationType t) => t switch
        {
            EActivationType.Instant => "瞬发",
            EActivationType.Charged => "蓄力",
            EActivationType.Channel => "持续",
            EActivationType.Toggle => "开关",
            _ => "?",
        };

        private static string AnimationLayerLabel(EAbilityAnimationLayer layer) => layer switch
        {
            EAbilityAnimationLayer.FullBody => "全身",
            EAbilityAnimationLayer.UpperBody => "上半身",
            _ => "?",
        };

        private static string SearchTypeLabel(ESearchType t) => t switch
        {
            ESearchType.Cone => "扇形",
            ESearchType.RayLine => "直线",
            ESearchType.Circle => "圆形",
            _ => "?",
        };

        private static void ExtractEffects(ActiveAbilitySO def, ref SkillCardData data)
        {
            var allEffects = new EffectSO[0];
            if (def.targetEffects != null) allEffects = Concat(allEffects, def.targetEffects);
            if (def.selfEffects != null) allEffects = Concat(allEffects, def.selfEffects);

            var damageList = new System.Collections.Generic.List<string>();
            var costList = new System.Collections.Generic.List<string>();
            var buffList = new System.Collections.Generic.List<string>();
            string impactStr = null;

            foreach (var effect in allEffects)
            {
                if (effect == null) continue;

                switch (effect)
                {
                    case DamageModifierEffectSO dm:
                    {
                        var sb = new StringBuilder();
                        if (dm.targetTag != null) sb.Append(dm.targetTag.name);
                        else sb.Append("伤害");
                        if (dm.modPercent != 0) sb.Append($" {dm.modPercent:+0%;-0%}");
                        if (dm.modAdd != 0) sb.Append($" {dm.modAdd:+0.#;-0.#}");
                        damageList.Add(sb.ToString());
                        break;
                    }
                    case ImpactEffectSO impact:
                    {
                        impactStr = $"硬直: {impact.reactionLevel}  冲击: {impact.staggerValue}"
                                  + (impact.knockbackForce > 0
                                      ? $"  击退: {impact.knockbackForce} ({KnockbackLabel(impact.knockbackDir)})"
                                      : "");
                        break;
                    }
                    case CostEffectSO cost:
                    {
                        var label = cost.def != null ? cost.def.name : "属性";
                        costList.Add($"{label} {(cost.amount >= 0 ? $"-{cost.amount:F0}" : $"+{-cost.amount:F0}")}");
                        break;
                    }
                    case BuffEffectSO buff:
                    {
                        if (buff.adjuncts != null)
                        {
                            foreach (var adj in buff.adjuncts)
                            {
                                var propName = adj.property != null ? adj.property.name : "属性";
                                var parts = new System.Collections.Generic.List<string>();
                                if (adj.valueAdd != 0) parts.Add($"{adj.valueAdd:+0.#;-0.#}");
                                if (adj.valueMultiply != 1f) parts.Add($"×{adj.valueMultiply:F2}");
                                if (adj.maxAdd != 0) parts.Add($"上限{adj.maxAdd:+0.#;-0.#}");
                                if (adj.maxMultiply != 1f) parts.Add($"上限×{adj.maxMultiply:F2}");

                                if (parts.Count > 0)
                                    buffList.Add($"{propName} {string.Join(" ", parts)}");
                            }
                        }

                        if (buff.grantedTags != null && buff.grantedTags.Length > 0)
                        {
                            foreach (var tag in buff.grantedTags)
                            {
                                if (tag != null)
                                    buffList.Add($"标签: {tag.name}");
                            }
                        }

                        break;
                    }
                }
            }

            data.damageModifiers = damageList.Count > 0 ? damageList.ToArray() : null;
            data.impactText = impactStr;
            data.costs = costList.Count > 0 ? costList.ToArray() : null;
            data.buffs = buffList.Count > 0 ? buffList.ToArray() : null;
        }

        private static EffectSO[] Concat(EffectSO[] a, EffectSO[] b)
        {
            var result = new EffectSO[a.Length + b.Length];
            a.CopyTo(result, 0);
            b.CopyTo(result, a.Length);
            return result;
        }

        private static string KnockbackLabel(EKnockbackDirection dir) => dir switch
        {
            EKnockbackDirection.HitDirection => "命中方向",
            EKnockbackDirection.TowardCaster => "拉向施法者",
            _ => "?",
        };
    }
}
