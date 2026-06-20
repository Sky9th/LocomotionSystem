---
description: 归档设计/技术/会话文档到 .agent 目录。编排 rd-session-doc、rd-tech-doc、rd-design-doc 三个子技能。
argument-hint: [主题描述 | 由 rd-commit 自动调用]
allowed-tools: Read, Write, Edit, Glob, Bash, Grep
---

将用户描述的改动或知识归档到 `.agent/` 目录的正确位置。

rd-doc 是一个**编排器（orchestrator）**：判断归档类型 → 调度子技能完成写入 → 执行质量检查 → 输出归档报告。rd-doc 自身不包含文档模板 — 模板由三个子技能各自负责。

---

## 一、子技能

| 子技能 | 文件 | 受众 | 内容 |
|--------|------|------|------|
| **rd-session-doc** | `.claude/skills/rd-session-doc/SKILL.md` | 开发者 | 本次改了什么、为什么、决策、已知问题、交叉引用 |
| **rd-tech-doc** | `.claude/skills/rd-tech-doc/SKILL.md` | 程序员 | 模块实现细节、调用链、API、架构，L1→L5 层级 |
| **rd-design-doc** | `.claude/skills/rd-design-doc/SKILL.md` | 策划 | 系统定位、玩法机制、数值设计、玩家体验、边界情况、A测范围 |

其他目录直接写入（不经过子技能）：
- `plans/` — 长期/短期开发计划
- `references/` — 截图、外部资料

> `archive/tech-v1/` 和 `archive/tech-v2/` 为旧文档归档，不再新增。

---

## 二、Phase 1：分类与分发

### 2.1 调用来源判断

| 来源 | 判断方式 | 路径 |
|------|---------|------|
| **rd-commit 触发** | 上下文中 rd-commit 已完成 `git diff --stat` + 版号升级 | → 第三节「完整三层归档」 |
| **用户直接调用** | 用户输入话题描述，无前置 rd-commit | → 2.2「知识归档分发」 |

### 2.2 知识归档分发

用户说"归档 XX 设计"或"记录 YY 方案"时，按内容类型路由：

| 内容特征 | 调用 | 写入位置 |
|---------|------|---------|
| 系统定位、玩法机制、数值设计、玩家体验 | **rd-design-doc** | `design/{subsystem}/{topic}.md` |
| 技术实现、架构设计、调用链、代码约定 | **rd-tech-doc** | `tech/`，遵循 L1→L5 层级 |
| 会话总结、本次做了什么、决策、已知问题 | **rd-session-doc** | `sessions/YYYY-MM-DD-{topic}.md` |
| 开发计划 | 直接写入 | `plans/` |
| 多种内容混合 | 依次调用多个子技能 | 每个子技能只处理自己负责的部分 |

用户只提供模糊描述时，先问清：涉及哪些模块？有没有设计面变更？然后按答案分发。

### 2.3 设计影响判断

代码改动时，是否需要创建 design doc：

**需要 design doc（调用 rd-design-doc）**：

| 情况 | 理由 |
|------|------|
| 新机制或新系统 | 策划需要了解新系统是什么、怎么玩 |
| 玩家可见的行为变化 | 改变了玩家体验或操作方式 |
| 数值/平衡设计 | 数值是策划核心关注点 |
| 系统间交互变化 | 改变了两个系统的协作方式 |
| UX 流程设计 | 屏幕、菜单、操作方式变化 |

**不需要 design doc（在 session 中注明）**：

| 情况 | 理由 |
|------|------|
| 内部重构（行为不变） | 策划不关心实现细节 |
| Bug 修复 | 恢复预期行为，无设计面变更 |
| 资产导入 | 资源管理，无设计面变更 |
| 纯代码风格/命名整理 | 无功能变化 |
| 仅修改方法内部实现（API 不变） | 外部行为不变 |

**需要 tech doc（调用 rd-tech-doc）**：

| 情况 | 动作 |
|------|------|
| 新增 .cs 文件 | 新建子模块文档 + 更新模块总领 + 更新 `tech/README.md` |
| 修改已有类的公开 API | 更新对应方法节 |
| 修改调用链 | 更新调用链 ASCII 图 |
| 删除类/文件 | 删除对应 .md + 更新总领索引 |

