# shared · 全局共享模块

> 全局 Helper 模块集合 — 不参与 L1-L5 层级体系，所有模块均处于"不限层级"的公共区域。被项目中任意 L1-L5 模块使用。

## 包含模块

| 模块 | 源目录 | 说明 |
|------|--------|------|
| [logging](logging/README.md) | `Assets/Scripts/Utility/Logging/` | 日志系统 — LogChannel + LogManager + Appender |
| [editor](editor/README.md) | `Assets/Scripts/Editor/` | Unity Editor 工具集 — Core 加载、调试面板、Prototype 浏览器 |
| [utility](utility/README.md) | `Assets/Scripts/Utility/` | 通用工具 — Gizmo 绘制辅助 |
| [data-assets](data-assets.md) | `Assets/Data/` | SO 资产目录结构与 CreateAssetMenu 路径约定 |
