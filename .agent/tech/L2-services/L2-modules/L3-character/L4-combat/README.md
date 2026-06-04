# L3_Ability · 通用能力子系统

> `L3_Ability/` — 独立模块。能力调度、伤害管道、效果管理。角色和陷阱通用。

## 层级定位

**L4** — Character 的领域子系统。负责战斗行为的编排，不负责动画播放（L4_Animation）、属性存储（L4_Stats）、移动控制（L4_Locomotion）。

## 三层架构

```
┌─────────────────────────────────────────────────────────┐
│              配置层 (Data)                               │
│                                                         │
│  SkillDefSO (主动技能)          PassiveSkillSO (被动)     │
│    ←── SkillActivationSO         ←── ETriggerEvent      │
│    ←── SkillSearchSO             ←── ConditionTag       │
│    ←── GameplayEffectSO[]        ←── GameplayEffectSO[] │
│    ←── CooldownEffectSO          ←── CooldownEffectSO   │
│    ←── NoiseEventSO                                     │
│    ←── TagMutualExclusionSO                             │
│                                                         │
│  GameplayEffectSO (abstract)                            │
│    ├── DamageEffectSO, ImpactEffectSO, ExecuteEffectSO │
│    ├── CostEffectSO                                    │
│    └── (BuffEffectSO: Phase 5+)                        │
│                                                         │
│  纯数据，无运行时状态。存放在 Assets/Data/                 │
└────────────────────┬────────────────────────────────────┘
                     │ 工厂创建
┌────────────────────▼────────────────────────────────────┐
│         管理层 (CombatComponent)                          │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ activeSkills │  │passiveSkills │  │ OwnedTags     │  │
│  │ 主动技能列表   │  │ 被动技能列表   │  │ 状态标签       │  │
│  └──────────────┘  └──────────────┘  └───────────────┘  │
│  TryActivate(SkillDefSO) — 不关心槽位                     │
│  被动匹配: 事件 → 条件 → 效果                              │
└────────────────────┬────────────────────────────────────┘
                     │ 驱动
┌────────────────────▼────────────────────────────────────┐
│         执行层 (Driver + Pipeline)                        │
│  CombatDriver         CombatPipeline                     │
│  技能生命周期管理      纯函数判定链                         │
│  (ICharacterAnimationDriver 实现)                         │
└──────────────────────────────────────────────────────────┘
```

## 调用链