**不需要 tech doc**：仅修改方法内部实现（API 不变）、仅修改 Unity Prefab/Scene。

---

## 三、Phase 2：三层编排（代码改动归档）

### 3.1 准备：确定改动范围

```
1. git diff --stat → 提取改动的 .cs 文件列表
2. 按模块分组，映射到 tech/ 对应文档路径
3. 识别涉及的设计域（角色、战斗、UI、音频等）
4. 按 2.3 节判断设计影响等级
```

### 3.2 第一步：rd-session-doc

通过 Skill 工具调用 `rd-session-doc`，写入 `sessions/YYYY-MM-DD-{topic}.md`。

session doc 五段结构（详见 rd-session-doc SKILL.md）：
- `## Background` — 为什么做、解决什么问题
- `## Changes` — 按子系统分组的改动列表
- `## Decisions` — 决策表（含至少一个被拒绝的替代方案）
- `## Known Issues` — 未解决的问题和临时方案
- `## Cross-References` — 关联 session/plan/tech/design 文档链接

**命名**：`YYYY-MM-DD-{topic}.md`，kebab-case ASCII only。

### 3.3 第二步：rd-tech-doc

通过 Skill 工具调用 `rd-tech-doc`，对 3.1 中识别出的每个受影响模块更新技术文档。

rd-tech-doc 执行：
1. 运行 Pre-Write Checks（文件存在性、死类检测、签名验证、计数验证）
2. 新模块 → 创建子模块文档 + 模块总领 + 更新 `tech/README.md`
3. 已有模块 → 更新调用链/方法/耦合模块节
4. 删除文件 → 删除对应 .md + 更新索引
5. 加盖 Last Verified 戳

### 3.4 第三步：rd-design-doc（条件执行）

仅当 3.1 中设计影响判断为「是」时，通过 Skill 工具调用 `rd-design-doc`。

design doc 六段结构（详见 rd-design-doc SKILL.md）：
- `## System Positioning` — 系统在游戏中的位置、目的、范围、输入输出
- `## Gameplay Mechanics` — 核心循环、玩家交互、规则、进阶
- `## Numeric Design` — 基础值、公式、曲线、平衡目标、调参入口
- `## Player Experience` — 上手、反馈、清晰度、情感弧线、失败状态
- `## Edge Cases` — 边界值、交互冲突、缺失依赖、存档、多实体（≥5 条）
- `## A测 Scope` — A测交付、推迟、简化

**命名**：`design/{subsystem}/{topic}.md`，kebab-case ASCII only。

**`design/character/` 特别规则**：如果改动涉及 L3-character 且该目录仍为空，**必须**创建角色系统的首个设计文档。

如果判定为**不需要** design doc，在 session 的 Cross-References 中注明：
```
### Flag for Design Doc Creation
- [x] No design doc needed — [refactor/fix/import], no design-facing changes.
```

### 3.5 第四步：交叉验证

```
□ sessions/YYYY-MM-DD-{topic}.md        — 存在，≥25 行，5 节齐全
□ tech/ 受影响模块文档                   — 存在且更新，Last Verified 戳为今天
□ tech/README.md                        — 文件树与代码一致
□ design/{subsystem}/{topic}.md         — 如果判定为「是」→ 存在；如果「否」→ session 中有说明
□ Cross-References 交叉引用              — session ↔ tech ↔ design 之间链接闭合
```

---

## 四、Phase 3：质量门

### 4.1 Session 质量门（rd-session-doc 内部执行）

| 检查项 | 标准 | 不通过动作 |
|--------|------|-----------|
| 行数 | ≥ 25 行（绝对下限 15） | 展开 Background/Decisions |
| 五段齐全 | Background + Changes + Decisions + Known Issues + Cross-References | 补缺失段 |
| 命名 | `YYYY-MM-DD-{topic}.md`，kebab-case ASCII | 修正命名 |
| 单主题 | 不混合无关子系统 | 拆分或标注 |

