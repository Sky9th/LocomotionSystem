# 2026-07-08 — LFS Art Asset Rules + PolygonApocalypse Import

## Background

项目 `.gitattributes` 的 LFS 规则长期使用扩展名全局匹配（`*.fbx`、`*.png` 等），覆盖了常见二进制美术格式，但遗漏了 Unity 专有格式 `.asset` 和 `.mat`。导入 PolygonApocalypse 末日场景资产包时发现 ~3,100 个 `.asset`/`.mat` 文件（57.7 MB）会直接进入 git blob 而非 LFS，导致仓库永久膨胀。

纯美术资产（`Assets/Art/` 下）不需要 git diff——它们不是代码，不会手动合并。正确的做法是按路径区分：Art 目录下的 Unity 格式走 LFS，其他目录的 `.asset`（可能是游戏数据/配置表）保留 diff 能力。

## Changes

### LFS 配置
- `.gitattributes` — 新增 `Assets/Art/**/*.asset` 和 `Assets/Art/**/*.mat` 路径规则，仅 Art 目录下走 LFS

### 资产导入
- `Assets/Art/PolygonApocalypse/` — 导入末日场景资产包，含模型/贴图/材质/Convex 碰撞体，~6,300 文件 ~220MB

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 路径规则 `Assets/Art/**/*.asset` 而非全局 `*.asset` | A: 全局 `*.asset` → 误伤 Scripts/ 下的游戏数据 SO，失去 diff 能力。B: 不处理 → 57.7 MB 永久留在 git 历史 | 路径规则精确限定美术资产范围，代码/数据目录不受影响 |
| `.meta` 文件不进 LFS | A: 也加 LFS → 5,233 个 ~5MB 小文件走 LFS 无意义，指针文件本身也占空间 | .meta 是文本、体积小、git 压缩效率高，且偶尔需要 diff 查看 GUID/引用变化 |

## Known Issues

- [ ] PolygonApocalypse 资产为第三方资源，需记录到待购买清单（license 确认） — P2
- [ ] Convex 目录下有文件名含空格的重复资产（`... 1.asset`、`... 2.asset`），疑似导入时命名冲突，需清理 — P2

## Cross-References

### Related Sessions
- [2026-07-08-item-editor-window.md](2026-07-08-item-editor-window.md) — 同日 ItemEditor 功能扩展
- [2026-07-08-design-dir-cleanup.md](2026-07-08-design-dir-cleanup.md) — 同日 design/ 目录清理

### Related Tech Docs
- None — 无 .cs 文件改动，不涉及技术文档更新

### Related Design Docs
- None — 纯资产导入 + 配置补全，无设计面变更

### Flag for Design Doc Creation
- [x] No design doc needed — asset import + LFS config, no design-facing changes.
