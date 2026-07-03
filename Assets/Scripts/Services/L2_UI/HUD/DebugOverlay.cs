using System.Text;
using RedDust.Ability;
using RedDust.Character;
using TMPro;
using UnityEngine;

namespace RedDust.UI
{
    /// <summary>
    /// 技能管道调试 Overlay。开发工具——显示管道状态、冷却计时、当前技能。
    ///
    /// 独立 Overlay，右上角固定，可随时开关不影响 gameplay HUD。
    /// 遵循 VitalsOverlay 模式：UIOverlay 子类 + refreshRate 轮询 + DeltaTime。
    /// </summary>
    public class DebugOverlay : UIOverlay
    {
        [Header("Pipeline State")]
        [SerializeField] private TMP_Text stateLabel;
        [SerializeField] private TMP_Text stateTimeLabel;
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
            // TODO: BuildContext 外部引用 — 同上，后续收敛到外部接口。
            var ctx = uiService.PlayerActor?.BuildContext;
            if (ctx == null) return;

            var ability = ctx.Ability;
            if (ability == null) return;

            var pipeline = ability.Pipeline;

            // ── Pipeline State ──
            if (stateLabel != null)
            {
                stateLabel.text = pipeline.IsIdle
                    ? "Idle"
                    : pipeline.CurrentState.ToString();
            }

            if (stateTimeLabel != null)
            {
                stateTimeLabel.text = pipeline.IsIdle
                    ? ""
                    : $"State Time: {pipeline.StateTime:F2}s";
            }

            if (currentAbilityLabel != null)
            {
                currentAbilityLabel.text = pipeline.IsIdle
                    ? "--"
                    : pipeline.Context.Ability?.internalName ?? "--";
            }

            // ── Cooldowns ──
            if (cooldownLabel != null)
            {
                _sb.Clear();
                var actives = ctx.AbilityForest?.ResolvedActives;
                if (actives != null)
                {
                    bool hasAny = false;
                    foreach (var a in actives)
                    {
                        if (a == null) continue;
                        var remaining = ability.GetAbilityCooldownRemaining(a);
                        _sb.AppendLine(remaining > 0f
                            ? $"{a.internalName}: {remaining:F1}s / {a.cooldownDuration:F1}s"
                            : $"{a.internalName}: Ready");
                        hasAny = true;
                    }
                    if (!hasAny)
                        _sb.AppendLine("No abilities resolved");
                }
                else
                {
                    _sb.AppendLine("No abilities resolved");
                }

                cooldownLabel.text = _sb.ToString();
            }
        }
    }
}
