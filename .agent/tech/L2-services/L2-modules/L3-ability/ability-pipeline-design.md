# Ability Pipeline — 技能管道设计

> `L3_Ability/` · 设计文档 · 2026-06-06

## 定位

定义技能从"按下按键"到"产生实际效果"的完整执行管道。回答：一个技能经过哪些阶段、各阶段谁负责、外部系统如何接入。

---

## 根隐喻

**技能 = Caster → [效果载荷] → Target，世界旁观。**

每个技能执行都产生一个显式的执行上下文：

```
AbilityExecutionContext
├── Caster: GameObject        ← 技能发起方
├── Targets: GameObject[]     ← 技能接收方
├── SourceAbility: AbilityDefSO | PassiveAbilitySO
└── TriggerEvent: ETriggerEvent (被动时)
```

---

## 八维度管道

> **维度 0（执行上下文）**不是独立维度。`AbilityExecutionContext { Caster, Targets, SourceAbility, TriggerEvent }` 渗透到 ②~⑧ 的每个维度中，是管道所有阶段的共享载体。

```
  Caster 侧                              Target 侧                世界
  ─────────                             ─────────                ────
  ① 身份   纯 SO 定义                   ⑥ 结算   防御公式         ⑧ 告知   事件广播
  ② 条件   门控检查                     ⑦ 反应   动画/位移
  ③ 释放   动画/阶段机
  ④ 寻找   搜索命中
  ⑤ 效果   理想伤害载荷
```

### ① 身份 (Identity) — "这是什么"

纯定义，不参与执行流程。`AbilityDefSO` 和 `PassiveAbilitySO` 的 `internalName` / `displayName` / `icon` / `categoryTag`。

### ② 条件 (Gating) — "能用吗"

释放前必须通过的门控。任一失败则拒绝，产出 `SAbilityEvent.Rejected(reason)`。

| 门控 | 数据来源 | 检查方 |
|------|---------|--------|
| 冷却 | `cooldownDuration` + `sharedCooldownTag` | Caster.AbilityComponent |
| 资源 | `CostEffectSO` (在 selfEffects 里) | Caster.Stats |
| 互斥 | `activeTag` + `TagMutualExclusionSO` | Caster.AbilityComponent |
| 被动条件 | `targetRequiredTag` (PassiveAbilitySO) | 事件 Subject |

**开放点**：`IConditionModifier.Check(ctx)` — 外部系统一票否决（如沉默/缴械）。

### ③ 释放 (Activation) — "怎么放"

门控通过后，Caster 的**表现行为**。`AbilityActivationSO` 定义输入模型和动画阶段。

| 子维度 | 说明 |
|--------|------|
| 输入模型 | `EActivationType` — 瞬发 / 蓄力 / 持续 / 开关 |
| 动画 | 动画引用 + 层 (FullBody/UpperBody) + 速度 + 根运动 |
| 阶段时机 | Windup → Fire → Recovery（Phase Markers 描述动画天然阶段）|
| 取消策略 | canCancelWindup / canCancelRecovery |

> ③ 阶段机由 AbilityDriver 消费。AbilityDriver 是独立组件（Phase 4.1 Slice 3）。

**扣费执行时机**：② 通过后、③ 正式开始前，commit-time 前置扣除 `CostEffectSO`（避免释放失败白扣资源）。

### ④ 寻找 (Search) — "打哪里 / 打谁"

Fire 阶段执行。从 Caster 出发，按搜索形状检测可命中目标。

| 子维度 | 说明 |
|--------|------|
| 搜索形状 | `ESearchType` — 锥 / 射线 / 圆 |
| 范围 | `range` + `angle` (锥) / `requiresLineOfSight` (射线) |
| 过滤 | `targetMask` (Layer) + `ETargetFilter` (敌/友) + `maxTargets` |

> 产出：`GameObject[] targets`。**此引用直接用于 ⑥⑦ 的直接调用，不经事件。**
>
> `ETargetFilter` 在 Phase 4.1 实现（非 4.2+），因为主动技能需区分敌我。

