# ⛔ OUTDATED — GameplayTag 旧版全量资产树

> **此文档已过时。** 2026-06-29 起 Tag 架构按模块产出域重构。
> 新文档：[gameplay-tag-ability.md](gameplay-tag-ability.md) — Ability 模块产出 Tag 域
>
> 保留此文档仅作历史参考。Superseded by commits `6b0c59bc` (RdTag rename) and `801eecc9` (Grip layering).

## 两层架构

```
设计时 (Unity Editor)              运行时 (C#)
━━━━━━━━━━━━━━━━━━━━━          ━━━━━━━━━━━━━━━
RdTagDefSO  ─隐式转换→  RdTag struct
 ├ leafName                        ├ Tag: string
 ├ parent (SO ref)                 ├ Depth: int
 └ FullTag (缓存 getter)           └ Matches/IsAncestorOf/IsDescendantOf

SO 资产: Assets/Data/Tags/   RdTagContainer
  按根标签分目录                        └ HashSet<RdTag> — O(1) 查询
```

## 10 处代码消费者

| # | SO 类型 | 字段 | 匹配方式 | 用途 |
|---|---------|------|---------|------|
| 1 | EffectSO | `effectTag` | `HasTag` 前缀 | 伤害/效果类型路由 → 防御公式 |
| 2 | EffectSO | `grantedTag` | 写入 OwnedTags | Buff/Debuff 标记，过期移除 |
| 3 | EffectSO | `applicationBlockedTags[]` | `HasTag` 前缀 | 施加前门控：目标持有任一即拒绝 |
| 4 | CostEffectSO | `statTag` | 精确查找 | 消耗/恢复哪个 Stat 资源 |
| 5 | AbilityDefSO | `categoryTag` | `HasTag` 前缀 | 技能大类（互斥匹配用） |
| 6 | AbilityDefSO | `activeTag` | 写入 OwnedTags | 激活期间持有，配合 TagMutualExclusionSO |
| 7 | PassiveAbilitySO | `conditionTag` | `HasTag` 前缀 | 触发条件：事件主体需持有此标签 |
| 8 | CooldownRuleSO | `cooldownTag` | **`HasTagExact` 精确** | 冷却门控——必须精确匹配，不能用前缀 |
| 9 | NoiseEventSO | `noiseType` | `HasTag` 前缀 | AI 听力行为路由 |
| 10 | TagMutualExclusionSO | `exclusionRoots[]` | 子树匹配 | 互斥组根标签，整棵子树互为排斥 |

## 完整资产树（144 叶标签，8 根）

> 目录根：`Assets/Data/Tags/`
>
> 命名规则：文件 `Tag_{leafName}.asset`；目录用 PascalCase，与标签名一致；子分支 ≥4 叶时建子目录。

---

### 1. State — 角色主动行为（互斥域）

> **全部互斥**：`TagMutualExclusionSO` 把 `State` 设为互斥根。同一时刻只能有一个 State 标签生效。
> 消费方：`AbilityDefSO.activeTag`, `TagMutualExclusionSO`, `PassiveAbilitySO.conditionTag`

```
State/
├── Tag_State.asset                  leafName="State"            parent=null
├── Tag_Idle.asset                   leafName="Idle"             parent=Tag_State
├── Tag_Dead.asset                   leafName="Dead"             parent=Tag_State
├── Tag_Sleeping.asset               leafName="Sleeping"         parent=Tag_State
├── Tag_Stunned.asset                leafName="Stunned"          parent=Tag_State
│
├── Combat/
│   ├── Tag_Combat.asset             leafName="Combat"           parent=Tag_State
│   ├── Tag_Attacking.asset           leafName="Attacking"        parent=Tag_Combat
│   ├── Tag_Blocking.asset            leafName="Blocking"         parent=Tag_Combat
│   ├── Tag_Dodging.asset             leafName="Dodging"          parent=Tag_Combat
│   ├── Tag_Aiming.asset              leafName="Aiming"           parent=Tag_Combat
│   ├── Tag_Reloading.asset           leafName="Reloading"        parent=Tag_Combat
│   ├── Tag_Staggered.asset           leafName="Staggered"        parent=Tag_Combat
│   └── Tag_DodgeIFrame.asset           leafName="DodgeIFrame"      parent=Tag_Combat
│
├── Movement/
│   ├── Tag_Movement.asset            leafName="Movement"         parent=Tag_State
│   ├── Tag_Running.asset             leafName="Running"          parent=Tag_Movement
│   ├── Tag_Crouching.asset           leafName="Crouching"        parent=Tag_Movement
│   ├── Tag_Swimming.asset            leafName="Swimming"         parent=Tag_Movement
│   └── Tag_Mounted.asset             leafName="Mounted"          parent=Tag_Movement
│
└── Interact/
    ├── Tag_Interact.asset            leafName="Interact"         parent=Tag_State
    ├── Tag_Mining.asset              leafName="Mining"           parent=Tag_Interact
    ├── Tag_Harvesting.asset          leafName="Harvesting"       parent=Tag_Interact
    ├── Tag_Building.asset            leafName="Building"         parent=Tag_Interact
    └── Tag_Crafting.asset            leafName="Crafting"         parent=Tag_Interact
```

