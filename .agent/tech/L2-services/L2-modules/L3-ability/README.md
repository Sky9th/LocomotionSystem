# L3_Ability · 通用能力子系统

> `L3_Ability/` — 独立模块。能力调度、伤害管道、效果管理。角色和陷阱通用。

## 层级定位

独立 L3 模块，位于 `Services/Modules/L3_Ability/`。负责战斗行为的编排，不负责动画播放、属性存储、移动控制。

## 架构概览

```
┌──────────────────────────────────────────────────────────────────┐
│                     配置层 (Config / SOs)                         │
│                                                                   │
│  AbilityDefSO (主动技能)           PassiveAbilitySO (被动)         │
│    ├── AbilityActivationSO           ├── ETriggerEvent            │
│    ├── AbilitySearchSO               │    OnEnterArea/OnKill/     │
│    │    ├── ConeSearchSO             │    OnHit/OnDamaged/...     │
│    │    ├── RaySearchSO              ├── targetRequiredTag        │
│    │    └── CircleSearchSO           ├── cooldownDuration         │
│    ├── EffectSO[] (targetEffects)    ├── sharedCooldownTag        │
│    ├── EffectSO[] (selfEffects)      ├── EffectSO[] (targetEffects)│
│    ├── cooldownDuration              ├── EffectSO[] (selfEffects) │
│    ├── sharedCooldownTag             └── triggerChannel (可选)     │
│    ├── NoiseEventSO                                               │
│    ├── activeTag + categoryTag                                    │
│    └── SComboLink[]                                                │
│                                                                   │
│  EffectSO (abstract) — 纯数据，无运行时方法                        │
│    ├── DamageEffectSO   (baseDamage + 穿透/上下限)                 │
│    ├── ImpactEffectSO   (staggerValue + knockback)                │
│    ├── ExecuteEffectSO  (hpThreshold)                             │
│    └── CostEffectSO     (statTag + amount)                        │
│                                                                   │
│  每个 EffectSO 是完整的效果概念（"500度火焰"），数值固有。        │
│  要不同的火就建不同的资产。SO 不写 Execute 方法，逻辑在管道层。   │
└─────────────────────────┬──────────────────────────────────────────┘
                          │
┌─────────────────────────▼──────────────────────────────────────────┐
│                   管道层 (Ability Pipeline)                           │
│                                                                     │
│  AbilityComponent (Caster 发送面)     HitReactionComponent (接收面) │
│    ②→③→④→⑤                            ⑥→⑦                      │
│          直接调用 targets[i].HitReactionComponent.Resolve()          │
│                                → SResolvedHit → HitEventSO (⑧)     │
│                                                                     │
│  五接口修改器开放 ②⑤⑥⑦ + 目标过滤：ICondition / ITargetFilter / IEffect / IResolution / IReaction│
│                                                                     │
│  完整设计 → [ability-pipeline-design.md](ability-pipeline-design.md)│
└─────────────────────────┬──────────────────────────────────────────┘
                          │
┌─────────────────────────▼──────────────────────────────────────────┐
│                   管理层 (AbilityComponent + HitReactionComponent)   │
│                                                                     │
│  AbilityComponent (发送中枢):                                        │
│    主动 — TryActivate(ability) → ②③④⑤ → HitReactionComponent      │
│    被动 — NotifyEvent(event, subject) → match PassiveAbilitySO     │
│    内部 — OwnedTags / runtimePassives[] / cooldownEndTimes          │
│                                                                     │
│  HitReactionComponent (接收中枢, 同 GameObject):                     │
│    主动命中 — Resolve(SResolvedHit[], caster) → ⑥⑦ → ⑧            │
│    裸伤害  — ReceiveRawDamage(damage, type, caster)                 │
│    ⑥ — IResolutionModifier: Avoid (短路) → Mitigate (链式) → Absorb │
│    ⑦ — IReactionModifier.React() → OnDamaged 被动匹配             │
└──────────────────────────────────────────────────────────────────────┘
```

