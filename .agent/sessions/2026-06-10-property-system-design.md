# 2026-06-10 — 属性系统完整设计 + Description 字段 + Editor UX

## 做了什么

1. **PropertyDefSO 新增 `Description` 字段** — 每个属性定义现在有中文用途说明
2. **全量属性体系推理** — 基于 GDD + 全部子系统设计文档，推演出 8 族 30 Tree ~185 Def
3. **双 Agent 交叉验证** — GDD/策划角度 + 架构/代码角度，发现并修复 3 个 Blocking Issues + 多个架构问题
4. **Editor UX 改进** — DefDetailPopup 详情弹窗、tooltip、创建对话框、去 Type 列
5. **数据落地** — `properties_export.json` 完整重写，所有 Def 带 description

## 关键决策

- Template 不能覆写 Default（PropertyNode 无 value 字段）
- 祖先优先合并，子同名 NodeId → 冲突告警
- FlashResist/NightVision 装备族(0~1) 和 Actor 族(0~100) 需独立 Def
- Armor 子节点保持 `Combat/` 路径（兼容现有 export）
- CurrentWeight 不进 Tree（Derived 属性）
- PainResist/KnockdownResist 补入 Human Resistance
- Container 族独立于 Weapon/Armor/Tool

## 改动的文件

- `PropertyDefSO.cs` — +Description
- `PropertyDefSOEditor.cs` — Inspector 显示 Description
- `PropertyImportExport.cs` — DTO + description
- `PropertyTreeEditorPopups.cs` — CreateDefDialog + DefDetailPopup
- `PropertyTreeEditorWindow.cs` — tooltip、? 按钮、去 Type 列
- `properties_export.json` — 全量重写
- `property-inventory.md` — 新建完整设计文档
- 删除 Test/aaa 测试树、test_import.json

## 已知问题

- 已有 .asset 文件的 Description 为空，需从 JSON 重新 Import 填充
