# Director Layer & HeadLook Fix

**日期**: 2026-05-30 | **分支**: feature/astar-pathfinding | **版本**: v0.3.0

## 目标

1. 修复 HeadLook —— 角色头部跟随鼠标方向
2. 抽象 Director 层 —— 将玩家输入意图从 Character 层解耦，为 AI 行为树做准备

## 核心架构

新增 L4_Director 子模块：

```
L3_Character/L4_Director/
  ├── ICharacterDirector.cs          ← 接口：SCharacterIntent Evaluate()
  ├── SCharacterIntent.cs            ← 意图 struct：LocomotionHeading, AimDirection, DesiredGait, DesiredPosture, JumpRequested
  ├── Player/
  │   ├── PlayerDirector.cs          ← 玩家 Director：缓存→意图翻译
  │   └── PlayerInputReceiver.cs     ← 事件订阅纯通道
  └── AI/
      └── .gitkeep                   ← 占位
```

核心原则：CharacterActor 不关心输入来源，只消费 SCharacterIntent。

## 改动

### HeadLook 修复
- CharacterKinematic.Evaluate() 签名分离 `locomotionHeading` 和 `aimDirection`
- `aimDirection` 传入 CharacterHeadLook.Evaluate() 替换原来的 `actorTransform.forward`

### 玩家输入迁移
- 删除 CharacterEventReceiver.cs、SCharacterInputActions.cs、Input/ 目录
- PlayerInputReceiver 独立订阅 Dispatcher，缓存原始事件
- PlayerDirector 从缓存翻译为 SCharacterIntent
- CharacterActor 接入 ICharacterDirector，heading 计算逻辑移除

### Locomotion 重构
- Motor/Stance 从 SCharacterInputActions 切换到 SCharacterIntent
- Stance 的 gait/posture toggle 状态机迁移到 PlayerDirector
- ILocomotionSimulator.Simulate() 接收 SCharacterIntent 参数

### 场景加载修复
- SceneService.LoadContentScene() 加幂等检查
- 新增 SReloadSceneRequest 事件

## 已知问题

- WASD 移动未启用（Phase 4），locomotionHeading 暂 fallback 到 aimDirection
- 旧 CharacterEventReceiver 的 PrimaryInteract/SecondaryInteract 功能未迁移
