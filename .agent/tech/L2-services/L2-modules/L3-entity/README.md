# L3_Entity — 实体数据层

> 独立 L3 模块。纯 C#，无 MonoBehaviour。被 Character、Item、Container 依赖。

## 层级定位

L3 数据层——游戏中最基础的"存在"概念。只依赖 L3_Properties（PropertyTable）。不依赖 Character、Item、Container、Ability。

## 子模块

| 文件 | 说明 |
|------|------|
| `Entity.cs` | 抽象基类 — Id + Properties + Tick |
| `EntityRegistry.cs` | 全实体注册表 — 按 Id 存/查/遍历 |
| `EntitySaveData.cs` | 存档结构 — Id + EntityType + PropertiesJson |

## 调用链

```
Entity 基类:
  CharacterEntity   → : Entity (Properties from BuildContext)
  ItemEntity        → : Entity (Properties from ItemDefSO)

EntityRegistry:
  EntityService     → 持有，提供跨模块查找
  SaveService       → 遍历 All 序列化存档
```

## 依赖

| 方向 | 模块 |
|------|------|
| 依赖 | L3_Properties.PropertyTable |

## 为何是 L3

Entity 自身是纯数据结构（Id + 数据引用），不协调任何服务。L2 的 EntityService 才是服务层。Entity 放在 L3——跟 PropertyTable 同级，是数据层的基础构件。

## 为何不是 ECS

Entity 持有 PropertyTable——不是空 ID。RedDust 的实体数量（角色个位数、物品上百、僵尸最多几百）不需要纯 ECS 的内存布局。PropertyTable 已是扁平字典存储，E 和 C 已合二为一。未来如有性能需求，可在 Entity.Id 上叠加 ECS 层，不影响现有架构。