## 目录结构

```
L3_Ability/
├── AbilityComponent.cs              # [MonoBehaviour] 中枢：触发器+被动匹配+冷却+SResolvedHit 管道
├── HitEventSO.cs               # [SO] GameEvent<SHitEvent> — ⑧ 广播通道，发布 SResolvedHit
├── Config/
│   ├── AbilityDefSO.cs              # [SO] 主动技能完整定义
│   ├── AbilityActivationSO.cs       # [SO] 激活方式 + 动画 + 阶段标记
│   ├── AbilityTreeSO.cs             # [SO] 技能/天赋/套路树 — 一切皆 AbilityTree
│   ├── PassiveAbilitySO.cs          # [SO] 被动技能定义
│   ├── NoiseEventSO.cs              # [SO] 噪音事件定义
│   ├── TagMutualExclusionSO.cs      # [SO] 全局标签互斥规则
│   ├── Search/
│   │   ├── AbilitySearchSO.cs       # [SO] 搜索定义抽象基类
│   │   ├── ConeSearchSO.cs          # [SO] 扇形搜索
│   │   ├── RaySearchSO.cs           # [SO] 射线搜索
│   │   └── CircleSearchSO.cs        # [SO] 圆形搜索 (Phase 4.2+)
│   ├── Effect/
│   │   ├── EffectSO.cs              # [SO] 效果抽象基类 — 纯数据
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
    ├── SAbilityEvent.cs           # UI 技能反馈事件
    ├── SComboLink.cs              # 连招衔接定义
    ├── SDamageInfo.cs             # 理想伤害载荷（⑤ 产出）
    ├── SHitEvent.cs               # 命中广播载荷（⑧ 发布）
    └── SNoiseEvent.cs             # 噪音广播载荷
```

> 已删除 `CooldownRuleSO.cs`。冷却变为 `cooldownDuration` + `sharedCooldownTag` 直接写在 SO 上。
> 不需要 `Trap.cs`。AbilityComponent 内置 OnTriggerEnter 处理。

## 调用链

```
CharacterActor.Update()
  ├── 0. ability.Tick() — 清理冷却 + HitReactionComponent 结算本帧收到的伤害
  ├── 1. director.Evaluate() -> SCharacterIntent
  ├── 2. Kinematic.Evaluate()
  ├── 3. Locomotion.Simulate()
  ├── 4. Stats.Update(ctx, dt) + Stats.ApplyDamage(resolvedHits)
  ├── 5. Animation.Apply(in ctx)
  └── 6. PathfindingAgent.SyncLocomotion()

主动技能 → AbilityComponent.TryActivate() → HitReactionComponent.Resolve()
陷阱触发 → AbilityComponent.OnTriggerEnter() → NotifyEvent()
```

## 核心机制

### GameplayTag — 层级标签

> 完整标签树文档：[gameplay-tag.md](../../../L1-core/gameplay-tag.md) · 9 根 · 190 资产

Ability 系统消费的标签用途：

| 用途 | 字段 | 匹配方式 | 示例 |
|------|------|---------|------|
| 互斥门控 | `activeTag` | `HasTag` 前缀 | `State.Combat.Attacking` — 持有期间阻止其他 State.* |
| 冷却门控 | `sharedCooldownTag` | `HasTagExact` 精确 | `Skill.Cooldown.FireGroup` — 联动冷却 |
| 技能分类 | `categoryTag` | `HasTag` 前缀 | `Skill.Combat.Melee` — 被动条件匹配 |
| 伤害路由 | `EffectSO.effectTag` | `HasTag` 前缀 | `Damage.Elemental.Fire` → 火抗公式 |
| Buff 标记 | `EffectSO.grantedTag` | 写入 OwnedTags | `Effect.Buff.Fortify` — 过期移除 |
| 消耗资源 | `CostEffectSO.statTag` | 精确查找 | `Stat.Vital.Stamina` — 定位 StatInstance |
| 噪音类型 | `NoiseEventSO.noiseType` | `HasTag` 前缀 | `Noise.Combat.WeaponFire` → AI 追击 |

