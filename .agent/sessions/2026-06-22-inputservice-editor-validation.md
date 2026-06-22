# 2026-06-22 — InputService Editor 自动验证

## Background

上一轮 commit 后，Equip3/Skill3 因未拖入 InputService.inputEvents[] 导致事件静默失效。手动双重连线（EventHub.channels + InputService.inputEvents）不可靠——需要 Editor 层自动检测遗漏。

## Changes

### InputEventBase
- 新增 `public InputActionRef` getter → 暴露 `inputAction` 引用供 Editor 读取

### InputService
- 新增 `[SerializeField] InputActionAsset` 字段
- 新增 `public InputEvents` / `public InputActionAsset` getter

### InputServiceEditor（新建）
- `[CustomEditor(typeof(InputService))]` — Inspector 增强
- 始终扫描项目 `InputEventBase` SO → 对比 `inputEvents[]`，遗漏时黄色 HelpBox
- 若 `InputActionAsset` 已赋值 → 额外对比 asset 所有 Action 是否被覆盖

### Scene
- Core.unity: 连线 Equip1/2/3 + Skill1/2/3 SO 到 InputService/EventHub

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Editor-only 验证（不动运行时） | A: 运行时自动发现 EventHub → 耦合度升高。B: SO 自注册 → 不确定生命周期。 | 最小改动，Editor 层问题 Editor 层解决 |
| 双重检查（项目 SO + InputActionAsset） | A: 只用 Action 对比 → 无法检测已有 SO 未拖入。 | 互补——SO 扫描发现"忘拖"，Action 扫描发现"Action 无对应 SO" |

## Known Issues

- [ ] `InputActionAsset` 字段在某些情况下可能被 Unity 自动绑定（用户反馈未赋值时跳过 null 门控）— 已改为不依赖该字段（P2 — 不影响功能）

## Cross-References

### Related Sessions
- [2026-06-22-character-debug-to-event-driven.md](2026-06-22-character-debug-to-event-driven.md) — 引入 Equip3/Skill3 的 session

### Flag for Design Doc Creation
- [x] No design doc needed — Editor tooling, no design-facing changes.
