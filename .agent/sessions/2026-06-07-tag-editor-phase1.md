# 2026-06-07 — Tag Editor Phase 1 (UI)

## 背景
创建技能需要大量标签——手动 `CreateAssetMenu` + 填 leafName + 拖 parent 引用，极其繁琐。决定先构建独立的标签编辑器。

## 成果
- **TagEditorWindow** — `Tools/RedDust/Tag Editor`，StatsTree 卡片风格窗口
  - Header + Toolbar + Search + 左 Tree(自适应)/右 Inspector(300px) + StatusBar
  - 硬编码 Damage 标签树验证 UI 流程
  - Inspector：已有标签 → 详情/未创建 → 创建表单（含缺失祖先提示）
- **TagTreeView** — 共享渲染器，StatsTree 嵌套布局
  - 严格参照 StatsTree 的 `BeginHorizontal → [foldout] [Space] → BeginVertical(右块) → 名称行 → 子 helpBox`
  - 折叠区固定在 `BeginHorizontal(Width=18px)` 内，绝不跳变
  - 字体统一 label 级别，废弃 miniLabel
  - 已有标签正常 · 未创建加粗灰显 · 选中加粗
- **TagPicker** — 可嵌入 Popup，`Show(rect, rootFilter, allowCreate, onSelected)`

## 关键技术点
- 编辑器放 `L1_Core/GameplayTag/Editor/`，跟随代码
- 嵌套布局避免手动像素缩进（子 Tree 不会空出一大截）
- 标签无 Branch/Tag 之分，目录只是视觉辅助
- Phase 2 接入真实数据（TagTreeModel + TagCreator）

## 关联
- plan: `.agent/plans/ability-creation-wizard.md`（Ability Wizard 方案归档）
- plan: `.agent/plans/indexed-sprouting-volcano.md`（当前 Tag Editor 计划）