### ⑤ 效果 (Effects) — "产生什么载荷"

读 EffectSO 字段，构造**理想伤害数据**。此时未触及目标防御。

| 方向 | 字段 | 处理 |
|------|------|------|
| 对外 | `targetEffects[]` | DamageEffectSO / ImpactEffectSO / ExecuteEffectSO → 构造 `SResolvedHit`（理想值）|
| 对内 | `selfEffects[]` | CostEffectSO 在 ②→③ 间扣费；Buff grantedTag 在此时写入 Caster.OwnedTags |

**开放点**：`IEffectModifier.Modify(ctx, hit)` — 链式修改伤害值（如力量加成、武器附魔）。

**产出**：`SResolvedHit[]`（理想值，FinalDamage = IncomingDamage）。

### ⑥ 结算 (Resolution) — "实际发生什么"

**接收方维度**。Target.HitReactionComponent 用自己的属性对抗理想伤害。分三个子阶段：

```
免疫检查 (applicationBlockedTags)
  → 命中 → Avoidance (闪避/格挡)    ← 短路：第一个 Avoided=true 即停止
  → 命中 → Mitigation (护甲/抗性)   ← 链式：每个修改器依次减免
  → 命中 → Absorption (护盾)        ← 链式：剩余伤害被护盾吸收
  → 产出：最终伤害值 (= FinalDamage)
```

**开放点**：`IResolutionModifier.Modify(ctx, phase)` — 外部系统按 `EResolutionPhase` 声明参与的子阶段。

| EResolutionPhase | 模式 | 说明 |
|------------------|------|------|
| `Avoidance` | 短路 | 闪避率、格挡率、无敌状态。第一个返回 Avoided 即停止 |
| `Mitigation` | 链式 | 护甲值、火抗 40%、"坚韧"-15% |
| `Absorption` | 链式 | 护盾 100 点、临时血量 |

> 非伤害效果（Impact / Execute）不参与 Avoidance→Mitigation→Absorption。它们由 HitReactionComponent 直接校验（斩杀阈值 vs HP% / 硬直值 vs 霸体阈值）。

### ⑦ 反应 (Reaction) — "怎么表现"

结算结果驱动表现。分两个子阶段：

```
⑦a 数值反应 → IReactionModifier.React()  (吸血/反伤/受击回血)
⑦b 技能触发 → 匹配 PassiveAbilitySO (OnDamaged 等)
```

| 触发条件 | Caster 表现 | Target 表现 |
|---------|------------|------------|
| 伤害 > 0 | 命中反馈 | 受伤动画 / 扣血飘字 |
| 硬直触发 | — | Stagger 动画 |
| 击退 | — | 位移 (knockbackForce) |
| 格挡/闪避 | — | 格挡动画 / 火花 VFX |
| 死亡 | — | 死亡动画 / 布娃娃 |

**开放点**：`IReactionModifier.React(ctx, hit)` — 追加反伤、吸血、触发 Buff。

> **被动 vs IReactionModifier 分工**：IReactionModifier 处理纯数值修改（吸血 = 修改回复值）；PassiveAbilitySO 处理条件触发的完整技能（反伤 = 施加新的伤害效果）。顺序：数值反应先于技能触发。

### ⑧ 告知 (Broadcast) — "告诉谁"

技能完成后对外发布事件。其他系统各自订阅。

| 事件 | 载体 | 发布时间 | 消费方 |
|------|------|---------|--------|
| 命中结果 | `HitEventSO` → `SResolvedHit` | ⑦ 完成后 | Combat 统计、VFX |
| 技能反馈 | `SAbilityEvent` | ② 拒绝时 / ③ 启动时 / ⑦ 完成时 | UI 冷却图标 |
| 噪音 | `SNoiseEvent` | ③ 释放时（不论是否命中）| AI 听觉 |

> ⑧ 走 EventChannel（SO 资产管道）。⑧ 是唯一使用事件的维度——因为观察者不确定、可扩展。

---

## 两个组件

