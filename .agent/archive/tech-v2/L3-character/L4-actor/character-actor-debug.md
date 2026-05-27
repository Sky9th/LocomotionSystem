# CharacterActor.Debug · 角色调试 Gizmo

> `Character/Components/CharacterActor.Debug.cs` — partial class，UNITY_EDITOR Gizmo 绘制

## 调用链

```
被谁调:
  Unity Editor → OnDrawGizmoSelected()   ← 选中 GameObject 时
  OnDrawGizmoSelected → DrawTextLabel / DrawHeading / DrawBodyForward /
                         DrawVelocity / DrawGround / DrawObstacle

调谁:
  GizmoDebugUtility.DrawArrowLine / DrawSphere   ← 工具方法
  Handles.Label                                   ← Unity Editor GUI
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GizmoDebugUtility | Gizmo 绘制封装 (DrawArrowLine/DrawSphere) |
| 依赖 | CharacterProfile | 读取探针/障碍参数 |
| 依赖 | SCharacterMotor | 显示速度/转向 |
| 依赖 | SCharacterDiscrete | 显示 Phase/Gait/Posture |
| 依赖 | SCharacterKinematic | 显示地面接触/障碍 |

## 公开属性

```csharp
[SerializeField] private bool drawDebugGizmos = true;     // Gizmo 总开关
[SerializeField, Min(0.1f)] private float debugArrowLength = 2f;  // 箭头长度
```

## 方法

### OnDrawGizmoSelected()
```csharp
private void OnDrawGizmoSelected()
```
- **用途**: 选中时绘制调试可视化 — 朝向/速度/地面探针/障碍检测
- **调用者**: Unity Editor
- **备注**: 仅在 UNITY_EDITOR 中编译；LastKinematic.Position 为默认值时跳过绘制

### DrawTextLabel()
```csharp
private static void DrawTextLabel(Vector3 pos, SCharacterDiscrete disc, SCharacterMotor mot)
```
- **用途**: 角色头顶文字 — "Phase | Gait | Posture | Turn | speed m/s"
- **调用者**: OnDrawGizmoSelected

### DrawHeading()
```csharp
private void DrawHeading(Vector3 pos, Vector3 heading, ELocomotionPhase phase)
```
- **用途**: 绘制运动朝向箭头，颜色随 Phase 变化（GroundedMoving=绿, Airborne=黄, 其他=青）
- **调用者**: OnDrawGizmoSelected

### DrawBodyForward()
```csharp
private void DrawBodyForward(Vector3 pos, Vector3 bodyForward)
```
- **用途**: 绘制身体朝向蓝色箭头（长度为 debugArrowLength 的 70%）
- **调用者**: OnDrawGizmoSelected

### DrawVelocity()
```csharp
private static void DrawVelocity(Vector3 pos, Vector3 velocity)
```
- **用途**: 绘制当前速度白色箭头
- **调用者**: OnDrawGizmoSelected
- **备注**: 速度太小 (<0.01) 时跳过

### DrawGround()
```csharp
private void DrawGround(Vector3 pos, SCharacterKinematic kin)
```
- **用途**: 绘制地面探针（蓝色线 + 黄色半透明球）+ 绿色接触点
- **调用者**: OnDrawGizmoSelected

### DrawObstacle()
```csharp
private void DrawObstacle(Vector3 pos, SCharacterKinematic kin)
```
- **用途**: 绘制障碍检测结果：命中点(品红)、法线(红)、顶部高度探测(白)、可攀爬高度标注
- **调用者**: OnDrawGizmoSelected

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 添加 Animation State 文字显示 | 待做 | 调试需求 |
| 添加 Stats 数值显示 | 待做 | 代码 TODO |
