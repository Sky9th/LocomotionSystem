# 2026-07-10 — HybridCLR 基础设施落地

## Background

上一 Session 完成了 Mod 社区化 10 项战略决策，确定 HybridCLR 社区版作为脚本运行时方案。本 Session 将决策落地为代码：
安装 HybridCLR 包、配置 IL2CPP、打通 AOT 补充元数据的 Addressables 加载管线、验证 `Assembly.Load()` 能力。

这是短期计划 short-term.md Step 1 的全部内容——HybridCLR 接入 + 元数据加载验证。Step 2（首个测试 Mod）留给下一 Session。

## Changes

### HybridCLR 包与构建管线
- `Packages/manifest.json` — 新增 `com.code-philosophy.hybridclr` 包依赖
- `ProjectSettings/ProjectSettings.asset` — Scripting Backend 切换 IL2CPP，Api Compatibility 切换 .NET Framework
- `ProjectSettings/HybridCLRSettings.asset` — 配置 generate 输出路径到 `Assets/Settings/HybridCLR/`
- `.gitignore` — 新增 `HybridCLRData/`、`hybridclr/`、`il2cpp_plus/`

### Addressables 元数据管线
- `AddressableAssetSettings.asset` — 新增 AOTMetadata Group 注册
- `Assets/Settings/AddressableAssetsData/AssetGroups/AOTMetadata.asset` — 新建 Group
- `Assets/Settings/HybridCLR/AOTMetadata/` — 66 个 `.bytes` 补充元数据文件（从 `HybridCLRData/AssembliesPostIl2CppStrip/StandaloneWindows64/` 复制）
- 所有 AOTMetadata 资产打 `aot-metadata` label

### 运行时代码
- `AssetService.cs` — 新增 `LoadAOTMetadata()` 方法：通过 `Addressables.LoadAssetsAsync<TextAsset>("aot-metadata")` 加载元数据 → 逐文件调用 `RuntimeApi.LoadMetadataForAOTAssembly(bytes, SuperSet)`
- `SceneService.cs` — `EnsureBootReady()` 在 `RunBootInit()` 后调用 `LoadAOTMetadata()`，确保 Mod 加载前元数据就绪

### 目录整理
- `Assets/AddressableAssetsData/`（冗余）→ 删除
- `Assets/HybridCLRGenerate/` → 迁入 `Assets/Settings/HybridCLR/`，更新 HybridCLRSettings 引用路径

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 元数据走 Addressables（非 StreamingAssets） | A: 丢 StreamingAssets → 两套资产管线并存。B: 硬编码路径 File.ReadAllBytes → 测试可用，正式不行 | RedDust 全量 Addressables。元数据和其他游戏资产同一管线 |
| `LoadAOTMetadata()` 放 AssetService | A: 新建 ModService 专门管 → 过度工程（Step 1 只验证元数据加载）。B: 放 GameService → 违反 L1 不持有 Addressables 细节 | AssetService 已持有所有 Addressables 加载逻辑，自然归属 |
| Editor 下测试不 Build | A: 切到 Use Existing Build 模式 Build 后测 → 慢 | Use Asset Database 模式，Label 立即生效，秒级迭代 |

## Known Issues

- [ ] Mod 加载尚未实现——元数据就绪只是前置条件（P0 — Step 2 做）
- [ ] 仅在 Editor 验证，未做 IL2CPP Build 测试（P1 — Step 2 写测试 Mod 时一并 Build 验证）
- [ ] `hybridclr/` 和 `il2cpp_plus/` 目录在项目根——Installer 克隆的，已 ignore 但占用磁盘（P2 — 可安全删除，下次 Install 会重新生成）

## Cross-References

### Related Sessions
- [2026-07-09-mod-community-strategy.md](2026-07-09-mod-community-strategy.md) — Mod 战略决策（确定 HybridCLR 方案）

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — Step 1 HybridCLR 接入（本 Session = 1.1~1.7 全部完成）
- [../plans/mod-community-decision-record.md](../plans/mod-community-decision-record.md) — 决策〇（脚本运行时选型）

### Related Tech Docs
- [../tech/reference/hybridclr-integration.md](../tech/reference/hybridclr-integration.md) — HybridCLR 技术分析（含 Addressables 集成细节）

### Flag for Design Doc Creation
- [x] No design doc needed — internal infrastructure, no player-facing behavior change.