**23 资产**，FullTag 示例：`State.Idle`, `State.Combat.Attacking`, `State.Interact.Building`

---

### 2. Skill — 技能分类 + 冷却

> 消费方：`AbilityDefSO.categoryTag`, `CooldownRuleSO.cooldownTag`
>
> 冷却标签是运行时动态生成的（如 `Skill.Cooldown.Slash_01`），不预建资产。`Cooldown/` 目录只放父节点 `Tag_Cooldown`。

```
Skill/
├── Tag_Skill.asset                  leafName="Skill"            parent=null
│
├── Combat/
│   ├── Tag_Combat.asset             leafName="Combat"           parent=Tag_Skill
│   ├── Tag_Melee.asset               leafName="Melee"            parent=Tag_Combat
│   ├── Tag_Ranged.asset              leafName="Ranged"           parent=Tag_Combat
│   ├── Tag_Throwable.asset           leafName="Throwable"        parent=Tag_Combat
│   ├── Tag_Defensive.asset           leafName="Defensive"        parent=Tag_Combat
│   └── Tag_Stealth.asset             leafName="Stealth"          parent=Tag_Combat
│
├── Utility/
│   ├── Tag_Utility.asset             leafName="Utility"          parent=Tag_Skill
│   ├── Tag_Medical.asset             leafName="Medical"          parent=Tag_Utility
│   ├── Tag_Survival.asset            leafName="Survival"         parent=Tag_Utility
│   ├── Tag_Craft.asset               leafName="Craft"            parent=Tag_Utility
│   ├── Tag_Trade.asset               leafName="Trade"            parent=Tag_Utility
│   └── Tag_Lockpicking.asset         leafName="Lockpicking"      parent=Tag_Utility
│
├── Tag_Trap.asset                   leafName="Trap"             parent=Tag_Skill
└── Tag_Cooldown.asset               leafName="Cooldown"         parent=Tag_Skill
```

**16 资产**，FullTag 示例：`Skill.Combat.Melee`, `Skill.Utility.Medical`, `Skill.Trap`, `Skill.Cooldown.Slash_01`（运行时）

---

### 3. Damage — 伤害类型路由

> 消费方：`DamageEffectSO.effectTag` → 防御公式查抗性
>
> 按 `HasTag` 前缀匹配：`HasTag("Damage.Physical")` 命中所有物理子类型。

```
Damage/
├── Tag_Damage.asset                 leafName="Damage"           parent=null
├── Tag_True.asset                   leafName="True"             parent=Tag_Damage
├── Tag_Fall.asset                   leafName="Fall"             parent=Tag_Damage
│
├── Physical/
│   ├── Tag_Physical.asset           leafName="Physical"         parent=Tag_Damage
│   ├── Tag_Slash.asset              leafName="Slash"            parent=Tag_Physical
│   ├── Tag_Blunt.asset              leafName="Blunt"            parent=Tag_Physical
│   ├── Tag_Pierce.asset             leafName="Pierce"           parent=Tag_Physical
│   └── Tag_Bite.asset               leafName="Bite"             parent=Tag_Physical
│
├── Elemental/
│   ├── Tag_Elemental.asset          leafName="Elemental"        parent=Tag_Damage
│   ├── Tag_Fire.asset               leafName="Fire"             parent=Tag_Elemental
│   ├── Tag_Cold.asset               leafName="Cold"             parent=Tag_Elemental
│   ├── Tag_Shock.asset              leafName="Shock"            parent=Tag_Elemental
│   ├── Tag_Acid.asset               leafName="Acid"             parent=Tag_Elemental
│   ├── Tag_Poison.asset             leafName="Poison"           parent=Tag_Elemental
│   └── Tag_Radiation.asset          leafName="Radiation"        parent=Tag_Elemental
│
└── Biological/
    ├── Tag_Biological.asset         leafName="Biological"       parent=Tag_Damage
    ├── Tag_Bleed.asset              leafName="Bleed"            parent=Tag_Biological
    ├── Tag_Disease.asset            leafName="Disease"          parent=Tag_Biological
    └── Tag_Suffocation.asset        leafName="Suffocation"      parent=Tag_Biological
```

