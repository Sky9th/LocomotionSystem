# PlayerService · 玩家管理

> `Core/PlayerService.cs` — Player Prefab 的 Spawn/Despawn + 位置追踪，继承 BaseService，实现 IGameplaySessionHandler

## 调用链

```
被谁调:
  GameService.Bootstrap()                    → Register()
  EventDispatcher                            → HandleSceneLoadComplete (订阅 SSceneLoadComplete)
  Unity Engine                               → Update() 每帧
  GameService.TeardownSession()              → OnGameplaySessionEnd()

调谁:
  GameContext                                → RegisterService(), UpdateSnapshot(SPlayer)
  Dispatcher                                 → Publish(SPlayerSpawnedEvent)
  CharacterActor (02-character)              → GetComponent<>(), 读取 LastStats
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext | 注册 + 每帧更新 SPlayer Snapshot |
| 依赖 | EventDispatcher | 订阅场景加载完成 + 发布 Spawn 事件 |
| 依赖 | SceneService | 接收 SSceneLoadComplete |
| 依赖 | 02-character (CharacterActor) | 实例化 Player Prefab，持有 CharacterActor 引用 |
| 被依赖 | CameraService | 提供玩家位置（通过 SPlayer Snapshot） |
| 被依赖 | GameService | Teardown 时调用 OnGameplaySessionEnd |

## 公开属性

```csharp
public Transform CurrentPlayerTransform { get; }   // 玩家 Transform（可能 null）
public CharacterActor CurrentPlayerActor { get; }  // 玩家 CharacterActor 组件
```

## 方法

### OnRegister()
```csharp
protected override bool OnRegister(GameContext context)
```
- **用途**: `context.RegisterService(this)`

### OnSubscriptionsActivated()
```csharp
protected override void OnSubscriptionsActivated()
```
- **用途**: 订阅 `SSceneLoadComplete`

### Update()
```csharp
private void Update()
```
- **用途**: 每帧更新 `SPlayer` Snapshot（`FromTransform(playerInstance.transform)`）
- **备注**: playerInstance 为 null 时跳过

### HandleSceneLoadComplete()
```csharp
private void HandleSceneLoadComplete(SSceneLoadComplete evt, MetaStruct meta)
```
- **用途**: 非 Core 场景加载完成时 → `CreatePlayer()`

### CreatePlayer()
```csharp
private void CreatePlayer()
```
- **用途**: 实例化 Player Prefab
- **流程**:
  1. 如果 `playerStartAnchor` 为空 → `GameObject.Find("PlayerStart")`
  2. 如果 `playerPrefab` 为空 → LogError
  3. `Instantiate(playerPrefab)` → 设置位置 = PlayerStart 位置/朝向
  4. 获取 `CharacterActor` 组件
  5. `PublishState(SPlayer)` + `Publish(SPlayerSpawnedEvent)`
- **备注**: Player 作为 GameService 的子 Transform 生成

### OnGameplaySessionEnd()
```csharp
public void OnGameplaySessionEnd()
```
- **用途**: 会话结束时清空引用 + Destroy Player 实例
- **调用者**: `GameService.TeardownSession()`

### TryGetPlayerStats()
```csharp
public bool TryGetPlayerStats(out Dictionary<string, (float current, float max)> stats)
```
- **用途**: 获取玩家当前 Stats 快照
- **返回**: `_currentPlayerActor.LastStats`（可能 null）
- **备注**: 代码中有 TODO 标记 — LastStats 是临时方案，应改为正式的 push/pull 接口

### OnDestroy()
```csharp
private void OnDestroy()
```
- **用途**: 取消 `SSceneLoadComplete` 订阅

## Player Spawn 时序

```
SceneService 发布 SSceneLoadComplete
  → PlayerService.CreatePlayer()
    → Instantiate(playerPrefab)
    → PublishState(SPlayer)
    → Publish(SPlayerSpawnedEvent)
      → CameraService 监听 → isFollowingPlayer = true
      → 其他系统可响应 Spawn 初始化
```

## 使用规则

- **Player Prefab 必须通过 PlayerService 创建** — 外部不允许直接 Instantiate
- **Player 实例 parent 到 GameService** — 确保 DontDestroyOnLoad，场景切换不销毁

## 已知问题

`_currentPlayerActor.LastStats` 是 stopgap 方案。PlayerService 不应直接读 CharacterActor 的内部字段，应通过正式的 Stats 查询接口获取。

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Player Stats 正式 push/pull 接口 | 待做 | 代码 TODO — 替换 LastStats stopgap |
| 多人支持 — 管理多个 Player 实例 | 远期 | 当前只支持单本地玩家 |
| PlayerStart 查找改为配置引用 | 待做 | 当前用 `GameObject.Find("PlayerStart")` |
