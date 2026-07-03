# PlayerService · 玩家管理

> **Last Verified**: 2026-07-03 | **Verification**: BindInput 泛型绑定 + Entity.Command + GameContext.CameraSnapshot

> `L2_PlayerService/PlayerService.cs` — Player Prefab 的 Spawn/Despawn + 输入→命令翻译。继承 ModuleChildMono，实现 IGameplaySessionHandler。

## Layer Position

L2 — Player Service。管理玩家 Entity 生命周期，订阅全局输入事件，翻译为 Entity.Command 调用。

## Coupled Modules

| 方向 | 模块 | 关系 |
|------|------|------|
| ← | EventHub | 订阅全局输入事件、场景事件 |
| → | Entity.Command | 下达 MoveTo/UseActiveAbility/CycleEquip 命令 |
| → | CharacterActor.InputState | 每帧写入 SCharacterInputState |
| → | GameContext | 读取 SCameraSnapshot（TryGetMouseGround） |
| → | EntityService | 获取 _playerEntity 引用 |

## Key Changes (v0.35.0)

- **BindInput\<T\>** 泛型辅助，输入事件直接调 Command，消除 15 个独立 handler
- **TryGetMouseGround** 从 GameContext 读鼠标位置，不再订阅 CameraSnapshotEvent
- **SetPosture / ToggleSprint** 直接写 CharacterActor.InputState
- 帧标志缓存全部消除（10+ 变量 → 4 个持久状态）
- `_playerEntity` / `_playerActor` 缓存替代 `playerInstance.GetComponent<>()`

## Future Plans

| 计划 | 状态 | 来源 |
|------|------|------|
| CameraSnapshot 连续帧推送改造 | TODO | WriteInputState |
| SpawnTestEntities 迁移到装备系统 | TODO | 硬编码 |