### 4.2 Tech 质量门（rd-tech-doc 内部执行）

| 检查项 | 标准 | 不通过动作 |
|--------|------|-----------|
| 新鲜度 | 引用的 .cs 文件存在、类名正确、签名匹配 | 标记 STALE |
| 五段齐全 | 调用链 + 耦合模块 + 公开属性 + 方法 + 未来规划 | 补缺失段 |
| Last Verified | 戳记为今天日期 | 加盖 |
| 根总领 | `tech/README.md` 与代码一致 | 更新 |

### 4.3 Design 质量门（rd-design-doc 内部执行）

| 检查项 | 标准 | 不通过动作 |
|--------|------|-----------|
| 存在性 | 判定「是」→ 已创建；「否」→ session 有说明 | 补创建或补说明 |
| 行数 | ≥ 30 行 | 展开 Numeric Design / Edge Cases |
| 六段齐全 | Positioning + Mechanics + Numeric + Experience + Edge Cases + A测 Scope | 补缺失段 |
| 具体性 | 有数字或有 TBD+原则，无空泛描述 | 补充具体内容 |

---

## 五、Phase 4：归档报告

所有文档写完后，输出以下格式的报告：

```
📄 Session: sessions/YYYY-MM-DD-{topic}.md (N 行 ✅)
📘 Tech:
   - tech/.../module-a.md (updated ✅)
   - tech/.../module-b.md (new ✅)
📐 Design:
   - design/{subsystem}/{topic}.md (new ✅)
   - 或: No design doc needed — internal refactor.

⚠ Stale docs detected:
   - tech/.../stale-doc.md — references deleted class AnimationAliasProfile
   - ...

📋 Missing coverage:
   - design/character/ — directory empty, consider creating character system design doc
   - tech/.../ — 3 .cs files without corresponding tech docs

💡 Suggestions:
   - 建议一
   - 建议二
```

状态标记：`✅` 通过 | `⚠` 通过但有警告 | `❌` 未通过，需补充

### Staleness 检测

在 tech doc 更新后，扫描本次改动目录下的**同级和父级未更新文档**：

```
1. 收集本次更新的 tech/ 目录路径
2. 对该目录下其他 .md 文件，提取调用链和耦合模块节中提到的类名
3. 对每个类名，在 Assets/Scripts/ 中 grep 搜索
4. 类名在源代码中不存在 → 标记为 stale，在报告 ⚠ 区列出
5. 文件头引用路径的 .cs 文件不存在 → 标记为 stale
```

**只扫描本次改动目录的同级和父级文档**，避免全量扫描。

---

## 六、与 rd-commit 的集成

rd-commit 在步骤 3 调用 rd-doc。rd-doc 接受以下上下文：

| 上下文 | 来源 | 用途 |
|--------|------|------|
| `git diff --stat` 结果 | rd-commit 步骤 1 | 确定改动模块 |
| 提交 type + scope | rd-commit 步骤 4 | 设计影响判断 |
| 版号 `vX.X.X` | rd-commit 步骤 2 | session doc 中记录版本 |
| body 列表 | rd-commit 步骤 4 | session「Changes」节复用 |

rd-doc **不负责**：版号升级、VERSION.md 写入、versions/vX.X.X.md 创建、commit message 生成。以上全部由 rd-commit 负责。

---

## 七、特殊场景

### 7.1 纯文档改动
仅 `.md` 文件改动 → 不调用 rd-tech-doc / rd-design-doc。≥3 文件或 ≥50 行时创建 session doc。

### 7.2 跨模块大改动
涉及 ≥3 个 L3 模块 → 每个模块独立调用 rd-tech-doc，创建汇总 session doc（每模块一节），逐模块判断 design doc 需求。

### 7.3 紧急修复（hotfix）
Commit type 为 `fix` → 正常创建 session doc，仅公开 API 变更时更新 tech doc，通常不需要 design doc。

### 7.4 用户模糊调用
用户只说"归档一下"→ 先 `git diff --stat` 获取改动范围，向用户确认设计面变更部分，再执行编排。
