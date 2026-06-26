# 2026-06-26 — PropertyType.Struct + 模块重命名

## Background

ItemDefSO 设计要求"零 C# 字段"，但 `SlotDef[] Slots` 仍在 C# 上。结构化数据应走 `PropertyType.Struct` + JSON blob，然而 Struct 枚举值不存在。同时 `EntityDefSO` / `EntityProperties` 的 "Entity" 命名与模块的 Property 前缀不协调，趁全链路改动一步到位统一。

这是 S2 容器系统底座——PropertyType.Struct 落地后，SlotDef[] 才能从 C# 字段迁入 PropertyTree，为 Container<T> 铺路。

## Changes

### 模块重命名
- `EntityDefSO` → `PropertyPresetSO`（属性预设 — Template + OverridesJson）
- `EntityProperties` → `PropertyTable`（运行时平表 — key→value + Tick/Guard/Modifier）
- 静态工厂 `Create(def)` → `FromPreset(preset)`
- `PropertyAgent._props` → `_table`，`_def` → `_preset`，参数 `def` → `preset`
- 全模块 Property 前缀统一：Def / Tree / Preset / Table / Agent
- C# 代码 10+ 文件，文档 18 个 .md 全部替换

### PropertyType.Struct
- `PropertyType.Struct` 枚举末尾追加（ordinal 8，不破坏序列化）
- `PropertyDefSO` + `StructTypeName` + `DefaultStructJson` 字段
- `PropertyTable` + `_structJsons` 字典 + `DoWrite` Struct case（三路径统一 + 裸数组自动包裹 `{"Items":[...]}`）
- `GetStruct<T>(path)` / `GetStructArray<T>(path)` — `Type.GetType()` 解析 StructTypeName → `typeof(T)` 校验
- `PropertyAgent` 透传两个 Struct 读方法

### [PropertyStruct] 属性化系统
- `PropertyStructAttribute` — struct 标记（`AttributeTargets.Struct`）
- `PropertyStructScanner` — Editor 扫描 + 下拉框，存 `Type.FullName`
- `Type.GetType()` 运行时解析 → 类型不匹配报错，不静默失败
- `SlotDef` 标记 `[PropertyStruct]`

### Editor
- `PropertyDefSOEditor` Struct 下拉框替换文本框
- `PropertyTreeEditorPopups` DefDetail / CreateDefDialog + Struct case
- `PropertyImportExport` DTO + Import/Export 支持 Struct
- **Bugfix**: 文件夹重命名 — IMGUI 失焦时 TextField 复位值导致改名失效。用 `_folderEdits` 字典缓存编辑态，解除 TextField value 参数对 `node.NodeId` 的依赖

### Properties 数据
- `properties_all.json` Actor 树 + `Slots` 属性（Struct/SlotDef/[]），所有 Actor 继承

### Bug 修复（三轮审查发现）
- `AbilityForest.AddTree` null guard
- `DoWrite` Struct case：null/空值安全、前导空格、OnPropertyChanged 广播、裸数组包装
- `GetStructArray<T>` null-safe 返回
- `PropertyStructScanner` 死 import、冗余列表、异常类型

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Struct 类型用 `Type.FullName` 字符串关联 C# 类型 | A: 手写短名 → 无唯一性保证，`Type.GetType()` 不可解析。B: enum 枚举所有 struct → 每加一个 struct 改一次框架。C: 不存类型名 → 消费方硬编码，静默 bug | FullName + `Type.GetType()` 解析 = 精确，Editor 下拉框 + `[PropertyStruct]` 标记 = 不用手写 |
| `DefaultStructJson` 保留 | 删掉 → 但 Float 有 `DefaultFloat`，Struct 同样需要基线值（如 11 槽人形身体） | 与标量类型一致：PropertyDef 提供基线，Preset OverridesJson 做差分 |
| Struct JSON 自动包裹 `{"Items":[...]}` | 要求用户写包裹格式 → 不自然。单独 `StructArray` 枚举 → 类型数量膨胀 | Unity JsonUtility 根级必须是对象，自动包裹用户无感 |
| `_structJsons` 命名 | `_structs` → 和 C# 关键字差一字。`_structBlobs` → 不统一 | 匹配模块 `_<containedType>` 风格（`_strings`、`_ints`），存入的是 JSON 字符串 |
| `_def` → `_preset` | 保留 → 与 `PropertyDefSO` 名冲突 | 类型是 `PropertyPresetSO`，名应一致 |
| 三审 | 一审就提交 | 用户坚持三轮子 Agent 交叉审查，逐轮修复，每次 2 Agent 并行 |

## Known Issues

- [ ] Unity 编译未验证（CLI 环境无法编译 C#）
- [ ] `PropertyDefSO.Id` 与 `PropertyNode.DefId` 命名不一致——26 处引用，跨 5 文件，独立 PR
- [ ] `IPropertyReader` 接口未加 `GetStruct<T>` / `GetStructArray<T>`——消费方只能走 `PropertyAgent` 具体类型
- [ ] `.asset` 文件中 `EntityDefSO` 引用为 GUID 而非类名，改名不影响序列化——但建议 Unity 内跑一次全量 AssetDatabase.Refresh 确认

## Cross-References

### Related Sessions
- [2026-06-25-ability-forest-landing.md](2026-06-25-ability-forest-landing.md) — AbilityForest 落地，S2 管道上一环
- [2026-06-24-equipment-item-architecture.md](2026-06-24-equipment-item-architecture.md) — Item/Container 架构设计，PropertyType.Struct 的根源需求

### Related Plans
- [../plans/short-term-plan.md](../plans/short-term-plan.md) — S2 装备→技能闭环，S2.1-S2.2
- [C:\Users\Sky9th\.claude\plans\sorted-growing-tarjan.md](sorted-growing-tarjan.md) — 本次实现计划

### Related Tech Docs
- [tech/L2-services/L2-modules/L3-properties/README.md](../tech/L2-services/L2-modules/L3-properties/README.md) — 更新了子文档引用
- [tech/L2-services/L2-modules/L3-properties/property-preset-so.md](../tech/L2-services/L2-modules/L3-properties/property-preset-so.md) — 原 entity-def-so.md，重命名 + 全文更新
- [tech/L2-services/L2-modules/L3-properties/property-table.md](../tech/L2-services/L2-modules/L3-properties/property-table.md) — 原 entity-properties.md，重命名 + 全文更新
- [tech/L2-services/L2-modules/L3-properties/property-agent.md](../tech/L2-services/L2-modules/L3-properties/property-agent.md) — 更新引用

### Flag for Design Doc Creation
- [x] No design doc needed — framework-level refactoring and new property type, no player-visible behavior changes.
