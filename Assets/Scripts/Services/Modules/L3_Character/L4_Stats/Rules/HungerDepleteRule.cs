namespace Game.Character.Stats.Rules
{
    // TODO: Demo 阶段确定归零伤害值
    internal class HungerDepleteRule : DepleteChainRule
    {
        protected override string SourcePath() => "Vitals/Hunger";
        protected override string TargetPath() => "Vitals/HP";
        protected override float DamagePerSec() => 5f;
    }
}
