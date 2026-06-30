# PropertyTree — Ammo 子树

> 日期: 2026-06-30 · 状态: 待推理
> 关联: `property-tree-structure.md`

```
AmmoBase : Entity                                弹药——决定弹道终端物理特性
│  继承: Common/DisplayName, Icon, Description, Weight, Tags
│         Slots/
│
├── Combat/
│   ├── BaseDamage          Float     基础伤害
│   ├── Penetration         Float     穿透值
│   ├── BulletWeight        Float     弹头重量 (grain)
│   ├── OverPenetration     Float     穿透掩体倾向
│   ├── NoiseRadius         Float     击发噪音
│   ├── MuzzleVelocity      Float     弹药初速 (m/s)
│   ├── RecoilFactor        Float     后座力倍率
│   ├── AmmoReliability     Float     击发可靠性
│   └── FoulingRate         Float     枪管污损倍率
│
├── Tags/
│   ├── DamageType          RdTag     伤害类型
│   └── Platform            RdTag     平台兼容
│
├── PistolAmmo : AmmoBase                      9mm 手枪弹
├── RifleAmmo : AmmoBase                       5.56mm 步枪弹
└── ShotgunShell : AmmoBase                    12ga 霰弹
    └── Combat/
        ├── PelletCount     Int        弹丸数量
        └── Spread          Float      散布角度

(ArrowBase — 延后)
```
