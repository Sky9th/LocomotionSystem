# AssetService · 资产管理中心

> `L2_AssetService/AssetService.cs` — ModuleChildMono，唯一资产加载入口 + 强引用锚。
>
> **v1 — 2026-07-07。** 替代原 `L2_AddressablesService` + 消除 `L2_SceneService/Pipeline/Boot/` 12 文件。
>
> **Last Verified**: 2026-07-07

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
