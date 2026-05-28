namespace Game.Character.Stats.Rules
{
    internal class DamageRule : BatchDamageRule
    {
        protected override string TargetPath() => "Vitals/HP";
    }
}