**19 资产**，FullTag 示例：`Damage.Physical.Slash`, `Damage.Elemental.Fire`, `Damage.Biological.Bleed`, `Damage.True`

---

### 4. Effect — Buff / Debuff / DoT / 状态标记

> 消费方：`EffectSO.effectTag`, `EffectSO.grantedTag`, `EffectSO.applicationBlockedTags[]`, `PassiveAbilitySO.conditionTag`
>
> **与 State 的关键区别**：Effect 下的标签**不参与互斥**。角色可以同时被点燃、中毒、减速，不影响攻击/移动。

```
Effect/
├── Tag_Effect.asset                 leafName="Effect"           parent=null
├── Tag_Heal.asset                   leafName="Heal"             parent=Tag_Effect
├── Tag_Shield.asset                 leafName="Shield"           parent=Tag_Effect
├── Tag_Cleanse.asset                leafName="Cleanse"          parent=Tag_Effect
├── Tag_Mark.asset                   leafName="Mark"             parent=Tag_Effect
├── Tag_Reveal.asset                 leafName="Reveal"           parent=Tag_Effect
├── Tag_Invulnerable.asset           leafName="Invulnerable"     parent=Tag_Effect
│
├── Buff/
│   ├── Tag_Buff.asset               leafName="Buff"             parent=Tag_Effect
│   ├── Tag_Fortify.asset             leafName="Fortify"          parent=Tag_Buff
│   ├── Tag_Haste.asset               leafName="Haste"            parent=Tag_Buff
│   ├── Tag_Strengthen.asset          leafName="Strengthen"       parent=Tag_Buff
│   ├── Tag_Endow.asset               leafName="Endow"            parent=Tag_Buff
│   ├── Tag_Inspire.asset             leafName="Inspire"          parent=Tag_Buff
│   └── Tag_Regeneration.asset        leafName="Regeneration"     parent=Tag_Buff
│
├── Debuff/
│   ├── Tag_Debuff.asset             leafName="Debuff"           parent=Tag_Effect
│   ├── Tag_Slow.asset                leafName="Slow"             parent=Tag_Debuff
│   ├── Tag_Stun.asset                leafName="Stun"             parent=Tag_Debuff
│   ├── Tag_Silence.asset             leafName="Silence"          parent=Tag_Debuff
│   ├── Tag_Disarm.asset              leafName="Disarm"           parent=Tag_Debuff
│   ├── Tag_Blind.asset               leafName="Blind"            parent=Tag_Debuff
│   ├── Tag_Weakness.asset            leafName="Weakness"         parent=Tag_Debuff
│   ├── Tag_Cripple.asset             leafName="Cripple"          parent=Tag_Debuff
│   ├── Tag_Fear.asset                leafName="Fear"             parent=Tag_Debuff
│   └── Tag_Taunt.asset               leafName="Taunt"            parent=Tag_Debuff
│
├── DoT/
│   ├── Tag_DoT.asset                leafName="DoT"              parent=Tag_Effect
│   ├── Tag_Bleeding.asset            leafName="Bleeding"         parent=Tag_DoT
│   ├── Tag_Burning.asset             leafName="Burning"          parent=Tag_DoT
│   ├── Tag_Poisoned.asset            leafName="Poisoned"         parent=Tag_DoT
│   └── Tag_Infected.asset            leafName="Infected"         parent=Tag_DoT
│
├── Status/
│   ├── Tag_Status.asset             leafName="Status"           parent=Tag_Effect
│   ├── Tag_Downed.asset              leafName="Downed"           parent=Tag_Status
│   ├── Tag_Zombifying.asset          leafName="Zombifying"       parent=Tag_Status
│   ├── Tag_Exposed.asset             leafName="Exposed"          parent=Tag_Status
│   └── Tag_Marked.asset              leafName="Marked"           parent=Tag_Status
│
├── Condition/
│   ├── Tag_Condition.asset          leafName="Condition"        parent=Tag_Effect
│   ├── Tag_Hungry.asset              leafName="Hungry"           parent=Tag_Condition
│   ├── Tag_Thirsty.asset             leafName="Thirsty"          parent=Tag_Condition
│   ├── Tag_Exhausted.asset           leafName="Exhausted"        parent=Tag_Condition
│   ├── Tag_Diseased.asset            leafName="Diseased"         parent=Tag_Condition
│   ├── Tag_Freezing.asset            leafName="Freezing"         parent=Tag_Condition
│   └── Tag_Overheating.asset         leafName="Overheating"      parent=Tag_Condition
│
└── Immunity/
    ├── Tag_Immunity.asset            leafName="Immunity"         parent=Tag_Effect
    ├── Tag_BleedImmune.asset          leafName="BleedImmune"      parent=Tag_Immunity
    ├── Tag_PoisonImmune.asset         leafName="PoisonImmune"     parent=Tag_Immunity
    ├── Tag_InfectionImmune.asset      leafName="InfectionImmune"  parent=Tag_Immunity
    └── Tag_StunImmune.asset           leafName="StunImmune"       parent=Tag_Immunity
```

