# SyntyPrototypeMenu
> **源文件**: `Assets/Scripts/Shared/Editor/Prototype/SyntyPrototypeMenu.cs`

菜单入口 + 分类扫描逻辑 + 实例化方法 + 数据模型。

## 调用链

```
被谁调:
  用户点击 RedDust/Synty Prototype Browser 菜单  → Browse()
  SyntyPrototypeBrowser                   → GetCategories() / InstantiateByPath()
  SyntyPrototypeBrowser.PlaceWithMaterial() → InstantiateByPath()

调谁:
  Browse()                 → SyntyPrototypeBrowser.Open(GetCategories())
  GetCategories()           → ScanAllFolders()
  ScanAllFolders()          → CreateEntry() foreach prefab
  CreateEntry()             → DetermineCategory() + FormatDisplayName()
  DetermineCategory()       → ExtractType()
  InstantiateByPath()        → AssetDatabase.LoadAssetAtPath() / PrefabUtility.InstantiatePrefab()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | SyntyPrototypeBrowser | 读取 CategoryData + PrefabEntry，调用 InstantiateByPath |
| 依赖 | UnityEditor.AssetDatabase | 加载 Prefab、检测资源存在性 |
| 依赖 | UnityEditor.PrefabUtility | 实例化 Prefab |
| 依赖 | UnityEditor.Undo | 注册撤销操作 |

## 方法

### Browse()
```csharp
[MenuItem("Window/Synty Prototype")]
private static void Browse()
```
- **用途**: 菜单入口，打开 Synty Prototype 浏览器
- **调用者**: 用户点击 `Window/Synty Prototype` 菜单
- **备注**: `[MenuItem]` 属性注册到 Unity Editor 菜单栏

### GetCategories()
```csharp
public static List<CategoryData> GetCategories()
```
- **用途**: 获取（或首次扫描并缓存）分类数据
- **返回**: 按分类组织的 Prefab 列表
- **调用者**: `Browse()`, `SyntyPrototypeBrowser.Open()`
- **缓存**: `s_cache` 静态字段，首次调用后永久缓存（编辑器会话期间）

### InstantiateByPath()
```csharp
public static void InstantiateByPath(string path)
```
- **用途**: 在场景中实例化 Prefab
- **参数**: `path` — Prefab 项目路径（如 "Assets/Art/PolygonPrototype/Prefabs/..."）
- **调用者**: `SyntyPrototypeBrowser.PlaceWithMaterial()`
- **逻辑**:
  1. 加载 Prefab → 失败则 `Debug.LogError` 并返回
  2. 设置 parent = 当前选中的 GameObject（如果有）
  3. `PrefabUtility.InstantiatePrefab()` 创建实例
  4. `instance.transform.SetParent(parent, worldPositionStays: false)`
  5. `instance.transform.localPosition = Vector3.zero`
  6. 实例名称 = 文件名（不含扩展名）
  7. `Undo.RegisterCreatedObjectUndo` 注册撤销
  8. `Selection.activeGameObject = instance` + `EditorGUIUtility.PingObject()` 选中并高亮

### ScanAllFolders()
```csharp
private static List<CategoryData> ScanAllFolders()
```
- **用途**: 扫描 PolygonPrototype/Prefabs 目录，按分类组织 Prefab
- **返回**: 排序后的分类数据列表
- **调用者**: `GetCategories()` 首次调用时
- **扫描目录**:
  - Buildings/Simple, Buildings/Polygon
  - Props
  - Primitives, Primitives/Polygon
  - Generic
  - Vehicle
- **排序**: 预定义顺序 Walls → Floors → Stairs → ... → Vehicles，未匹配的分类追加到最后
- **每个分类内**: Prefab 按 displayName 字典序排列

### CreateEntry()
```csharp
private static PrefabEntry CreateEntry(string filePath)
```
- **用途**: 将 Prefab 文件路径解析为 PrefabEntry 条目
- **参数**: `filePath` — 完整 .prefab 路径
- **返回**: PrefabEntry，如果应跳过则返回 null
- **调用者**: `ScanAllFolders()`
- **逻辑**:
  - 调用 `DetermineCategory()` 确定分类
  - 如果分类为空（被跳过） → 返回 null
  - 调用 `FormatDisplayName()` 生成显示名
  - 识别 Polygon 类型（文件名以 "P" 结尾）

### DetermineCategory()
```csharp
private static string DetermineCategory(string fileName)
```
- **用途**: 根据文件名中命名规则确定所属分类
- **参数**: `fileName` — Prefab 文件名（不含扩展名）
- **返回**: 分类名称字符串，或 null（跳过该 Prefab）
- **匹配规则**:
  - `SM_Buildings_*` → Walls / Floors / Stairs / Ramps / Roofs / Railings / Columns / Blocks / "Doors & Windows"
  - `SM_Prop_*` → Ladders / Environment / Props（跳过 SkipTypes 中的武器道具）
  - `SM_Primitive_*` → Primitives
  - `SM_Generic_*` → Environment
  - `SM_Veh_*` → Vehicles
  - `SM_Switch_*` → Props
  - `SM_FX_*` → null（跳过特效）
- **调用者**: `CreateEntry()`

### ExtractType()
```csharp
private static string ExtractType(string nameWithoutPrefix)
```
- **用途**: 从文件名中提取类型部分（去掉尺寸编号后缀）
- **参数**: `nameWithoutPrefix` — 去掉前缀后的文件名部分
- **返回**: 类型名称（如 "Wall_Straight_1x1" → "Wall_Straight"）
- **调用者**: `DetermineCategory()` 用于 Buildings 和 Props 分类判断
- **逻辑**: 按 `_` 分割，遇到纯数字或 `NxN` 格式时停止

### FormatDisplayName()
```csharp
private static string FormatDisplayName(string fileName)
```
- **用途**: 将文件名格式化为人可读的显示名称
- **参数**: `fileName` — Prefab 文件名
- **返回**: 格式化后的显示名（下划线 → 空格，尾部编号去除）
- **调用者**: `CreateEntry()`
- **处理**:
  - 去除 SM_Buildings_ / SM_Prop_ / SM_Primitive_ / SM_Generic_ / SM_Veh_ / SM_Switch_ / SM_FX_ 前缀
  - 去除尾部编号如 `_01`, `_01P`
  - 特殊处理 `Ramp_N_` → `Ramp N°`
  - 下划线替换为空格

## 数据模型

### CategoryData
```csharp
public class CategoryData
{
    public string name;                          // 分类名 (如 "Walls", "Props")
    public readonly List<PrefabEntry> prefabs = new();  // 分类下的 Prefab 列表
}
```

### PrefabEntry
```csharp
public class PrefabEntry
{
    public string path;          // Prefab 项目路径
    public string category;      // 所属分类
    public string displayName;   // 显示名
    public bool isPolygon;       // 是否为 Polygon 类型 (文件名以 'P' 结尾)
}
```

## 静态字段

```csharp
private static List<CategoryData> s_cache;  // 分类数据缓存 (Editor 会话期间)

private static readonly string BasePath = "Assets/Art/PolygonPrototype/Prefabs";

private static readonly string[] ScanDirs = { ... };  // 扫描子目录列表

private static readonly HashSet<string> SkipTypes = new()
{
    "Bat", "BoostPad", "C4", "Knife", "Pistol", "Rifle", "Sword"
};  // 武器类道具，原型阶段不需要
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 缓存过期机制 — 编辑器刷新资源时重建缓存 | 待做 | 代码分析 |
| SkipTypes 改为可配置列表 | 远期 | 代码分析 |
