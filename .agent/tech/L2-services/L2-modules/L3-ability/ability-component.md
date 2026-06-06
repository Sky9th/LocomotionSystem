# AbilityComponent — 能力执行中枢

> `L3_Ability/AbilityComponent.cs` · `namespace RedDust.Ability`

## 定位

挂载在 Caster GameObject 上的能力系统发送中枢。角色、陷阱、Boss 通用。

**发送面组件**：只负责 ①②③④⑤ 发送管道 + selfEffects + 冷却管理。

接收面由 `HitReactionComponent` 负责（⑥ 结算 ⑦ 反应）；详见 [ability-pipeline-design.md](ability-pipeline-design.md#两个组件)。

## 职责

| 职责 | 说明 |
|------|------|
| 状态容器 | `OwnedTags` — 运行时标签，冷却/互斥/Buff 共用 |
| 事件接收 | 物理触发 (OnTriggerEnter/Exit)、Combat 通知 (NotifyEvent)、输入 (TryActivate) |
| 被动匹配 | 遍历 `passives[]`，匹配 `ETriggerEvent`，执行效果管道 |
| 效果管道 | 读 EffectSO 字段 → 构造 SResolvedHit → 经管道 ⑤⑥⑦ → 发布 HitEventSO (⑧) |
| 冷却管理 | 时间戳存储 + 低频懒清理 |

## 两路入口

```
                     AbilityComponent
                    ┌──────────────────────────┐
  被动 — 事件驱动     │  OnTriggerEnter          │ → match passives → ⑤⑥⑦⑧
                    │  OnTriggerExit           │ → match passives → ⑤⑥⑦⑧
                    │  NotifyEvent()           │ → match passives → ⑤⑥⑦⑧
                    │                          │
  主动 — 输入驱动     │  TryActivate()           │ → ②③④⑤ → HitReactionComponent
                    │        │                 │
                    │        ▼ (Slice 3)       │
                    │  AbilityDriver           │  阶段机 Windup→Fire→Recovery
                    └──────────────────────────┘

                      HitReactionComponent (Target 侧)
                    ┌──────────────────────────┐
                    │  Resolve(hits, caster)   │ → ⑥⑦ → ⑧ HitEventSO
                    │  ReceiveRawDamage()      │ → 环境裸伤害入口
                    └──────────────────────────┘
```

## 被动管道

被动入口统一由 NotifyEvent 处理。完整执行流参见 [ability-pipeline-design.md](ability-pipeline-design.md#被动管道)。

```
NotifyEvent(eventType, subject)
  → Match: p.trigger == eventType
  → Gate: targetRequiredTag? → subject.OwnedTags.HasTag(targetRequiredTag)
  → Gate: cooldown? → OwnedTags.HasTagExact(cdKey)
  → Filter: ITargetFilterModifier.Filter(caster, subject) — 短路排除
  → ⑤ selfEffects (grantedTag 写入)
  → ⑤ targetEffects → 构造 SResolvedHit (若 Subject 存在)
  → ⑥ HitReactionComponent.Resolve(hits) — 防御公式 (Avoid/Mitigate/Absorb)
  → ⑦ IReactionModifier.React — 反伤/吸血
  → ⑧ HitEventSO.Raise(SResolvedHit)
  → Cooldown: cooldownEndTimes[cdKey] = Time.time + cooldownDuration
```

## 主动管道（Slice 1–3）

```
TryActivate(AbilityDefSO, origin, direction)
  → ② Gate: cooldown + 互斥 + IConditionModifier
  → ③ Cost: selfEffects (CostEffectSO → stats.Modify, ②通过后扣费)
  → ③ 启动 AbilityDriver 阶段机 (Windup→Fire→Recovery)
  │
  → (AbilityDriver 驱动到 Fire 阶段)
  │
  → ④ Search: targets[]
  → 过滤 ITargetFilterModifier.Filter() 逐目标短路
  → ⑤ 构造 SResolvedHit[] (IncomingDamage = baseDamage)
  → ⑤ IEffectModifier.Modify 链式
  → 直接调用 target.HitReactionComponent.Resolve(hits, caster)
  │
  → ⑥ IResolutionModifier.Modify (Avoidance/Mitigation/Absorption)
  → ⑦ IReactionModifier.React
  → ⑧ HitEventSO.Raise(SResolvedHit[])
  → Cooldown
```

## 冷却模型

- **独立冷却**: `cooldownDuration > 0, sharedCooldownTag = null` → 按技能名生成 `Skill.Cooldown.{Name}`
- **联动冷却**: `sharedCooldownTag != null` → 所有引用同一 Tag 的技能共享冷却
- **清理**: `Update()` 每 0.5s 检查过期时间戳，移除标签

## 公开 API

| 方法 | 用途 |
|------|------|
| `AddCooldown(tag, duration)` | 对自身施加冷却标签 |
| `IsOnCooldown(tag)` | 查询标签是否在冷却中 |
| `NotifyEvent(eventType, subject)` | 通知内部事件，触发被动匹配 |
| `TryActivate(AbilityDefSO, origin, direction)` | 主动技能入口 → ②③④⑤ → HitReactionComponent |

## 开放回调

> **AbilityComponent 不持有修改器数组。** 暴露回调委托，由同 GameObject 的外部实体在 Awake 设置。

| 回调 | 签名 | 用途 | Phase |
|------|------|------|-------|
| `TargetFilterCallback` | `Func<PassiveAbilitySO, GameObject, string>` | 过滤目标。null=放行 | ✅ Done |
| `ConditionCallback` | `Func<AbilityDefSO, string>` | ② 条件门控。null=通过 | Slice 1 |
| `EffectCallback` | `Action<AbilityPipelineContext, SResolvedHit, GameObject>` | ⑤ 效果修改 | Slice 1 |

## 检视面板字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `hitEventChannel` | HitEventSO | ⑧ 告知通道 — 发布 SResolvedHit 给 UI/Audio/VFX |
| `passives[]` | PassiveAbilitySO[] | 持有的被动技能 |

## 依赖

| 依赖 | 方向 |
|------|------|
| HitEventSO | 发布 SResolvedHit (⑧ 告知) |
| PassiveAbilitySO | 被动匹配 |
| EffectSO / DamageEffectSO | 读字段构造 SResolvedHit (⑤ 效果) |
| GameplayTagContainer | 标签门控 |
| GameplayTagDefinitionSO | 冷却 key 解析 |
| HitReactionComponent | ⑥ 结算入口 (通过 CharacterActor 桥接) |