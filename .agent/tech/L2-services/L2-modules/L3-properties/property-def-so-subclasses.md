# PropertyDefSO 子类体系

> `L3_Properties/Definition/` — 9 个子类 + PropertyDefSO 基类 · 技术文档 · 2026-06-30
> **Last Verified**: 2026-06-30 | **Verification**: All 9 subclass files exist. rTag→RdTag rename complete. OnEnable sets Type. ComputeWriteValue overrides verified.

## 层级定位

L3 资产层。每个子类自含字段声明 + 类型专属解析逻辑。替代旧版 PropertyDefSO 的 12 字段平铺结构。

## 基类

`PropertyDefSO` — 仅 4 个 Identity 字段 (Id, Description, [HideInInspector]Type, IsDeprecated) + 3 个 virtual：

| Virtual | 用途 |
|---------|------|
| `ComputeWriteValue(object, bool isRaw, bool isDefault)` | 类型专属解析。默认 passthrough，子类覆写 |
| `TypeMatches<T>()` | 仅 Struct 覆写，校验泛型类型匹配 |
| `static Create(PropertyType)` | 工厂，按枚举创建对应子类实例 |

## 子类一览

| 子类 | 自有字段 | ComputeWriteValue 内容 |
|------|---------|----------------------|
| FloatPropertyDefSO | Min, Max, DefaultValue | isDefault→clamped DefaultValue, isRaw→Parse, else→SafeFloat |
| IntPropertyDefSO | Min, Max, DefaultValue | isDefault→clamped DefaultValue, isRaw→int.Parse, else→SafeInt, clamp |
| BoolPropertyDefSO | DefaultValue | isDefault→DefaultValue, isRaw→bool.Parse, else→SafeBool |
| StringPropertyDefSO | DefaultValue | isDefault→DefaultValue, else→as string |
| RTagPropertyDefSO | DefaultValue | 同 String（语义独立，Tag 校验在消费层） |
| RTagListPropertyDefSO | (无) | isDefault→empty, isRaw→ParseTagArray, else→as string[] |
| AssetRefPropertyDefSO | DefaultAssetGUID, AssetTypeConstraint | isDefault→LoadByGuid, else→Resolve |
| AssetRefListPropertyDefSO | AssetTypeConstraint | isDefault→empty, else→ResolveList |
| StructPropertyDefSO | StructTypeName, DefaultJson | isDefault→DefaultJson, isRaw→raw, else→JsonUtility.ToJson, 自动包裹数组 |

## 调用链

```
被谁调:
  PropertyTable.DoWrite()        → def.ComputeWriteValue(value, isRaw, isDefault)
  PropertyTable.StructTypeMismatch → ((StructPropertyDefSO)def).TypeMatches<T>()
  PropertyDefSO.Create(type)     → 所有 Editor 创建路径

调谁:
  (自含，无外部依赖——仅 Parse/Resolve helper 为 private 或 static)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承自 | PropertyDefSO | 基类：Identity 字段 + virtual 方法 |
| 被消费 | PropertyTable | DoWrite 通过 ComputeWriteValue 分发 |
| 被消费 | PropertyImportExport | PopulateDef/ReadDef 按 is 守卫分发 |
| 被消费 | PropertyTreeEditorPopups | CreateDef/DrawTypeFields 按 is 守卫分发 |
| 被消费 | PropertyDefSOEditor | Inspector 按 is 守卫选择字段组 |

## 设计原则

- **每个子类自文档化**：打开 `FloatPropertyDefSO.cs` 看到所有字段 + 解析逻辑
- **字段名去类型后缀**：容器类已表明类型 → `DefaultValue` 代替 `DefaultFloat`/`DefaultInt`
- **OnEnable 设 Type**：`[HideInInspector]` 防止 Inspector 误改，运行时 OnEnable 保证一致性
- **不做 legacy fallback**：代码只认子类 cast，旧 .asset 通过 JSON Import/Export 迁移
