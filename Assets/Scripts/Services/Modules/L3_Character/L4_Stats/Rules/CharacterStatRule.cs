using RedDust.Character;
using RedDust.Stats;

namespace RedDust.Character.Stats
{
    internal abstract class CharacterStatRule
    {
        internal abstract void Apply(CharacterStats stats, CharacterFrameContext ctx, float dt);
    }
}
