# PropertyTree — Equipment 子树

> 日期: 2026-06-30 · 状态: 讨论中
> 关联: `property-tree-structure.md`

---

```
Equipment : Entity
│  继承: Common/DisplayName, Icon, Description, Weight, Tags
│         Slots/
│  自有: Equipment/Durability              Float     当前耐久（Max 覆写=上限）
│         Presentation/VisualPrefab        AssetRef  3D 模型
│         Behavior/AnimationProfile        AssetRef  动画配置
│         Behavior/AudioProfile            AssetRef  音效配置
│
├── WeaponBase                                      所有能造成伤害的装备
│   ├── Weapon/ATK               AssetList<DamageEffectSO>  武器自身伤害效果
│   ├── Weapon/AttackSpeed       Float     攻击速度倍率
│   ├── Weapon/AttackRange       Float     攻击距离
│   ├── Weapon/NoiseRadius       Float     噪音半径
│   ├── Weapon/IsTwoHanded       Bool      是否双手
│   │
│   ├── MeleeWeapon              [—]              伤害=武器自身
│   │   ├── Blade                Weapon/BleedChance       Float     流血概率
│   │   ├── Blunt                [—]                      
│   │   ├── Axe                  Weapon/ArmorPierce       Float     破甲比例
│   │   └── Polearm              [—]                      
│   │
│   ├── RangedWeapon                               伤害=弹药决定
│   │   ├── Combat/Accuracy          Float     基础精度
│   │   ├── Combat/ReloadSpeed       Float     换弹速度倍率
│   │   ├── Combat/MagSize           Int       弹匣容量
│   │   ├── Combat/AmmoCount         Int       当前弹量（运行时值）
│   │   ├── Tags/CompatibleAmmo      RdTagList  兼容弹药口径
│   │   │
│   │   ├── Firearm                                火器：枪管+自动机+配件槽
│   │   │   ├── Combat/FireRate          Float     射速 (发/秒)
│   │   │   ├── Combat/BarrelLength      Float     枪管长度（修正弹药初速）
│   │   │   ├── Combat/RecoilModifier    Float     后坐力修正（枪设计削减）
│   │   │   ├── Combat/Reliability       Float     可靠性
│   │   │   ├── Firearm/IsAutomatic      Bool      是否全自动
│   │   │   ├── Firearm/GearType         RdTag      装备类型标签
│   │   │   ├── Slots/Scope              Struct<SlotDef>  瞄具槽位
│   │   │   ├── Slots/Magazine           Struct<SlotDef>  弹匣槽位
│   │   │   ├── Slots/Muzzle             Struct<SlotDef>  枪口槽位
│   │   │   │
│   │   │   ├── Pistol                                手枪
│   │   │   │   ├── Combat/HolsterSpeed      Float     拔枪/收枪速度
│   │   │   │   └── Combat/HipFirePenalty    Float     腰射精度惩罚
│   │   │   │
│   │   │   ├── Rifle                                 步枪
│   │   │   │   └── Combat/AimTime       Float     瞄准时间
│   │   │   │
│   │   │   └── Shotgun                               霰弹枪
│   │   │       [—]
│   │   │
│   │   └── Bow                                     人力蓄能，弓弦+力量决定初速
│   │       ├── Combat/DrawSpeed         Float     拉弓速度（受力量影响）
│   │       ├── Combat/ArrowVelocity     Float     箭矢初速（弓弦张力基准）
│   │       └── Combat/HoldStamina       Float     满弓体力消耗
│   │
│   └── Throwable                                   一次性消耗，投掷即毁
│       ├── Combat/BlastRadius       Float     爆炸半径
│       └── Combat/FuseTime          Float     引信时间
│
├── ArmorBase                                      防具
│   ├── Combat/DEF                   Float     基础防御
│   ├── Combat/Coverage              Float     防护面积
│   ├── Combat/TraumaTransfer        Float     冲击传导
│   ├── Combat/ResistTypes           RdTagList  抗性类型
│   ├── Penalty/MoveSpeedPenalty     Float     移速惩罚
│   ├── Penalty/StaminaRegenPenalty  Float     体力恢复惩罚
│   │
│   ├── HeadArmor                                   头部
│   │   ├── Combat/FlashResist       Float     闪光抗性
│   │   └── Combat/NightVision       Float     夜视能力
│   │
│   ├── BodyArmor                                   躯干
│   │   ├── Combat/KnockdownResist   Float     击倒抗性
│   │   └── Combat/StanceStability   Float     站姿稳定性
│   │
│   └── LegArmor                                    腿部
│       ├── Combat/MoveSpeed         Float     移动速度
│       └── Combat/SneakSpeed        Float     潜行速度
│
├── ToolBase                                       工具（共用，通过覆写区分）
│   ├── Work/Efficiency              Float     工作效率
│   ├── Work/MaterialTier            Int       材料等级
│   ├── Work/StaminaCostPerUse       Float     每次使用体力
│   └── Tags/ToolType                RdTag      工具类型标签
│
└── Backpack : Equipment                           穿戴式容器
    └── Slots/
        └── ContainerSlot   Struct<SlotDef>  内部物品格子
```