**46 资产**，FullTag 示例：`Effect.Buff.Haste`, `Effect.DoT.Bleeding`, `Effect.Condition.Hungry`, `Effect.Immunity.BleedImmune`

---

### 5. Noise — AI 听觉行为路由

> 消费方：`NoiseEventSO.noiseType` → AI 系统根据噪音类型决定行为（追击/警戒/无视）

```
Noise/
├── Tag_Noise.asset                  leafName="Noise"            parent=null
│
├── Combat/
│   ├── Tag_Combat.asset             leafName="Combat"           parent=Tag_Noise
│   ├── Tag_WeaponFire.asset          leafName="WeaponFire"       parent=Tag_Combat
│   ├── Tag_MeleeSwing.asset          leafName="MeleeSwing"       parent=Tag_Combat
│   ├── Tag_Explosion.asset           leafName="Explosion"        parent=Tag_Combat
│   └── Tag_Impact.asset              leafName="Impact"           parent=Tag_Combat
│
├── World/
│   ├── Tag_World.asset               leafName="World"            parent=Tag_Noise
│   ├── Tag_Footstep.asset            leafName="Footstep"         parent=Tag_World
│   ├── Tag_Door.asset                leafName="Door"             parent=Tag_World
│   ├── Tag_ItemUse.asset             leafName="ItemUse"          parent=Tag_World
│   └── Tag_BodyFall.asset            leafName="BodyFall"         parent=Tag_World
│
└── Alert/
    ├── Tag_Alert.asset               leafName="Alert"            parent=Tag_Noise
    ├── Tag_Voice.asset               leafName="Voice"            parent=Tag_Alert
    ├── Tag_Death.asset               leafName="Death"            parent=Tag_Alert
    ├── Tag_Alarm.asset               leafName="Alarm"            parent=Tag_Alert
    ├── Tag_TrapTrigger.asset         leafName="TrapTrigger"      parent=Tag_Alert
    └── Tag_Distraction.asset         leafName="Distraction"      parent=Tag_Alert
```

**17 资产**，FullTag 示例：`Noise.Combat.WeaponFire`, `Noise.World.Footstep`, `Noise.Alert.Alarm`

> AI 可用 `HasTag("Noise.Combat")` 匹配所有战斗噪音，`HasTag("Noise.Alert")` 匹配所有警戒触发。

---

### 6. Impact — 冲击/击退效果路由

> 消费方：`ImpactEffectSO.effectTag` → 防御公式查霸体阈值，VFX/AI 路由
>
> 代码 tooltip 约定：`Impact.Launch`

```
Impact/
├── Tag_Impact.asset                  leafName="Impact"           parent=null
└── Tag_Launch.asset                  leafName="Launch"           parent=Tag_Impact
```

**2 资产**，FullTag 示例：`Impact.Launch`

> 注意：与 `Noise.Combat.Impact`（撞击声）不同。`Impact.Launch` 是**效果类型**，`Noise.Combat.Impact` 是**声音类型**。

---

### 7. Stat — 属性资源标识

> 消费方：`CostEffectSO.statTag` → 定位要消耗/恢复的 StatInstance
>
> **层级与 StatsTree 路径对齐**：`Stat.Vital.HP` 对应 `"Vitals/HP"` 节点。

