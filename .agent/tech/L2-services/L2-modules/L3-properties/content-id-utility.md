# ContentIdUtility

> [!WARNING] DELETED — 2026-07-11
> `ContentIdUtility.cs` 已删除。`Common/Id` 现存储完整路径（`Entity.Equipment.Weapon.Melee.Blade.machete`），不再需要 `BuildContentId` Category+Id 拼接。
> `rd.` 前缀移至 `CommonConstants.OfficialNamespace`。contentId 由 `PropertyPresetSO._contentId` 字段直接提供。
>
> 详见 [2026-07-11-p1-contentid-catalog.md](../../../sessions/2026-07-11-p1-contentid-catalog.md)

# ContentIdUtility

**File**: `Assets/Scripts/Services/Modules/L3_Properties/ContentIdUtility.cs`

## Layer Position

**L3_Properties** — 纯静态工具类，依赖 `PropertyTable`（同层）。不依赖任何 Service 或 GameContext。

## Call Chain

```
调用方 (未来 P1 AssetCatalog / Mod 系统)
  └── ContentIdUtility.GetContentId(props)
        ├── props.GetRdTag("Common/Category")  → PropertyTable._strings
        └── props.GetString("Common/Id")       → PropertyTable._strings
              └── BuildContentId(prefix, categoryFullPath, id)
                    → "rd.Entity.Equipment.Weapon.Melee.Blade.katana"
```

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| ↓ depends | `PropertyTable` | 读取 Common/Category (RdTag) 和 Common/Id (String) |
| → consumer | `AssetCatalog` (P1) | 以 contentId 为 key 索引物品 |
| → consumer | Mod System (远期) | Mod 自定义前缀标识资产来源 |

## Public Properties

| Property | Type | Purpose |
|----------|------|---------|
| `OfficialPrefix` | `const string` | 官方内容前缀 `"rd."`。编译时常量，发布后不会变更 |

## Methods

### BuildContentId(prefix, categoryFullPath, id)

```csharp
public static string BuildContentId(string prefix, string categoryFullPath, string id)
```

- **Purpose**: 拼接 contentId = `{prefix}{categoryFullPath}.{id}`
- **Params**: `prefix` — 来源标识（官方 `"rd."`, Mod 自定义）；`categoryFullPath` — Category Tag 的 FullPath；`id` — 实体唯一标识
- **Returns**: 拼接后的 contentId；任一参数为空时返回 `null`
- **Callers**: P1 AssetCatalog / Mod System

### BuildContentId(categoryFullPath, id)

```csharp
public static string BuildContentId(string categoryFullPath, string id)
```

- **Purpose**: 使用官方前缀 `OfficialPrefix` 拼接
- **Callers**: 内部系统（不需要自定义前缀的场景）

### GetContentId(props)

```csharp
public static string GetContentId(PropertyTable props)
```

- **Purpose**: 从 PropertyTable 直接读取 Category + Id 并拼接
- **Params**: `props` — 实体的 PropertyTable 实例
- **Returns**: contentId；Category 或 Id 缺失时返回 `null`
- **Callers**: P1 AssetCatalog 索引

## Future Plans

| Plan | Status | Dependency | Source |
|------|--------|------------|--------|
| P1 AssetCatalog contentId 查找 | 规划中 | ContentIdUtility 已就绪 | short-term.md |
| Mod namespace prefix 注入 | 远期 | Mod 系统设计 | mod-architecture-framework.md |
