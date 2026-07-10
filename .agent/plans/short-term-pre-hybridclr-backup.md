# 短期开发计划

> 更新: 2026-07-09 | 分支: `feature/phase5-item-economy`
> 聚焦: 前置准备（Mod 补课）+ Phase 5 物品经济（P5.1 → P5.5）
> 长期路线: 见 `.agent/plans/long-term.md`
> Mod 框架: 见 `.agent/tech/mod-architecture-framework.md`

---

## 前置准备 — Mod 架构补课

> Mod 框架已定稿。以下是项目如果在 Day 1 就有 Mod 约束应该做的事——现在补上。**和 Phase 5 并行推进，不阻塞物品经济开发。**

### 🔴 P0：PropertyPresetSO 加 contentId 字段

**为什么是 P0**：Phase 5 正在建物品。不现在做，以后 15 个子类 + 50+ 资产批量重命名。

| 步骤 | 操作 | 文件 |
|------|------|------|
| P0.1 | `PropertyPresetSO` 加 `public string contentId;` | `L3_Properties/Definition/PropertyPresetSO.cs` |
| P0.2 | Editor 中 contentId 默认 = asset name（迁移兼容） | `EntityEditorWindow` 基类 |
| P0.3 | 新物品按 `category.subcategory.name` 规范填写 contentId | Phase 5 每个新 SO 创建时 |
| P0.4 | P5.0 已有 49 件物品 contentId 回填 | 批量赋值规范 ID（当前默认 = asset name，如 `CannedBeans` → `item.consumable.food.canned_beans`） |

**命名示例**：
```
MeleeWeapon:   item.weapon.melee.iron_sword
RangedWeapon:  item.weapon.ranged.pistol.glock_17
Throwable:     item.weapon.thrown.frag_grenade
Armor Head:    item.armor.head.steel_helmet
Armor Body:    item.armor.body.steel_vest
Armor Leg:     item.armor.leg.steel_greaves
Ammo:          item.ammo.pistol.nine_mm           ← FMJ/JHP 是 StatOverride，不进 contentId
Tool:          item.tool.axe_steel
Container:     item.container.backpack_hiking
Consumable:    item.consumable.medical.bandage
Food:          item.consumable.food.canned_beans
Material:      item.consumable.material.wood_plank
Character:     entity.human.player_male
Zombie:        entity.zombie.runner
AbilityTree:   ability.innate.human_base
Building:      building.defense.wooden_wall       ← Phase 7，约定先行
SceneItem:     sceneitem.furniture.wooden_chair    ← Phase 7，约定先行
```

### 🟡 P1：AssetCatalog 改为 contentId 查找

> ⚠️ 2 session。AssetCatalog 有 8 个注册表，需分批切换。

| 步骤 | 操作 | 文件 |
|------|------|------|
| P1.1 | Items + Characters 注册表 contentId 优先，asset name fallback | `AssetCatalog.cs` |
| P1.2 | 其余 6 个注册表 contentId 优先（PropertyTrees/AnimProfiles 等） | `AssetCatalog.cs` |
| P1.3 | `PlayerService` 中硬编码引用改为 contentId | `PlayerService.cs`（`"Human"`, `"Blade"` 等 ~5 处） |

### 🟡 P1：Entity 加 ContentId 字段

| 步骤 | 操作 | 文件 |
|------|------|------|
| P1.4 | `Entity` 加 `public string ContentId { get; }` | `Entity.cs` |
| P1.5 | 构造函数/Register 时从 Preset.contentId 赋值 | `EntityService.cs` |

### 🟡 P1：AbilityTreeSO 加 contentId

| 步骤 | 操作 | 文件 |
|------|------|------|
| P1.6 | `AbilityTreeSO` 加 `public string contentId;`（保留 `treeId` fallback） | `AbilityTreeSO.cs` |

### 🟢 P2：Phase 1-4 关键类审查

> 不改代码，只逐项确认。标注"需修改"的条目。

