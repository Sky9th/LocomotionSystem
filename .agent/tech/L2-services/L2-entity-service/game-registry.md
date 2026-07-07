# GameRegistry — 集中化资产注册表

> `L2_EntityService/GameRegistry.cs` · `namespace RedDust.Entities` · 纯 C# 实例类
>
> **Last Verified**: 2026-07-07 | **Verification**: All 8 registries verified. ResolveAbilityTrees() added. InvalidatePropertyDefs() removed.

## 定位

GameRegistry 是全部 boot-loaded ScriptableObject 的集中运行时注册表。由 `GameService.AssetRegistry`（DontDestroyOnLoad）持有，BootPipeline 填充，所有服务/MonoBehaviour 读取。

替代 8 个已删除的 standalone static Registry 类。实例化而非 static 确保生命周期跟随 GameService——不因场景切换丢失状态。

## 八个注册表

| 注册表 | 类型 | Key |
|--------|------|-----|
| Characters | `CharacterDefSO` | asset name |
| Items | `PropertyPresetSO` | asset name |
| AbilityTrees | `AbilityTreeSO` | `treeId` |
| PropertyDefs | `PropertyDefSO` | `def.Id` |
| PropertyTrees | `PropertyTreeSO` | asset name |
| AnimationProfiles | `CharacterAnimationProfileSO` | asset name |
| GroundConfigs | `GroundSystemConfigSO` | asset name |
| AudioConfigs | `CharacterAudioConfigSO` | asset name |

## 调用链

```
BootPipeline.Run
  ├── Phase 1: Addressables.LoadByLabel("boot") → assets[]
  ├── Phase 2: new BootAssetCatalog(assets)
  │     └── GameService.Instance.AssetRegistry.SetCatalog(catalog)  ← 强引用根
  ├── Phase 3: BootTask.Resolve(catalog)
  │     ├── AbilityBootTask     → AssetRegistry.InitAbilityTrees(trees)
  │     ├── CharacterBootTask   → AssetRegistry.InitCharacters(defs)
  │     ├── ItemBootTask        → AssetRegistry.InitItems(presets)
  │     ├── PropertyBootTask    → AssetRegistry.InitPropertyDefs(defs)
  │     ├── PropertyTreeBootTask → AssetRegistry.InitPropertyTrees(trees)
  │     ├── ConfigBootTask      → AssetRegistry.InitAnimProfiles / InitGroundConfigs / InitAudioConfigs
  │     └── ...
  └── Phase 4: Scene load

Consumers:
  ├── CharacterActor.Awake   → FindAnimProfile / FindAudioConfig / FindGroundConfig
  ├── CharacterActor.Start   → (optional) def.InnateTreeIds → ResolveAbilityTrees → SetInnateTrees
  ├── PlayerService          → FindCharacter / FindItem<T>
  ├── PropertyTable          → FindPropertyTree
  └── PropertyTreeSO         → FindPropertyDef
```

## 公开 API

### 初始化（BootTask 调用）

```csharp
public void InitCharacters(List<CharacterDefSO> defs);
public void InitItems(List<PropertyPresetSO> presets);
public void InitAbilityTrees(List<AbilityTreeSO> trees);
public void InitPropertyDefs(List<PropertyDefSO> defs);
public void InitPropertyTrees(List<PropertyTreeSO> trees);
public void InitAnimProfiles(List<CharacterAnimationProfileSO> profiles);
public void InitGroundConfigs(List<GroundSystemConfigSO> configs);
public void InitAudioConfigs(List<CharacterAudioConfigSO> configs);
```

每个 Init 方法：创建新 Dictionary → 遍历去重 → 填充 → 设 ready flag → 输出日志。

### 查找（消费者调用）

```csharp
public CharacterDefSO FindCharacter(string key);
public PropertyPresetSO FindItem(string key);
public T FindItem<T>(string key) where T : PropertyPresetSO;
public bool ContainsItem(string key);
public AbilityTreeSO FindAbilityTree(string treeId);
public IReadOnlyList<AbilityTreeSO> AllAbilityTrees { get; }
public PropertyDefSO FindPropertyDef(string id);
public bool ContainsPropertyDef(string id);
public PropertyTreeSO FindPropertyTree(string treeId);
public bool ContainsPropertyTree(string treeId);
public CharacterAnimationProfileSO FindAnimProfile(string key);
public GroundSystemConfigSO FindGroundConfig(string key);
public CharacterAudioConfigSO FindAudioConfig(string key);
```

### 批量解析

```csharp
/// <summary>treeId[] → AbilityTreeSO[] 的唯一入口。调用方不应自己写 Registry 遍历。</summary>
public AbilityTreeSO[] ResolveAbilityTrees(string[] treeIds);
```

### Catalog

```csharp
public void SetCatalog(BootAssetCatalog catalog);  // BootPipeline 调用，保存强引用根
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| ↓ | 8 种 ScriptableObject | 注册表持有 SO 引用 |
| ↑ | BootPipeline + 7 BootTask | 填充数据 |
| ↑ | CharacterActor, PlayerService, PropertyTable, PropertyTreeSO | 查找数据 |
| ← | GameService | 持有实例（DontDestroyOnLoad） |

## 设计决策

| 决策 | 原因 |
|------|------|
| 实例类（非 static） | 生命周期跟随 GameService.DontDestroyOnLoad，避免场景切换丢数据 |
| 与 BootAssetCatalog 双重引用 | catalog._allAssets 作为二级强引用根，配合 AddressablesService.PinnedLabels 形成双重保护 |
| 每种类型独立字典 | 类型安全——不需要 `as` 转型 + null check |
| `_*Ready` 每个独立 flag | 逐类型延迟初始化，BootTask 按序填充 |
| `ResolveAbilityTrees()` 集中批量查找 | 消除散布各地的 ID 数组遍历逻辑 |

## 未来规划

| 计划 | 状态 | 来源 |
|------|------|------|
| 支持运行时热重载（非 boot 标签） | 远期 | — |
| 增量更新单类型而非全量重建 | 远期 | — |
| Add/Remove 方法支持动态注册 | 远期 | — |