```
Stat/
├── Tag_Stat.asset                   leafName="Stat"             parent=null
│
├── Vital/
│   ├── Tag_Vital.asset              leafName="Vital"            parent=Tag_Stat
│   ├── Tag_HP.asset                  leafName="HP"               parent=Tag_Vital
│   ├── Tag_Stamina.asset             leafName="Stamina"          parent=Tag_Vital
│   ├── Tag_Hunger.asset              leafName="Hunger"           parent=Tag_Vital
│   ├── Tag_Thirst.asset              leafName="Thirst"           parent=Tag_Vital
│   └── Tag_BodyTemp.asset            leafName="BodyTemp"         parent=Tag_Vital
│
├── Attribute/
│   ├── Tag_Attribute.asset          leafName="Attribute"        parent=Tag_Stat
│   ├── Tag_Strength.asset            leafName="Strength"         parent=Tag_Attribute
│   ├── Tag_Agility.asset             leafName="Agility"          parent=Tag_Attribute
│   ├── Tag_Endurance.asset           leafName="Endurance"        parent=Tag_Attribute
│   ├── Tag_Intelligence.asset        leafName="Intelligence"     parent=Tag_Attribute
│   ├── Tag_Perception.asset          leafName="Perception"       parent=Tag_Attribute
│   └── Tag_Charisma.asset            leafName="Charisma"         parent=Tag_Attribute
│
├── Secondary/
│   ├── Tag_Secondary.asset          leafName="Secondary"        parent=Tag_Stat
│   ├── Tag_Blood.asset               leafName="Blood"            parent=Tag_Secondary
│   ├── Tag_Infection.asset           leafName="Infection"        parent=Tag_Secondary
│   ├── Tag_Pain.asset                leafName="Pain"             parent=Tag_Secondary
│   └── Tag_Fatigue.asset             leafName="Fatigue"          parent=Tag_Secondary
│
├── Derived/
│   ├── Tag_Derived.asset            leafName="Derived"          parent=Tag_Stat
│   ├── Tag_MoveSpeed.asset           leafName="MoveSpeed"        parent=Tag_Derived
│   ├── Tag_AttackSpeed.asset         leafName="AttackSpeed"      parent=Tag_Derived
│   ├── Tag_CarryWeight.asset         leafName="CarryWeight"      parent=Tag_Derived
│   ├── Tag_MaxHP.asset               leafName="MaxHP"            parent=Tag_Derived
│   ├── Tag_MaxStamina.asset          leafName="MaxStamina"       parent=Tag_Derived
│   └── Tag_Armor.asset               leafName="Armor"            parent=Tag_Derived
│   └── Tag_Dodge.asset               leafName="Dodge"            parent=Tag_Derived
│
└── Pool/
    ├── Tag_Pool.asset               leafName="Pool"             parent=Tag_Stat
    ├── Tag_Ammo.asset                leafName="Ammo"             parent=Tag_Pool
    ├── Tag_Fuel.asset                leafName="Fuel"             parent=Tag_Pool
    ├── Tag_Durability.asset          leafName="Durability"       parent=Tag_Pool
    └── Tag_Charge.asset              leafName="Charge"           parent=Tag_Pool
```

**27 资产**，FullTag 示例：`Stat.Vital.HP`, `Stat.Attribute.Strength`, `Stat.Derived.Armor`, `Stat.Pool.Ammo`

---

### 8. Equip — 装备槽位 + 物品类型（未来系统）

> 消费方：（Phase 5+）库存系统、装备约束、配方/战利品表

```
Equip/
├── Tag_Equip.asset                  leafName="Equip"            parent=null
│
├── Slot/
│   ├── Tag_Slot.asset               leafName="Slot"             parent=Tag_Equip
│   ├── Tag_Head.asset                leafName="Head"             parent=Tag_Slot
│   ├── Tag_Chest.asset               leafName="Chest"            parent=Tag_Slot
│   ├── Tag_Legs.asset                leafName="Legs"             parent=Tag_Slot
│   ├── Tag_Feet.asset                leafName="Feet"             parent=Tag_Slot
│   ├── Tag_Hands.asset               leafName="Hands"            parent=Tag_Slot
│   ├── Tag_WeaponPrimary.asset       leafName="WeaponPrimary"    parent=Tag_Slot
│   ├── Tag_WeaponSecondary.asset     leafName="WeaponSecondary"  parent=Tag_Slot
│   └── Tag_Backpack.asset            leafName="Backpack"         parent=Tag_Slot
│
└── Type/
    ├── Tag_Type.asset               leafName="Type"             parent=Tag_Equip
    ├── Tag_MeleeWeapon.asset         leafName="MeleeWeapon"      parent=Tag_Type
    ├── Tag_RangedWeapon.asset        leafName="RangedWeapon"     parent=Tag_Type
    ├── Tag_ThrowableWeapon.asset     leafName="ThrowableWeapon"  parent=Tag_Type
    ├── Tag_ShieldItem.asset          leafName="ShieldItem"       parent=Tag_Type
    ├── Tag_AmmoItem.asset            leafName="AmmoItem"         parent=Tag_Type
    ├── Tag_ArmorItem.asset           leafName="ArmorItem"        parent=Tag_Type
    ├── Tag_Tool.asset                leafName="Tool"             parent=Tag_Type
    ├── Tag_Consumable.asset          leafName="Consumable"       parent=Tag_Type
    ├── Tag_MedicalItem.asset         leafName="MedicalItem"      parent=Tag_Type
    ├── Tag_Material.asset            leafName="Material"         parent=Tag_Type
    └── Tag_Component.asset           leafName="Component"        parent=Tag_Type
```

