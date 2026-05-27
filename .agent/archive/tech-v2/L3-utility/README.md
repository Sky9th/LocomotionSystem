# 11-utility · 通用工具

> Gizmo 绘制辅助和旧版 Logger，提供编辑器调试可视化和日志格式化能力。

## 架构

```
Utility/
├── GizmoDebugUtility.cs           # Gizmo 绘制辅助 — 箭头/线框/球体 + 文字标签
├── Logger.cs                      # 旧 Logger (待废弃) — 智能序列化 + 格式化日志
└── Logging/                       # → 见 08-logging (新日志系统)
```

## 调用链

```
GizmoDebugUtility:
  CharacterActor.Debug.cs                    → DrawArrowLine / DrawSphere / DrawWireBox
  其他模块的 OnDrawGizmos / OnDrawGizmosSelected   → 直接调用静态方法

Logger (旧):
  ConsoleAppender.Append()                   → Logger.Log / LogWarning / LogError
  旧代码中残留的 Logger.Log() 调用               ← 逐渐迁移到 Logging 系统
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| GizmoDebugUtility | 02-character (CharacterActor.Debug) | 角色 Gizmo 调试绘制 |
| GizmoDebugUtility | 所有模块的 Gizmo 回调 | 公共绘制辅助 |
| Logger (旧) | 08-logging (ConsoleAppender) | ConsoleAppender 将格式化委托给旧 Logger |
| Logger (旧) | 01-core (GameContext) | GameContext 中 Log = LogManager.GetChannel() (新系统) |
| Logger (旧) | MetaStruct | Logger 私有的 StructWithMeta 使用 MetaStruct |

## GizmoDebugUtility · Gizmo 辅助

提供三个静态绘制方法，封装 Gizmos + Handles 的常见操作，支持可选的文字标签。

| 方法 | 用途 | Gizmo 类型 |
|------|------|-----------|
| DrawArrowLine | 带箭头指示的线段 | Gizmos.DrawLine + 箭头三角形 |
| DrawWireBox | 线框盒体 + 可选标签 | Gizmos.DrawWireCube |
| DrawSphere | 实心球体 + 可选标签 | Gizmos.DrawSphere |

## Logger (旧) · 旧版日志格式器

> 即将迁移到 08-logging 系统。当前作为 ConsoleAppender 的格式化后端保留。

功能:
- 三级日志: Log / LogWarning / LogError (各自对应 Unity Debug 方法)
- 智能序列化: 自动处理原始类型 / Unity struct / IDictionary / IEnumerable / struct 反射
- 深度限制: 最大 4 层递归，防止无限循环
- 循环引用检测: 引用类型对象只序列化一次
- 标签系统: 自动取 payload 类型名作为标签，可手动覆盖

## 设计决策

| 决策 | 原因 |
|------|------|
| Gizmo 方法用 `internal` 而非 `public` | 仅限项目内部使用，不对外暴露 |
| Gizmo 方法加 `label` 可选参数 | 一次调用完成图形+文字，简化调用方代码 |
| Logger 旧类保留作为 ConsoleAppender 后端 | ConsoleAppender 需要格式化能力，旧 Logger 直接可用 |
| StructWithMeta 未在公开 API 中使用 | 是日志框架预留的扩展点 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| Logger 废弃，功能全部迁至 Logging | 待做 | 08-logging | 代码注释 + 架构规划 |
| GizmoDebugUtility 扩展 2D 形状 | 待做 | — | 代码分析 |
| GizmoDebugUtility 增加持久绘制支持 | 待做 | — | 代码分析 |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [gizmodebugutility.md](gizmodebugutility.md) | Gizmo 绘制辅助方法 |
| [logger.md](logger.md) | 旧版 Logger 序列化与格式化 |
