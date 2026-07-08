# 2026-07-08 — Synty 资产迁移

## Background

`Assets/Art/PolygonApocalypse/` 即将废弃，Synty 资产已迁入 `Assets/Art/Synty/` 目录。需要删除旧位置并确认道具数据不受影响。同步更新短期计划反映 v0.42.0 的 P5.0 完成状态。

## Changes

### 资产迁移
- 删除 `Assets/Art/PolygonApocalypse/`（~10400 文件）
- Synty 资产已就位 `Assets/Art/Synty/PolygonApocalypse/` + `Assets/Art/Synty/PolygonGeneric/`
- 验证全部 30 个道具 Prefab GUID 未变——`.meta` 文件随资产迁移，JSON 数据无需任何修改
- `Packages/manifest.json` + Addressables 数据刷新

### 文档维护
- 短期计划更新：P5.0 标记完成，L6 技术债标记修复，依赖树反映当前进度
- 版本升级至 v0.42.1

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 不修改 JSON 道具数据 | A: 重新扫描 Synty GUID 并映射 → GUID 相同，无意义。B: 不做验证直接假设没问题 → 风险。 | 批量扫描验证 30 个 Prefab GUID 全部匹配，确认无需改动 |

## Known Issues

- _None — 资产迁移完成，GUID 验证通过。_

## Cross-References

### Related Sessions
- [2026-07-08-s5-item-economy-data.md](2026-07-08-s5-item-economy-data.md) — 本日道具数据落地 session，资产迁移为后续操作

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — P5.0 完成标记，依赖树更新

### Flag for Design Doc Creation
- [x] No design doc needed — pure asset migration, no design-facing changes.
