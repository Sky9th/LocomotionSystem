# AIService · AI 服务

> **Last Verified**: 2026-07-03 | **Verification**: New stub module

> `L2_AIService/AIService.cs` — NPC 行为驱动中心。MVP 存根，后续集成行为树。

## Layer Position

L2 — AI Service。集中管理所有 NPC 的行为评估，每帧产生 Commands → Entity.Command。

## Future Plans

| 计划 | 状态 |
|------|------|
| 行为树集成 | 待实现 |
| 订阅 EntitySpawnedEvent 跟踪 NPC | 待实现 |
| 每帧评估 NPC → 产生 Commands | 待实现 |
