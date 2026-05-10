using System.Collections.Generic;
using Game.Stats;

namespace Game.Character.Stats
{
    public class CharacterStats
    {
        private readonly Dictionary<string, StatInstance> stats = new();

        internal CharacterStats(StatsTreeSO tree)
        {
            if (tree == null) return;

            var resolved = tree.Resolve();
            foreach (var rs in resolved)
                stats[rs.Def.Id] = StatFactory.Create(rs);
        }

        public StatInstance Get(string id) => stats.TryGetValue(id, out var s) ? s : null;

        public void TickAll(float dt)
        {
            foreach (var kv in stats)
                kv.Value.Tick(dt);
        }
    }
}