```
CharacterActor.Update()
  │
  ├── 0. combat.Tick(dt)                        ← 冷却倒计时、Effect 过期
  │
  ├── 1. director.Evaluate() → SCharacterIntent
  │     └── ActiveSkillSlot: int               ← 透传输入（不检查冷却/体力）
  │
  ├── 2~4. Kinematic / Locomotion / Stats     ← 不变
  │
  ├── 5. characterAnimation.Apply(ctx)
  │     └── DriverArbiter.Resolve()
  │           ├── LocomotionDriver             ← 默认驱动
  │           ├── TraversalDriver              ← 攀爬
  │           └── CombatDriver                 ← ★ 技能驱动
  │                 │
  │                 ├── Evaluate(ctx, dt):
  │                 │     slot = ctx.Intent.ActiveSkillSlot
  │                 │     combat.TryActivate(slot)  → 门控+冷却+体力
  │                 │     brain.SubmitRequest(AnimationRequest)
  │                 │
  │                 ├── Drive(ctx, dt):
  │                 │     命中窗口内每帧检测 → CombatPipeline.Execute()
  │                 │     → combat.ApplyDamage() → Stats.DamageRule
  │                 │     → GameEvent<SHitEvent>.Raise()
  │                 │     → GameEvent<SNoiseEvent>.Raise()
  │                 │
  │                 └── OnCompleted():
  │                       combat.EndAbility()  → 移除技能标签
  │
  └── 6. pathfindingAgent.SyncLocomotion()     ← 不变
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterActor | 持有 CombatComponent 实例，Update 中调 Tick |
| 被依赖 | PlayerDirector | 读取 SkillBar 技能信息（未来），设置 ActiveSkillSlot |
| 依赖 | L4_Animation (DriverArbiter) | CombatDriver 注册为 Driver，通过 SubmitRequest 播放动画 |
| 依赖 | L4_Stats (CharacterStats) | 扣体力 (Stamina.Modify)、扣血 (DamageRule.Add) |
| 依赖 | L1_Core (EventChannels) | 通过 GameEvent<T> 发布 SHitEvent / SNoiseEvent / SSkillEvent |
| 依赖 | L4_Director (SCharacterIntent) | 消费 ActiveSkillSlot 字段 |
| 被依赖 | 未来 L5_Drivers | CombatDriver 参考实现，后续 RangedDriver 等 |

## 核心机制

### GameplayTag — 层级标签

字符串层级标签（`"State.Stunned"`），替代分散的 bool 状态标记。四个用途：

| 用途 | 机制 | 示例 |
|------|------|------|
| **门控** | SkillDefSO 配置 `ActivationBlockedTags`，激活时检查 | `State.Stunned` → 阻塞所有技能 |
| **冷却** | 激活时施加 Duration Effect，授予冷却标签，过期自动移除 | `Skill.Cooldown.Slash` 存在 → 横斩不可激活 |
| **状态标记** | 技能激活时添加 `AbilityTags`，结束移除。外部系统查询 | `State.Attacking` → 判断是否可以移动 |
| **查询** | 任何系统通过 `CombatComponent.HasTag()` 查询 | AI 查询目标是否 `State.Dead` |

**与 Locomotion 枚举的关系**：Tag 管战斗状态，`ELocomotionPhase`/`EMovementGait` 管移动状态。两者并行，Tag 不替代 Locomotion 枚举。

### 冷却模型

冷却不是计时器变量，是**对自身施加的 Duration Effect**：

```
激活横斩 → 施加 CooldownEffect(duration=1.5s, tag="Skill.Cooldown.Slash")
  → ActiveEffect 加入 activeEffects[]
  → 标签加入 ownedTags
  → 每帧 Tick: remaining -= dt
  → 1.5s 后过期 → 自动 RemoveTag
```

**为什么这样设计**：
- 冷却和 Buff/Debuff 是同一概念——"持续一段时间的标签效果"，走同一 Tick 管道
- 标签在冷却期间可被外部查询（UI 冷却图标、AI 判断技能可用性）
- 连招时可以主动 RemoveTag 豁免冷却，不需要特殊逻辑

### 连招设计

```
横斩 (冷却 1.5s)
  │
  ├── 动画 1.0s → 添加 "State.ComboWindow" 标签（持续 0.5s）
  │     │
  │     ├── 窗口内按 E → 主动 RemoveTag("Skill.Cooldown.Slash")
  │     │             → TryActivate(回旋斩) → 成功
  │     │
  │     └── 窗口过期 → 标签自动移除
  │
  └── 1.5s 冷却自然结束 → 横斩重新可用
```

配置在 SkillDefSO 中：`ComboWindowStart`、`ComboWindowDuration`、`ComboNextSlots`、`ComboBypassCooldowns`。

### 动画分层

| 层 | ChannelMask | 移动 | 典型技能 |
|----|-------------|------|---------|
| UpperBody | `1 << 1` | 不锁移动 | 轻击、快速斩击 |
| FullBody | `1 << 0` | 锁移动 | 重击、蓄力、位移技 |

由 SkillDefSO 的 `AnimationLayer` 字段决定。复用 AnimationBrain 现有六层蒙版设计。

### 技能阶段模型

CombatDriver 内部维护一个阶段状态机，不同技能类型使用不同的阶段组合：

```
None → Windup → Active → Fire → Recovery → None
                        ↓            ↓
                    Cancelled    Cancelled
