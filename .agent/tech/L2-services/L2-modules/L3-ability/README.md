# L3_Ability · 通用能力子系统

> `L3_Ability/` — 独立模块。能力调度、伤害管道、效果管理。角色和陷阱通用。

## 层级定位

独立 L3 模块，位于 `Services/Modules/L3_Ability/`。负责战斗行为的编排，不负责动画播放、属性存储、移动控制。

## 三层架构

```
┌──────────────────────────────────────────────────────────────────┐
│                     配置层 (Config / SOs)                         │
│                                                                   │
│  AbilityDefSO (主动技能)           PassiveAbilitySO (被动)         │
│    ├── AbilityActivationSO           ├── ETriggerEvent            │
│    ├── AbilitySearchSO               ├── ConditionTag             │
│    │    ├── ConeSearchSO             ├── CooldownRuleSO           │
│    │    ├── RaySearchSO              └── EffectSO[]               │
│    │    └── CircleSearchSO (4.2+)                                 │
│    ├── EffectSO[] (targetEffects)                                 │
│    ├── EffectSO[] (selfEffects)                                    │
│    ├── CooldownRuleSO                                              │
│    ├── NoiseEventSO                                                │
│    ├── TagMutualExclusionSO                                        │
│    └── SComboLink[]                                                │
│                                                                   │
│  EffectSO (abstract)                                              │
│    ├── DamageEffectSO                                              │
│    ├── ImpactEffectSO                                              │
│    ├── ExecuteEffectSO                                             │
│    └── CostEffectSO                                                │
│                                                                   │
│  纯数据，无运行时状态。                                             │
└─────────────────────────┬──────────────────────────────────────────┘
                          │ 运行时构造
┌─────────────────────────▼──────────────────────────────────────────┐
│                   管理层 (AbilityComponent)                          │
│  TryActivate(AbilityDefSO) -- 不关心槽位                             │
│  被动匹配: 事件→条件→效果                                            │
│  Tick(dt): 冷却倒计时、Effect 过期                                   │
└─────────────────────────┬──────────────────────────────────────────┘
                          │ 驱动 (Slice 3+)
┌─────────────────────────▼──────────────────────────────────────────┐
│                   执行层 (Future)                                    │
│  AbilityPipeline (未实现)              AbilityDriver (未实现)         │
│  纯函数判定链                          技能生命周期管理                │
│                                                                     │
│  Slice 1: TryActivate 占位返回 false                                │
│  Slice 2: Tick 冷却 + AbilityPipeline 接入                          │
│  Slice 3+: AbilityDriver 阶段机 + 动画驱动                           │
└──────────────────────────────────────────────────────────────────────┘
```

## 目录结构

```
L3_Ability/
├── AbilityComponent.cs              # [MonoBehaviour] 中枢
├── Config/
│   ├── AbilityDefSO.cs              # [SO] 主动技能完整定义
│   ├── AbilityActivationSO.cs       # [SO] 激活方式 + 动画 + 阶段标记
│   ├── PassiveAbilitySO.cs          # [SO] 被动技能定义
│   ├── CooldownRuleSO.cs            # [SO] 冷却规则
│   ├── NoiseEventSO.cs              # [SO] 噪音事件定义
│   ├── TagMutualExclusionSO.cs      # [SO] 全局标签互斥规则
│   ├── Search/
│   │   ├── AbilitySearchSO.cs       # [SO] 搜索定义抽象基类
│   │   ├── ConeSearchSO.cs          # [SO] 扇形搜索
│   │   ├── RaySearchSO.cs           # [SO] 射线搜索
│   │   └── CircleSearchSO.cs        # [SO] 圆形搜索 (Phase 4.2+)
│   ├── Effect/
│   │   ├── EffectSO.cs              # [SO] 效果抽象基类
│   │   ├── DamageEffectSO.cs        # [SO] 伤害效果
│   │   ├── ImpactEffectSO.cs        # [SO] 冲击效果
│   │   ├── ExecuteEffectSO.cs       # [SO] 斩杀效果
│   │   └── CostEffectSO.cs          # [SO] 资源消耗/恢复效果
│   └── Enum/
│       ├── EAbilityAnimationLayer.cs
│       ├── EAbilityEventType.cs
│       ├── EAbilityPhase.cs
│       ├── EActivationType.cs
│       ├── EKnockbackDirection.cs
│       ├── ESearchType.cs
│       ├── ETargetFilter.cs
│       └── ETriggerEvent.cs
└── Structs/
    ├── SAbilityEvent.cs
    ├── SComboLink.cs
    ├── SDamageInfo.cs
    ├── SHitEvent.cs
    └── SNoiseEvent.cs
```