### EffectSO 设计原则

- **纯数据**：SO 不写 Execute/Apply 方法，逻辑全在管道层
- **共享契约**：DamageEffectSO 的 `baseValue` 由装备填入，`modAdd/modMult/priority` 由 Ability 填入。详见 [damage-effect-so.md](damage-effect-so.md)
- **运行时叠加**：`(baseValue + modAdd) × modMult`，同 effectTag 多 Effect 按 priority 排序叠算
- **无需 SEffectInstance**：要不同值就建不同资产

### 互斥模型

`TagMutualExclusionSO` 只设 `[Tag_State]` 为互斥根。`State.*` 下的所有标签互为排斥——角色不能同时处于两个 State。

`categoryTag`（如 `Skill.Combat.Melee`）**不**参与互斥。它用于被动条件匹配和目标过滤。

> 冷却模型、被动技能细节 → [ability-component.md](ability-component.md)
> 伤害管道 (八维度) → [ability-pipeline-design.md](ability-pipeline-design.md)

## Phase 状态

| 状态 | 说明 |
|------|------|
| Done | 配置层就绪：AbilityDefSO, PassiveAbilitySO, EffectSO 子类, SearchSO 子类, HitEventSO |
| Done | AbilityComponent: OnTriggerEnter/Exit 内置、被动匹配、冷却管道 |
| ✅ 设计完成 | 八维度管道、五回调修改器、二组件拆分、SDamageInfo 定义 — 详见 [ability-pipeline-design.md](ability-pipeline-design.md) |
| ✅ Done | TryActivate 主动技能入口：②门控→③扣费(P eek+Modify)→④搜索→⑤效果→⑥结算→⑧广播 |
| Slice 2 | 落地 IResolutionModifier 分阶段 Avoidance/Mitigation/Absorption |
| Slice 3 | AbilityDriver 阶段机 + 动画驱动 |
| Phase 4.2+ | BuffEffectSO、投射物、位移、环境修改器注入 |

## 子文档索引

| 文档 | 说明 |
|------|------|
| [ability-pipeline-design.md](ability-pipeline-design.md) | Ability Pipeline — 八维度技能管道完整设计 |
| [ability-pipeline-states.md](ability-pipeline-states.md) | Ability Pipeline States — 状态机实现（IState / StateMachine / Gating / Search / Cost / Execution） |
| [ability-inventory.md](ability-inventory.md) | Ability Inventory — 154 技能全量树 + 闭环测试集 |
| [ability-component.md](ability-component.md) | AbilityComponent — 能力执行中枢，API + 调用链 |
| [ability-editor.md](ability-editor.md) | Ability Editor — 编辑器架构 |
| [effect-so.md](effect-so.md) | EffectSO — 效果抽象基类 |
| [damage-effect-so.md](damage-effect-so.md) | DamageEffectSO — 伤害效果契约（装备/Ability 共享） |
| [ability-search-assets.md](ability-search-assets.md) | **Search 资产树** — 完整 SearchSO JSON 清单 (~45 资产, Cone/Ray/Circle/Line) |
| [ability-activation-assets.md](ability-activation-assets.md) | **Activation 资产树** — 完整 ActivationSO JSON 清单 (~28 资产, Instant/Charged/Channel/Toggle) |
| [ability-noise-assets.md](ability-noise-assets.md) | **Noise 资产树** — 完整 NoiseEventSO JSON 清单 (~44 资产) + Noise Tag 依赖树 (17) |
| [ability-tree.md](ability-tree.md) | **AbilityTreeSO** — 技能/天赋/套路树，一切皆 AbilityTree |
| [ability-forest.md](ability-forest.md) | **AbilityForest** — 运行时技能森林，多来源活跃树集合 |

> GameplayTag 基础设施文档位于 [L1-core](../../../L1-core/)：资产树、运行时 struct、容器用法。
