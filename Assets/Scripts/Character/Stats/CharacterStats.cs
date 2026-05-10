using System.Collections.Generic;
using Game.Stats;

namespace Game.Character.Stats
{
    public class CharacterStats
    {
        private readonly Dictionary<string, StatInstance> stats = new();

        public IReadOnlyDictionary<string, StatInstance> All => stats;

        internal CharacterStats(StatsTreeSO tree)
        {
            if (tree == null) return;

            foreach (var instance in tree.Resolve())
                stats[instance.Path] = instance;
        }

        public StatInstance Get(string path) => stats.TryGetValue(path, out var s) ? s : null;

        public void TickAll(float dt)
        {
            foreach (var kv in stats)
                kv.Value.Tick(dt);
        }

    }
}
