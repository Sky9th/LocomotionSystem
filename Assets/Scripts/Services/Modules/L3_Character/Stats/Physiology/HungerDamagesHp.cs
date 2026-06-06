namespace RedDust.Character.Stats
{
    /// <summary>
    /// 饥饿伤害——饥饿归零时持续扣血。
    /// </summary>
    // TODO: Demo 阶段确定归零伤害值
    internal class HungerDamagesHp : ChainDepletion
    {
        protected override string SourcePath() => "Vitals/Hunger";
        protected override string TargetPath() => "Vitals/HP";
        protected override float DamagePerSec() => 5f;
    }
}