```

| 阶段 | 职责 | 持续时间 | 典型用途 |
|------|------|---------|---------|
| **Windup** | 前摇。播放起手动画。此阶段被中断→取消技能。 | 0.05~0.3s | 挥砍蓄力、举枪 |
| **Active** | 持续/循环阶段。等待外部输入或自身计时。不是所有技能都有此阶段。 | 0~∞（等待输入） | 瞄准、蓄力、架盾、战吼 |
| **Fire** | 激发阶段。执行命中检测、施加伤害/效果。 | 单帧~0.3s 窗口 | 挥砍命中、开枪、下砸 |
| **Recovery** | 后摇。动画收招。结束后回到 None。 | 动画剩余时长 | 收刀、拉栓 |
| **Cancelled** | 被取消。主动取消（右键）或被中断（受击）。清理后回到 None。 | 瞬发 | 取消瞄准、受击中断 |

横斩：`Windup(0.05s) → Fire(0.2s窗口) → Recovery`
瞄准：`Windup(0.1s) → Active(循环, 等左键) → Fire(单帧) → Recovery`
蓄力：`Windup → Active(计时) → Fire(松R触发) → Recovery`

### 事件通信

```
CombatDriver
  ├── 直接调用（同步，必备）
  │   ├→ CombatComponent.ApplyDamage()  → Stats.DamageRule
  │   └→ Brain.SubmitRequest()          → DriverArbiter
  │
  └── 事件发布（异步，可选订阅）
      ├→ GameEvent<SHitEvent>      → Audio / VFX / Proficiency
      ├→ GameEvent<SNoiseEvent>    → ZombieAI (Phase 4.3)
      └→ GameEvent<SSkillEvent>    → UI Hotbar
```

**分界原则**：必须发生的（扣血、播动画）→ 直接调用。可选的、多订阅者的（音效、VFX、UI、AI）→ 事件。

## 代表性场景验证

以下 8 个场景覆盖了不同技能类型和边界情况，验证架构在不同需求下的完备性。

### 场景汇总

| # | 场景 | 操作 | 阶段组合 | 搜索类型 | 动画层 | 噪音 | Phase |
|---|------|------|---------|---------|--------|------|-------|
| 1 | **横斩** | Q → 自动完成 | W→F→R | Cone 扇形 | FullBody | Lv4 HumanActivity | 4.1 |
| 2 | **手枪射击** | R → 自动完成 | W→F→R | RayLine 射线 | UpperBody | Lv6 WeaponExplosion | 4.1 |
| 3 | **蓄力重击** | 按住R蓄力 → 松R下砸 | W→A(计时)→F→R | Cone 扇形 | FullBody | Lv4 HumanActivity | 4.1+ |
| 4 | **连招链** | Q→(窗口内)E→(窗口内)R | W→F→[(切换)→W→F] | 各段独立 | FullBody | 各段独立 | 4.1b |
| 5 | **位移打击** | R → 冲刺4m → 沿途判定 | W→A(位移)→F→R | OverlapCapsule 轨迹 | FullBody | Lv4 HumanActivity | 4.1+ |
| 6 | **弹道投射物** | R → 火球飞行 → 碰敌爆炸 | W→F→R | 投射物自检测 | UpperBody | 投射物自发布 | 4.2 |
| 7 | **被中断** | 横斩中途被丧尸击中 | W→(中断)→Cancelled | — | — | — | 4.1b |
| 8 | **自身增益** | R → 战吼 → +Buff 10s | W→A→R(无Fire) | Circle 自身 | FullBody | Lv3 HumanActivity | 4.2 |

### 各场景对架构的要求

| # | 新要求 | 涉及组件 | 4.1 是否预留 |
|---|--------|---------|-------------|
| 1 | 基础链路全通 + Cone 扇形搜索 | CombatComponent + CombatDriver + Pipeline.SearchCandidates(Cone) | ✓ 实现 |
| 2 | UpperBody 动画、LoSType.RayLine + FilterByLoS 射线遮挡、远距离单目标 | Pipeline.SearchCandidates(RayLine) + FilterByLoS | ✓ 实现 |
| 3 | Active 阶段自身计时；Fire 阶段根据运行时参数(chargeAmount)算伤害 | CombatPipeline.CalculateDamage(runtimeParams) | ✓ 预留动态参数接口 |
| 4 | 连招窗口 timer + 临时标签；冷却豁免(RemoveTag)；技能中途切换 | CombatComponent.RemoveTag；SkillDefSO.CanComboTo | ✗ Phase 4.1b |
| 5 | CombatDriver 通过 CharacterRig 写入位移 | CombatDriver 持有 Rig 引用 | ✓ 预留（参考 TraversalDriver） |
| 6 | 投射物独立 GameObject 自检测 + 自发布事件 | Projectile；事件发布方从 Driver 转移到 Projectile | ✗ Phase 4.2 |
| 7 | HitReactDriver（Resistance=15）；OnInterrupted 清理标签+hitTargets+冷却不返还 | DriverArbiter 协商 | ✗ Phase 4.1b |
| 8 | ApplyEffect 施加 Buff（Duration + Modifier + Tag）；过期自动 RemoveModifier | CombatComponent.ApplyEffect；ActiveGameplayEffect 过期回调 | ✓ 预留 ApplyEffect 接口 |

### 门控分层

所有技能阻塞（冷却、体力不足、状态不允许、正在攻击中）统一在 `CombatComponent.TryActivateAbility` 中通过标签门控处理：

```
技能执行中阻塞: 每个攻击技能加 Tag "State.Attacking"
                每个攻击技能阻挡 Tag "State.Attacking"
                → 放任何技能时, 其他技能自动被标签门控挡住
