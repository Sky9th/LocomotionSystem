# AssetService · 资产管理中心

> `L2_AssetService/AssetService.cs` — ModuleChildMono，唯一资产加载入口 + 强引用锚。
>
> **v1 — 2026-07-07。** 替代原 `L2_AddressablesService` + 消除 `L2_SceneService/Pipeline/Boot/` 12 文件。
>
> **Last Verified**: 2026-07-11

## 模块结构

```
L2_AssetService/
├── AssetService.cs               # L2 入口 — ModuleChildMono + IGameplaySessionHandler
├── Core/
│   └── AssetHandleCache.cs       # Addressables handle 缓存 + pin 机制
├── Boot/
│   └── BootInitRunner.cs         # Registry 初始化序列 + booloaded 强引用锚
└── Structs/
    └── (预留)
```

## 职责

| 文件 | 职责 |
|------|------|
| `AssetService.cs` | L2 入口，公开 API，委托给子模块，`IGameplaySessionHandler.OnGameplaySessionEnd` |
| `Core/AssetHandleCache.cs` | Addressables `InitializeAsync`, `LoadByLabel<T>`, handle 缓存 (key=`"Type:label"`), `Release` (跳过 PinnedLabels), `ReleaseAll` |
| `Boot/BootInitRunner.cs` | `RunBootInit()` — 同步执行全部 Registry 初始化序列 (Tag→Property→Ability→Item→Character→Config→TagFinalize)，持有 `List<Object> _bootAssets` 强引用锚 |

## 调用链

```
SceneService.TransitionTo(config)
  └─ AssetService.EnsureInitialized()     // idempotent — Addressables init
       └─ TransitionGate.Begin(config)
            └─ AssetService.LoadByLabels(["boot", ...sceneLabels])
            └─ AssetService.RunBootInit()  // 首次执行 Registry 初始化
            └─ AssetService.LoadAOTMetadata(cb)  // HybridCLR AOT 补充元数据加载
```

## 公开 API

```csharp
// Lifecycle
IEnumerator EnsureInitialized();          // Addressables init, idempotent

// Loading
void LoadByLabel<T>(string label, Action<List<T>> onComplete);
void LoadByLabels<T>(List<string> labels, Action onComplete);
void ReleaseLabel(string[] labels);       // 跳过 pinned labels

// Boot
bool BootAssetsLoaded { get; }
void RunBootInit();                       // 同步, idempotent — Registry 初始化

// HybridCLR
void LoadAOTMetadata(Action onComplete);  // 异步，通过 Addressables 加载 aot-metadata label 的 TextAsset → RuntimeApi.LoadMetadataForAOTAssembly
```

## Pinned Labels

| Label | 说明 |
|-------|------|
| `boot` | 系统数据 SO，永不释放 |

`ReleaseLabel` / `ReleaseAll(includePinned:false)` 自动跳过 pinned labels。

## GC 回收问题解决

`BootInitRunner._bootAssets` (DontDestroyOnLoad 生命周期) 持有所有 boot 资产的强引用。
Addressables 原生侧释放 handle 时，C# 侧 `GameRegistry` 字典的 value 不会变成 `MissingReference`。

## 关键设计点

- **AssetService 是一切资产加载的唯一入口**：其他 L2 服务不直接接触 Addressables API
- **Handle 缓存**：同 `Type:label` 重复加载返回缓存结果（`LoadByLabel` callback 立即触发）
- **Boot 初始化收敛**：原 12 个文件（IBootTask + BootPipeline + 8 Tasks + Composer + Catalog）→ `BootInitRunner.Run()` 一个方法
- **过渡期共存**：原 `AddressablesService` 被删除，功能吸收进 `AssetHandleCache`

## AssetCatalog — contentId 索引

`AssetCatalog` 是 `GameService.Assets` 持有的运行时资产查找表。v0.45.3 从名称索引切换为 contentId 索引：

### 索引结构
- `_byContentId : Dictionary<string, PropertyPresetSO>` — 唯一字典，字段初始化 `= new()` 保证非空
- Key 格式：`{namespace}.{itemPath}`，官方内容 key = `rd.Entity.Equipment.Weapon.Melee.Blade.machete`
- `InitPresets(List<PropertyPresetSO>)` — 单入口，Items + Characters 合并。从 `p.ContentId` 读 itemPath，加 `CommonConstants.OfficialNamespace` 前缀存入

### 查找流程
```
FindItem("Entity.Equipment.Weapon.Melee.Blade.machete")
  → _byContentId["Entity..."]           — 第1步: 精确匹配（跨命名空间）
  → _byContentId["rd.Entity..."]        — 第2步: 自动补官方命名空间
  → Error                                — 未找到
```
`FindCharacter` 同流程，`as CharacterDefSO` 转型 + 类型不匹配时 warning。

### 命名空间隔离
- 调用方不带前缀 → 程序自动补官方命名空间（`rd.`）
- 调用方带前缀 → 精确匹配目标命名空间（Mod 覆写用）
- 命名空间前缀来自 `CommonConstants.OfficialNamespace`
