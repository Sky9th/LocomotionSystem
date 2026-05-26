# 2026-05-25 俯视角切换

## 完成

- **SCameraContext → SCameraSnapshot** 改名
- **CharacterInputModule → CharacterEventReceiver** 改名，Camera 与 Input Actions 代码分离
- **Cinemachine 俯拍** — Body: Transposer(WorldSpace, offset 0/15/-10), Aim: HardLookAt — 代码不操作 Cinemachine 组件
- **CameraService 简化** — TickCameraPivot 只设 pivot.position = 玩家位置，Cinemachine 处理其余
- **Motor.ConvertToWorld** — 固定屏幕相对 `Vector3(local.x, 0f, local.y)`
- **Stance.EvaluateTurning** — 删除 lookStability 检测，直接用 TurnAngle 判断
- **LocomotionProfile** — 删除 lookStabilityAngle、lookStabilityDuration
- **CharacterKinematic.Evaluate** — viewForward → heading 参数
- **CharacterActor** — heading 来源 `(mouseWorldPos - position).XZ`
- **GameStateService** — Playing 光标 Confined + visible
- **鼠标地面坐标** — SCameraSnapshot.MouseGroundPosition / IsMouseGroundValid

## 已知问题

角色朝向鼠标不精准：TurnAngle 比较的是 bodyForward vs locomotionHeading（WASD 移动方向），而非 bodyForward vs mouseGround 方向。导致 WASD 和鼠标不同方向时转身逻辑错误。需要单独修。

## 提交

`e199e3c` refactor: top-down camera and mouse-based heading
