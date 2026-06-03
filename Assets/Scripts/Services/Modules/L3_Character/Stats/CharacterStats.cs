using System.Collections.Generic;
using RedDust.Character;
using RedDust.Character.Stats;
using RedDust.Stats;

namespace RedDust.Character.Stats
{
    public class CharacterStats
    {
        private readonly Dictionary<string, StatInstance> stats = new();
        private readonly List<CharacterStatRule> rules = new();

        public IReadOnlyDictionary<string, StatInstance> All => stats;
        public Dictionary<string, (float current, float max)> LastStats { get; private set; }
        internal DamageRule DamageRule { get; private set; }

        internal CharacterStats(StatsTreeSO tree)
        {
            if (tree == null) return;

            foreach (var instance in tree.Resolve())
                stats[instance.Path] = instance;

            // TODO: Demo 阶段确定具体数值和生效条件
            rules.Add(new SprintStaminaRule(this));
            rules.Add(new HungerDepleteRule());
            rules.Add(DamageRule = new DamageRule());
        }

        public StatInstance Get(string path) => stats.TryGetValue(path, out var s) ? s : null;

        internal void Update(CharacterFrameContext ctx, float dt)
        {
            foreach (var r in rules)
                r.Apply(this, ctx, dt);

            foreach (var kv in stats)
                kv.Value.Tick(dt);

            var dict = new Dictionary<string, (float current, float max)>();
            foreach (var kv in stats)
                dict[kv.Key] = (kv.Value.Current, kv.Value.Def.Max);
            LastStats = dict;
        }
    }
}
