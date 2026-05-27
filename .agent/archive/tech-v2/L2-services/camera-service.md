# CameraService · 摄像机

> `Core/CameraService.cs` — Cinemachine 配置 + CameraPivot 跟随 + 鼠标地面坐标，继承 BaseService，实现 IGameplaySessionHandler

## 调用链

```
被谁调:
  GameService.Bootstrap()                    → Register()
  EventDispatcher                            → HandlePlayerSpawned (订阅 SPlayerSpawnedEvent)
  Unity Engine                               → Update() 每帧 (当 isFollowingPlayer)
  GameService.TeardownSession()              → OnGameplaySessionEnd()

调谁:
  GameContext                                → RegisterService(), TryGetSnapshot(SPlayer), PublishState(SCameraSnapshot)
  Cinemachine (CinemachineBrain, VirtualCamera) → Follow/LookAt 设置
  Camera (Unity)                             → main, ScreenPointToRay
  CommonConstants                            → FollowAnchorName
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext | 注册 + 读取 SPlayer + 发布 SCameraSnapshot |
| 依赖 | EventDispatcher | 订阅 PlayerSpawned 事件 |
| 依赖 | PlayerService | 等待 SPlayerSpawnedEvent 开始跟随 |
| 依赖 | GameProfile | 读取 cameraLookRotationSpeed |
| 被依赖 | 02-character, 外部系统 | 提供 SCameraSnapshot（鼠标地面坐标、Anchor 位姿） |

## 公开属性

```csharp
public Transform CameraPivot { get; }        // 摄像机跟随锚点（位置=玩家位置，朝向=鼠标方向）
```

## 方法

### OnRegister()
```csharp
protected override bool OnRegister(GameContext context)
```
- **用途**: 验证配置 → 确保 CinemachineBrain 存在 → 创建 CameraPivot → 初始化 VirtualCamera → 注册自身
- **返回**: CinemachineBrain 不存在时返回 false

### OnSubscriptionsActivated()
```csharp
protected override void OnSubscriptionsActivated()
```
- **用途**: 订阅 `SPlayerSpawnedEvent` → `HandlePlayerSpawned`

### Update()
```csharp
private void Update()
```
- **用途**: 每帧 `TickCameraPivot()`（仅在 `isFollowingPlayer` 时）

### HandlePlayerSpawned()
```csharp
private void HandlePlayerSpawned(SPlayerSpawnedEvent evt, MetaStruct meta)
```
- **用途**: 如果非本地玩家 → 忽略；设置 `isFollowingPlayer = true`

### CreateCameraPivot()
```csharp
private void CreateCameraPivot()
```
- **用途**: 创建 CameraPivot GameObject（从 anchorPrefab 或 new GameObject），命名 `FollowAnchor`

### TickCameraPivot()
```csharp
private void TickCameraPivot()
```
- **用途**: 每帧更新 Pivot 位置和朝向
- **流程**:
  1. 读取 `SPlayer` Snapshot → `pivotPos = player.Character.Position`
  2. `ComputeMouseGroundPosition()` → 鼠标屏幕坐标 → Raycast 与 Y=0 平面求交
  3. Pivot 朝向鼠标地面位置
  4. `PublishState(SCameraSnapshot)` — Camera 位姿 + Anchor 位姿 + 鼠标地面坐标
- **备注**: `cameraPivot.rotation` 按鼠标方向旋转，CharacterActor 的 heading 由这个旋转驱动

### ComputeMouseGroundPosition()
```csharp
private (Vector3 WorldPosition, bool IsValid) ComputeMouseGroundPosition()
```
- **用途**: 将鼠标屏幕坐标投影到 Y=0 的地面平面上
- **返回**: 地面交点 + 是否有效
- **实现**: `outputCamera.ScreenPointToRay(mousePos)` → `Plane(Vector3.up, 0).Raycast(ray, out distance)` → `ray.GetPoint(distance)`

### OnGameplaySessionEnd()
```csharp
public void OnGameplaySessionEnd()
```
- **用途**: `isFollowingPlayer = false` + 销毁 CameraPivot

### OnDestroy()
```csharp
private void OnDestroy()
```
- **用途**: 取消 `SPlayerSpawnedEvent` 订阅

## 内部机制

- `ValidateConfiguration()` — 检查 GameProfile 引用
- `EnsureCinemachineBrain()` — 自动从 Camera.main 或 FindObjectOfType 查找 CinemachineBrain
- `EnsureDefaultVirtualCamera()` — 自动从子 Component 查找 VirtualCamera
- `InitializeDefaultRig()` — 设置 VirtualCamera.Follow/LookAt = cameraPivot
- `FindCinemachineBrain()` — 查找策略：Camera.main → FindObjectOfType
- `DestroyCameraPivot()` — 销毁 Pivot GameObject

## SCameraSnapshot 字段

| 字段 | 类型 | 用途 |
|------|------|------|
| CameraPosition | Vector3 | 渲染摄像机世界位置 |
| CameraRotation | Quaternion | 渲染摄像机世界旋转 |
| AnchorPosition | Vector3 | 游戏 Anchor 位置（用于朝向计算） |
| AnchorRotation | Quaternion | 游戏 Anchor 旋转（LookAt 鼠标） |
| LookDelta | Vector2 | 本帧朝向变化量 (x=yaw, y=pitch) |
| MouseGroundPosition | Vector3 | 鼠标与地面(Y=0)交点 |
| IsMouseGroundValid | bool | 交点是否有效（射线是否命中平面） |

## 关键设计

`cameraPivot.rotation` 由鼠标地面位置驱动 — Pivot 始终 LookAt 鼠标。CharacterActor 读取这个旋转作为 heading，实现"角色面向鼠标"的俯视角控制。

## Cinemachine 配置

| 项目 | 配置 |
|------|------|
| Body | `CinemachineTransposer`, BindingMode = WorldSpace, Follow Offset = `(0, 15, -10)`, Damping = 0 |
| Aim | `CinemachineHardLookAt` |
| Follow / LookAt | cameraPivot（代码中动态设置） |

这些配置在 Scene 中的 VirtualCamera 上设置，非代码层面。

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| GameProfile.cameraLookRotationSpeed 实际接入 | 待做 | 当前 GameProfile 中定义了字段但代码未使用 |
| 摄像机碰撞检测 (Cinemachine Collider) | 待做 | 避免摄像机穿过墙壁 |
