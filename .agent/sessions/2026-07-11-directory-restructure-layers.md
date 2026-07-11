# 2026-07-11 — 目录结构重整 + Assembly Definition 引入

## Background

项目已有 352 个 `.cs` 文件，全部编译到默认 `Assembly-CSharp.dll`。编译时间长、Editor/Runtime 代码混合、L1/L2/L3 分层仅存在于文档和 namespace 约定中，目录结构未对齐。

本次分两步完成基础设施建设：Step 1 实现 Editor/Runtime 程序集分离并修复 namespace 问题；Step 2 重整目录结构使 L1/L2/L3 三层在文件系统中清晰可见。

是 [assembly-definition-split plan](../plans/ancient-fluttering-kahn.md) 的 Step 1+2 部分。

## Changes

### Step 1: Editor/Runtime 程序集分离
- 新建 `Assets/Scripts/Editor/RedDust.Editor.asmdef`（`includePlatforms: ["Editor"]`）
- 14 个 Editor/ 目录通过 asmref 挂入 `RedDust.Editor`
- 新建 `Assets/Scripts/RedDust.Runtime.asmdef` 包裹所有运行时代码
- `Shared/Editor/` 搬迁至 `Scripts/Editor/`（EUI 组件库 + Editor 基础设施）
- DOTween 生成 `DOTween.Modules.asmdef`
- 删除 `DOTweenModuleEPOOutline.cs`（引用不存在的 EPOOutline 库）
- 修正 `RdTagLookup` namespace → `RedDust.Ability.Editor`
- 修正 `TagImportExport` namespace → `RedDust.Core.Editor`

### Step 2: L1/L2/L3 目录结构重整
- `L1_Core/` → `Core/`
- Core 子目录加 `L1_` 前缀：`Events/`→`L1_Events/`, `Modules/`→`L1_Modules/`, `RdTag/`→`L1_RdTag/`, `Structs/`→`L1_Structs/`
- 松散 `.cs` 归入 `Core/L1_GameService/`, `Core/L1_GameContext/`
- 新建 `Gameplay/`（与 Services 平级），移入 10 个 `L3_*` 模块
- 删除空的 `Services/Modules/`

### 最终目录结构
```
Scripts/
├── Core/       ← L1: 数据模型 + 基础设施（6 子目录，L1_ 前缀）
├── Shared/     ← 跨层（asmref → Core）
├── Editor/     ← Editor 程序集
├── Services/   ← L2: 13 个系统服务
└── Gameplay/   ← L3: 10 个领域模块
```

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| `L1_Core/` → `Core/`，子目录加 `L1_` 前缀 | A: 保留 `L1_Core/` 不变 → 容器名累赘。B: 去掉所有 L 前缀 → 丢失层级信息。 | 容器名简洁，L1_ 前缀只标在子目录上，与 L2_/L3_ 前缀对称 |
| Gameplay 与 Services 平级 | A: 保留 `Services/Modules/` 嵌套 → L3 看起来像 L2 的子集。B: 每个 L3 独立顶层 → 太碎片。 | L3 不是 L2 的子集，是独立领域层。平级结构清晰反映 L2↔L3 对等关系 |
| 本次只做目录移动，不做跨层搬迁 | A: 同时搬迁类型+解耦 → 改动太大，风险高。 | 先让目录结构清晰，Step 3 再在干净的骨架上搬迁类型 |
| `overrideReferences: false` | A: 改成 `true` 手动枚举所有引擎模块 → 工作量大且不增值。 | `false` 自动引用引擎 DLL + 显式引用包程序集，是最优平衡 |
| DOTween 模块用官方 asmdef 生成 | A: 手写 asmdef → 被 Unity 删除/不识别。 | DOTween Utility Panel 的 ASMDEF 按钮生成合法文件，避免 GUID 冲突 |

## Known Issues

- [ ] 跨层类型搬迁（Step 3）未执行 — L2 和 L3 仍互相 import，存在循环依赖 (P1 — 后续 session)
- [ ] Runtime 仍为单一 `RedDust.Runtime` 程序集 — 待 Step 4 拆分为 Core/Services/Gameplay (P2)
- [ ] Burst 编译器在 Editor 模式下报 `RedDust.Editor` 解析失败 — 已知 Unity 2022 问题，不阻塞编译 (P3)

## Cross-References

### Related Sessions
- [2026-07-11-p1-contentid-catalog.md](2026-07-11-p1-contentid-catalog.md) — same branch, earlier work
- [2026-07-11-p5.1-item-spawn.md](2026-07-11-p5.1-item-spawn.md) — same branch, earlier work

### Related Plans
- [../plans/ancient-fluttering-kahn.md](../plans/ancient-fluttering-kahn.md) — Assembly Definition 拆分计划

### Related Tech Docs
- [tech/README.md](../tech/README.md) — 需要更新目录路径以反映新结构

### Flag for Design Doc Creation
- [x] No design doc needed — pure directory restructure and assembly definition, no design-facing changes.
