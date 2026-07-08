# PropertyTree — Ammo 子树

> **Last Verified**: 2026-07-08 | **Verification**: All referenced files exist, signatures match code

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
├── Weapon/
│   └── ATK                 AssetRefList<DamageEffectSO>  弹药伤害效果
│
├── Tags/
│   ├── DamageType          RdTag     伤害类型
│   └── Platform            RdTag     平台兼容
│
├── PistolAmmo : AmmoBase                      9mm 手枪弹
├── RifleAmmo : AmmoBase                       5.56mm / 7.62mm 步枪弹
└── ShotgunShell : AmmoBase                    12ga 霰弹
    └── Combat/
        ├── PelletCount     Int        弹丸数量
        └── Spread          Float      散布角度

(ArrowBase — 延后)
```

## 设计决策

| 决策 | 原因 |
|------|------|
| Weapon/ATK 指向 Ballistic DamageEffectSO | 弹药伤害按口径独立建 DamageEffectSO（9mm=22 / 5.56=45 / 7.62=55 / 12ga=80），不复用通用 Pierce(12)。AmmoSO.GetDamageEffects() 读 Weapon/ATK 与 MeleeWeaponSO 同逻辑 |
