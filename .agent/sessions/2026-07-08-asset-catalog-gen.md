# 2026-07-08 — PolygonApocalypse Asset Catalog Generation

## Background

PolygonApocalypse 资产包（1820 个 Prefab）已完整引入项目，但策划侧没有任何可查阅的资产清单。后续还有更多资产包引入，每次都需要将 Prefab 目录结构转译为策划可读的 markdown 清单。之前完全靠人工列出，效率低且容易遗漏。

本次目标是建立一套自动化流程：用脚本一次性扫描所有 Prefab，提取结构化信息，按类目生成 MD 文档。同时保证脚本可复用于后续资产包，只改参数即可增量追加。

## Changes

### 资产清单生成脚本
- 新增 `.agent/scripts/gen_asset_docs.py` — 通用 Prefab 扫描 + MD 生成脚本
  - 解析 Unity YAML 文本格式的 `.prefab` 文件
  - 提取根 GameObject `m_Name` 和所有子物体名称
  - 按类目分组输出，同文件合并、不同来源加「来源」列区分
  - Props (672) 按名称关键词自动聚类为 100+ 子类

### 资产清单文档
- 新增 `.agent/design/art-assets/_index.md` — 总览：统计 + 命名规范 + 各文件索引
- 新增 10 个类目 MD，共 1820 条目：

| 文件 | 内容 | 数量 |
|------|------|------|
| `buildings.md` | 建筑结构 (Bunker组件 + 公寓/修车厂) | 207 |
| `characters.md` | 角色 + 附件 (装甲/背包/发型) | 116 |
| `dead-bodies.md` | 尸体 | 55 |
| `environment.md` | 环境地形 (道路/桥梁/植被/海滩) | 242 |
| `fx.md` | 特效 (火/烟/血/雨/辐射) | 28 |
| `generic.md` | 通用 (天空球/树/石头/草地) | 26 |
| `items.md` | 物品道具 (弹药/消耗品/工具) | 93 |
| `props.md` | 道具 (按 100+ 子类分段) | 672 |
| `vehicles.md` | 载具 + 附件 (装甲/引擎/轮胎) | 112 |
| `weapons.md` | 武器 (枪械/近战/模块配件/载具武器) | 269 |

### 表格格式
- 资产名列（带反引号方便复制）
- 来源列（`PolygonApocalypse`，后续新包增量行）
- 子物体列（复杂 Prefab 的内部结构可见，上限 8 个 + 溢出计数）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 按类目分文件（buildings.md / weapons.md），不同来源加「来源」列 | A: 按来源分文件 → 后续资产多时策划要跨文件找同类资产。B: 一文件全量 → 1820 条太长无法阅读。 | 类目是策划查阅的主维度，来源是辅助过滤条件 |
| Props 自动按关键词聚类 | A: 不做聚类，672 条平铺 → 无法检索。B: 手工预设子类 → 维护成本高，新包不匹配。 | 自动聚类虽有边界模糊（如 Barrel + BarrelStack 分到两组），但随着数据增多可以持续调优关键词表 |
| Python 脚本而非 C# Editor 脚本 | A: C# 可利用 Unity API 读取 Prefab → 需 Unity 运行，编译慢。B: Bash 纯文本 grep → 无法正确处理多 GameObject 嵌套。 | Python 解析 YAML 文本足够提取 m_Name + 子物体，不依赖 Unity，命令行直接跑 |
| Props 子类索引放在文件头部 | A: 纯平铺 → 太长无法导航。B: 拆成多个 props-*.md → 破坏了「类目 = 文件」的一致性。 | 单文件 + 锚点索引 = 查阅友好 + 结构简单 |

## Known Issues

- [ ] Props 自动聚类存在边界不清：`Barrel` 和 `BarrelStack` 分到不同子类，`JunkPile` / `TrashPile` / `TrashBag` 也未合并 — P2 — 后续调整关键词表即可
- [ ] 脚本未提取 Material/Mesh 引用 GUID — 当前不必要，策划不关心材质路径 — 后续如需可扩展
- [ ] 子物体列表截断为 8 个（+ 溢出计数），复杂 Prefab 如 `SM_Wep_Revolver_02`（12 个子物体）有截断 — P3 — 一般够用

## Cross-References

### Related Sessions
- [2026-07-08-lfs-art-asset-rules.md](2026-07-08-lfs-art-asset-rules.md) — 同一资产包引入时配置了 LFS 规则
- [2026-07-08-design-dir-cleanup.md](2026-07-08-design-dir-cleanup.md) — design/ 目录清理，为 art-assets/ 腾出空间

### Related Design Docs
- [../design/art-assets/_index.md](../design/art-assets/_index.md) — 本次生成的资产总览
- [../design/game-overview.md](../design/game-overview.md) — GDD，资产清单服务于其中世界观/美术方向

### Related Tech Docs
- None — 纯文档和工具脚本，无 C# 代码变更。

### Flag for Design Doc Creation
- [x] No design doc needed — asset catalog is reference material, not game design.