### AbilityComponent — 发送中枢

挂载在 Caster GameObject 上。负责发送面（①②③④⑤）+ selfEffects + 冷却管理。

```
AbilityComponent
├── 发送面 (Sender API)
│   ├── TryActivate(AbilityDefSO, direction)    ← 主动技能入口
│   └── NotifyEvent(eventType, subject)          ← 被动触发入口
│
├── 开放回调 (由同 GameObject 的外部实体在 Awake 设置)
│   ├── TargetFilterCallback    ← Func<PassiveAbilitySO, GameObject, string>
│   ├── ConditionCallback       ← (Slice 1) Func<AbilityDefSO, string>
│   └── EffectCallback          ← (Slice 1) Action<AbilityPipelineContext, SResolvedHit, GameObject>
│
└── 内部状态
    ├── OwnedTags (GameplayTagContainer)
    ├── runtimePassives[]
    └── cooldownEndTimes (Dictionary)
```

**不持有**：Stats、Animation、Kinematic 引用；**不持有修改器数组**。通过 CharacterActor 桥接。

> **回调模式**：AbilityComponent 不拥有修改器。它暴露回调委托，同 GameObject 上的外部实体（Trap、Stats、Buff 组件）在 Awake 设置回调。管道匹配到条件后回传信息给外部，外部自己做决定。详见 [修改器注入](#修改器注入)。

### HitReactionComponent — 接收中枢

挂载在 Target GameObject 上。负责接收面（⑥⑦）+ 被动 OnDamaged 匹配。

```
HitReactionComponent
├── 接收面 (Receiver API)
│   ├── Resolve(SResolvedHit[], caster)          ← 主动命中入口
│   └── ReceiveRawDamage(damage, type, caster)   ← 环境/陷阱裸伤害入口
│
├── 开放回调 (由同 GameObject 的外部实体在 Awake 设置)
│   ├── ResolutionCallback    ← (Slice 2) Action<AbilityPipelineContext, EResolutionPhase>
│   └── ReactionCallback      ← (Slice 2) Action<AbilityPipelineContext, SResolvedHit>
│
└── 产出
    └── SResolvedHit[] → Raise HitEventSO + 通知自身 AbilityComponent (OnDamaged)
```

**不持有**：Stats、Animation、Kinematic 引用。通过 CharacterActor 桥接。

> **两个组件的互相引用**：HitReactionComponent 结算后需通知同实体的 AbilityComponent 触发 OnDamaged 被动。此引用由 CharacterActor.Awake() 显式做双向绑定，而非各自 GetComponent。

---

## 五个修改器接口

> 粒度原则：**接口数量 = 需要开放的维度数量**。①②⑧ 不开放（内部逻辑）。④ 搜索步骤中目标过滤对外开放。

```
L3_Ability/Modifiers/
├── IConditionModifier.cs        ← ② 条件
├── ITargetFilterModifier.cs     ← 过滤 (④→⑤ 之间)
├── IEffectModifier.cs           ← ⑤ 效果
├── IResolutionModifier.cs       ← ⑥ 结算
└── IReactionModifier.cs         ← ⑦ 反应
```

### IConditionModifier（条件回调 · Slice 1）

```csharp
// 接口（外部实体实现）
public interface IConditionModifier
{
    int Priority { get; }
    void Check(AbilityPipelineContext ctx, AbilityDefSO ability);
}

// AbilityComponent 暴露的回调签名 (Slice 1 落地):
// (AbilityDefSO) → null=通过, 非null=拒绝原因
public System.Func<AbilityDefSO, string> ConditionCallback;
```

- **挂钩维度**：② 条件
- **承载方式**：同 GameObject 上的外部实体（如 Buff 组件）在 Awake 设 `ability.ConditionCallback`
- **执行模式**：短路。回调返回非 null 即拒绝激活
- **示例**：沉默 Debuff → `return "Silenced"`
- **参考**：遵循与 ITargetFilterModifier 相同的回调模式

### ITargetFilterModifier（目标过滤回调）

```csharp
// AbilityComponent 暴露的回调签名：
// (匹配到的被动技能, 候选目标) → null=放行, 非null=过滤原因
public System.Func<PassiveAbilitySO, GameObject, string> TargetFilterCallback;
```

**接口 `ITargetFilterModifier` 保留**，作为外部实体实现过滤逻辑的类型约束。但**不注入到 AbilityComponent**——外部实体在 Awake 直接设回调：

```csharp
// Trap.cs — 外部实体设回调
ability.TargetFilterCallback = (passive, target) =>
{
    // 检查 layer / faction / tag ...
    return null; // 放行
};
```

- **挂钩维度**：过滤（④→⑤ 之间，主动与被动共用）
- **粒度**：回调签名把**匹配到的 Ability 回传给外部**——外部实体（Trap）收到具体的 PassiveAbilitySO，可做出 per-ability 的过滤决定（物理攻击无法锁定灵体 / 魔法攻击可以）
- **执行模式**：AbilityComponent 匹配到被动后调用回调，null = 放行，非 null = 跳过 + 打日志
- **位置**：被动管道中 trigger 匹配之后、⑤ 效果之前；主动管道中 ④ Search 之后、⑤ 效果之前
- **所有权**：过滤逻辑属于外部实体（Trap），AbilityComponent 只负责协调——不持有修改器数组
- **与 IConditionModifier 的区别**：IConditionModifier 回答"我能放吗？"（检查 Caster）。ITargetFilterModifier 回答"该对他生效吗？"（检查 Target）。两者都走回调模式

### IEffectModifier（效果回调 · Slice 1）

```csharp
// 接口（外部实体实现）
public interface IEffectModifier
{
    int Priority { get; }
    void Modify(AbilityPipelineContext ctx, SResolvedHit hit, GameObject target);
}

// AbilityComponent 暴露的回调签名 (Slice 1 落地):
// (AbilityPipelineContext, SResolvedHit, GameObject target) → void
public System.Action<AbilityPipelineContext, SResolvedHit, GameObject> EffectCallback;
```

- **挂钩维度**：⑤ 效果
- **承载方式**：同 GameObject 上的外部实体（如 Stats 组件）在 Awake 设 `ability.EffectCallback`
- **执行模式**：链式传递。每个修改器修改 `hit.FinalDamage`，传给下一个
- **示例**：力量 +30% 伤害 → `hit.FinalDamage *= 1.3f`
- **参考**：遵循与 ITargetFilterModifier 相同的回调模式

### IResolutionModifier（结算回调 · Slice 2）

```csharp
// 接口（外部实体实现）
public enum EResolutionPhase { Avoidance = 1 << 0, Mitigation = 1 << 1, Absorption = 1 << 2, }

public interface IResolutionModifier
{
    int Priority { get; }
    EResolutionPhase Phases { get; }
    void Modify(AbilityPipelineContext ctx, EResolutionPhase phase);
}

// HitReactionComponent 暴露的回调签名 (Slice 2 落地):
// (AbilityPipelineContext, EResolutionPhase) → void
public System.Action<AbilityPipelineContext, EResolutionPhase> ResolutionCallback;
```

- **挂钩维度**：⑥ 结算
- **承载方式**：同 GameObject 上的外部实体在 Awake 设 `hitReaction.ResolutionCallback`
- **执行模式**：
  - `Avoidance`：短路，第一个 `ctx.Avoid("Dodged")` 即停止后续全部
  - `Mitigation`：链式，`ctx.CurrentDamage *= 0.6f`
  - `Absorption`：链式，`ctx.CurrentDamage -= shield`
- **Phases 使用 Flags**：一个修改器可参与多个子阶段
- **参考**：遵循与 ITargetFilterModifier 相同的回调模式

### IReactionModifier（反应回调 · Slice 2）

```csharp
// 接口（外部实体实现）
public interface IReactionModifier
{
    int Priority { get; }
    void React(AbilityPipelineContext ctx, SResolvedHit hit);
}

// HitReactionComponent 暴露的回调签名 (Slice 2 落地):
// (AbilityPipelineContext, SResolvedHit) → void
public System.Action<AbilityPipelineContext, SResolvedHit> ReactionCallback;
```

- **挂钩维度**：⑦ 反应
- **承载方式**：同 GameObject 上的外部实体在 Awake 设 `hitReaction.ReactionCallback`
- **执行模式**：链式追加。修改 `ctx.ReflectDamage`、攻击者回血等
- **示例**：荆棘 Buff → `ctx.ReflectDamage += 10f`
- **参考**：遵循与 ITargetFilterModifier 相同的回调模式

---

## 修改器回调模式

### 核心原则

**管道组件不持有修改器数组。** 它们暴露回调委托，同 GameObject 上的外部实体在 Awake 设置回调。管道匹配到条件后**回传信息给外部**，外部自己做决定。

```
外部实体 (Trap / Stats / Buff)          管道组件 (AbilityComponent / HitReactionComponent)
─────────────────────────────          ─────────────────────────────────────────────
Awake():                              暴露回调字段:
  ability.TargetFilterCallback          public Func<..., string> TargetFilterCallback;
    = MyFilter;                        public Func<..., string> ConditionCallback;
                                       public Action<...> EffectCallback;
                                       public Action<...> ResolutionCallback;
                                       public Action<...> ReactionCallback;

                                       OnTriggerEnter / TryActivate / Resolve():
                                         匹配条件 → 调用回调 → 外部返回结果
```

### 为什么是回调而不是数组注入？

| 对比 | 数组注入 (旧) | 回调模式 (新) |
|------|-------------|-------------|
| 所有权 | AbilityComponent 持有修改器 | 外部实体持有逻辑，AbilityComponent 只协调 |
| 信息流 | 修改器被动等待调用 | AbilityComponent 回传匹配到的 Ability 信息给外部 |
| 粒度 | 修改器不知道触发它的具体是哪个技能 | 回调签名包含 PassiveAbilitySO / AbilityDefSO，外部可做 per-ability 决策 |
| 注册 | CharacterActor 启动时收集 → Configure | 外部实体 Awake 时直接赋值 |
| 优先级 | 数组排序 | 外部实体自己组合/排序后再设回调 |

### 注册

外部实体在 `Awake()` 获取同 GameObject 上的管道组件，直接赋值回调：

```csharp
// Trap.cs
private void Awake()
{
    var ability = GetComponent<AbilityComponent>();
    ability.TargetFilterCallback = ShouldTrigger;
}

private string ShouldTrigger(PassiveAbilitySO passive, GameObject target)
{
    // 收到具体的 Ability + 目标，做 per-ability 的过滤决定
    if ((targetLayers.value & (1 << target.layer)) == 0)
        return $"Layer({LayerMask.LayerToName(target.layer)})";
    return null;
}
```

### 优先级分层

外部实体（如 Stats、Buff、装备）自己管理优先级。组合多个修改器时，由外部在赋值回调前排序：

| 范围 | 来源 | 示例 |
|------|------|------|
| 0–99 | 硬规则（免疫/无敌）| 无敌 → Avoidance 短路 |
| 100–199 | 角色基础属性 | 护甲、抗性、闪避基础值 |
| 200–299 | 装备 | 武器附魔、防具属性 |
| 300–399 | Buff / Debuff | 力量 Buff、减速 |
| 400–499 | 被动技能 | "闪避大师" +15% 闪避 |
| 500+ | 环境 / 全局 | 场景规则、全局修正 |

**管道只看到回调委托，不知道后面是谁。** Stats、装备、Buff、被动各自在 Awake 设回调，互不 import。

---

## 完整管道执行流

```
CharacterActor.Update()
  │
  ├── AbilityComponent.Tick()
  │     ├── 清理过期冷却
  │     └── (被动事件在此帧被 NotifyEvent() 推入)
  │
  ├── [输入驱动] AbilityComponent.TryActivate(ability, direction)
  │     │
  │     ├── ② 冷却/互斥检查
  │     ├── ② IConditionModifier.Check() 遍历
  │     │     └── 若 Cancel → SAbilityEvent.Rejected(reason) → 终止
  │     │
  │     ├── ②→③ selfEffects 扣费 (CostEffectSO → Stats.Modify)
  │     ├── ③ 挂 activeTag + 启动 AbilityDriver 阶段机
  │     │
  │     ├── (AbilityDriver 驱动到 Fire 阶段)
  │     │
  │     ├── ④ Search.Execute(caster, direction) → targets[]
  │     │
  │     ├── 过滤 ITargetFilterModifier.Filter() 逐目标 — 短路排除
  │     │
  │     ├── ⑤ 遍历 targetEffects → 构造 SResolvedHit[] (IncomingDamage = baseDamage)
  │     ├── ⑤ IEffectModifier.Modify() 链式传递
  │     ├── ⑤ selfEffects Buff → grantedTag 写入 caster.OwnedTags
  │     │
  │     └── → 直接调用 target.HitReactionComponent.Resolve(hits, caster)
  │
  ├── HitReactionComponent.Resolve(hits, caster)
  │     │
  │     ├── 标签免疫检查 (applicationBlockedTags → 过滤命中)
  │     │
  │     ├── ⑥ IResolutionModifier.Modify(Avoidance) — 短路
  │     ├── ⑥ IResolutionModifier.Modify(Mitigation) — 链式
  │     ├── ⑥ IResolutionModifier.Modify(Absorption)— 链式
  │     │     └── FinalDamage = 结算结果
  │     │
  │     ├── ⑦a IReactionModifier.React() — 反伤/吸血
  │     ├── ⑦b 通知自身 AbilityComponent.NotifyEvent(OnDamaged)
  │     │     └── 匹配 PassiveAbilitySO → 触发被动管道
  │     │
  │     └── ⑧ HitEventSO.Raise(SResolvedHit[]) + SAbilityEvent.Completed
  │
  ├── Stats.ApplyDamage(resolvedHits)       ← CharacterActor 桥接
  ├── Animation.Apply(resolvedHits)         ← CharacterActor 桥接
  └── Kinematic.ApplyKnockback(resolvedHits) ← CharacterActor 桥接
```

---

## 被动管道

被动技能复用 ⑤⑥⑦⑧，但触发入口不同：

```
事件发生 (物理/战斗/系统)
  → AbilityComponent.NotifyEvent(eventType, subject)
    → 遍历 runtimePassives，匹配 ETriggerEvent
    → ② 标签门控 (targetRequiredTag)
    → 过滤 ITargetFilterModifier.Filter(caster, subject) — 短路排除
    → ⑤ selfEffects (grantedTag)
    → ⑤ targetEffects → 构造 SResolvedHit（若 Subject 存在）
    → ⑥⑦⑧ (同上，若 Subject 存在)
```

| ETriggerEvent | Subject | Targets |
|---------------|---------|---------|
| `OnKill` | 被杀死的敌人 | Subject |
| `OnHit` | 被打中的敌人 | Subject |
| `OnDamaged` | 伤害来源 | Subject（反击目标）|
| `OnLowHP` | 无 | 空（仅 selfEffects） |
| `OnDodge` | 攻击被闪避的敌人 | Subject |
| `OnComboStage` | 无 | 空（仅 selfEffects） |
| `OnEquip` | 无 | 空（仅 selfEffects） |
| `OnEnterArea` | 进入者 | Subject |
| `OnExitArea` | 离开者 | Subject |

---

## 关键数据结构

### SResolvedHit — 管道统一产出

```csharp
public readonly struct SResolvedHit
{
    public readonly GameObject Target;
    public readonly GameObject Caster;

    // 伤害
    public readonly float IncomingDamage;   // ⑤ 产出（理想值）
    public readonly float FinalDamage;      // ⑥ 结算后（实际值）
    public readonly GameplayTag DamageType;

    // 冲击
    public readonly float StaggerValue;
    public readonly float KnockbackForce;
    public readonly Vector3 KnockbackDirection;

    // 状态
    public readonly bool Avoided;
    public readonly string AvoidReason;     // "Dodged" / "Blocked" / "Invulnerable"
    public readonly bool Executed;          // 斩杀触发

    // 来源
    public readonly AbilityDefSO SourceAbility;
}
```

### AbilityPipelineContext — 管道运行时状态

```csharp
public class AbilityPipelineContext
{
    public GameObject Caster;
    public GameObject CurrentTarget;
    public AbilityDefSO Ability;

    // ② 条件
    public bool IsCancelled;
    public string CancelReason;

    // ⑤⑥ 效果/结算
    public float CurrentDamage;
    public GameplayTag DamageType;
    public bool IsAvoided;
    public string AvoidReason;

    // ⑦ 反应
    public float ReflectDamage;

    public void Cancel(string reason) { ... }
    public void Avoid(string reason) { ... }
}
```

---

## 设计决策记录

| 决策 | 原因 |
|------|------|
| **④→⑥ 直接调用，不走事件** | ④ 已拿到 targets[] 引用，不需要事件自过滤 |
| **⑧ 走 EventChannel** | 观察者不确定、可扩展 |
| **修改器走回调模式，不走数组注入** | 管道组件不持有修改器。暴露回调委托，外部实体在 Awake 设置。回调签名把匹配到的 Ability 回传，外部做 per-ability 决策 |
| **接口数 = 开放维度数（5 个）** | ②⑤⑥⑦ + 目标过滤。避免每个子阶段一接口太碎、一个太虚 |
| **ITargetFilterModifier 独立于 IConditionModifier** | 条件检查 Caster（"我能放吗？"），过滤检查 Target（"该对他生效吗？"）。不同对象，不同钩子 |
| **EResolutionPhase 用 Flags** | 一个修改器可能参与多个子阶段（魔法护盾：减免 50% + 吸收 30 点） |
| **接口收集在 Start() 而非 Awake()** | 修改器可能需要其他组件先初始化 |
| **CharacterStats.ApplyDamage() 改为 internal** | 唯一伤害入口是 HitReactionComponent，防止绕过防御公式 |
| **修改器抛异常 → 捕获 + 跳过该修改器** | 不阻塞管道。整个修改器标记无效，所有子阶段跳过 |
| **SO 纯数据：EffectSO 不写 Execute()** | 逻辑全在管道。移除了现有的虚拟 Execute 方法 |
| **被动数值 vs 被动技能分工** | IReactionModifier 处理数值追加；PassiveAbilitySO 触发完整技能链 |
| **SDefenseContext（已考虑未落地）** | 曾考虑从 AbilityPipelineContext 独立出防御上下文，避免跨对象传参。当前选用统一上下文保持简洁。如后续需要客户端/服务端分离防御逻辑，可从此处拆分 |

---

## 子文档索引

| 文档 | 说明 |
|------|------|
| [README.md](README.md) | L3_Ability 模块总览 |
| [ability-component.md](ability-component.md) | AbilityComponent — 中枢 API + 调用链 |

---

## Phase 状态

| 状态 | 内容 |
|------|------|
| ✅ 设计完成 | 八维度管道、五接口修改器、回调模式（不持有数组）、二组件拆分、SResolvedHit 定义 |
| ✅ Done | TargetFilterCallback 回调模式落地（Trap 注入 → AbilityExecutor 回传） |
| ✅ Done | TryActivate 主动技能入口：②门控→③扣费→④搜索→⑤效果→⑥结算→⑧广播全链路 |
| ✅ Done | PeekStatCallback + ModifyStatCallback 拆分（预检+扣除两阶段） |
| Slice 2 | 落地 AbilityReactor + IResolutionModifier 分阶段 Avoidance/Mitigation/Absorption |
| Slice 3 | AbilityDriver 阶段机 + 动画驱动 |
| Phase 4.2+ | BuffEffectSO、投射物、位移、Circle 搜索、环境修改器注入 |
