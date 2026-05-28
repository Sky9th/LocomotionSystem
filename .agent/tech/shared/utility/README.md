# utility · 通用工具

> Gizmo 绘制辅助，提供编辑器调试可视化能力。

**源文件目录**: `Assets/Scripts/Shared/Utility/`

## 层级定位

全局 Helper — 不限层级。Utility 模块提供无状态的静态工具方法，不继承任何框架类型。

- **被所有模块消费**: GizmoDebugUtility 被任何模块的 OnDrawGizmos 回调调用。
- **无上层依赖**: 纯工具代码，仅依赖 UnityEditor API（条件编译）。
- **与 Logging 分离**: 旧 Logger（Utility/Logger.cs）已归入 Shared/Logging 管理，不在此重复。

## 调用链

```
GizmoDebugUtility:
  CharacterActor.Debug.cs                    → DrawArrowLine / DrawSphere / DrawWireBox
  其他模块的 OnDrawGizmos / OnDrawGizmosSelected   → 直接调用静态方法
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| GizmoDebugUtility | CharacterActor.Debug | 角色 Gizmo 调试绘制 |
| GizmoDebugUtility | 所有模块的 Gizmo 回调 | 公共绘制辅助 |

## 设计决策

| 决策 | 原因 |
|------|------|
| Gizmo 方法用 `internal` 而非 `public` | 仅限项目内部使用，不对外暴露 |
| Gizmo 方法加 `label` 可选参数 | 一次调用完成图形+文字，简化调用方代码 |
| 旧 Logger 归入 logging 模块 | 职责分离 — utility 只保留纯工具方法 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| GizmoDebugUtility 扩展 2D 形状 | 待做 | — | 代码分析 |
| GizmoDebugUtility 增加持久绘制支持 | 待做 | — | 代码分析 |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [gizmo-debug-utility.md](gizmo-debug-utility.md) | Gizmo 绘制辅助方法 |
