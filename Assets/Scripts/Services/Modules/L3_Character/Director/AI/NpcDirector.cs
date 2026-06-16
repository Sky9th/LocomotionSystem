using RedDust.Core;
using RedDust.Character.Director;

namespace RedDust.Character.Director
{
    /// <summary>
    /// NPC 空对象导演。返回 Idle 意图，满足 pipeline 契约。
    /// TODO Phase 4: 替换为 AI 行为树导演。
    /// </summary>
    internal sealed class NpcDirector : Module, ICharacterDirector
    {
        internal NpcDirector(ModuleRegistry registry) : base(registry) { }
        public SCharacterIntent Evaluate() => SCharacterIntent.None;
    }
}
