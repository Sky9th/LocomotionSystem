# 2026-07-11 — Namespace 统一修正

## Background

v0.45.4 完成了目录结构重整（Core/Services/Gameplay 三层平级），但 namespace 仍沿用旧约定（容器目录跳过，不参与拼装）。例如 `Core/L1_Events/EventHub.cs` 使用 `namespace RedDust.Core` 而非 `RedDust.Core.Events`，`Services/L2_UI/` 下文件使用 `RedDust.UI` 而非 `RedDust.Services.UI`。

为贯彻"目录即 namespace"原则，容器目录（Core/Services/Gameplay）纳入 namespace 拼装，~346 个 .cs 文件全局修正。

## Changes

### Namespace 声明修正
- Core/ 下 ~57 文件：`RedDust.Core` → `RedDust.Core.{Events,GameService,GameContext,Modules,RdTag,Structs}`
- Services/ 下 ~75 文件：全部改为 `RedDust.Services.*`（13 个 L2 服务）
- Gameplay/ 下 ~191 文件：全部改为 `RedDust.Gameplay.*`（10 个 L3 模块）
- Shared/ 不变

### using 语句更新
- 全项目 ~346 文件 `using` 语句同步更新
- 处理 namespace/class 同名冲突：`GameContext`、`AssetService`、`Camera`、`EntityService`、`ModService` 使用类型别名规避
- Editor 文件的 `using` 语句移至 `#if UNITY_EDITOR` 内部

### 数据资产修正
- `properties_all.json`：`RedDust.Container.SlotDef` → `RedDust.Gameplay.Container.SlotDef`
- `Struct/` 下 15 个 `.asset`：`StructTypeName` 同步更新

### 约定文档
- `tech/conventions/namespace-rules.md` 更新为新约定
- `tech/README.md` 目录路径映射表更新

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 容器目录参与 namespace 拼装 | A: 保持容器跳过 → namespace 与目录结构不一致。B: 去掉所有层次前缀 → 丢失 L1/L2/L3 层级信息。 | 目录即 namespace，无需记忆特殊规则 |
| 用 sed 批量替换而非逐文件手改 | A: IDE rename → 太慢，346 文件。 | sed + grep 脚本化，可重复可审计 |
| namespace/class 同名冲突用别名 | A: 重命名类 → 影响太大。B: 全限定名 → 代码臃肿。 | 别名局部解决，不扩散 |

## Known Issues

- [x] `StructTypeName` 在 SO 中为硬编码字符串，namespace 变更后需手动更新 .asset + .json
- [x] 多 using 同行格式问题 — 已拆分
- [ ] `Struct/` .asset 需在 Unity 中 Reimport 才能清除运行时缓存 (P2)

## Cross-References

### Related Sessions
- [2026-07-11-directory-restructure-layers.md](2026-07-11-directory-restructure-layers.md) — 前置目录重整

### Related Plans
- [../plans/ancient-fluttering-kahn.md](../plans/ancient-fluttering-kahn.md) — Assembly Definition 拆分计划

### Related Tech Docs
- [tech/conventions/namespace-rules.md](../tech/conventions/namespace-rules.md) — 更新为容器参与拼装规则
- [tech/README.md](../tech/README.md) — 更新目录路径映射

### Flag for Design Doc Creation
- [x] No design doc needed — pure namespace refactor, no design-facing changes.
