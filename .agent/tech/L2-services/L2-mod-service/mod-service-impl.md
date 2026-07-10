# ModService (Implementation)

> `Assets/Scripts/Services/L2_ModService/ModService.cs`
>
> **Last Verified**: 2026-07-10 | **Verification**: All referenced files exist, signatures match code

## Internal Mechanics

`ModService` extends `ModuleChildMono` — discovered by `ModuleHub.Awake()` via `GetComponentsInChildren`. No Unity-specific lifecycle beyond the ModuleChildMono protocol.

## Call Chain

- **Called by**: `SceneService.EnsureBootReady()` (via `GameContext.TryResolveService<ModService>`)
- **Called by**: `ModuleHub.Awake()` → `OnAssemble()`, `ModuleHub.Start()` → `OnWire()`
- **Calls**: `Assembly.Load()`, `Activator.CreateInstance()`, `IModEntry.Initialize()`

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| Consumer | `SceneService` | `EnsureBootReady()` 调用 `LoadAllMods()` |
| Registry | `GameContext` | `OnAssemble()` 中注册，SceneService 通过 `TryResolveService` 解析 |
| Parent | `ModuleHub` (GameService) | 通过 `GetComponentsInChildren` 自动发现 |

## Public Properties

| Property | Type | Purpose |
|----------|------|---------|
| `Results` | `IReadOnlyList<ModLoadResult>` | 所有 Mod 的加载结果，供 UI 查询 |
| `LoadedCount` | `int` | `_results.Count` |

## Methods

### OnAssemble()
```csharp
public override void OnAssemble()
```
- **Purpose**: 创建日志通道，注册到 GameContext
- **Callers**: `ModuleHub.Awake()` → `Registry.OnAssembleAll()`

### OnWire()
```csharp
public override void OnWire()
```
- **Purpose**: 无跨服务依赖（预留）

### LoadAllMods()
```csharp
public void LoadAllMods()
```
- **Purpose**: 扫描 Mods 目录，逐个加载 Mod。AOT metadata 加载后调用
- **Callers**: `SceneService.EnsureBootReady()`
- **Flow**:
  1. `ResolveModsPath()` → 确定扫描路径 (`dataPath/../Mods`)
  2. `Directory.Exists` → 不存在则创建空目录
  3. `Directory.GetDirectories` → 遍历 → `LoadSingleMod()`
  4. 汇总：成功/失败计数 + 失败详情日志

### LoadSingleMod()
```csharp
private void LoadSingleMod(string modDir)
```
- **Purpose**: 加载单个 Mod 文件夹
- **Params**: `modDir` — Mod 文件夹绝对路径
- **Flow**:
  1. 读 `manifest.json` → `JsonUtility.FromJson<ModManifest>()`
  2. 验证 `modId` 非空
  3. `Directory.GetFiles("*.dll")` → 找 DLL
  4. `File.ReadAllBytes` → `Assembly.Load(bytes)`
  5. `DiscoverAndInvokeEntries()` — 反射发现入口

### DiscoverAndInvokeEntries()
```csharp
private void DiscoverAndInvokeEntries(string modId, string folderName, Assembly modAssembly)
```
- **Purpose**: 从已加载的程序集中发现并执行 `[ModEntry]` 入口
- **Flow**:
  1. `GetExportedTypes()` → 遍历
  2. `GetCustomAttribute<ModEntryAttribute>()` → 找标记类
  3. `typeof(IModEntry).IsAssignableFrom(type)` → 检查接口实现
  4. `Activator.CreateInstance(type)` → 实例化（需无参构造）
  5. `(IModEntry)instance.Initialize()` → 调用初始化
  6. 每步 catch → log → skip

### ResolveModsPath()
```csharp
private string ResolveModsPath()
```
- **Purpose**: 返回 Mods 目录绝对路径
- **Returns**: `Path.GetFullPath(Path.Combine(Application.dataPath, "../Mods"))`

### ModLoadResult (class)
```csharp
public class ModLoadResult
```
- **Fields**: `ModId`, `FolderName`, `AssemblyName`, `Success`, `Error`
- **Purpose**: 记录单 Mod 加载结果，供 UI 和诊断使用

## Error Handling

| 异常类型 | 处理 |
|----------|------|
| `FileNotFoundException` (manifest) | Warning + skip folder |
| JSON parse error | Warning + skip folder |
| `BadImageFormatException` (DLL) | Error + skip DLL |
| `FileLoadException` (DLL) | Error + skip DLL |
| `MissingMethodException` (no paramless ctor) | Error + skip entry class |
| `IModEntry` not implemented | Warning + skip entry class |
| `Initialize()` throws | Error + skip entry class, record failure |

所有异常 per-mod 隔离，不会阻塞游戏启动或其他 Mod 加载。

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| Dependency topological sort | TODO S1 | mod-architecture-framework §4.1 |
| Conflict detection + loadPriority | TODO S1 | mod-architecture-framework §4.3 |
| `ModManifest` extended fields | TODO S1 | mod-json-reference.md |
| Mod management UI querying `Results` | TODO S1 | mod.md §4.2 |
