# 短期开发计划

> 更新: 2026-07-08
> 分支: `feature/phase5-item-economy`
> 原则: 每步有可玩增量，先完成基础设施再铺玩法
> 前置: Character 模块重构 ✅ · Properties 系统 ✅ · Animation 重构 ✅ · Ability 数据资产 ✅ · AbilityTreeSO ✅ · EntityService + Container ✅ · Tag 6 域 339 标签 ✅ · Equipment→技能闭环 ✅ · PropertyTree Equipment 层重构 ✅ · Ability Pipeline 8 State 全就位 ✅ · S1-S5 全部完成 ✅ · 道具数据 49 件落地 ✅

---

## Phase 4 已完成（2026-06 ~ 2026-07-07）

<details>
<summary>S1-S5 全部完成，点击展开</summary>

| # | 阶段 | 核心产出 |
|---|------|---------|
| S1 | Properties 深度接入 | Physique 删除，GroundLocomotion 公式化（Agility × bonus − WeightPenalty） |
| S2 | 装备→技能闭环 | Entity→Container→GripTags→AbilityForest→ResolvedActives→PlayerDirector→Q 键释放 |
| S3 | Ability Pipeline 8 State | Gating→Cost→Windup→Cooldown→Execution→Recovery→Completed/Rejected，ref TContext 零拷贝 |
| S4 | Combat 管线补完 | 属性修正（Strength）、OnHit 通知通路、Damage 类型拆分（per-channel DamageEntry[] + 公式 base×(1+Σ%)+Σadd） |
| S5.0 | 受击动画管线 | HitReactionDriver + DriverArbiter 抢占（H2 抢占一切）+ LocomotionAnimationSetSO 4 hitReaction 字段 |
| S5.4 | AirLand 分级落地 | Gait 驱动 LinearMixer.State.Parameter（0=Idle, 1=Walk, 2=Run/Sprint） |
| S5.5 | Traversal 动画迁移 | LocomotionAnimationSetSO traversal + DotProduct 方向验证（dot > 0.8 正面顶墙） |
| — | 被动技能管线 | AbilityForest→SyncInstances + OnEquip/OnHit/OnKill/OnDamaged dispatch + PassiveBarOverlay |
| — | 伤害飘字 | DamageNumberOverlay + DamageNumberWidget |
| — | Pathfinding 缓存 | GraphCache.bytes 尊重，跳过冗余 Scan() |

🔒 延后：S4.5 自伤、S5.1 Footstep、S5.2 HeadLook IK、S5.3 Crawl（俯视角优先级低）

</details>

---

## 低优先级 / 技术债

> 不阻塞当前里程碑，远期处理。

| # | 事项 | 阻塞原因 |
|---|------|----------|
| L1 | Avoidance/Mitigation/Absorption 三阶段拆分 | 回避/护盾系统未就位 |
| L2 | 回避判定 — 闪避率 + 短路 | 闪避属性/装备系统 |
| L3 | 吸收结算 — 护盾伤害吸收 | 护盾系统未设计 |
| L4 | 霸体阈值判定 | 霸体值属性体系 |
| L5 | ComputeDamage 交叉乘积按 element tag 匹配 | — |
| L6 | RangedWeaponSO 临时 SO 泄漏 | ✅ 已修复 — v0.42.0 沿容器链查找弹药返回 DamageEffectSO |
| L7 | AddBuffTags 默认 owner=null | — |
| L8 | Reactor ApplyEffects 确认 public/private | — |
| L9 | 伤害类型转换 — 防弹衣穿刺→钝伤 | 防弹衣系统未就位 |

---

## 不纳入短期计划

- 建造基础（Phase 6）
- 时间日夜（Phase 7）
- 农业烹饪 / NPC / 尸潮 / 科技树（Phase 8-11）
- 扩展打磨 — 连招 / 投射物 / 噪音连锁 / 特殊感染者 / 丧尸化（Phase 12+）
- 角色创建 UI
- 技能槽溢出处理（actives > 4 排序）
- PropertyType.Struct（GrantedAbilityTrees 远期迁移）

---

## 优先级依赖

```
S1-S5 ✅ 全部完成 ──── Phase 4 封闭
    │
    └── Phase 5 — 物品经济 [施工中]
         ├── P5.0 基础设施 + 道具数据 ✅ (v0.42.0)
         ├── P5.1 物品能存在于世界上 ← 下一步
         ├── P5.2 玩家能拾取物品
         ├── P5.3 玩家能看到背包
         ├── P5.4 玩家能装备/卸下物品
         ├── P5.5 玩家能使用消耗品
         └── P5.6 游戏能存档/读档
```

---

## Phase 5 — 物品经济 [施工中]

> 从玩家视角出发：这是一个俯视角生存游戏。玩家探索世界→发现物品→捡起来→装备/使用→管理负重→存档回家。

### 玩家故事

| # | 玩家能做什么 | 为什么重要 |
|---|------------|----------|
| P5.1 | 在地上看到物品 | 世界有东西可以交互——这是物品经济的第一步 |
| P5.2 | 点击物品捡起来 | 从"看到"到"拥有"——最基本的采集循环 |
| P5.3 | 打开背包看到所有物品 | 拥有感——"这是我的东西" |
| P5.4 | 把武器装备到手上 / 卸下来 | 武器显示在角色手上——视觉反馈 |
| P5.5 | 使用消耗品（食物/绷带） | 物品产生效果——闭环完成 |
| P5.6 | 存档 / 读档 | 进度不丢失——这是游戏，不是 demo |