```

UI 不是门控执行者，是状态展示者。技能被拒绝时发布 `SSkillEvent(Rejected, Reason)`，UI 据此显示反馈（闪红、冷却倒计时等）。

### 命中检测：物理搜索 + 数值投骰（参考环世界）

战斗不是"碰撞体碰到=命中"，而是两阶段管道：**物理搜索找候选目标 → 数值投骰决定是否命中**。

```
CombatPipeline.Execute(skill, origin, direction, targetMask, stats, params)
  │
  ├── ① SearchCandidates(range, angle, searchType)
  │      └── Cone:  OverlapSphere + 角度过滤（横斩）
  │      └── Circle: OverlapSphere（旋风斩、战吼）
  │      └── RayLine: Raycast + 近线目标检测（手枪、步枪）
  │      → List<Collider> 候选目标
  │
  ├── ② FilterByLineOfSight(candidates, origin)
  │      └── 远程技能：Raycast 遮挡检查，排除墙后目标
  │      → 有效目标
  │
  ├── ③ RollHit(attackerStats, targetStats, accuracyMod)
  │      └── 命中投骰（4.1 跳过：有候选即命中，100%）
  │      → List<Collider> 命中目标
  │
  └── ④ CalculateDamage(hits, skill, params)
         └── 伤害数值（4.1 简化：flat damage × multiplier）
         → List<DamageInfo>
