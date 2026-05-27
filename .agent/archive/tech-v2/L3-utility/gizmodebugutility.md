# GizmoDebugUtility · Gizmo 绘制辅助

> `Utility/GizmoDebugUtility.cs` — 将常见的 Gizmos + Handles 绘制模式封装为静态方法，降低调试代码重复

## 调用链

```
被谁调:
  CharacterActor.Debug.cs       → OnDrawGizmos() / OnDrawGizmosSelected()
  任何模块的 Gizmo 回调         → 直接调静态方法

调谁:
  DrawArrowLine / DrawWireBox / DrawSphere → Gizmos.DrawLine / DrawWireCube / DrawSphere
  (带 label 时)                               → Handles.Label()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 所有模块的 Gizmo 绘制 | 提供 Gizmo 封装，任何模块的 OnDrawGizmos 均可调用 |
| 依赖 | UnityEditor.Handles | 文字标签通过 Handles.Label 绘制 (仅 Editor) |

## 方法

### DrawArrowLine()
```csharp
public static void DrawArrowLine(Vector3 from, Vector3 to, Color color, string label = null)
```
- **用途**: 绘制带箭头的线段，箭头指向 to 方向
- **参数**:
  - `from` — 线段起点
  - `to` — 线段终点 (箭头位置)
  - `color` — 线条颜色
  - `label` — 可选，在 `(from + to) * 0.5` 处显示文字标签
- **调用者**: 需要可视化方向/向量的 Gizmo 绘制
- **备注**:
  - `from == to` 时跳过，避免除零错误
  - 箭头使用 `Quaternion.AngleAxis(20°, Vector3.up)` 绕 Y 轴旋转，适合俯视角
  - 箭头大小固定 0.15 单位

### DrawWireBox()
```csharp
public static void DrawWireBox(Vector3 center, Vector3 size, Color color, string label = null)
```
- **用途**: 绘制线框盒体
- **参数**:
  - `center` — 盒体中心位置
  - `size` — 盒体尺寸 (Vector3)
  - `color` — 线框颜色
  - `label` — 可选，在 center 处显示文字标签
- **调用者**: 需要可视化碰撞体/区域范围的 Gizmo 绘制
- **备注**: `size.sqrMagnitude <= Epsilon` 时跳过

### DrawSphere()
```csharp
public static void DrawSphere(Vector3 center, float radius, Color color, string label = null)
```
- **用途**: 绘制实心球体
- **参数**:
  - `center` — 球心位置
  - `radius` — 球体半径
  - `color` — 球体颜色
  - `label` — 可选，在 center 处显示文字标签
- **调用者**: 需要可视化检测范围/接触点的 Gizmo 绘制
- **备注**: `radius <= Epsilon` 时跳过

## 内部机制

### 条件编译
```csharp
#if UNITY_EDITOR
```
- 整个文件仅在 Editor 下编译

### 访问权限
```csharp
internal static class GizmoDebugUtility
```
- `internal` 可见性，限制在程序集内部使用

### 箭头方向
箭头三角使用 `Vector3.up` 作为旋转轴，适合俯视角 (Top-down) 游戏的场景。对于 3D 游戏可能需要改为任意轴向。

## 依赖的 Unity API

| API | 用途 |
|-----|------|
| `Gizmos.color` | 设置 Gizmo 颜色 |
| `Gizmos.DrawLine` | 线段绘制 |
| `Gizmos.DrawWireCube` | 线框盒体 |
| `Gizmos.DrawSphere` | 实心球体 |
| `Handles.Label` | 世界空间文字标签 (Editor-only) |
| `Handles.color` | Handle 颜色 |