**23 资产**，FullTag 示例：`Equip.Slot.WeaponPrimary`, `Equip.Type.Tool`

---

### 9. Body — 角色身体物理配置

> 消费方：Animation（FSM + 动画集选择）、AI（行为决策）、UI（状态图标）、Audio（脚步类型）、Combat（格挡判定）
>
> **设计原则：Tag 是枚举的派生产物，不是独立数据源。** Posture / Locomotion / Form 的真相源是 `SCharacterDiscrete` 枚举（`EPosture` / `EMovementGait` / `EBodyForm`）。Tag 由 `CharacterActor.RefreshBodyTags()` 每帧从枚举单向派生，外部系统只读 Tag、不写 Tag。互斥由全量刷新天然保证——每次全清 Body.* 标签后重建，不可能同时存在两个 Posture。

```
Body/
├── Tag_Body.asset                   leafName="Body"             parent=null
│
├── Form/                            ← 战备形态 — 身体放松还是进入战备
│   ├── Tag_Form.asset               leafName="Form"             parent=Tag_Body
│   ├── Tag_Relax.asset              leafName="Relax"            parent=Tag_Form
│   └── Tag_Combat.asset             leafName="Combat"           parent=Tag_Form
│
├── Posture/                         ← 高度姿态 — 站立 / 蹲伏 / 匍匐
│   ├── Tag_Posture.asset            leafName="Posture"          parent=Tag_Body
│   ├── Tag_Standing.asset           leafName="Standing"         parent=Tag_Posture
│   ├── Tag_Crouching.asset          leafName="Crouching"        parent=Tag_Posture
│   └── Tag_Prone.asset              leafName="Prone"            parent=Tag_Posture
│
├── Locomotion/                      ← 移动步态 — 静止 / 走 / 跑 / 冲刺
│   ├── Tag_Locomotion.asset         leafName="Locomotion"       parent=Tag_Body
│   ├── Tag_Idle.asset               leafName="Idle"             parent=Tag_Locomotion
│   ├── Tag_Walk.asset               leafName="Walk"             parent=Tag_Locomotion
│   ├── Tag_Run.asset                 leafName="Run"              parent=Tag_Locomotion
│   └── Tag_Sprint.asset             leafName="Sprint"           parent=Tag_Locomotion
│
├── Vital/                           ← 预留：生命状态（Alive / Downed / Dead）
└── Part/                            ← 预留：命中部位（Head / Chest / Legs / ...）
```

**13 资产（Form 3 + Posture 4 + Locomotion 5 + Body 根）**，FullTag 示例：`Body.Form.Combat`, `Body.Posture.Crouching`, `Body.Locomotion.Sprint`

#### 与 Body 无关的概念（不进入此树）

| 概念 | 归属 | 原因 |
|------|------|------|
| 移动介质（空中/游泳/攀爬） | `ELocomotionPhase` 枚举 或未来的 `Movement.*` | 环境的物理结果，不是身体配置 |
| 行为动作（攻击/闪避/交互） | `State.*` | 角色在做什么，不是身体是什么状态 |
| 武器握持（单手/双手/双持） | `Equip.Grip.*` | 装备系统，不是身体 |
| Buff/Debuff（减速/残废/倒地） | `Effect.*` | 效果修饰符，可能约束 Body 但不属于 Body |

#### 与其他 Tag 树的关系

| 位置 A | 位置 B | 关系 |
|--------|--------|------|
| `Body.Form.Combat` | `State.Combat.Attacking` | 正交共存——战备姿态 ≠ 正在攻击。可以 Body.Form.Combat + State.Idle（持枪待命） |
| `Body.Posture.Crouching` | `State.Movement.Crouching`（规划中） | **冗余**——`State.Movement.Crouching` 应在 State 树中删除，统一用 `Body.Posture.Crouching` |
| `Body.Form.Combat` | `Equip.Grip.1H_Sidearm` | 正交共存——Form 决定"怎么握"，Grip 决定"握什么" |
| `Body.Locomotion.Sprint` | `Effect.Debuff.Slow` | Effect 约束 Body——Slow 阻止 Sprint 可用 |

---

### 10. Actor — 物种 + 身份 + 阵营 + 职业

