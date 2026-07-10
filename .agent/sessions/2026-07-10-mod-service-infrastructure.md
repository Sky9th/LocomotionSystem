# 2026-07-10-mod-service-infrastructure

## Background

HybridCLR 社区版已在 v0.45.0 接入（`ee054aad`），IL2CPP 获得 `Assembly.Load(byte[])` 能力，66 AOT metadata assemblies 通过 Addressables 加载。下一步需要端到端验证外部 C# Mod DLL 能被游戏加载执行。

这是 Step 2A：游戏侧 ModLoader 基础设施。测试 Mod 在打包后单独验证（Phase B）。

## Changes

### L2_ModService 新模块（4 文件）
- `ModEntryAttribute.cs` — `[ModEntry]` 标记属性，`AttributeTargets.Class`，`Inherited = false`
- `IModEntry.cs` — `void Initialize()` 接口，对齐 `mod-architecture-framework.md` §1.1
- `ModManifest.cs` — `[Serializable]` 类，`JsonUtility.FromJson` 解析 manifest.json（modId/name/version/author/description）
- `ModService.cs` — L2 服务核心，继承 `ModuleChildMono`
  - `LoadAllMods()`: 扫描 `Application.dataPath/../Mods` → 读 manifest → `Assembly.Load(byte[])` → 反射找 `[ModEntry]` → 调 `IModEntry.Initialize()`
  - 路径：Editor = 项目根，Standalone = exe 旁
  - 详细分级日志（Debug/Info/Warning/Error），per-mod 错误隔离
  - `ModLoadResult` 记录每个 Mod 加载结果供后续 UI 查询

### SceneService 修改
- `EnsureBootReady()` 末尾：AOT metadata 加载完成后 `modService.LoadAllMods()`，确保 IL2CPP 下 AOT 类型先于 Mod DLL 注册

### ProjectSettings
- `companyName` → Sky9th, `productName` → RedDust

### .gitignore
- 新增 `/Mods/` 忽略规则（运行时目录，开发者自行填充）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Mod 入口发现：`[ModEntry]` + `IModEntry` 接口 | A: Attribute + 按名字反射搜 `Initialize()` 方法 → 无编译期检查，拼写错误静默失败 | 接口 + Attribute 给 Mod 作者 IDE 提示，实现缺失编译报错，与架构框架 §1.1 对齐 |
| ModService 创建：手动挂 GO | A: `GameService.Awake()` 中代码 auto-instantiate → 与其他 L2 服务不一致 | 保持场景配置统一，ModuleHub 自动发现 |
| Mods 路径：`dataPath/../Mods` | A: `persistentDataPath/Mods` → 藏在 AppData 里，玩家找不到 | 游戏根目录是行业惯例（RimWorld、Cities: Skylines），玩家直观 |
| 加载时机：`EnsureBootReady()` after AOT metadata | A: `GameService.Start()` after `base.Start()` → AOT metadata 还未加载 | IL2CPP 下 HybridCLR 解释器必须先注册 AOT 类型，Mod DLL 才能 resolve 引用 |

## Known Issues

- [ ] `JsonUtility` 不支持 top-level array，`ModManifest` 加 `dependencies[]` 时需切 `Newtonsoft.Json` (P2 — S1 处理)
- [ ] 未实现依赖拓扑排序 (P1 — S1，参考 `mod-architecture-framework.md` §4.1)
- [ ] 未实现 Mod ID 冲突检测 + `loadPriority` (P1 — S1，参考 §4.3)
- [ ] `link.xml` 为空，IL2CPP stripping 可能裁掉 Mod 引用的 public 类型 (P1 — 需在 S1 添加 API surface 保留规则)

## Cross-References

### Related Sessions
- `ee054aad` feat(infra): HybridCLR 社区版接入 — 本次工作的前置依赖

### Related Plans
- [../plans/twinkling-finding-cloud.md](../../plans/twinkling-finding-cloud.md) — Step 2A 实现计划
- [../plans/mod-community-decision-record.md](../../plans/mod-community-decision-record.md) — 10 项战略决策

### Related Tech Docs
- [tech/mod-architecture-framework.md](../tech/mod-architecture-framework.md) — Mod 架构框架，开发约束
- `tech/L2-services/L2-mod-service/` — 待 rd-tech-doc 创建

### Related Design Docs
- [../design/systems/mod.md](../design/systems/mod.md) — Mod 系统总览
- [../design/systems/mod-json-reference.md](../design/systems/mod-json-reference.md) — Mod JSON 格式手册

### Flag for Design Doc Creation
- [x] No design doc needed — Mod 系统设计文档已存在，本次是基础设施实现，无设计面变更。