## 调用链

```
CharacterActor.Update()
  ├── 0. ability.Tick(dt)
  ├── 1. director.Evaluate() -> SCharacterIntent
  ├── 2. Kinematic.Evaluate()
  ├── 3. Locomotion.Simulate()
  ├── 4. Stats.Update(ctx, dt)
  ├── 5. Animation.Apply(in ctx)
  │     └── DriverArbiter.Resolve()
  │           └── AbilityDriver (Slice 3+)
  │                 └── ability.TryActivate(skill, pos, dir)
  │                       ├── Gate: TagMutualExclusionSO + CooldownRuleSO
  │                       ├── Cost: CanPayCosts() -> stats.Modify()
  │                       ├── Self: activeTag, selfEffects
  │                       ├── Search: AbilitySearchSO -> AbilityPipeline
  │                       ├── Target: targetEffects -> EffectSO.Apply()
  │                       └── Noise: NoiseEventSO.Publish()
  └── 6. PathfindingAgent.SyncLocomotion()
```

## 核心机制

### GameplayTag -- 层级标签

| 用途 | 机制 | 示例 |
|------|------|------|
| 门控 | TagMutualExclusionSO 集中管理互斥 | State.Attacking vs State.Reloading |
| 冷却 | CooldownRuleSO 施加 cooldownTag | Skill.Cooldown.Slash |
| 状态标记 | 技能激活时添加 activeTag | State.Attacking -> AI 读取 |
| 路由 | effectTag 路由防御/AI/VFX | Tag_Damage_Fire -> 火抗公式 |

### 冷却模型

冷却不是计时器变量，是对自身施加的 Duration 标签：
激活 -> CooldownRuleSO(duration, cooldownTag) -> 标签加入 OwnedTags -> Tick -> 过期移除

### 技能阶段模型

None -> Windup -> Fire -> Recovery -> None
(Cancelled 可打断 Windup/Recovery)

### 动画即时间轴

- Phase Markers 描述动画本身的自然阶段（speed=1.0 基准）
- animationSpeed 是唯一调参旋钮：实际时间 = marker / speed
- Recovery = clipLength/speed - (windup+fire)/speed

## 被动技能

与主动技能共享 EffectSO 体系，不走 activation/search/noise/combo 管道。

- trigger 枚举: OnKill/OnHit/OnDamaged/OnLowHP/OnDodge/OnComboStage/OnEquip
- triggerChannel: 外部 EventChannel，非 null 覆盖枚举

## Phase 状态

| 状态 | 说明 |
|------|------|
| Done | 配置层全部 29 个 .cs 文件就绪，AbilityComponent 骨架 |
| Slice 2 | Tick 冷却倒计时、CanPayCosts、AbilityPipeline 接入 |
| Slice 3+ | AbilityDriver 阶段机、动画驱动、连招匹配 |
| Phase 4.2+ | BuffEffectSO、投射物、位移、Circle 搜索 |

## 子文档索引

| 文档 | 说明 |
|------|------|
| [gameplay-tag.md](gameplay-tag.md) | GameplayTag |
| [gameplay-tag-container.md](gameplay-tag-container.md) | GameplayTagContainer |