---

## 要点

- **EffectSO 不展开**：DamageEffectSO / ImpactEffectSO / CostEffectSO 已覆盖伤害、冲击、消耗。武器属性树只放 EffectSO 管道不覆盖的字段，不把 EffectSO 内部字段平铺为独立属性
- **归属判定**：属性跟从物理本体——初速跟弹药、倍率跟瞄具、弹丸跟霰弹。不把附件/弹药的属性写在武器上
- **槽位是 Entity 通用能力**：万物都可有槽位，不需要 ContainerBase 分支。背包是 Equipment 叶子
- **MaxDurability 删除**：Durability 的 Max 覆写替代，Preset 级 Min/Max 已支持

## 已定论

| 属性 | 决策 | 原因 |
|------|------|------|
| MaxDurability | 删除 | Durability.Max 覆写替代 |
| Reach | 删除 | AttackRange 统一命名 |
| DamageType | 删除 | DamageEffectSO.effectTag 已覆盖；Def 不存在 |
| StaminaCost | 删除 | CostEffectSO 承载 |
| StunChance | 删除 | ImpactEffectSO.staggerValue 承载 |
| Knockback | 删除 | ImpactEffectSO.knockbackForce 承载 |
| CritChance | 删除 | DamageEffectSO 管道承载 |
| CritMulti | 删除 | DamageEffectSO.modMult 承载 |
| MuzzleVelocity | 保留在 AmmoBase | 弹药属性，BarrelLength 做乘数修正 |
| ScopeZoom | 删除 | 瞄具属性（Slots/Scope 上的物品） |
| PelletCount | 删除 | 霰弹属性（ShotgunShell） |
| Spread | 删除 | 霰弹+喉缩属性 |
| Recoil | 重构 | 改为 Ammo.RecoilFactor（弹药冲量）+ Firearm.RecoilModifier（枪设计削减），从 RangedWeapon 移除 |

## 已定论（结构）

| # | 决策 |
|---|------|
| MeleeWeapon | **保留** — 与远程做区分，分类层 |
| Shotgun | **保留** — 空叶但作为霰弹枪分类 |
| Blunt / Polearm | **保留** — 空叶但作为钝器/长柄分类 |
| ArrowVelocity | **留在 Bow** — 弓弦张力决定基准初速，箭重和力量为运行时修正 |

## 延后

| # | 问题 |
|---|------|
| 1 | NoiseRadius 最终层级 — WeaponBase vs RangedWeapon |
| 2 | 箭矢弹药 (ArrowBase) — 当前不需要 |
| 3 | ArmorBase: Insulation / NoiseLevel / WaterResist |
| 4 | ToolBase: WearRate / RepairMaterialTier / StaminaCostPerUse vs CostEffectSO |

## 已定论（其他）

| # | 决策 |
|---|------|
| RangedWeapon ATK | **删除** — 死属性，文档说不用就不该在树里 |
| Firearm 槽位 | **保留在 Firearm** — 特殊情况（左轮无枪口槽）用 AcceptTags=[] 处理 |
| Description | **提升到 Entity/Common** — 与 DisplayName/Icon 同级 |
| CompatibleAmmo → Tags/ | **统一路径** — Compat/ 合并到 Tags/CompatibleAmmo |
| ArmorPierce → Penetration | **统一命名** — 与 Ammo.Penetration 同概念。近战用 ratio，远程用 absolute，但名字一致 |
| BodyArmor.CarryWeightBonus | **改名 Combat/StanceStability** — 战斗稳定性，非负重加成 |
| BleedChance | **保留** — 当前仅 Blade 有，复杂度可控 |
| HoldStamina → CostEffectSO | **保留** — 弓的物理属性，运行时由 Ability Pipeline 读取并拼 CostEffectSO |
| 弩 | **不建** — 有明确设计时再加 |
| SMG | **不建** — 走 Spawn Config + GearType 标签 |
