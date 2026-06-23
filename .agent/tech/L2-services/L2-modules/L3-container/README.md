# L3_Container · 容器系统

> `L3_Container/` — 独立模块。通用容器抽象——管理物品的放置、取出、过滤和嵌套。身体装备槽、背包、技能栏共享同一套容器逻辑。

> ⚠ **DRAFT** — 未定稿。设计方向已确定，细节仍在讨论中。
>
> **Last Verified**: 2026-06-23 | **Verification**: DESIGN PHASE — 代码尚未创建

## 层级定位

独立 L3 模块，位于 `Services/Modules/L3_Container/`。是比 `L3_Item` 更基础的层——物品系统定义"是什么"，容器系统回答"在哪"。

不是管线系统。没有 Update、没有 Tick。纯粹的放置/取出/查询。

## 核心

**容器是物品的"位置"。物品放进容器获得状态——放进身体槽=装备，放进背包=物品，放进技能槽=可用技能。**

"装备"不是物品的类型——是物品在特定容器中的状态。同一把刀，在身体槽是装备，在背包是物品。

## 架构

```
Container<T>
  ├── 槽位标识: string key          ← "右手", "头部", "背包主空间"
  ├── 过滤规则: GameplayTag[]        ← 接受什么类型的 T
  ├── 容量: int                      ← 1（装备槽）或 N（背包）
  ├── 占用: T?                       ← 当前装了什么
  │
  ├── CanAccept(T item) → bool       ← 过滤检查
  ├── Place(T item) → bool           ← 放入
  ├── Remove() → T?                  ← 取出
  └── IsEmpty → bool

容器嵌套:
  身体槽（Container<ItemDefSO>）
    └── 背包装甲（ItemDefSO，ItemTags 含 Equipment.Backpack）
          └── 自身的子槽位（GearSlot[]，由容器系统管理）
```

## 容器类型

所有容器都是同一个 `Container<T>`。区别只在 T 的类型、过滤规则和容量：

| 容器 | T | 过滤 | 容量 | 额外规则 |
|------|---|------|------|---------|
| 右手 | ItemDefSO | Weapon.Blade, Weapon.Pistol, Tool... | 1 | 可与左手联合占用（双手） |
| 头部 | ItemDefSO | Armor.Head | 1 | — |
| 背包主空间 | ItemDefSO | 无（重量限制） | N | 总重量 ≤ 上限 |
| 背包武器挂槽 | ItemDefSO | Weapon.Rifle, Weapon.Shotgun | 2 | — |
| 技能槽 Q | AbilityDefSO | 武器Tag ∩ 技能树 | 1 | — |
| 世界箱子 | ItemDefSO | 无 | N | — |

## 调用链

```
物品放入:
  container.CanAccept(item) → true/false    ← 过滤检查（容器负责）
  container.Place(item)                      ← 放入（容器负责）

物品取出:
  item = container.Remove()                  ← 取出（容器负责）

物品移动（跨容器）:
  Registry.Transfer(itemId, fromSlot, toSlot) → bool
    内部: fromSlot.Remove() → Untrack → toSlot.Place() → Track
    Place 失败 → 回滚
  容器不知 Registry 存在，只负责 Place/Remove/CanAccept。
```

## 容器所有者与 Tick

容器不负责 Tick。**容器所有者**在 Update 中遍历容器物品并调用 `item.Tick(dt)`。不同所有者可以用不同频率。

```
CharacterActor.Update()           ← 每帧 Tick 装备槽和背包物品
WorldManager.Update()             ← 0.5Hz Tick 世界箱子中的物品
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| Container\<T\> | L3_Item（ItemInstance） | 存放物品实例 |
| Container\<T\> | L3_Ability（AbilityDefSO） | 技能槽存放技能定义 |
| Container\<T\> | EquipmentComponent | EquipmentComponent 是身体容器的管理器 |
| Container\<T\> | UI（装备栏/物品栏/技能栏） | 展示容器内容 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 泛型 Container\<T\> | 身体槽装 ItemDefSO、技能槽装 AbilityDefSO——同一个结构，不同 T |
| 容器不负责 Tick | 不同容器所有者需要不同 Tick 频率。角色 60fps，箱子 0.5Hz |
| 过滤用 GameplayTag | 武器类型、护甲类型不需要枚举——Tag 匹配即可 |
| 嵌套容器 | 背包装甲自身有子容器。容器树表达嵌套关系 |
| 装备是状态不是类型 | 物品在身体槽容器里 = 装备态。同一个 ItemDefSO |
| 没有 ItemManager | 容器管理物品位置，Registry 管理物品身份。不需要第三个管理者 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| Container\<T\> 代码实现 | 待做 | — |
| EquipmentComponent | 待做 | Container\<T\> |
| 背包嵌套容器 | 远期 | Container\<T\> + GearSlot[]（ItemTags 标记容器能力） |
| 技能栏容器 | 远期 | Container\<T\> + RoutineSO |
| 世界容器（箱子/地面） | 远期 | Container\<T\> + WorldManager |

## 子文档索引

| 文档 | 说明 |
|------|------|
| （待创建）container.md | Container\<T\> — 泛型容器，API 与过滤规则 |
| （待创建）container-slot.md | ContainerSlot — 容器+槽位引用，供 Registry 索引 |