> 消费方：目标过滤（targetRequiredTag）、AI 决策、阵营声望

```
Actor/
├── Tag_Actor.asset                  leafName="Actor"            parent=null
│
├── Species/                         ← 生物学属性（不可变）
│   ├── Tag_Species.asset            leafName="Species"          parent=Tag_Actor
│   ├── Tag_Human.asset              leafName="Human"            parent=Tag_Species
│   ├── Tag_Mutant.asset             leafName="Mutant"           parent=Tag_Species
│   ├── Tag_Robot.asset              leafName="Robot"            parent=Tag_Species
│   └── Tag_Creature.asset           leafName="Creature"         parent=Tag_Species
│
├── Identity/                        ← 游戏系统中的角色类型
│   ├── Tag_Identity.asset           leafName="Identity"         parent=Tag_Actor
│   ├── Tag_Player.asset             leafName="Player"           parent=Tag_Identity
│   ├── Tag_Companion.asset          leafName="Companion"        parent=Tag_Identity
│   ├── Tag_NPC.asset                leafName="NPC"              parent=Tag_Identity
│   └── Tag_Hostile.asset            leafName="Hostile"          parent=Tag_Identity
│
├── Faction/                         ← 社会/政治归属（可变）
│   ├── Tag_Faction.asset            leafName="Faction"          parent=Tag_Actor
│   ├── Tag_Survivor.asset           leafName="Survivor"         parent=Tag_Faction
│   ├── Tag_Raider.asset             leafName="Raider"           parent=Tag_Faction
│   ├── Tag_Mercenary.asset          leafName="Mercenary"        parent=Tag_Faction
│   ├── Tag_Enclave.asset            leafName="Enclave"          parent=Tag_Faction
│   └── Tag_Nomad.asset              leafName="Nomad"            parent=Tag_Faction
│
└── Role/                            ← 战斗/职能定位
    ├── Tag_Role.asset               leafName="Role"             parent=Tag_Actor
    ├── Tag_Scout.asset              leafName="Scout"            parent=Tag_Role
    ├── Tag_Guard.asset              leafName="Guard"            parent=Tag_Role
    ├── Tag_Brute.asset              leafName="Brute"            parent=Tag_Role
    ├── Tag_Sniper.asset             leafName="Sniper"           parent=Tag_Role
    ├── Tag_Medic.asset              leafName="Medic"            parent=Tag_Role
    ├── Tag_Engineer.asset           leafName="Engineer"         parent=Tag_Role
    ├── Tag_Trader.asset             leafName="Trader"           parent=Tag_Role
    └── Tag_Leader.asset             leafName="Leader"           parent=Tag_Role
```

**26 资产**，FullTag 示例：

| 分类 | FullTag |
|------|---------|
| Species | `Actor.Species.Human`, `Actor.Species.Mutant`, `Actor.Species.Robot`, `Actor.Species.Creature` |
| Identity | `Actor.Identity.Player`, `Actor.Identity.Companion`, `Actor.Identity.NPC`, `Actor.Identity.Hostile` |
| Faction | `Actor.Faction.Survivor`, `Actor.Faction.Raider`, `Actor.Faction.Mercenary`, `Actor.Faction.Enclave`, `Actor.Faction.Nomad` |
| Role | `Actor.Role.Scout`, `Actor.Role.Guard`, `Actor.Role.Brute`, `Actor.Role.Sniper`, `Actor.Role.Medic`, `Actor.Role.Engineer`, `Actor.Role.Trader`, `Actor.Role.Leader` |

---

## 汇总

| 根标签 | 目录 | 资产数 | 状态 | 说明 |
|--------|------|--------|------|------|
| State | `State/` | 23 | Phase 1 | 主动行为，全部互斥 |
| Skill | `Skill/` | 16 | Phase 1 | Combat/ + Utility/ + Trap + Cooldown |
| Damage | `Damage/` | 19 | Phase 1 | Physical/ + Elemental/ + Biological/ + True + Fall |
| Effect | `Effect/` | 46 | Phase 2 | Buff/ + Debuff/ + DoT/ + Status/ + Condition/ + Immunity/ |
| Noise | `Noise/` | 17 | Phase 2 | Combat/ + World/ + Alert/ |
| Impact | `Impact/` | 2 | Phase 2 | 击退/硬直效果路由 |
| Stat | `Stat/` | 27 | Phase 1 | Vital/ + Attribute/ + Secondary/ + Derived/ + Pool/ |
| Body | `Body/` | 13 | Phase 1 | Form/ + Posture/ + Locomotion/，枚举派生 Tag |
| Equip | `Equip/` | 23 | Phase 5+ | Slot/ + Type/ |
| Actor | `Actor/` | 26 | Phase 4.1 | Species/ + Identity/ + Faction/ + Role/ |
| **合计** | | **212** | | |

