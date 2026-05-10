namespace Game.Stats
{
    public struct ResolvedStat
    {
        public StatDefSO Def;
        public float OverrideDefault;
        public StatBehaviorSO[] EffectiveBehaviors;

        public bool HasOverride => OverrideDefault >= 0f;
    }
}
