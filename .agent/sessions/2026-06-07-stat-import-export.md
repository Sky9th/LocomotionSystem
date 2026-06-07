# 2026-06-07 — Stat 导入导出系统

## 目标

仿照已完成 Tag 导入导出（TagImportExport.cs），为 Stat 系统添加 JSON ↔ .asset 批量导入导出。

## 做了什么

1. 新建 `Assets/Scripts/Services/Modules/L3_Stats/Editor/StatImportExport.cs` (799行)
   - `StatImporter` — 静态工具类（导入/导出核心逻辑）
   - `StatImportWindow` — EditorWindow GUI（RedDust/Stat Import-Export, priority=41）
   - DTO: StatDefEntry / StatsTreeEntry / StatExportFile（纯 POCO）
   - 导出: 扫描 StatDefinitionSO + StatsTreeSO，category/defRefs/inheritsFrom 转字符串
   - 导入: 五阶段 — 创建定义 → 解析 defRefs → 创建树 → 链接继承 → 持久化

2. 导出 `Assets/Data/Stats/stats_export.json`（10 个定义 + 1 个 Human 树）
3. 删除全部 .asset → 重新导入 → 双向交叉验证通过（字段级一致）
4. 修复 6 个 bug：category 提取、treeJson 静默吞错、批次内重复 Id、Phase 1→2 刷新间隙、重复 Tree name collision、误导注释

## 设计决策

- DTO 独立于 ScriptableObject（StatDefinitionSO 直接序列化会带上 m_Name/m_Enabled 等字段）
- defRefs 使用 Id 字符串而非整数索引，方便人工编辑
- nodes 复用 JsonStatNode（已是 [Serializable]）避免重复定义
- inheritsFrom 使用 Tree name 字符串
- 导入为非破坏性幂等操作（已存在资产跳过）
- 遵循 TagImportExport.cs 的 card 布局模式（pad=6f, helpBox）

## 已知问题

- 树声明于 StatsRoot 外时 directory 导出异常路径（低优先级，不应发生）
- Unity JsonUtility 将 null string 序列化为 ""（inheritsFrom 空值），用 IsNullOrWhiteSpace 兼容处理
- JSON consumeRate 显示 0.009999999776482582（IEEE 754 精度），往返值不变
