# 俯视角摄像机

日期: 2026-05-25

## Cinemachine 配置

| 项目 | 配置 |
|------|------|
| Body | `CinemachineTransposer`，BindingMode = WorldSpace，Follow Offset = `(0, 15, -10)`，Damping = 0 |
| Aim | `CinemachineHardLookAt` |
| Follow / LookAt | cameraPivot（代码设置） |

代码 `TickCameraPivot`:
- 设 `cameraPivot.position = 玩家位置`
- 设 `cameraPivot.rotation = 指向鼠标（Y轴旋转）`
- Anchor Prefab（含 DebugArrow 子对象）在 Inspector 配置，`CreateCameraPivot` 实例化

其余由 Cinemachine 处理。

## 数据流

```
PlayerService.Update → GameContext.UpdateSnapshot(SPlayer)
CameraService.TickCameraPivot → GameContext.TryGetSnapshot<SPlayer>() → pivot.position
CameraService.PublishState(SCameraSnapshot) → 含相机位置、朝向、鼠标地面坐标
```

## 鼠标地面位置

`ComputeMouseGroundPosition()` — `Plane(Vector3.up, 0).Raycast(ScreenPointToRay(mousePosition))` → 数学运算，无物理开销。结果写入 `SCameraSnapshot`。
