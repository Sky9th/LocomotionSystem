# Session: 技术文档全量审计与修复

> 2026-07-03 ~ 2026-07-04 · 文档维护

## 触发

最近 20 次 commit 涉及大量架构改动，用户要求逐一校对技术文档，删除过时和矛盾内容。

## 执行方式

派发 6 个审计子 Agent，每个负责一个模块的文档-vs-代码校对：
- Events（EventDispatcher→EventHub）
- Entity/Character（Command/Query + CharacterContainer 删除）
- Ability（Pipeline 8 State + RdTag + AnimationClip）
- Properties（PropertyTree 重构 + Equipment 层）
- Animation（DriverArbiter + AbilityDriver + ArmPoseLayer）
- Tags/UI（RdTag 改名 + Grip 分层 + DebugOverlay）

审计完成后，派发 4 个修复 Agent 重写 13 个关键文档。

## 改动

### 删除 (4)
- `event-dispatcher.md`, `event-channel-base.md`, `base-idle-to-moving-state.md`, `base-turn-in-moving-state.md`

### DEPRECATED 标记 (6)
- `ability-component.md`, `event-hub.md`, `entity-service.md`, `entity-service-impl.md`, `base-state-key.md`, `base-layer.md`

### 重写 (13)
Events 3 + Entity 2 + Animation 4 + UI 5 + Ability 2

### 全局改名 (10)
Tag 文档 GameplayTag→RdTag

## 保留
`property-inventory.md`、`ability-*-assets.md`、`ability-inventory.md`、`property-tree-equipment.md` 等设计蓝图 — 尚未实现但设计意图有效。

## 后续建议
- `base-layer.md` 仍需关注 — 已加重写但 FSM 细节变化频繁
- `event-hub.md` — 已修正，后续 EventHub 改动需同步更新文档
- UI 新增 Overlay（AbilityBar/WeaponBar/Debug）的独立文档尚未创建