```

**物理检测是基础**——必须通过 Unity 物理层确认目标在范围内、无遮挡。**数值投骰是上层**——后续扩展距离衰减、命中率、掩护修正时只改③④，不改物理搜索。

### 搜索类型

| 类型 | 检测方式 | 典型用途 | 4.1 |
|------|---------|---------|-----|
| `Cone` | OverlapSphere + 前方角度过滤 | 横斩、霰弹、龙息 | ✓ 横斩 Q |
| `RayLine` | Raycast 指向 + 近线目标检测 | 手枪、步枪、弓 | ✓ 手枪 R |
| `Circle` | OverlapSphere 自身周围 | 旋风斩、战吼、践踏 | — 4.2+ |

## 设计决策

| 决策 | 原因 |
|------|------|
| CombatComponent 为纯类 | 与 PlayerDirector/Kinematic/Locomotion 同级，不引入新 MonoBehaviour |
| 冷却用 Effect + Tag | 与 Buff/Debuff 统一管道；标签可查询、连招可豁免 |
| CombatDriver 走 DriverArbiter | 复用 Resistance 协商；与 Loco/Traversal Driver 同级注册 |
| Director 不检查冷却/体力 | 分离意图与执行；避免"吞输入"（冷却开了但动画没播） |
| Effect 统一修改属性 | 伤害/消耗/Buff 都走 CombatComponent.ApplyDamage/ApplyEffect |
| 物理搜索 + 数值投骰两阶段 | 近战远程走同一管道；物理检测确认前置条件（够得着、看得见），数值投骰决定结果（后续扩展距离/掩护/命中率） |
| GameplayTag 不替代 Locomotion 枚举 | Tag 管战斗状态，枚举管移动状态。各司其职 |
| 4.1 管道简化 | 物理搜索完整实现（Cone + RayLine），投骰跳过（100%），伤害简化（flat）

## 与现有系统的集成

| 集成点 | 改动 | 影响 |
|--------|------|------|
| `CharacterActor.Awake()` | `combat = new CombatComponent(stats)` | +1 行 |
| `CharacterActor.Update()` | 新增第 0 步 `combat.Tick(dt)` | +1 行 |
| `SCharacterIntent` | 新增 `ActiveSkillSlot: int` | +1 字段 |
| `PlayerDirector.Evaluate()` | 透传 `input.SkillXRequested → intent.ActiveSkillSlot` | ~5 行 |
| `PlayerInput` | +4 个 `SkillRequested` bool + 4 个 InputEvent 订阅 | ~20 行 |
| `AnimationAliasProfile` | +技能动画 StringAsset 字段 | ~4 个字段 |
| `DriverArbiter` | CombatDriver 注册为第三个 Driver | 0 改动 |
| `CharacterStats` / `DamageRule` | 已有基础设施，直接使用 | 0 改动 |
| `EventChannels` / `GameEvent<T>` | 已有基础设施，直接使用 | 0 改动 |

## Phase 4.1 范围

**纳入**：
- GameplayTag + GameplayTagContainer（层级标签，门控/冷却/状态标记）
- SkillDefSO + SkillActivationSO + SkillSearchSO（身份、激活方式、阶段、搜索形状）
- ECombatSearchType 枚举（Cone / RayLine / Circle，4.1 实现 Cone + RayLine）
- CombatComponent + SkillBar(4槽) + ActiveGameplayEffect（池化冷却）
- CombatPipeline：物理搜索（SearchCandidates Cone+RayLine + FilterByLoS）+ 投骰跳过（100%）+ 伤害简化（flat）
- CombatDriver（标准 Driver 模式，阶段机 Windup→Fire→Recovery）
- PlayerInput +4 skill slot 输入 (Q/E/R/F)
- SCharacterIntent.ActiveSkillSlot + SkillConfirm/SkillCancel（预留字段）
- SHitEvent + SNoiseEvent + SSkillEvent 发布
- 两个技能全链路验证：**横斩 Q（Cone 扇形）+ 手枪 R（RayLine 射线）**

**不纳入**：
- Circle 搜索类型（Phase 4.2 旋风斩/战吼时实现）
- 完整投骰管道 RollHit / CalculateDamage（接口预留，4.1 跳过）
- 技能效果（击退/眩晕/流血）→ 依赖伤病系统
- 连招系统（ComboWindow 标签机制已设计，Phase 4.1b 实现）
- 熟练度增长、Buff/Debuff 系统 → Phase 5
- 敌人 AI 受击（HitReactDriver）→ Phase 4.1b
- 多武器切换 → Phase 5

## Phase 4.1 预留接口

以下接口在 Phase 4.1 实现时必须预留，即使只实现基础版本：

| 接口 | 4.1 行为 | 后续场景 | 预留方式 |
|------|---------|---------|---------|
| `CombatDriver.currentPhase` (SkillPhase enum) | Windup/Fire/Recovery | 3/5 需要 Active/Cancelled | enum 定义完整 6 阶段，Drive() 用 switch |
| `CombatPipeline.SearchCandidates(searchType)` | 实现 Cone + RayLine | 8 需要 Circle | ECombatSearchType 完整定义，Pipeline 用 switch |
| `CombatPipeline.FilterByLineOfSight()` | 实现 RayLine 遮挡 | — | 方法签名独立 |
| `CombatPipeline.RollHit(stats)` | 跳过（有候选即命中 100%） | 2+ 距离衰减/命中率 | 方法签名接受 stats，null→跳过 |
| `CombatPipeline.CalculateDamage(params)` | flat damage × multiplier | 3 需要 chargeAmount | 方法签名接受 `Dictionary<string,float>` |
| `CombatComponent.ApplyEffect(EffectSpec)` | 只有冷却 Effect | 8 需要 Buff(Modifier+过期) | 方法签名 + EffectSpec 结构体 |
| `SCharacterIntent.SkillConfirm / SkillCancel` | 不消费（始终 false） | 3 需要二次输入 | 字段定义 + Director 透传 |
| `SkillDefSO.AnimationLayer` (enum) | Q=FullBody, R=UpperBody | — | enum 完整，AnimationRequest 设置 ChannelMask |
| `ActiveGameplayEffect` + 过期回调 | 冷却过期→RemoveTag | 8 Buff 过期→RemoveModifier | 过期回调委托 |
| `CombatDriver.OnInterrupted(by)` 清理 | 基础清理（RemoveTag） | 7 冷却不返还 | 完整清理逻辑放在方法内 |
| `SkillDefSO.CanComboTo[]`+`ComboWindow*` | 空/不触发 | 4 连招 | 字段定义在 SO 中 |

## 目录结构

> **注意**: `GameplayTag.cs`、`GameplayTagContainer.cs`、`GameplayTagDefinitionSO.cs` 已提升至 `L1_Core/GameplayTag/`，作为全系统基础设施。

```
Combat/
├── CombatComponent.cs              # 中枢：技能/效果/标签管理
├── Config/
│   ├── SkillDefSO.cs               # [SO] 单技能完整定义
│   ├── SkillActivationSO.cs        # [SO] 激活方式 + 动画 + 阶段标记
│   ├── SkillSearchSO.cs            # [SO] 搜索形状（抽象基类）
│   ├── ConeSearchSO.cs             # [SO] 扇形搜索
│   ├── RaySearchSO.cs              # [SO] 射线搜索
│   ├── CircleSearchSO.cs           # [SO] 圆形搜索
│   ├── GameplayEffectSO.cs         # [SO] 统一持续效果（冷却/Buff/Debuff）
│   ├── ESkillAnimationLayer.cs     # enum: FullBody / UpperBody
│   ├── ESkillPhase.cs              # enum: 6 阶段
│   ├── ESkillEventType.cs          # enum: Activated / Completed / Rejected
│   └── ECombatSearchType.cs        # enum: Cone / RayLine / Circle
├── Runtime/
│   ├── SkillBar.cs                 # 4 槽冷却管理
│   ├── SkillSlot.cs                # 单槽状态
│   ├── ActiveGameplayEffect.cs     # 运行中效果（池化）
│   └── CombatPipeline.cs           # static 四阶段判定链
├── Drivers/
│   └── CombatDriver.cs             # 技能动画驱动
└── Structs/
    ├── SDamageInfo.cs              # 伤害结果
    ├── SHitEvent.cs                # GameEvent 载荷
    ├── SSkillEvent.cs              # GameEvent 载荷
    └── SNoiseEvent.cs              # 噪音事件载荷
