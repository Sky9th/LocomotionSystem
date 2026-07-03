using System.Text;
using RedDust.Ability;
using TMPro;
using UnityEngine;

namespace RedDust.UI
{
    /// <summary>
    /// 技能管道调试 Overlay。右上角固定，开发工具。
    /// 纯 Entity.Query.Ability 读数据。
    /// </summary>
    public class DebugOverlay : UIOverlay
    {
        [Header("Pipeline State")]
        [SerializeField] private TMP_Text stateLabel;
        [SerializeField] private TMP_Text currentAbilityLabel;

        [Header("Cooldowns")]
        [SerializeField] private TMP_Text cooldownLabel;

        [Header("Refresh")]
        [SerializeField] private float refreshRate = 0.1f;

        private float _refreshTimer;
        private readonly StringBuilder _sb = new();

        private void Update()
        {
            _refreshTimer += DeltaTime;
            if (_refreshTimer < refreshRate) return;
            _refreshTimer = 0f;

            if (uiService == null) return;
            var entity = uiService.PlayerEntity;
            if (entity == null) return;

            var ability = entity.Query.Ability;
            if (ability == null) return;

            var actives = ability.ActiveAbilities;

            // ── Cooldowns ──
            if (cooldownLabel != null)
            {
                _sb.Clear();
                if (actives.Length > 0)
                {
                    foreach (var a in actives)
                    {
                        if (a == null) continue;
                        var remaining = ability.GetCooldownRemaining(a);
                        _sb.AppendLine(remaining > 0f
                            ? $"{a.internalName}: {remaining:F1}s / {a.cooldownDuration:F1}s"
                            : $"{a.internalName}: Ready");
                    }
                }
                else
                {
                    _sb.AppendLine("No abilities resolved");
                }
                cooldownLabel.text = _sb.ToString();
            }

            // ── Active Ability ──
            if (currentAbilityLabel != null)
            {
                string activeName = "--";
                foreach (var a in actives)
                {
                    if (a != null && ability.IsActive(a))
                    {
                        activeName = a.internalName;
                        break;
                    }
                }
                currentAbilityLabel.text = activeName;
            }

            if (stateLabel != null)
                stateLabel.text = actives.Length > 0 ? $"{actives.Length} abilities" : "Idle";
        }
    }
}
