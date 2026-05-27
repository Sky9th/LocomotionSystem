# 2026-05-27 Synty Prototype 场景搭建工具

## 改动

- 新建 `SyntyPrototypeMenu.cs` — 扫描 PolygonPrototype 预制体，按类型自动分为 14 个分类
- 新建 `SyntyPrototypeBrowser.cs` — 可停靠 EditorWindow，缩略图网格 + 搜索 + 材质选择
- 扫描目录从 Buildings 扩展到 Props / Primitives / Generic / Vehicle，跳过武器和 FX
- PolygonPrototype 从 `Assets/External/Synty/` 移动到 `Assets/Art/PolygonPrototype/`
- PlayerService 修复：`"NewGame"` → `"Core"` 反转判断，所有非 Core 场景均可生成角色

## 技术细节

- `SyntyPrototypeMenu.GetCategories()` 扫描 7 个子目录，按文件名前缀归类（Wall*/Floor*/Stairs*/Ramp*/Roof*/Rail*/Column*/Block*/Door*/Window*/Ladder → Ladders；Tree/Bush → Environment；SM_Primitive_ → Primitives；SM_Generic_ → Environment；SM_Veh_ → Vehicles；SM_Switch_ → Props；其余 SM_Prop_ → Props）
- 武器类（Bat/BoostPad/C4/Knife/Pistol/Rifle/Sword）和 FX 自动跳过
- `InstantiateByPath()` 使用 `PrefabUtility.InstantiatePrefab`，保持 Prefab 连接，支持 Undo，创建到选中父节点下
- Browser 使用 `AssetPreview.GetAssetPreview()` 显示缩略图，`GetWindow<>()` 实现可停靠
- 材质选择条扫描 `PolygonPrototype_Global_Grid_01~10` 和 `PolygonPrototype_Texture_01~10`，选中后自动应用到创建物体

## 已知问题

- 转身 180° 时有概率速度异常慢，未定位根因
