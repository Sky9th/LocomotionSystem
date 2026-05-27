# 2026-05-25 数据流架构重构

## 改动背景

讨论俯视角切换时发现多个架构问题，集中解决。

## 决策与改动

### 1. PublishState — Service 统一写入入口

`BaseService.PublishState<T>()` 同时写 GameContext + Dispatcher，消除分别调用导致的数据不一致。

```csharp
protected void PublishState<TSnapshot>(TSnapshot snapshot) where TSnapshot : struct
{
    GameContext?.UpdateSnapshot(snapshot);
    Dispatcher?.Publish(snapshot);
}
```

### 2. GameContext 职责划定

- **全局单例状态** → GameContext — SGameState、SCameraContext、SSceneTransition、SPlayer
- **个体实体数据** → Component public 属性 → GetComponent<T>() 读取
- **Component 禁止直接调用** `GameContext.Instance.UpdateSnapshot()`

### 3. Service 间通信规则

- Service 间通过 Dispatcher（push）或 GameContext（pull）通信
- **禁止** Service 直接持有其他 Service 引用（如 `private PlayerService _playerService`）
- Service 可持有自己创建的 GameObject/内部对象引用

### 4. PlayerService 作为玩家数据权威

- 持有 player GameObject，收集 CharacterActor 生成的数据
- 每帧 Update 刷新 `SPlayer` 到 GameContext
- 提供 `CurrentPlayerActor`、`TryGetPlayerStats()` 给其他 Service

### 5. 删除 SCharacterSnapshot

- CharacterActor 不再打包 SCharacterSnapshot，直传 `CharacterFrameContext` 给 Animation 管线
- Animation 管线全部改用 `CharacterFrameContext`
- `SLocomotionState` 同时删除

## 改后数据流

```
CharacterActor.Update()
  → ctx (CharacterFrameContext: Input, Kinematic, Motor, Discrete)
  → AnimationBrain.Apply(ctx)

CameraService
  → GameContext.TryGetSnapshot<SPlayer>() → character.Position

VitalsOverlay → UIService → PlayerService → CharacterActor.LastStats

PlayerService (Update)
  → GameContext.UpdateSnapshot(SPlayer.FromTransform(...))
```

## 涉及文件

12 文件修改，2 文件删除。详见 git diff。
