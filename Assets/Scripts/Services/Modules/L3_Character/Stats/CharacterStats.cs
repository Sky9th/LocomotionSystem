using System.Collections.Generic;
using RedDust.Stats;

namespace RedDust.Character.Stats
{
    public class CharacterStats
    {
        private readonly Dictionary<string, StatInstance> stats = new();

        public IReadOnlyDictionary<string, StatInstance> All => stats;
        public Dictionary<string, (float current, float max)> LastStats { get; private set; }

        internal CharacterStats(StatsTreeSO tree)
        {
            if (tree == null) return;

            foreach (var instance in tree.Resolve())
                stats[instance.Path] = instance;

            LastStats = BuildSnapshot();
        }

        private Dictionary<string, (float current, float max)> BuildSnapshot()
        {
            var dict = new Dictionary<string, (float current, float max)>();
            foreach (var kv in stats)
                dict[kv.Key] = (kv.Value.Current, kv.Value.Def.Max);
            return dict;
        }

        internal void Update(float dt)
        {
            foreach (var kv in stats)
                kv.Value.ApplyRates(dt);

            LastStats = BuildSnapshot();
        }

        public StatInstance Get(string path) => stats.TryGetValue(path, out var s) ? s : null;
    }
}
