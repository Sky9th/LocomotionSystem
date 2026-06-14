# editor · 编辑器

> Unity Editor 扩展，仅在 `#if UNITY_EDITOR` 下编译，不参与 Runtime 构建。

```
editor/
├── conventions/          ← EUI 设计体系（如何构建编辑器 UI）
│   ├── README.md
│   ├── components.md
│   └── design-tokens.md
│
├── tools/                ← 编辑器工具（源码目录镜像）
│   ├── Shared/
│   ├── L1_Core/
│   ├── L3_Ability/
│   └── L3_Properties/
│
└── README.md             ← 本文件
```

## 设计体系

| 文档 | 内容 |
|------|------|
| [conventions/README.md](conventions/README.md) | EUI 入口 — 铁律 / 快速查找 / 组件速查 |
| [conventions/components.md](conventions/components.md) | 组件 API + 布局模式 + 常见陷阱 |
| [conventions/design-tokens.md](conventions/design-tokens.md) | 视觉令牌 — 颜色 / 字号 / 间距 / 圆角 |

## 编辑器工具

| 模块 | 目录 | 源文件 |
|------|------|--------|
| Shared | [tools/Shared/](tools/Shared/) | `Assets/Scripts/Shared/Editor/` |
| GameplayTag | [tools/L1_Core/GameplayTag/Editor/](tools/L1_Core/GameplayTag/Editor/) | `Assets/Scripts/L1_Core/GameplayTag/Editor/` |
| Ability | [tools/L3_Ability/Editor/](tools/L3_Ability/Editor/) | `Assets/Scripts/Services/Modules/L3_Ability/Editor/` |
| Properties | [tools/L3_Properties/Editor/](tools/L3_Properties/Editor/) | `Assets/Scripts/Services/Modules/L3_Properties/Editor/` |
