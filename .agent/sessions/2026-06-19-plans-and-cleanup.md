# 2026-06-19 — 计划更新 + Animancer 清理

## 做了什么

### 计划重构
- **长期计划** — 更新当前进度（Module 系统 / Properties / Animation / Ability 数据资产），新增「施工历史」章节记录 6/11-6/19 实际日历工期，修正坐标系约定（右键寻路替代 WASD）
- **短期计划** — 全量重写：旧 Phase 4 L4_Combat 三层架构 → S1-S4 四阶段 Ability Pipeline + Properties 深度接入 + Combat 补完 + 动画补完。工期从拍脑袋的"天/周"改为基于施工历史校准的实际天数（~10天/2周）
- 新增 S4.2 Head Look IK 任务（Unity Animation Rigging 包 + MultiAimConstraint 替代动画 Vector2MixerState）
- 记录施工节奏基线到 memory（1天级架构改动 ≈ 1-2天，Editor 整批迁移 ≈ 1-2天）

### Animancer 清理
- 删除 `Assets/Data/Animancer/` 整目录（42 孤儿 clip/parameter SO + HumanTransitions + DefaultAnimator.controller）— 动画 SO 重构后全部无外部引用
- 清除 `Player.prefab` / `NPC.prefab` / `PathFinding.unity` 中 `animancerTransitions` 过期引用

### 代码微修
- `CharacterActor.cs` — 移除两个未使用 import + `GroundSystemConfigSO` 类型引用简化
