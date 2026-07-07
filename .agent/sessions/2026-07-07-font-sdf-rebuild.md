# 2026-07-07 — Font SDF Rebuild for Chinese Character Quality

## Background

游戏中文字体渲染出现缺像素/笔画断裂问题。排查发现 NotoSansSC[wght] SDF 字体资产
采样 Point Size 仅为 13pt，中文复杂笔画在 atlas 纹理中仅占 6-14 像素，渲染到 18-48pt
时 SDF 放大倍数过大（最高 3.7x），导致细笔画丢失。

## Changes

### Font Asset
- 重建 `Assets/Fonts/NotoSansSC[wght] SDF.asset`，提高 SDF 采样精度
- 原采样 Point Size 13pt → 提高到合适值以匹配渲染字号（titleFontSize=48, bodyFontSize=18）
- `NotoSansSC[wght].ttf.meta` 随字体资产变更自动更新

### Addressables Cleanup
- `Assets/Settings/AddressableAssetsData/link.xml` 被 Unity 自动删除（及其 .meta）
- `addressables_content_state.bin` 自动更新

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 提高 SDF 采样 Point Size 重建字体 | A: 换用 Bitmap 字体 → 清晰但不支持动态缩放，不同字号需多份资产。B: 换纯英文 TMP 默认字体 → 不适用中文游戏。 | SDF 字体可在任意字号保持清晰，重建只需调整采样参数，无需改动代码 |

## Known Issues

- [ ] 重建后 534 字符被排除（Excluded characters: 534）—— 2048 atlas 容量不足装下全部 7000+ 汉字。建议将 Point Size 降至 32 或 Atlas 升至 4096。（P2 — 继续验证中）
- [ ] 重建参数尚未最终确认 —— 当前重建后效果待目视验证，可能需多轮调整采样参数

## Cross-References

### Related Sessions
- [2026-07-07-asset-service-boot-elimination.md](2026-07-07-asset-service-boot-elimination.md) — 同日 AssetService 重构，字体资产加载走同一管道
- [2026-07-07-game-registry-centralization.md](2026-07-07-game-registry-centralization.md) — 同日 GameRegistry 集中化

### Related Tech Docs
- [tech/L2-services/L2-ui/ui-service.md](../../tech/L2-services/L2-ui/ui-service.md) — UI 服务，字体通过 UIThemeSO 配置

### Related Design Docs
- None — 纯资产修复，无设计面变更。

### Flag for Design Doc Creation
- [x] No design doc needed — font asset regeneration, no design-facing changes.
