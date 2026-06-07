# abilityTag 设计

> 2026-06-07 — 替代 categoryTag + activeTag 的统合方案

## 设计决策

**为什么删掉 categoryTag？**

- `categoryTag` 和 `abilityTag` 在运行时承担相同职责——标记"角色正在做什么"
- `abilityTag` 的层级前缀（如 `Ability.Melee.Blade.LightCut` → Parent=`Ability.Melee.Blade`）天然包含分类信息
- 双 Tag 冗余：激活时挂两个、冷却结束删两个、互斥检查两个——合并后代码量减半

**为什么 abilityTag 必须是叶标签？**

- 如果选了 `Ability.Melee` 作为 abilityTag，则 `HasTag("Ability.Melee")` 会命中所有子标签
- 无法精确标识"当前是哪个技能"——叶标签保证唯一性
- `OnValidate` 强制校验：扫描全部 GameplayTag 资产，有子则报错清空

**互斥粒度怎么控制？**

```
abilityTag = Ability.Melee.Blade.LightCut
                 ────────── Parent → 默认互斥粒度

默认：同父标签下互斥（Ability.Melee.Blade 阻止其他刀系）
扩展：extraExclusionTags[] 添加额外互斥标签（如 [Ability.Ranged] 阻止远程）
```

**Skill.* 树还建吗？**

- 不建了。`Skill.*` 回归纯编辑时引用（目录组织），不作为运行时 Tag
- `State.*` 树同理——不再用于技能激活状态标记

## 运行时流程

```
TryActivate(ability)
  └── ② 互斥 (overrideExclusion=false)
        ├── abilityTag.Parent 存在于 OwnedTags → 拒绝 (同父互斥)
        └── extraExclusionTags[] 任一存在于 OwnedTags → 拒绝 (跨分类互斥)
  └── ③b (cooldown>0) → AddTag(abilityTag)
  └── ApplyCooldown → 记录 key→abilityTag
  └── CleanupExpiredCooldowns → RemoveTag(abilityTag)
```

## 对比

| | 旧 (activeTag + categoryTag) | 新 (abilityTag) |
|---|---|---|
| 激活时挂 Tag | 2 个 | 1 个 |
| 冷却结束清理 | 2 个 | 1 个 |
| 互斥检查 | activeTag 单 Tag | Parent 前缀 + extraExclusionTags |
| 跨分类互斥 | 不支持 | extraExclusionTags[] |
| 叶标签验证 | 无 | OnValidate |
| 编辑组织 | categoryTag | abilityTag 父链 |
