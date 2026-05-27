# Prototype Builder — Editor 场景搭建工具

## 概述

基于 Synty PolygonPrototype 资产包，通过 `Window > Synty Prototype` 打开的 EditorWindow，提供预制体浏览、分类、缩略图预览、材质选择和快速创建功能。

## 架构

```
SyntyPrototypeMenu (static, 扫描 + API)
  └── SyntyPrototypeBrowser (EditorWindow, UI)
```

- **Menu** 负责扫描 `Assets/Art/PolygonPrototype/Prefabs/` 下 7 个目录，按文件名前缀自动分类，提供 `InstantiateByPath()` 公共 API
- **Browser** 从 Menu 获取分类数据，展示缩略图网格、搜索过滤、材质选择条，响应点击创建

## 分类映射

| 分类 | 文件名前缀 |
|------|-----------|
| Walls | SM_Buildings_Wall* |
| Floors | SM_Buildings_Floor* |
| Stairs | SM_Buildings_Stairs* |
| Ramps | SM_Buildings_Ramp* |
| Roofs | SM_Buildings_Roof* |
| Railings | SM_Buildings_Rail* |
| Columns | SM_Buildings_Column* |
| Blocks | SM_Buildings_Block* |
| Doors & Windows | SM_Buildings_Door*, Window* |
| Ladders | SM_Prop_Ladder |
| Primitives | SM_Primitive_* |
| Props | SM_Prop_* (非 Tree/Ladder), SM_Switch_* |
| Environment | SM_Prop_Tree*, SM_Generic_* |
| Vehicles | SM_Veh_* |

跳过：Bat, BoostPad, C4, Knife, Pistol, Rifle, Sword (武器), SM_FX_* (特效)

## 材质系统

`SyntyPrototypeBrowser.PlaceWithMaterial()` 在创建后遍历 Renderer 替换 `sharedMaterial`：

- 默认 "Orig" — 保持预制体自带材质
- Grid 01~10 — `PolygonPrototype_Global_Grid_*.mat`
- Texture 01~10 — `PolygonPrototype_Texture_*.mat`

## 文件

| 路径 | 职责 |
|------|------|
| `Assets/Scripts/Editor/SyntyPrototypeMenu.cs` | 扫描、分类、Menu API |
| `Assets/Scripts/Editor/SyntyPrototypeBrowser.cs` | EditorWindow UI、缩略图、材质选择 |
