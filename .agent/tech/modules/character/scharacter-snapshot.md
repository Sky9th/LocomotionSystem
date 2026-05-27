# 数据结构

## SCharacterSnapshot — 已删除 (2026-05-25)

原 `SCharacterSnapshot` 打包 Input + Kinematic + Motor + Discrete + Stats，供 Animation 管线和 GameContext 消费。

现已删除。Animation 管线直接消费 `CharacterFrameContext`：

```
CharacterFrameContext (唯一数据载体)

├── Input        SCharacterInputActions
├── Kinematic    SCharacterKinematic
├── Motor        SCharacterMotor
└── Discrete     SCharacterDiscrete
```

`ctx` 由 CharacterActor.Update() 逐级填充后直传 `AnimationBrain.Apply(in ctx)`。

外部需要玩家数据时通过 PlayerService 获取：
- 位置 → SPlayer (GameContext)
- Stats → PlayerService.TryGetPlayerStats()
