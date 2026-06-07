# 2026-06-08 — 全量 Stat 设计 + 资产落地

## 做了什么

1. 从策划文档 (GDD/stats-inventory/injury-system/noise-system) + Tag 全量 + Ability 全量 出发，设计完整 Stat 体系
2. 两轮 Agent 分析 (Tag/Ability/GDD → Stat 需求) → 两轮交叉验证 → 落地技术文档
3. 第三轮 Agent 硬核真实性分析 + Tree 变种审计 → 砍 Numerical Variant / 注入真实属性
4. 最终 21 Trees + 136 Stats 通过 stats_all.json 导入 → 多次修正 → 全量验证通过

## 关键里程碑

- Tree 继承链: Actor(2) → Human(+57)/Zombie(+4), WeaponBase(3) → Melee→Firearm→Pistol/Rifle/Shotgun, AmmoBase(9)→ShotgunShell(+1), ArmorBase(9)→Head/Body/Leg(+2)
- 砍掉 35 Numerical Variant → Spawn Config
- 发现并修复 `CreateAsset` 后赋字段导致子 Tree treeJson 为空的 Bug
- 发现并修复 Stamina/Blood/Pain isConsumable=true+consumeRate=0 逻辑矛盾
- Human Tree 三级 Proficiency→Combat/Work 扁平化为两级

## 已知问题

- 编辑器仅支持两级 Tree，未来多级需扩展 StatsTreeEditorWindow
- PistolAmmo/RifleAmmo 口径基线走 Spawn Config 而非 Tree
- 21 Proficiency Stat 定义存在但未被 Ability/Physiology 消费（远期预留）
