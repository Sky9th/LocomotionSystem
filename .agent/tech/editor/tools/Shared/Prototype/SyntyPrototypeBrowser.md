# SyntyPrototypeBrowser
> **源文件**: `Assets/Scripts/Shared/Editor/Prototype/SyntyPrototypeBrowser.cs`

EditorWindow，分类展示 PolygonPrototype Prefab 缩略图，支持材质替换和搜索。

## 调用链

```
被谁调:
  SyntyPrototypeMenu.Browse()     → Open(GetCategories())  ← 菜单入口
  用户交互                        → 点击缩略图 → PlaceWithMaterial

调谁:
  Open()                          → ScanMaterials()
  PlaceWithMaterial()             → SyntyPrototypeMenu.InstantiateByPath()
  GetMatPreview()                 → AssetPreview.GetAssetPreview()
  GetPrefabRef()                  → AssetDatabase.LoadAssetAtPath<GameObject>()
  DrawFilteredResults()           → DrawThumbnailCell()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | SyntyPrototypeMenu | 接收 CategoryData + PrefabEntry 数据，调用 InstantiateByPath |
| 依赖 | UnityEditor.AssetPreview | 生成 Prefab/Material 预览缩略图 |
| 依赖 | UnityEditor.AssetDatabase | 加载 Prefab/Material 资源 |

## 方法

### Open()
```csharp
public static void Open(List<SyntyPrototypeMenu.CategoryData> categories)
```
- **用途**: 打开（或聚焦）Synty Prototype 浏览器窗口
- **参数**: `categories` — 由 SyntyPrototypeMenu.ScanAllFolders() 生成的分类 Prefab 列表
- **调用者**: `SyntyPrototypeMenu.Browse()`
- **备注**:
  - 调用 `GetWindow<SyntyPrototypeBrowser>()` 确保窗口唯一
  - 窗口最小尺寸 480x420
  - 设置 `_selectedCategoryIndex = 0` 默认选中第一个分类
  - 调用 `ScanMaterials()` 加载材质列表

### OnGUI()
```csharp
private void OnGUI()
```
- **用途**: 绘制窗口 UI，包含搜索栏、材质栏、分类侧栏和缩略图网格
- **调用者**: Unity Editor 每帧自动调用
- **UI 结构**:
  - 无数据 → `EditorGUILayout.HelpBox` 显示提示
  - 有数据 → `DrawSearchBar()` → `DrawMaterialBar()` → (搜索模式 ? DrawFilteredResults() : DrawCategorySidebar() + DrawPrefabGrid())

### ScanMaterials()
```csharp
private void ScanMaterials()
```
- **用途**: 扫描 PolygonPrototype 材质目录，加载 Grid 和 Texture 材质变体
- **调用者**: `Open()` 方法
- **逻辑**: 扫描 `Assets/Art/PolygonPrototype/Materials/` 下 `PolygonPrototype_Global_Grid_01.mat` ~ `10.mat` 和 `PolygonPrototype_Texture_01.mat` ~ `10.mat`，共 20 个
- **备注**: 材质文件名严格按编号规则命名，硬编码路径

### PlaceWithMaterial()
```csharp
private void PlaceWithMaterial(string path)
```
- **用途**: 实例化 Prefab 并应用选中的材质变体
- **参数**: `path` — Prefab 路径
- **调用者**: 用户点击缩略图时从 `DrawThumbnailCell()` 调用
- **逻辑**:
  1. 调 `SyntyPrototypeMenu.InstantiateByPath(path)` 实例化
  2. 如果 `_selectedMatIndex >= 0`（已选中材质） → 遍历实例的所有 Renderer，替换 sharedMaterial
- **备注**: 如果 `_selectedMatIndex == -1` (Original) 则不替换材质

### DrawSearchBar()
```csharp
private void DrawSearchBar()
```
- **用途**: 绘制搜索工具栏
- **调用者**: `OnGUI()` 顶部
- **UI**: TextField 输入 + "X" 清除按钮
- **备注**: 搜索时切换到 `DrawFilteredResults()` 视图

### DrawMaterialBar()
```csharp
private void DrawMaterialBar()
```
- **用途**: 绘制材质选择工具栏
- **调用者**: `OnGUI()` 搜索栏之后
- **UI**: "Orig" 按钮（还原原始材质）+ Grid 材质 1-10 + 分隔 + Texture 材质 1-10，选中项高亮 Cyan
- **备注**: 材质预览图懒加载，通过 GetMatPreview 获取缩略图

### GetMatPreview()
```csharp
private Texture2D GetMatPreview(Material mat)
```
- **用途**: 获取材质的预览缩略图（懒加载 + 缓存）
- **参数**: `mat` — 材质资源
- **返回**: Texture2D 缩略图，或 null
- **调用者**: `DrawMaterialBar()`
- **缓存**: `_matPreviews` 字典，避免重复调用 AssetPreview

### DrawCategorySidebar()
```csharp
private void DrawCategorySidebar()
```
- **用途**: 绘制左侧分类列表
- **调用者**: `OnGUI()` 非搜索模式时调用
- **UI**: 垂直 ScrollView，每个分类显示名称 + Prefab 数量，选中项 boldLabel 高亮
- **备注**: 点击分类时重置 `_prefabScroll`

### DrawPrefabGrid()
```csharp
private void DrawPrefabGrid()
```
- **用途**: 绘制当前分类的 Prefab 缩略图网格
- **调用者**: `OnGUI()` 非搜索模式时调用
- **逻辑**: 根据窗口宽度自动计算列数，每行最多 `Mathf.FloorToInt(availableWidth / cellWidth)` 列
- **备注**: cellWidth = thumbnailSize(80) + 12

### DrawThumbnailCell()
```csharp
private void DrawThumbnailCell(SyntyPrototypeMenu.PrefabEntry entry)
```
- **用途**: 绘制单个 Prefab 缩略图格子
- **参数**: `entry` — Prefab 条目（含路径、分类、显示名、是否为 Polygon 类型）
- **调用者**: `DrawPrefabGrid()` 和 `DrawFilteredResults()`
- **UI**: 缩略图按钮 + 标签显示名称。Polygon 类型显示 "(P)" 后缀
- **交互**: 点击缩略图 → `PlaceWithMaterial(entry.path)`
- **降级**: 无预览图时用 `GUI.Box` 显示文本，鼠标事件手动检测

### DrawFilteredResults()
```csharp
private void DrawFilteredResults()
```
- **用途**: 绘制跨分类搜索匹配结果
- **调用者**: `OnGUI()` 当搜索框非空时调用
- **逻辑**: 遍历所有分类，收集包含搜索词的 Prefab，按分类分组显示
- **UI**: 每个分类标题 + 缩略图行，同 DrawThumbnailCell 渲染

### GetPrefabRef()
```csharp
private GameObject GetPrefabRef(string path)
```
- **用途**: 懒加载 Prefab 引用（带缓存）
- **参数**: `path` — Prefab 在项目中的路径
- **返回**: 加载的 GameObject，或 null
- **调用者**: `DrawThumbnailCell()`
- **缓存**: `_prefabRefs` 字典，避免重复 AssetDatabase.Load

## 私有字段

```csharp
private List<SyntyPrototypeMenu.CategoryData> _categories;        // 分类数据
private int _selectedCategoryIndex;                                 // 当前选中分类
private string _searchFilter = "";                                  // 搜索过滤文本
private Vector2 _categoryScroll;                                    // 分类侧栏滚动
private Vector2 _prefabScroll;                                      // Prefab 网格滚动
private readonly float _thumbnailSize = 80f;                       // 缩略图尺寸
private List<Material> _materials;                                  // 材质列表
private int _selectedMatIndex = -1;                                 // 选中材质 (-1=原始)
private readonly Dictionary<string, GameObject> _prefabRefs = new(); // Prefab 引用缓存
private readonly Dictionary<Material, Texture2D> _matPreviews = new(); // 材质预览缓存
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 收藏/最近使用功能 | 待做 | 代码分析 |
| 多选批量放置 | 待做 | 代码分析 |
