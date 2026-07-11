# PropertyPresetSO — 属性预设基类

> `L3_Properties/Definition/PropertyPresetSO.cs` · 技术文档 · 2026-06-26
> **Last Verified**: 2026-07-11 | **Verification**: _contentId field added. All referenced files exist.

## 层级定位

L3 资产层。PropertyPresetSO 是 PropertyTreeSO（结构）与实例之间的桥梁——它绑定「这个实体用哪棵属性树」和「这个变种覆写了哪些默认值」。本身不存属性值——displayName/icon/description 等已在 Tree 的属性节点上。

## 调用链

```
被谁调:
  PropertyComponent.Awake()   → new PropertyTable(_def)

调谁:
  PropertyTreeSO.ResolveStructure()  → 由 PropertyTable 构造时调用
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | PropertyTreeSO | Template 字段引用 |
| 被消费 | PropertyComponent | 初始化时传入 PropertyTable 构造 |
| 子类 | GearDefSO | 装备定义（已存在，待迁移到 PropertyPresetSO） |
| 子类 | BuildingDefSO | 建筑定义（远期） |

## 字段

### Template
```csharp
public PropertyTreeSO Template;
```
- **用途**: 指向该实体使用的属性树（如 Zombie、Pistol、Human）
- **备注**: 同一棵 Tree 可被多个 PropertyPresetSO 共用（如 TankZombie.asset 和 FastZombie.asset 都指向 Zombie Tree）

### OverridesJson
```csharp
public string OverridesJson;
```
- **用途**: 变种覆写 JSON。覆写 Tree 中声明的属性的默认值
- **格式**: `{"Overrides":[{"Path":"Vitals/HP","Value":"300"},{"Path":"Vitals/Speed","Value":"1.5"}]}`
- **优先级**: OverridesJson > PropertyDefSO.Default。运行时传入的额外覆写 > OverridesJson

### Prefab
```csharp
public GameObject Prefab;
```
- **用途**: 实体 GO 载体 Prefab。EntityService.Spawn 时 Instantiate
- **备注**: 不同 Preset 指向不同 Prefab（Player.prefab、NPC.prefab、GroundItem.prefab 等）

### _contentId
```csharp
[SerializeField, HideInInspector]
private string _contentId;
public string ContentId => _contentId;
public void SetContentId(string id) => _contentId = id;
```
- **用途**: Mod 可寻址的稳定标识符。格式为完整 itemPath（`Entity.Equipment.Weapon.Melee.Blade.machete`），不含命名空间前缀
- **写入时机**: Editor Save（`EntityEditorWindow.Save`）+ JSON Import（`EntityImporter.SyncContentId`）。运行时只读
- **备注**: `AssetCatalog.InitPresets` 读取后加 `CommonConstants.OfficialNamespace` 前缀作为 `_byContentId` 的 key

## 设计决策

| 决策 | 原因 |
|------|------|
| 抽象基类，不直接实例化 | 每种实体类型有各自的机械规则字段（slots、spawnBehavior），需要子类承载 |
| Template + OverridesJson 两个字段 | 分离「有什么属性」（Tree）和「值差多少」（Overrides），职责清晰 |
| 不放属性值字段（displayName 等） | 这些是 Tree 上的属性节点，通过 OverridesJson 覆写即可 |
| CreateAssetMenu 在子类定义 | PropertyPresetSO 是 abstract，不能直接创建资产 |