---

### P5.0 — 基础设施 + 道具数据 ✅

> ✅ v0.42.0 完成。Equipment/Ammo/Consumable 三个 Editor + ImportExport 已就位，49 件道具数据全量落地。

| # | 任务 | 状态 |
|---|------|------|
| P5.0a | ItemEditor 窗口 | ✅ Equipment/Ammo/Consumable 三个独立 EditorWindow |
| P5.0b | ItemImportExport | ✅ 三个 Import-Export + JSON 文件，EntityImporter 支持按 entityType 分子目录，PropertyImporter 支持 update 已有 Tree |
| P5.0c | Addressables 兼容 | ✅ 验证通过 |
| P5.0d | **道具数据落地** | ✅ 49 件成品道具（防具10+容器3+近战6+热武7+弹药12+消耗品11），Ballistic DamageEffectSO x4，AmmoBase PropertyTree + Weapon/ATK，AmmoSO/RangedWeaponSO 伤害管道接通 |

**产出**：`equipment_all.json`(26) + `ammo_all.json`(12) + `consumable_all.json`(11)，PolygonApocalypse Prefab 19 个映射，tags_all.json fullTag 补全

### 任务拆解

#### P5.1 — 物品能存在于世界上

> 玩家走进一个房间，地上有一把剑、三根绷带。它们有物理位置，能被看到。

| # | 任务 | 说明 |
|---|------|------|
| P5.1a | **ItemService 创建** | L2 服务：物品身份索引（物品在哪）+ 物品生命周期 |
| P5.1b | **世界物品容器** | 地面物品放进一个隐式的"世界容器"，统一管理世界上的所有物品 |
| P5.1c | **首次物品创建** | 用已有 `ItemDefSO` + `EntityService` 创建物品 Entity，GO 显示在世界坐标 |

**已有基础**：`ItemDefSO`（零字段 PropertyPresetSO）、`EntityService.Register/Spawn`（创建 GO 在世界上）、`Entity.NestedContainer`（物品自身容器）

#### P5.2 — 玩家能拾取物品

> 玩家右键点地上的剑→剑从地面消失→出现在背包里。

| # | 任务 | 说明 |
|---|------|------|
| P5.2a | **拾取交互** | 检测玩家点击/靠近世界物品 → 触发拾取 |
| P5.2b | **跨容器转移** | 物品从世界容器 → 角色背包容器（原子操作，失败回滚） |

**已有基础**：`RdContainer.Place/Remove/CanAccept`、`CharacterBuildContext.Container`（角色身体容器）、`InventoryQuery`（背包读接口）

#### P5.3 — 玩家能看到背包

> 按 Tab 打开背包面板——看到物品图标、名称、数量、当前负重。

| # | 任务 | 说明 |
|---|------|------|
| P5.3a | **背包面板 UI** | 新建 UIScreen，网格显示背包物品 |
| P5.3b | **物品图标** | ItemDefSO 需要图标字段（PropertyTree 表达或 C# 字段） |
| P5.3c | **负重显示** | 当前重量/负重上限 + 等级标签（轻/中/重/超载） |

**已有基础**：`WeaponBarOverlay`（武器栏 HUD 参考）、`UIIconSlot` 组件、`RdContainer.CurrentWeight`

#### P5.4 — 玩家能装备 / 卸下物品

> 从背包拖武器到右手槽→武器出现在角色手上。拖回背包→武器从手上消失。

| # | 任务 | 说明 |
|---|------|------|
| P5.4a | **装备槽 UI** | 背包面板旁显示装备槽（右手/左手/头/胸/腿/脚） |
| P5.4b | **装备操作** | 背包槽↔装备槽 物品转移 |

**已有基础**：`CharacterEquipment.SyncEquipment()`（Container diff → GO 生成/销毁 + GripTag 同步）、`SlotBoneMapper` + `WeaponAttachPoint`（武器挂载到骨骼）

#### P5.5 — 玩家能使用消耗品

> 背包里右键绷带→绷带消耗→HP 恢复。右键食物→食物消耗→饥饿恢复。

| # | 任务 | 说明 |
|---|------|------|
| P5.5a | **消耗品使用** | 右键触发 Use → 读物品的 EffectSO[] → AbilityReactor 执行效果 |
| P5.5b | **消耗后数量变化** | Count--，Count==0 时物品销毁 |

**已有基础**：`PropertyPresetSO.GetDamageEffects()`（读物品效果）、`AbilityReactor.Resolve`（执行效果）、`Entity.StackCount`

#### P5.6 — 游戏能存档 / 读档

> 暂停菜单点"保存"→游戏状态写入磁盘。标题画面点"继续"→恢复上次状态。

| # | 任务 | 说明 |
|---|------|------|
| P5.6a | **存档数据结构** | 定义 `SGameSave`：玩家位置/属性、物品列表+位置、世界物品列表 |
| P5.6b | **序列化/反序列化** | PropertyTable → JSON、Entity 列表 → JSON |
| P5.6c | **存档 UI** | 暂停菜单"保存"/"加载"按钮、标题画面"继续"按钮 |

**已有基础**：`PropertyPresetSO.OverridesJson`（JSON 覆写模式）、`Entity.Id`（持久标识）
