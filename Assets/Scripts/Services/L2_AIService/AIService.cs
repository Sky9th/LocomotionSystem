using RedDust.Core;
using UnityEngine;

namespace RedDust.AI
{
    /// <summary>
    /// AI Service — 集中处理所有 NPC 的行为驱动。
    ///
    /// MVP 存根。后续集成行为树：每帧评估 NPC → 产生 Commands → Entity.Command。
    /// 替代已删除的 NpcDirector（曾经返回 SCharacterIntent.None）。
    /// </summary>
    [DisallowMultipleComponent]
    public class AIService : ModuleChildMono
    {
        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            // MVP：无行为。所有 NPC 默认闲置站立。
            // Future：订阅 EntitySpawnedEvent，对 NPC Entity 启动行为树实例。
        }

        // Future API 示意：
        // public void RegisterNpc(Entity entity) { ... }
        // public void UnregisterNpc(string entityId) { ... }
    }
}
