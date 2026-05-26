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

## 后续修复

- **Anchor 旋转** — pivot 每帧旋转指向鼠标方向，箭头作为 Prefab 子对象跟随
- **转身退出条件** — 移除 `!wantsTurn` 提前退出，只在 `turnDone(≤5°)` 时退出

## 提交

`e199e3c` refactor: top-down camera and mouse-based heading
`a8a4565` fix: anchor rotation toward mouse and turning exit condition