```

## 子文档索引

| 文档 | 说明 |
|------|------|
| [gameplay-tag.md](gameplay-tag.md) | GameplayTag — 层级标签 struct |
| [gameplay-tag-container.md](gameplay-tag-container.md) | GameplayTagContainer — 标签集合 |
| combat-component.md | CombatComponent — 中枢管理器（实现时编写） |
| config/skill-def-so.md | SkillDefSO + WeaponSkillSetSO（实现时编写） |
| runtime/skill-bar.md | SkillBar + SkillSlot（实现时编写） |
| runtime/active-gameplay-effect.md | ActiveGameplayEffect（实现时编写） |
| runtime/combat-pipeline.md | CombatPipeline — 四阶段判定链（实现时编写） |
| drivers/combat-driver.md | CombatDriver — 技能动画驱动（实现时编写） |
| structs/damage-info.md | DamageInfo 结构体（实现时编写） |
| structs/s-hit-event.md | SHitEvent + SSkillEvent（实现时编写） |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 四阶段完整判定管道 | Phase 4.1b | CombatPipeline 接口已预留 | injury-system.md |
| 技能效果（击退/眩晕/流血） | Phase 4.1b | 伤病系统 | injury-system.md |
| 连招系统 | Phase 4.1b | ComboWindow 机制已设计 | 本设计 |
| 熟练度增长 | Phase 4.2 | 武器 Stats 集成 | game-overview.md |
| Buff/Debuff 系统 | Phase 4.2 | ActiveEffect 已预留 | stats-inventory.md |
| 热武器 (RangedDriver) | Phase 4.2+ | CombatDriver 参考实现 | game-overview.md |
| AIDirector + 敌人技能 | Phase 4.3 | CombatComponent 复用 | short-term.md |
| 噪音系统集成 | Phase 4.3-4.4 | SNoiseEvent 已预留 | noise-system.md |
| 多武器切换 | Phase 8+ | WeaponSkillSetSO 已预留 | game-overview.md |