| 审查项 | 模块 | 确认什么 |
|--------|------|---------|
| EffectSO | L3_Ability | base class 是否 `sealed`？——预期 ✅ virtual |
| Ability 管道状态 | L3_Ability | 8 状态类是否 `sealed`？StateMachine 能否注册新状态？ |
| AbilityReactor 回调 | L3_Ability | 5 回调 delegate 数量是否固定？Mod 能否加新回调？ |
| **AbilityReactor sealed** | L3_Ability | ⚠️ 已确认 `sealed`——**需修改**（Mod 框架要求不 sealed） |
| **AbilityExecutor sealed** | L3_Ability | ⚠️ 已确认 `sealed`——**需修改**（同上） |
| RdContainer | L3_Container | 操作 Entity 具体类型——Mod 自定义 Entity 子类能否兼容？ |
| CharacterEquipment | L3_Character | SyncEquipment 槽位从 `SlotDef[]` 来（数据驱动 ✅），但槽位定义在 CharacterBuildContext——是否 hardcode？ |
| Phase 2.5 Rule 基类 | L3_Character | 类名可能不是 `*Rule`——需先定位实际文件，再确认 sealed/可注册性 |
| HitReaction | L3_Character | ⚠️ DriverArbiter 抢占规则已确认 hardcode——**需修改**（提取为 IAnimationArbiter 接口） |
| L2 Service public API | L2 全服务 | EntityService/PlayerService/AssetService 等——暴露了哪些 internal 实现？ |
| Character 模块 sealed | L3_Character | 18+ `internal sealed` 类——哪些将来需升为 public？ |

### ⚪ P3：远期的锦上添花

> S2 根据社区反馈做。不改代码，只标注。

- 管道状态机可扩展性增强
- Locomotion/生存指标公式 → delegate
- 装备槽位可注册
- AbilityReactor / AbilityExecutor 解 sealed（破坏性变更，需评估影响）

### ⚪ P3：远期的锦上添花

> S2 根据社区反馈做。不改代码，只标注。

- 管道状态机可扩展性增强
- Locomotion/生存指标公式 → delegate
- 装备槽位可注册

---

## 当前进度

```
Phase 4 ✅ ──── 战斗/动画/属性/技能管线全封闭
    │
Phase 5 🔄 ──── 物品经济
    ├── P5.0 基础设施 + 道具数据 ✅ (v0.42.0)
    ├── P5.1 物品能存在于世界上 ← 当前
    ├── P5.2 玩家能拾取物品
    ├── P5.3 玩家能看到背包
    ├── P5.4 玩家能装备/卸下物品
    └── P5.5 玩家能使用消耗品
```

---

## Phase 5 — 物品经济

> 从玩家视角：探索世界→发现物品→捡起来→装备/使用→管理负重。

### 玩家故事

| # | 玩家能做什么 | 为什么重要 |
|---|------------|----------|
| P5.1 | 在地上看到物品 | 世界有东西可以交互——物品经济的第一步 |
| P5.2 | 点击物品捡起来 | 从"看到"到"拥有"——最基本的采集循环 |
| P5.3 | 打开背包看到所有物品 | 拥有感——"这是我的东西" |
| P5.4 | 把武器装备到手上 / 卸下来 | 武器显示在角色手上——视觉反馈 |
| P5.5 | 使用消耗品（食物/绷带） | 物品产生效果——闭环完成 |

---

### P5.0 — 基础设施 + 道具数据 ✅

> v0.42.0。Equipment/Ammo/Consumable 三个 Editor + ImportExport 已就位，49 件道具数据全量落地。

---

### P5.1 — 物品能存在于世界上

> 玩家走进房间，地上有物品。它们有坐标，能被看到。

| # | 任务 | 说明 |
|---|------|------|
| P5.1a | **GroundItem 组件** | 新建 L3 组件：泛型地面物品视图，从 Entity 数据构建 3D 模型，fallback 立方体解决空 Prefab |
| P5.1b | **EntityService 改造** | 按 `preset is CharacterDefSO` 分流——角色走原有 Prefab，物品走 GroundItem 泛型 Prefab |
| P5.1c | **GroundItem.prefab** | Unity Editor 手工创建：Identity + GroundItem + SphereCollider(isTrigger) + fallback Mesh |
| P5.1d | **ItemService 创建** | L2 服务：EntityId→世界坐标索引 + SpawnWorldItem + GetNearbyItems |
| P5.1e | **测试验证** | PlayerService.SpawnTestEntities 中生成一个 CannedBeans 在地上，验证 3D 模型可见 |

