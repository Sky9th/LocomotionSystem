# PropertyTree — Consumable 子树

> 日期: 2026-06-30 · 状态: 待推理
> 关联: `property-tree-structure.md`

```
ConsumableBase : Entity                          一次性使用，无耐久
│  继承: Common/DisplayName, Icon, Description, Weight, Tags
│
├── Base/
│   ├── ConsumeTime        Float     使用时间 (s)
│   └── StackSize          Int       最大堆叠数
│
├── Tags/
│   └── ConsumableType     RdTag     消耗品类型
│
├── Food : ConsumableBase                        食物
│   ├── Tags/
│   │   └── FoodType       RdTag     Meat/Vegetable/Grain/Dairy/Drink/Preserved/Foraged
│   ├── Nutrition/
│   │   ├── Nutrition      Float     饥饿恢复
│   │   └── Hydration      Float     口渴恢复
│   └── Quality/
│       ├── MoraleBonus        Float     士气加成
│       ├── ShelfLife          Float     保质期(小时)。0=永久。
│       ├── FoodQuality        RdTag     Raw/Cooked/Burnt/Preserved/Canned
│       ├── ContaminationRisk  Float     0-100, def=0。0=安全 >0触发感染
│       └── TemperatureBonus   Float     -50~50, def=0。负=降温正=升温
│
├── Medical : ConsumableBase                     药品
│   ├── Tags/
│   │   └── MedicalType    RdTag     Bandage/Pill/Injection/Herbal/Stimulant/Splint
│   ├── Heal/
│   │   ├── HealAmount             Float     HP恢复
│   │   ├── BleedReduction         Float     止血效果
│   │   ├── InfectionCleanse       Float     感染降低
│   │   ├── PainRelief             Float     疼痛降低
│   │   ├── FractureHeal           Float     0-100，骨折治疗
│   │   ├── ConsciousnessRestore   Float     0-100，意识恢复
│   │   ├── StaminaRestore         Float     0-100，体力恢复
│   │   └── HealDuration           Float     s, 0=即时，持续治疗时长
│   └── Quality/
│       └── MoraleBonus        Float     士气加成
│
├── Material : ConsumableBase                    材料
│   └── Quality/
│       ├── ScarcityTier       Int       Abundant/Common/Uncommon/Scarce/Rare
│       └── MaterialType       RdTag     材料类型
│
├── Seed : ConsumableBase                        种子
│   └── Crop/
│       ├── CropType           RdTag     作物类型
│       ├── GrowthTime         Float     生长时间 (h)
│       ├── Yield              Float     产量
│       └── SeedReturnRate     Float     0-1，种子回收率
│
└── RepairKit : ConsumableBase                   修理工具
    └── Repair/
        ├── RepairAmount       Float     修复量
        └── CompatibleToolType RdTag     兼容工具类型
```
