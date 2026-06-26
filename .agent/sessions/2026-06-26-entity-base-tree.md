# 2026-06-26 — Entity 基树设计

## Background

PropertyTree 系统有 9 棵根树（Actor, WeaponBase, ArmorBase, ContainerBase, ToolBase, ConsumableBase, AmmoBase, Building, Environment），互不继承。`Weight` 在 6 棵根树中各自独立定义，`DisplayName`/`Icon` 只在武器叶树零散出现，`Slots` 仅 Actor 有。缺乏万物共享的属性底座。

v0.25.0 的 PropertyType.Struct 落成后，开始重新审视整个 PropertyTree 的树结构设计。

## Changes

### Entity 基树
- 新增 `Entity` 隐式基树——`Common/` 文件夹 + 4 个叶节点
- `Common/DisplayName` (String) / `Common/Icon` (AssetRef) / `Common/Weight` (Float) / `Common/Slots` (Struct, 默认 [])
- 9 棵根树全部 `inheritsFrom: "Entity"`——无例外
- 删除 20 个重复节点（Weight ×6, DisplayName ×7, Icon ×7, Slots ×1）
- `PropertyTreeSO.cs` 零改动——import 时自动建 `InheritsFrom` 引用链

### Editor 修复
- 左面板：`BuildLeftTree` 按 `t.name == "Entity"` 过滤隐式基树
- 中心面板：`DrawCenterContent` 新增根级叶节点渲染——Environment 不再空白
- Entity 的 Common/ 文件夹及子属性以灰色"继承"样式显示

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Entity 作为真实 .asset + import 链 | A: 代码注入（CollectInheritedLayers 硬编码）→ 污染运行时。B: 完全不建基树 → 重复声明蔓延 | import 管道的 InheritsFrom 链接已处理一切，零 C# 改动 |
| 9 棵根树全部继承 Entity（含 Environment） | 排除 Environment → 多一个特殊处理 | 4 属性对 Environment 零伤害，无例外更干净 |
| Common 文件夹包裹 4 属性 | 根级平铺 → 破两层级铁则 | 一级目录 + 二级属性，遵守模块约定 |
| Entity 在 Editor 按名隐藏 | 加 `HideInBrowser` 字段 → 多一个 SO 字段 | 隐式基树，按名过滤够用 |
| Environment 未继承时的根级叶渲染 bug | — | Editor 只画根级文件夹，跳过了所有根级叶节点——顺带修复 |

## Known Issues

- [ ] Entity.asset 需在 Unity 中 Import JSON 后生成（CI 无法创建 .asset）
- [ ] Import 后需手动删除旧的重复节点 .asset（已修改 treeJson 指向 Common 文件夹）

## Cross-References

### Related Sessions
- [2026-06-26-property-struct-and-rename.md](2026-06-26-property-struct-and-rename.md) — 同日前置工作，为 Entity 基树铺路
- [2026-06-24-equipment-item-architecture.md](2026-06-24-equipment-item-architecture.md) — 一切皆属性 + PropertyType.Struct 的根源

### Related Plans
- [C:\Users\Sky9th\.claude\plans\sorted-growing-tarjan.md](sorted-growing-tarjan.md) — Entity 基树实现计划

### Related Tech Docs
- [tech/L2-services/L2-modules/L3-properties/property-preset-so.md](../tech/L2-services/L2-modules/L3-properties/property-preset-so.md)
- [tech/L2-services/L2-modules/L3-properties/property-table.md](../tech/L2-services/L2-modules/L3-properties/property-table.md)

### Flag for Design Doc Creation
- [x] No design doc needed — architecture-layer change, no player-facing behavior.
