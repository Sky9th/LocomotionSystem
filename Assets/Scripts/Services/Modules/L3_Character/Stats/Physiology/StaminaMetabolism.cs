using RedDust.Character;
using RedDust.Stats;

namespace RedDust.Character.Stats
{
    /// <summary>
    /// 耐力代谢——冲刺时消耗速率翻倍。
    /// </summary>
    // TODO: Demo 阶段确定冲刺体力倍率
    internal class StaminaMetabolism : StateDrivenModifier
    {
        internal StaminaMetabolism(object owner) : base(owner, new StatModifier
        {
            Apply = (s, ctx) => ctx.Multiplier = 3f
        }) { }

        protected override string StatPath() => "Vitals/Stamina";
        protected override bool ShouldActivate(CharacterFrameContext ctx)
            => ctx.Discrete.Gait == EMovementGait.Sprint;
    }
}
