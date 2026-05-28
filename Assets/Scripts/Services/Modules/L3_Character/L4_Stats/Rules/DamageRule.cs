namespace RedDust.Character.Stats
{
    internal class DamageRule : BatchDamageRule
    {
        protected override string TargetPath() => "Vitals/HP";
    }
}
