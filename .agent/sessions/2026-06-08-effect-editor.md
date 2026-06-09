# Session: Effect Editor 独立化 + Effect 树设计 + 伤害地基模型

> 2026-06-08 · 分支 `feature/ability-pipeline`

## 产出

### 代码
- `Assets/Scripts/Services/Modules/L3_Ability/Editor/EffectEditorWindow.cs` — 独立 Effect 编辑器（2 栏：树 + 编辑），复用 AbilityTreeView
- `Assets/Scripts/Services/Modules/L3_Ability/Editor/EffectImportExport.cs` — EffectImporter + EffectImportWindow（JSON ↔ .asset，5 阶段导入）

### 设计文档
- `.agent/design/effect-inventory.md` — 全量 Effect 树（54 个资产），经 2 子 Agent 交叉验证（覆盖率审计 + 架构合规审计）
- `.agent/design/damage-source-model.md` — **核心设计**：装备定义伤害地基，Ability 只定义动作模式

## 关键设计决策

### 1. 伤害地基模型（最重要）
```
装备层 → 伤害基底（生锈刀 vs 精钢刀 = 不同物理物体）
Ability 层 → 动作模式（怎么砍/怎么开枪/怎么包扎），不持有 Damage Effect
管道 ⑤ → 近战有力量修正，枪械无（子弹动能不随人变）
管道 ⑥ → 目标侧修正（部位、角度、护甲、抗性）
```

### 2. Effect 树归属
- 装备基底 → 装备体系管理（武器伤害、弹药类型、医疗物品）
- Ability 内联 → CostEffectSO（体力/弹药消耗）、特殊动作效果
- 环境/被动 → 环境伤害、陷阱、DoT

### 3. 管道缺口（代码验证发现）
| 缺口 | 影响 |
|------|------|
| ImpactEffectSO 管道不消费 | 硬直/击退全部失效 |
| ExecuteEffectSO 管道不消费 | 斩杀机制不存在 |
| DoT tick 系统缺失 | duration>0 只加标签 |
| BuffEffectSO / HealEffectSO 未实现 | Phase 5+ 预留 |

### 4. Effect Editor
- 已修复: float? → float (JsonUtility 不支持 Nullable)
- Import 按钮已移除 → 走独立 EffectImportWindow
- 左侧宽度 260→300
- 添加了名称重命名功能

## 下一步
1. 按 damage-source-model 重新整理 effect-inventory.md 的归属分类
2. 补齐 ImpactEffectSO / ExecuteEffectSO 管道消费
3. 落地 SResolvedHit 替代 SDamageInfo