> Phase 1（陷阱验证只需 ~25 个）：State(9) + Skill.Combat(7) + Skill(Trap+Cooldown) + Damage.Physical(5) + Damage.Biological.Bleed + Stat.Vital(6)
>
> Phase 2 补齐：剩余 State + 全部 Damage/Effect/Noise/Impact/Stat + Body.Form.Posture.Locomotion (13)

## 设计原则

| 原则 | 说明 |
|------|------|
| **State 是唯一互斥域** | TagMutualExclusionSO 只设 `[State]`。Effect/Buff/Debuff 不互斥——角色可以同时中毒+减速+燃烧 |
| **Body Tag 是枚举派生** | Body.* 标签由 `CharacterActor.RefreshBodyTags()` 从 `SCharacterDiscrete` 枚举单向派生。外部只读不写。全量刷新天然保证互斥，不使用 TagMutualExclusionSO |
| **冷却必须精确匹配** | `HasTagExact` 而非 `HasTag`。`Skill.Cooldown` 不能前缀匹配到 `Skill.Cooldown.Fireball` |
| **目录 = 层级可视化** | 目录结构直接反映标签树，Project 窗口中一眼看出父子关系 |
| **叶标签 = 1 个资产** | 不预建冷却叶标签（运行时动态生成），不建空目录 |
| **Stat 路径对齐 StatsTree** | `Stat.Vital.HP` 对应 `"Vitals/HP"`，`Stat.Attribute.Strength` 对应 `"Attribute/Strength"` |

## 互斥配置

```
TagMutualExclusionSO (全局单例)
  exclusionRoots = [ Tag_State ]
```

即 `State.Combat.Attacking` 与 `State.Interact.Building` 互斥，但 `State.Combat.Attacking` 与 `Effect.DoT.Bleeding` **不**互斥。

## 命名边界说明

以下标签对名字相似但语义不同，分别用于不同系统：

| 位置 A | 位置 B | 区别 |
|--------|--------|------|
| `State.Stunned` | `Effect.Debuff.Stun` | 前者是**动作状态**（被打断了，不能行动），后者是**Debuff 标记**（身上挂着 Stun 效果） |
| `State.Combat.DodgeIFrame` | `Effect.Invulnerable` | 前者是**闪避无敌帧**（短暂不可击中），后者是**无敌 Buff**（效果授予的无敌状态） |
| `Damage.Biological.Bleed` | `Effect.DoT.Bleeding` | 前者是**伤害类型**（出血伤害，用于抗性公式），后者是**DoT 效果标记**（身上正在流血） |
| `Effect.Mark` | `Effect.Status.Marked` | 前者是**主动技能效果**（施加标记），后者是**被动状态标记**（已被标记） |
| `Effect.Condition.Diseased` | `Damage.Biological.Disease` | 前者是**生存状态**（角色生病了），后者是**伤害类型**（疾病伤害） |

> **规则**：`State.*` = 角色此刻在做什么（互斥）；`Body.*` = 角色身体是什么物理配置（枚举派生，外部只读）；`Effect.*` = 角色身上挂着的 Buff/Debuff/状态；`Damage.*` = 伤害来源的类型分类。
>
> **补充**：`Body.Posture.Crouching` 与 `State.Movement.Crouching` 是冗余概念——蹲伏应统一走 Body。State.Movement.* 标签应在 State 树落地时剔除，仅保留瞬态动作（Swimming / Mounted 等环境强制覆盖）。
>
> **再补充**：`Body.Form.Combat` 与 `State.Combat.*` 不互斥——前者描述身体战备姿态（持久），后者描述瞬态动作。角色可以在 Body.Form.Combat 姿态下 State.Idle（持枪待命），也可以在 Body.Form.Relax 姿态下 State.Combat.Attacking（被偷袭仓促应战）。

## 目录组织规则

| 规则 | 阈值 | 例外 |
|------|------|------|
| 子分支 ≥4 叶建子目录 | ≥4 | — |
| 根目录平铺文件 ≤5 个 | ≤5 | Effect/ 有 7 个，监控增长 |
| 最大目录深度 3 层 | 3 | Tags/X/Y/file；允许 Tags/X/file |
| 1-2 个叶标签不建子目录 | ≤2 | Cooldown 已打平到 Skill/ 根下 |
| 语义优先于机械计数 | — | Noise 14→3 组，每组 4-6 个 |
