# 2026-06-02 — GameplayTag L1 提升 + SO 定义层

## 目标

将 GameplayTag 从 L4_Combat 提升到 L1_Core 作为全系统基础设施，同时一步到位引入 `GameplayTagDefinitionSO`（父子引用 ScriptableObject）。

## 关键决策

### GameplayTag 是全系统设施，非 Combat 专属

对标 UE GAS，`FGameplayTag` 是引擎核心插件。RedDust 中 Tag 覆盖：战斗门控/冷却、敌人 AI 状态、伤病标记、Buff/Debuff、免疫冲突、交互锁定、任务条件、建造模式等。

后期标签量预测：4.1 → 5 个，4.2 → 13 个，Phase 5 → 60~80 个，Phase 12+ → 100~150 个。

### 双层架构：设计时 SO + 运行时 struct

| 层 | 类型 | 职责 |
|----|------|------|
| 设计时 | `GameplayTagDefinitionSO` | 父子引用，改父 leafName → 子孙 FullTag 自动级联 |
| 运行时 | `GameplayTag` (readonly struct) | HashSet 存储，O(1) 查询，零 GC |

`implicit operator` 实现 SO → struct 自动转换，`SkillDefSO` 等配置层用 SO 引用，运行时代码不感知差异。

### GameplayTag struct 增强

- `Depth`: 构造时 `.` 计数预计算，O(1) 层级深度查询
- `IsAncestorOf` / `IsDescendantOf`: 祖先/后代结构化判断
- `IsValid`: 非空校验，门控 guard 用
- `Matches(GameplayTag)`: 类型安全重载

### GameplayTagContainer 增强

- `HasTagExact`: 精确匹配，冷却标签必须精确（`"Skill.Cooldown.Slash"` ≠ `"Skill.Cooldown.Slash.Extra"`）
- `HasTagAtDepth` / `MaxDepthUnder`: 深度查询，伤病严重度等场景用
- 所有写入/查询方法加 `GameplayTag` 类型安全重载

### 零消费者 → 搬迁安全

Grep 确认无任何其他文件引用 GameplayTag。趁现在搬，代价为零。

### 后续计划

- `so-cleanup.md` — SO 资产与菜单整理独立任务（20 个 SO 类统一到 `RedDust/` 菜单根 + 资产目录重组）

## 关联

- Tech: `L1-core/gameplay-tag.md`, `L4-combat/gameplay-tag.md`, `L4-combat/gameplay-tag-container.md`
- Plans: `short-term.md` Phase 4.1 Item 1, `so-cleanup.md`
- Versions: `v0.6.4`
- Previous session: `2026-06-02-plan-restructure.md`
