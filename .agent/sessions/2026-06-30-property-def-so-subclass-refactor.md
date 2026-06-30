# 2026-06-30 — PropertyDefSO 子类化重构

## Background

PropertyDefSO 原本是一个包含 17 个平铺字段的 God-object — Float 的 Min/Max/DefaultFloat、Int 的 MinInt/MaxInt/DefaultInt、Bool 的 DefaultBool、String 的 DefaultString 等全部字段混在一个 ScriptableObject 上。打开文件完全看不出"Float 属性由什么组成"。

同时发现 Float 的 Min/Max 在 Preset 层无法覆写（不同实例可能需要不同的约束范围），而 OverrideJSON 的 TextArea 编辑体验在讨论中被质疑。

本轮 session 完成了三件事：Min/Max 覆写系统、PropertyDefSO 从 God-object 到 9 个类型化子类的架构重构、PropertyTable 的 DoWrite 瘦身（类型解析逻辑收拢到 SO 子类）。

## Changes

### Min/Max 约束覆写
- `PropertyTable` 新增 `_minOverrides` / `_maxOverrides` 字典
- `PropertyPresetSO.OverridesJson` 支持 `"Min": "0.5"`, `"Max": "500"` 可选字段
- `ParseOverrides` 解析 Min/Max 字符串并写入约束字典
- `GetMin`/`GetMax`/`EffectiveMin`/`EffectiveMax` 覆写优先，无覆写则取 Def 值
- `DoWrite` / `EnsureFloatState` / `WireFloatState` 全部改用 Effective 值

### PropertyDefSO 子类化
- 删除 12 个 LEGACY 平铺字段（Min, Max, DefaultFloat, MinInt…）
- 保留 Identity 字段（Id, Description, Type, IsDeprecated）
- Type 加 `[HideInInspector]` 防 Inspector 误改
- 新增 3 个 virtual 方法：`ComputeWriteValue()`, `TypeMatches<T>()`, 静态工厂 `Create(PropertyType)`

### 9 个新子类 SO
- `FloatPropertyDefSO` { Min, Max, DefaultValue } — Parse + SafeFloat
- `IntPropertyDefSO` { Min, Max, DefaultValue } — Parse + SafeInt
- `BoolPropertyDefSO` { DefaultValue } — SafeBool
- `StringPropertyDefSO` { DefaultValue }
- `RTagPropertyDefSO` { DefaultValue }
- `RTagListPropertyDefSO` { } — ParseTagArray
- `AssetRefPropertyDefSO` { DefaultAssetGUID, AssetTypeConstraint } — ResolveAssetRef + Load
- `AssetRefListPropertyDefSO` { AssetTypeConstraint } — ResolveAssetRefList
- `StructPropertyDefSO` { StructTypeName, DefaultJson } — TypeMatches + JSON wraparr

### PropertyTable 瘦身
- `DoWrite`: 9-case switch → if Float → WriteFloatValue + WriteSimpleTyped（其余 8 种类型统一处理）
- 6 个 helper（SafeFloat/ParseInt/ResolveAssetRef/LoadAssetByGuid/ResolveAssetRefList/ParseTagArray）搬到对应子类
- 删除 `OnFloatChanged` 事件（与 OnPropertyChanged 冗余，零外部订阅者）
- `StructTypeMismatch` 简化为委托 `TypeMatches<T>()`
- 删除 `TagListWrapper` 内部类

### Editor 适配
- `PropertyDefSOEditor` — 每个 case 加 `is` 守卫 + 字段名更新（DefaultFloat→DefaultValue 等）
- `PropertyImportExport` — PopulateDef/ReadDef helper + DTO 补 defaultBool +
  改用 PropertyDefSO.Create() 工厂
- `PropertyTreeEditorPopups` — CreateDefDialog 创建子类 + DefDetail 用 cast 读字段 +
  改用 PropertyDefSO.Create() 工厂

### 清理
- 删除 `Type/` 文件夹（11 个临时 TypeDescriptor 文件）
- 删除 `Data/Properties/Definitions/` 全部旧 Def .asset（~200）
- 删除 `Data/Properties/Trees/` 全部旧 Tree .asset（~30）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| SO 继承（9 个子类）而非 SerializeReference | A: SerializeReference 多态类 — 改类名数据静默丢失；B: JSON blob — 又回到文本存数据的问题 | SO 继承是 Unity-native 方案，每个子类 Inspector 自动只显示自己的字段，字段重命名有 FormerlySerializedAs 保护 |
| 代码只认新子类，不保留 legacy fallback | A: `def as FloatPropertyDefSO ?? def.Min` — 双路径维护负担 | 旧 .asset 保留序列化兼容（能加载不报错），新代码直接 cast；迁移通过 JSON 导入/导出 |
| ComputeWriteValue 放在 SO 子类 | A: 保留在 PropertyTable switch → 90 行 switch 难以维护；B: 抽出独立 handler 类 → 多一套文件 | SO 子类自含字段+解析，打开文件一眼看尽类型全貌 |
| 删除 OnFloatChanged | A: 保留 → 和 OnPropertyChanged 对 Float 总是成对触发，完全冗余 | 零订阅者，安全删除 |
| Int 也声明 AllowsConstraintOverride | A: 只给 Float → 不对称 | 用户要求 Int 也加，descriptor 标注为已声明（PropertyTable 实现 pending）|

## Known Issues

- [ ] Int MinInt/MaxInt 约束覆写已声明但 PropertyTable 实现 pending（P2 — 跟随 Float 模式即可）
- [ ] 旧 PropertyDefSO .asset 在 Inspector 中不显示类型字段（因为不再匹配任何子类 `is` 守卫）— 需通过 JSON Import/Export 迁移（P2 — 当前 feature 分支，迁移一次即可）
- [x] SafeFloat/SafeInt fallback 回归已修复（从硬编码 0 恢复为 DefaultValue）
- [x] isDefault 路径 clamp 回归已修复（Float/Int 默认值路径补回 Mathf.Clamp）
- [x] AssetRefList 遇到无效 GUID 现在报 Debug.LogWarning（不再静默跳过）

## Cross-References

### Related Sessions
- [2026-06-27-property-table-and-preset.md](2026-06-27-property-table-and-preset.md) — 同一模块前序工作，PropertyTable 初始实现

### Related Plans
- [../plans/properties-float-min-max-description-ty-precious-tide.md](../plans/properties-float-min-max-description-ty-precious-tide.md) — 本次重构的设计方案

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-properties/property-table.md](../tech/L2-services/L2-modules/L3-properties/property-table.md) — 需更新，反映 DoWrite 瘦身后结构
- [../tech/L2-services/L2-modules/L3-properties/property-preset-so.md](../tech/L2-services/L2-modules/L3-properties/property-preset-so.md) — 需更新，反映 Min/Max 覆写

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactoring, no gameplay-facing changes.
