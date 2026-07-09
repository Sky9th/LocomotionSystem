# 短期开发计划

> 更新: 2026-07-08 | 分支: `feature/phase5-item-economy`
> 聚焦: Phase 5 物品经济（P5.1 → P5.5）
> 长期路线: 见 `.agent/plans/long-term.md`

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

---

### P5.2 — 玩家能拾取物品

> 玩家点击地上的物品→物品从地面消失→出现在背包里。

| # | 任务 | 说明 |
|---|------|------|
| P5.2a | **拾取交互** | 鼠标射线检测 GroundItem → 触发拾取 |
| P5.2b | **跨容器转移** | 物品从地面 → 角色背包容器（原子操作，失败回滚） |

**已有基础**: RdContainer.Place/Remove/CanAccept、CharacterBuildContext.Container、InventoryQuery

---

### P5.3 — 玩家能看到背包

> 按 Tab 打开背包面板——物品图标、名称、数量、负重。

| # | 任务 | 说明 |
|---|------|------|
| P5.3a | **背包面板 UI** | UIScreen 网格显示背包物品 |
| P5.3b | **物品图标** | ItemDefSO 图标字段（PropertyTree 或 C#） |
| P5.3c | **负重显示** | 当前重量/负重上限 + 轻/中/重/超载 |

**已有基础**: WeaponBarOverlay、UIIconSlot、RdContainer.CurrentWeight

---

### P5.4 — 玩家能装备 / 卸下物品

> 背包拖武器到右手槽→武器出现在角色手上。拖回→武器消失。

| # | 任务 | 说明 |
|---|------|------|
| P5.4a | **装备槽 UI** | 背包面板旁装备槽（右手/左手/头/胸/腿/脚） |
| P5.4b | **装备操作** | 背包↔装备槽 物品转移 |

**已有基础**: CharacterEquipment.SyncEquipment()、SlotBoneMapper、WeaponAttachPoint

---

### P5.5 — 玩家能使用消耗品

> 右键绷带→消耗→HP 恢复。右键食物→消耗→饥饿恢复。

| # | 任务 | 说明 |
|---|------|------|
| P5.5a | **消耗品使用** | 右键触发 Use → 读 EffectSO[] → AbilityReactor 执行 |
| P5.5b | **数量变化** | StackCount--，归零时物品销毁 |

**已有基础**: PropertyPresetSO.GetDamageEffects()、AbilityReactor.Resolve()、Entity.StackCount

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
