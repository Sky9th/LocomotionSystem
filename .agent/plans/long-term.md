# 长期开发计划

> 更新: 2026-07-07
> 来源: `.agent/design/` GDD + 子系统设计文档
> 原则: 每步有可玩增量，每个子系统先跑通基本闭环 → 全生态联通 → 数值统一规划
>      **设计文档不设具体数值，所有数值在系统骨架完成后从上至下统一规划**

---

## 当前进度

```
短期:  Ability Pipeline 运行时 (feature/ability-pipeline 分支) — ✅ Phase 4 封闭
  前置: Character 模块重构 ✅  Properties 系统 ✅  Animation 重构 ✅
        PropertyTree Equipment 层重构 ✅  Container 系统 ✅
  目标: Ability Pipeline 8 State + Combat 补完 + 动画补完 — 全部完成
  已完成:
    S1  Properties 深度接入 ✅（Physique 删除，GroundLocomotion 公式化）
    S2  装备→技能闭环 ✅（Entity→Container→GripTags→AbilityForest→ResolvedActives）
    S3  Ability Pipeline 8 State ✅（Gating→Cost→Windup→Cooldown→Execution→Recovery→Completed/Rejected）
    S4  Combat 管线补完 ✅（属性修正 + 路径常量 + OnHit 通路 + Damage 类型拆分）
    S5  动画系统补完 ✅（S5.0 受击动画 + S5.4 AirLand 分级 + S5.5 Traversal 迁移，S5.1-S5.3 延后）
    Pathfinding 缓存修复 ✅（GraphCache.bytes 尊重，跳过冗余 Scan）

已完成:
  角色运动 ✅    音效骨架 ✅    数值系统（Properties 替代旧 Stats SO）✅
  PropertyAgent + Modifier 管道 ✅  HUD UI ✅
  Module 系统 + 树形生命周期 ✅  ctx 全链路 ✅
  俯视角切换 ✅   A* 寻路集成 ✅  SO Event Channel ✅
  Ability 数据资产 ✅ (Search/Activation/Effect/Noise/Passive 全量)
  GameplayTag → rTag 199 标签 ✅  CharacterCombat 骨架 ✅
  Animation LinearMixer 统一 + In-line Transition ✅
  PropertyTree Equipment 层 + 6 分支文档 ✅
  Container AcceptTags 层级匹配 ✅
  受击反应管线 ✅ — SDamageInfo.ImpactEffect → HitReactionDriver
  受击动画数据层 ✅ — LocomotionAnimationSetSO 4 hitReaction 字段
  DriverArbiter 抢占规则 ✅
  被动技能管线 ✅ — AbilityForest→SyncInstances + OnEquip/OnHit/OnKill/OnDamaged dispatch
  FloatAdjunct Max 扩容 ✅ — MaxAdd/MaxMultiply
  PassiveBarOverlay ✅ — 15Hz 轮询被动技能栏

延期:
  S5.1 Footstep / S5.2 HeadLook IK / S5.3 Crawl — 俯视角优先级低
  S4.5 自伤 Amount=0 — 等实际自伤技能需求

设计完成:
  GDD ✅  伤病系统 ✅  噪音系统 ✅  负重/背包 ✅  死亡/存档 ✅
  Ability Pipeline 八维度管道 ✅ (2026-06-06)
  Properties 全量属性体系 ✅ (~185 PropertyDef / 30 Trees)
  受击反应系统 ✅ (2026-07-05)

---

## 施工历史（用于工期校准）

基于 `git log --date=short` 的实际日历日，非预估。

| 日期 | 事项 | commit 数 | 耗时 |
|------|------|-----------|------|
| 6/11 | Cost 标签体系 + Effect 标签接入 | 1 | 1天 |
| 6/12 | EditorForm + SearchEditor + ActivationEditor + 目录重组 | 3 | 1天 |
| 6/13 | Editor UI 组件化 + Ability 编辑器补全 + FloatAdjunct + BuffEffectSO | 3 | 1天 |
| 6/14 | EUI 组件架构重构（Slot/回调 + 令牌体系 + 四轮标准化） | 4 | 1天 |
| 6/15 | EditorTreeView 组件 + 5 个 Editor 迁移 + Card API 简化 + 删除废弃树 | 11 | 1天 |
| 6/15 | Character 配置集中到 CharacterActor + Model 运行时装配 | 2 | 同天 |
| 6/16-17 | Module 系统 + 树形生命周期 + ctx 全链路 + Service 标准化 | 3 | 2天 |
| 6/18-19 | Animation 重构（废弃 State 清理 + SO 重构 + FSM In-line Transition） | 3 | 2天 |
| 6/19 | PolygonApocalypse 武器导入 + Properties 接管角色物理 | 2 | 1天 |
| 6/30 | Ability Pipeline StateMachine 框架 + Gating/Cost State + Container AcceptTags 修复 | 1 | 1天 |
| 7/04 | 受击动画数据层 — LocomotionSet +4 Mixer2D + Importer 扩展 | 1 | 1天 |
| 7/05 | 受击反应管线 — ImpactEffect→Combat→Animation→Driver 全链路 | 1 | 1天 |
| 7/06 | S1 Physique 删除 + GroundLocomotion 公式化 + S2 装备闭环 + S3 RecoveryState + S4 Combat 补完 | 4 | 1天 |
| 7/07 | S5.4 AirLand 分级落地 + S5.5 Traversal 动画迁移 + Pathfinding 缓存修复 | 1 | 1天 |

**节奏特征**:
- 跨模块架构重构（Module 系统、Animation 重构）≈ **2 天**，约 3 个 commit
- Editor 工具整批迁移/重构（EditorTreeView、EUI 组件体系）≈ **1-2 天**，4-11 个 commit
- 单一模块深度改动（Properties 接管物理、Cost 标签）≈ **1 天**，1-2 个 commit
- 新建组件 + 消费者迁移（类似 AbilityComponent / AbilityDriver）在历史中没有直接对照，但 Module 系统（新建 4 类型 + 迁移 CharacterActor）是 2 天

---

## 视角与坐标系约定

游戏为**俯视角**（类似《僵尸毁灭工程》），所有空间计算在 **XZ 地面平面** 上进行。

| 系统 | 坐标系 |
|------|--------|
| 移动 | 右键点击地面 → A* Pathfinding 寻路，AIPath 驱动位移 |
| 角色朝向 | 寻路中 = `desiredVelocity.normalized`（移动方向）；静止 = 保持当前朝向 |
| 瞄准方向 | 鼠标光标 → 地面 Y=0 投影 → ModelRoot 到鼠标的 XZ 方向 |
| 技能释放 | Q/E 键 → 以角色前方（`ModelRoot.forward`）为攻击方向 |
| 敌人感知 | 2D 地面扇形（视觉）+ 圆形半径（听觉） |
| 寻路 | A\* Pathfinding Project（XZ 平面 GridGraph） |
| 噪音传播 | 球形半径（3D，视野判定在 XZ） |

---

## 子系统全景

```
RedDust
├── 1. 战斗系统      — 三层架构(SkillDefSO→CombatComponent→CombatDriver)、Q/E/R/F 四槽、GameplayTag 门控、命中判定管道、6 武器类型扩展
├── 2. 敌人 AI       — 丧尸感知(听觉+视觉)/追击/攻击、噪音连锁反应
├── 3. 伤病系统      — 5 伤害类型×3 严重度×2 部位、治疗流程、丧尸化过程
├── 4. 噪音系统      — 6 级噪音、4 种类型、障碍衰减、丧尸连锁反应、潜行降噪
├── 5. 资源系统      — 材料/工具/武器/消耗品/零件/知识 六大类、仓库
├── 6. 负重/背包     — 纯重量制、四级负重、软上限、背包可丢弃
├── 7. 建造系统      — 网格化建造、建筑耐久/破坏/维修、防御工事
├── 8. 农业系统      — 开垦/播种/生长/收获
├── 9. 烹饪系统      — 篝火/灶台、固定食谱、食物→士气联动
├── 10. NPC 系统     — Rimworld 式征召/指派、工作熟练度成长、永久死亡
├── 11. 死亡/存档    — 死亡=读档、手动+自动存档、NPC 永久损失
├── 12. 尸潮系统     — 伪随机触发、规模正相关、构成随机
├── 13. 科技树       — 图纸消耗型、六条核心线、前置条件 AND 逻辑
└── 14. 时间/环境    — 日夜交替、季节（后续）、天气（后续）
```

---

## 开发阶段

每个子系统分两阶段：**基本闭环**（能玩通的最小版本）→ **扩展**（丰富性与打磨）。

### Phase 4: Ability Pipeline + 敌人 AI + 噪音骨架 ✅ 已封闭

> 管道设计: [ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md)
> 施工计划: [short-term.md](short-term.md) — S1-S5 全部完成

**4.1 Ability Pipeline 运行时** ✅：AbilityExecutor（发送中枢 → ②③④⑤ 门控/释放/搜索/效果）+ AbilityReactor（接收中枢 → ⑥⑦ 结算/反应）+ AbilityDriver（③ Windup→Fire→Recovery 动画）+ ⑧ 事件广播。8 State 全就位，Q 键闭环测试通过，被动技能管线完整（OnEquip/OnHit/OnKill/OnDamaged dispatch）。受击反应全链路完成（SDamageInfo.ImpactEffect → CharacterCombat → DriverArbiter → HitReactionDriver）。

**4.1a 日志格式规范化** 🔒 延期 — 管道已完成，此项为独立 quality-of-life 改进，非阻塞。

**4.2 敌人 AI 基础** 🔒 延后到 Phase 5 — 复用 AbilityExecutor + AbilityReactor（纯类，不绑 Player），行为 FSM，听觉感知（消费 SNoiseEvent）+ 视觉感知。

**移入 Phase 5 的能力扩展**（依赖资源/装备系统）：连招系统 ComboWindow、Buff/Debuff（BuffEffectSO 已定义）、多武器切换、熟练度、Circle 搜索落地。

---

### Phase 5: 资源系统 + 装备落地 + 存档

> 设计文档: `inventory-weight.md` / `death-mechanics.md` / `equipment-system.md`
> Properties 已替代旧 Stats SO，装备属性直接进 PropertyTree。

| 子系统 | 基本闭环 |
|--------|----------|
| 物品数据 | ItemDefSO（ID/名称/图标/类型/重量/堆叠上限），引用 PropertyTree |
| 拾取交互 | 地面物品→点击拾取→进背包 |
| 背包 UI | 物品列表 + 当前负重/负重上限 + 负重等级标签 |
| 消耗品 | 食物回饥饿、水回口渴、绷带回 HP、止痛药降疼痛 |
| 负重 | 每物品有重量 → 总负重 vs 负重上限 → 轻/中/重/超载四级 |
| 存档 | 暂停菜单手动存档 + 据点自动存档 → 3 手动槽 + 1 自动槽 |
| **GearInstance 工厂** | `GearDefSO` Properties 迁移 → `GearInstance` 运行时个体 |
| **装备槽位** | `EquipmentComponent` + `AbilitySlotManager`（替换 Actor 临时槽位）|
| **战斗扩展** | Buff/Debuff（BuffEffectSO 已定义）、多武器切换、熟练度 |

**可玩增量**: 地上有东西→捡→负重增加→取舍。捡到新武器→装备→技能槽替换。吃食物回饥饿→存档回家。

**不做的**: 六大类完整分类、仓库系统、工具耐久/维修、大背包/军用背包。

---

### Phase 6: 建造基础

| 子系统 | 基本闭环 |
|--------|----------|
| 建造模式 | 按 B 进入建造、鼠标选位置、点击确认 |
| 基础建筑 | 木墙、木门、木地板（各 1 种） |
| 材料消耗 | 建造消耗木材（从背包扣除） |
| 建筑耐久 | 墙体有 HP，丧尸可攻击破坏 |

**可玩增量**: 采集木材→打开建造→放置墙壁→围出安全区→丧尸被挡在外面。

**不做的**: 高级材料（石头/金属）、工作台/家具、拆除回收、网格吸附优化。

---

### Phase 7: 时间与日夜

| 子系统 | 基本闭环 |
|--------|----------|
| 时间系统 | 游戏内时钟（24 分钟 = 1 天，加速演示） |
| 日夜光照 | Directional Light 旋转 + 强度/色温变化 |
| 视野限制 | 夜晚玩家视野缩小，丧尸视野不变 |
| ClockOverlay | HUD 显示当前时间和天数 |

**可玩增量**: 天会黑→视野变差→丧尸在暗处更危险→天亮恢复→昼夜节奏建立。

**不做的**: 四季、天气、温度系统、作物生长绑定（先做固定计时）。

---

### Phase 8: 农业 + 烹饪

| 子系统 | 基本闭环 |
|--------|----------|
| 农业 | 锄头开垦→播种（1 种作物）→固定天数生长→收获 |
| 烹饪 | 篝火设施→1 个基础食谱（烤肉）→消耗食材→产出食物 |
| 士气 | NPC 吃好食物→工作效率微幅提升（简化版） |

**可玩增量**: 开垦农田→种土豆→等生长→收获→篝火烤熟→吃饱→士气提升。

**不做的**: 多种作物、留种机制、灶台、多食谱、食物变质。

---

### Phase 9: NPC 基础

| 子系统 | 基本闭环 |
|--------|----------|
| NPC 生成 | 1-2 名 NPC，基础属性（饥饿/口渴/体力/HP） |
| 简单指令 | 跟随/停留/工作（点击指定工作点） |
| 自主 AI | 饿了吃、困了睡、被攻击自卫 |
| 工具绑定 | 分配工具后可在工作点工作 |

**可玩增量**: 救一个 NPC→带回家→分配工具→让他种田/建造→NPC 自己吃喝睡觉。

**不做的**: 环世界框选、熟练度成长、多 NPC 招募、尸潮行为。

---

### Phase 10: 尸潮基础

| 子系统 | 基本闭环 |
|--------|----------|
| 触发 | 每 N 天一次（固定间隔），有简单预警 |
| 规模 | 固定数量（10-15 只），从地图边缘生成 |
| 行为 | 向据点移动、攻击建筑/玩家/NPC |
| 后效 | 击杀后掉落战利品、短暂安全期 |

**可玩增量**: 预警→准备防御→尸潮来袭→战斗→清理→修理→收集战利品→准备下一次。

**不做的**: 伪随机触发、综合评分关联、构成随机、复杂预警。

---

### Phase 11: 科技树基础

| 子系统 | 基本闭环 |
|--------|----------|
| 图纸系统 | 搜刮获得图纸→科技界面学习→图纸消耗→配方永久解锁 |
| UI | 列表式科技面板，显示前置条件和已解锁状态 |
| 节点 | 5-6 个代表性节点（木工基础、基础烹饪、工具维修等） |
| 解锁效果 | 新配方直接出现在对应菜单中 |

**可玩增量**: 搜到图纸→打开科技面板→学习→获得新配方→制作新东西。

**不做的**: 六条线全部展开、AND 前置条件复杂链路、参数修改类解锁。

---

### Phase 12+: 全生态联通 + 扩展打磨

此时所有子系统基本闭环跑通，进入扩展阶段：

- **战斗扩展**: 连招系统（ComboWindow + 冷却豁免）、技能效果（击退/眩晕/流血）、投射物系统、完整四阶段判定管道（命中率/破防/暴击/格挡）、死亡系统（Ragdoll + 复活）
- **伤病扩展**: 医疗熟练度、粉碎性骨折永久惩罚、烧伤分级、丧尸化 4 阶段完整过程
- **噪音扩展**: 噪音连锁反应（第 2 层）、障碍物衰减、昼夜倍率、环境噪音
- **敌人扩展**: 视觉感知（光线影响）、尸群协调、特殊感染者
- **资源扩展**: 六大类完整分类、仓库系统、工具耐久/维修
- **负重扩展**: 大背包/军用背包、快速使用栏位、睡袋挂载
- **建造扩展**: 高级材料（石头/金属）、工作台、家具、拆除回收
- **农业扩展**: 多种作物、留种机制、灶台、多食谱
- **NPC 扩展**: 环世界框选、多 NPC、熟练度/等级成长、招募对话
- **死亡扩展**: 角色继承机制（死后切 NPC）、尸体丧尸化、收尸任务、Game Over 条件
- **尸潮扩展**: 伪随机、综合评分、构成随机、特殊丧尸类型
- **科技扩展**: 六条线 15-18 节点、复杂前置条件、解锁效果多元化
- **数值统一规划**: 所有系统的具体数值（消耗速率、伤害值、成长曲线、噪音半径等）从上至下统一设定
- **资产替换**: 美术资源整合、动画完善、音效补全

---

## 依赖关系

```
Phase 4 (Ability Pipeline + 敌人 AI) ← 短期计划当前
    │
    ├──→ Phase 5 (资源 + 装备 + 存档)
    │         │
    │         ├──→ Phase 6 (建造) ──→ Phase 8 (农业+烹饪)
    │         │         │                    │
    │         └──→ Phase 9 (NPC) ────────────┤
    │                                          │
    └──→ Phase 7 (时间/日夜) ──→ Phase 10 (尸潮) ──┤
                                                         │
                                          Phase 11 (科技树)
```

- Phase 4-7 可并行度较高
- Phase 5 依赖 Phase 4（装备系统接 Ability 管道 ⑤ 伤害载荷）
- Phase 8 依赖 Phase 5（资源）+ Phase 6（建造篝火）
- Phase 9 依赖 Phase 5（资源/工具）+ Phase 6（床位）+ Phase 8（食物）
- Phase 10 依赖 Phase 4 + Phase 6（防御建筑）+ Phase 9（NPC）
- Phase 11 几乎依赖所有系统

---

## 时间预估

| 阶段 | 内容 | 状态 |
|------|------|------|
| Phase 4 | Ability Pipeline + Combat 补完 + 动画补完 | ✅ 完成 |
| Phase 5 | 资源系统 + 装备落地 + 存档 | ← 下一站 |
| Phase 6-7 | 建造 + 时间日夜 | 后续 |
| Phase 8-9 | 农业烹饪 + NPC | 后续 |
| Phase 10-11 | 尸潮 + 科技树 | 后续 |
| Phase 12+ | 扩展打磨 | 后续 |

> 预估基于单人力、每阶段完成可玩增量即交付的原则。实际以里程碑为准，不设硬性截止日。
