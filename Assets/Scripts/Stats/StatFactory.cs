namespace Game.Stats
{
    public static class StatFactory
    {
        public static StatInstance Create(ResolvedStat rs)
        {
            var instance = new StatInstance(rs.Def, rs.OverrideDefault, rs.EffectiveBehaviors);
            return instance;
        }
    }
}
