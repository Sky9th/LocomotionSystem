# PropertyTree — Building 子树

> 日期: 2026-06-30 · 状态: 待推理
> 关联: `property-tree-structure.md`

```
Building : Entity                                建筑——可被破坏/维修
│  继承: Common/DisplayName, Icon, Description, Weight, Tags
│         Slots/
│
├── Vitals/
│   ├── Durability         Float     当前耐久
│   ├── WeatherResist      Float     耐候性
│   └── (MaxDurability 已删除)
│
├── Combat/
│   ├── DEF_Building           Float     建筑防御
│   ├── MaterialType_Building  Int       材料类型 (0-4)
│   ├── Flammability           Float     可燃性
│   └── SoundDampening         Float     隔音 (%)
│
└── Work/
    ├── WorkSpeed_Building     Float     设施效率
    └── RestComfort            Float     休息舒适度
```
