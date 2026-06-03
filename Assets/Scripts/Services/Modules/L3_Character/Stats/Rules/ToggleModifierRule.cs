using RedDust.Character;
using RedDust.Stats;
using UnityEngine;

namespace RedDust.Character.Stats
{
    internal abstract class ToggleModifierRule : CharacterStatRule
    {
        private readonly StatModifier mod;
        private bool wasActive;

        protected ToggleModifierRule(object owner, StatModifier m)
        {
            mod = m;
            mod.Owner = owner;
        }

        protected abstract bool ShouldActivate(CharacterFrameContext ctx);
        protected abstract string StatPath();

        internal override void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt)
        {
            var s = stats.Get(StatPath());
            if (s == null) return;

            bool active = ShouldActivate(ctx);
            if (active && !wasActive)
            {
                s.AddModifier(mod);
                wasActive = true;
                Debug.Log($"[ToggleRule] {GetType().Name}: modifier ON at {StatPath()}");
            }
            if (!active && wasActive)
            {
                s.RemoveByOwner(mod.Owner);
                wasActive = false;
                Debug.Log($"[ToggleRule] {GetType().Name}: modifier OFF at {StatPath()}");
            }
        }
    }
}
