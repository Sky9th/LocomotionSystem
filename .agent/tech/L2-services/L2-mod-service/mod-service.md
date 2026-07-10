# ModService · Mod 加载管理中心

> `L2_ModService/ModService.cs` — ModuleChildMono，外部 C# Mod DLL 的加载入口。
>
> **Last Verified**: 2026-07-10 | **Verification**: All referenced files exist, signatures match code

## 模块结构

```
L2_ModService/
├── ModEntryAttribute.cs    # [ModEntry] 标记 — Attribute
├── IModEntry.cs            # 入口接口 — void Initialize()
├── ModManifest.cs          # manifest.json 反序列化
└── ModService.cs           # L2 入口 — ModuleChildMono，Mod 扫描/加载/初始化
    └── ModLoadResult       # 内部类 — 加载结果记录
```

## 职责

| 文件 | 职责 |
|------|------|
| `ModEntryAttribute.cs` | `[AttributeUsage(Class)]`，Mod 作者标记入口类 |
| `IModEntry.cs` | `void Initialize()`，入口类必须实现 |
| `ModManifest.cs` | `[Serializable]`，`JsonUtility.FromJson` 解析 manifest.json |
| `ModService.cs` | L2 入口，`LoadAllMods()` 扫描→加载→反射→初始化，`ModLoadResult` 记录结果 |

## 调用链

```
SceneService.EnsureBootReady()             // coroutine — SceneService.cs:89
  ├─ AssetService.RunBootInit()            // 填充 AssetCatalog
  ├─ AssetService.LoadAOTMetadata(cb)      // HybridCLR AOT 66 assemblies
  └─ ModService.LoadAllMods()              // ← NEW
       ├─ ResolveModsPath()                // dataPath/../Mods
       ├─ Directory.GetDirectories()
       └─ foreach → LoadSingleMod(folder)
            ├─ File.ReadAllText(manifest.json) → JsonUtility.FromJson<ModManifest>()
            ├─ Directory.GetFiles("*.dll") → File.ReadAllBytes → Assembly.Load(bytes)
            ├─ GetExportedTypes() → [ModEntry] + typeof(IModEntry).IsAssignableFrom
            ├─ Activator.CreateInstance → (IModEntry)instance.Initialize()
            └─ ModLoadResult → _results.Add()
```

## 公开 API

```csharp
// Main entry — called by SceneService after AOT metadata
void LoadAllMods();

// Diagnostics
IReadOnlyList<ModLoadResult> Results { get; }
int LoadedCount { get; }
```

## 耦合模块

| 模块 | 关系 | 说明 |
|------|------|------|
| `SceneService` | Consumer | `EnsureBootReady()` 末尾调用 `LoadAllMods()` |
| `GameContext` | Registry | `OnAssemble()` 中 `RegisterService(this)`，SceneService 通过 `TryResolveService` 获取 |
| `ModuleHub` | Parent | GameService 的 `Awake()` 通过 `GetComponentsInChildren` 自动发现 |
| `HybridCLR.RuntimeApi` | Dependency | `Assembly.Load(byte[])` 由 HybridCLR 在 IL2CPP 下拦截 |

## 设计决策

| 决策 | 原因 |
|------|------|
| `[ModEntry]` + `IModEntry` 双重检查 | Attribute 标记意图，接口保证编译期检查 |
| Mods 路径 = `dataPath/../Mods` | 玩家直观（exe 旁），与 RimWorld 等一致 |
| Per-mod 错误隔离 | 一个坏 Mod 不阻塞其他 Mod 加载 |
| `ModLoadResult` 列表 | 为以后 Mod 管理 UI 提供数据源 |
| 同步加载 | Mod 加载只需磁盘 IO + 反射，无需协程 |

## 未来规划

| 计划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 依赖拓扑排序 + 环检测 | TODO | — | mod-architecture-framework §4.1 |
| Mod ID 冲突检测 + loadPriority | TODO | — | mod-architecture-framework §4.3 |
| ModManifest 扩展 dependencies[]/content | TODO | 切换 Newtonsoft.Json | mod-json-reference.md |
| Mod 管理 UI | TODO | ModLoadResult 已就位 | mod.md §4.2 |

## 子文档索引

| 文件 | 文档 |
|------|------|
| `ModEntryAttribute.cs` | [mod-entry-attribute.md](mod-entry-attribute.md) |
| `IModEntry.cs` | [imod-entry.md](imod-entry.md) |
| `ModManifest.cs` | [mod-manifest.md](mod-manifest.md) |
| `ModService.cs` | [mod-service-impl.md](mod-service-impl.md) |
