using Game.Character.Components;
using Game.Stats;

namespace Game.Character.Stats.Rules
{
    internal abstract class CharacterStatRule
    {
        internal abstract void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt);
    }
}