**详细计划**: `.agent/plans/p5.1-world-item-spawn.md`

**已有基础**: EntityService 事件管线、Identity 组件、PropertyPresetSO.Prefab 字段、AssetCatalog.FindItem

**Mod 义务**：ItemService 公开 API 走 `IItemReadOnly` 接口，不暴露 PropertyTable 内部。测试代码用 contentId 而非 asset name（`FindItem("item.consumable.food.canned_beans")`）。

---

### P5.2 — 玩家能拾取物品

> 玩家点击地上的物品→物品从地面消失→出现在背包里。

| # | 任务 | 说明 |
|---|------|------|
| P5.2a | **拾取交互** | 鼠标射线检测 GroundItem → 触发拾取 |
| P5.2b | **跨容器转移** | 物品从地面 → 角色背包容器（原子操作，失败回滚） |

**已有基础**: RdContainer.Place/Remove/CanAccept、CharacterBuildContext.Container、InventoryQuery

**Mod 义务**：跨容器转移是引擎操作——不暴露给 Mod，但转移结果通过 event 通知（Mod 可订阅）。

---

### P5.3 — 玩家能看到背包

> 按 Tab 打开背包面板——物品图标、名称、数量、负重。

| # | 任务 | 说明 |
|---|------|------|
| P5.3a | **背包面板 UI** | UIScreen 网格显示背包物品 |
| P5.3b | **物品图标** | ItemDefSO 图标——走 PropertyTree AssetRef 节点（非 C# 字段，Mod 可覆写） |
| P5.3c | **负重显示** | 当前重量/负重上限 + 轻/中/重/超载 |

**已有基础**: WeaponBarOverlay、UIIconSlot、RdContainer.CurrentWeight

**Mod 义务**：背包 UI 数据接口不暴露 PropertyTable 内部——通过只读接口获取显示用属性。

---

### P5.4 — 玩家能装备 / 卸下物品

> 背包拖武器到右手槽→武器出现在角色手上。拖回→武器消失。

| # | 任务 | 说明 |
|---|------|------|
| P5.4a | **装备槽 UI** | 背包面板旁装备槽（右手/左手/头/胸/腿）——5 槽，匹配 PropertyTree 4 护甲类 + 2 武器槽 |
| P5.4b | **装备操作** | 背包↔装备槽 物品转移 |

**已有基础**: CharacterEquipment.SyncEquipment()、SlotBoneMapper、WeaponAttachPoint

**Mod 义务**：装备操作的 EffectSO 调用链预留 virtual。SyncEquipment 槽位来自 SlotDef[]（✅ 数据驱动），但 CharacterBuildContext 中槽位定义需确认非 hardcode。

---

### P5.5 — 玩家能使用消耗品

> 右键绷带→消耗→HP 恢复。右键食物→消耗→饥饿恢复。

| # | 任务 | 说明 |
|---|------|------|
| P5.5a | **消耗品使用** | 右键触发 Use → 读 EffectSO[] → AbilityReactor 执行 |
| P5.5b | **数量变化** | StackCount--，归零时物品销毁 |

**已有基础**: PropertyPresetSO.GetDamageEffects()、AbilityReactor.Resolve()、Entity.StackCount

**Mod 义务**：EffectSO 基类已 virtual ✅，但注意叶子类（BuffEffectSO/DamageEffectSO 等）是 sealed——Mod 只能新建 EffectSO 子类，不能继承已有效果类型。

---

## 后续预览

Phase 5 完成后进入 **Phase 6 — 生存威胁**（见 `long-term.md`）：
- 时间 + 昼夜
- 生存指标（饥饿/口渴/体力/HP）
- 僵尸 AI（视线检测 + 追击 + 攻击）

---

## 不纳入短期

- 地图 / LootSystem / 建造 → Phase 7
- NPC / 农业 / 烹饪 → Phase 8
- 科技树 / 尸潮 / 工具 → Phase 9
- 存档 / 收尾 → Phase 10
- 角色创建 UI / 技能槽溢出 / PropertyType.Struct
