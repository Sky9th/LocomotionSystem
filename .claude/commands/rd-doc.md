---
description: 归档设计/技术文档到 .agent 目录
argument-hint: [主题描述]
allowed-tools: Read, Write, Edit, Glob, Bash, Grep
---

将用户描述的内容归档到 `.agent/` 目录的正确位置。

## 分类规则

根据内容性质选择目录：
- **design/** — 设计决策、系统定位、玩家体验（WHY）
- **tech/modules/** — 技术实现、数据结构、调用链（HOW）
- **tech/conventions/** — 命名规范、代码风格
- **plans/** — 长期/短期开发计划
- **sessions/** — 会话归档（YYYY-MM-DD-主题.md）
- **references/** — 截图、外部资料

## 子系统子目录

| 系统 | design/ | tech/modules/ |
|------|---------|---------------|
| 角色 | design/character/ | tech/modules/character/ |
| 战斗 | design/combat/ | tech/modules/character/ |
| AI | design/ai/ | — |
| 关卡 | design/level/ | — |
| 动画 | — | tech/modules/animation/ |
| 输入 | — | tech/modules/input/ |

## 命名

- 中文，清晰描述内容
- design: `子系统名-主题.md`
- tech: `模块-主题.md`
- sessions: `YYYY-MM-DD-主题.md`

## 流程

1. 确认用户想记录的内容属于哪个分类
2. 如果目录不存在，先创建
3. 写入文档，开头标注日期和状态
4. 必要时更新 .agent/README.md 目录树

## 代码改动后归档（重要）

如果本次会话修改了代码，**必须同时归档三层**：

| 层 | 目录 | 内容 | 示例 |
|----|------|------|------|
| 会话 | `sessions/` | 本次改了什么、为什么、已知问题 | `YYYY-MM-DD-主题.md` |
| 技术 | `tech/modules/` | 改动的模块实现细节、配置、数据流 | 相机 → `camera-system.md`，角色 → `character/index.md` |
| 设计 | `design/` | 设计决策、为什么这样改 | 俯视角 → 更新 `game-overview.md` 或新建设计文档 |

**流程**：
1. 通过 `git diff --stat` 确定改动了哪些模块
2. 创建/更新 session 文件（会话归档）
3. 创建/更新对应 `tech/modules/<模块>.md`（技术归档）
4. 涉及设计决策时更新 `design/<子系统>.md`（设计归档）
5. 已有文档过时时同步更新（如改名、删除、调用链变化）

## 版本控制

版本号 `.agent/VERSION.md`，格式 `v0.0.1`。

每次提交代码时自动更新 `.agent/CHANGELOG.md`：

```
## v0.0.x (YYYY-MM-DD)

- type: change description
```

### 版号规则

| 级别 | 触发条件 | 示例 |
|------|---------|------|
| Patch `0.0.x` | bug 修复、小调整 | `v0.0.1` → `v0.0.2` |
| Minor `0.x.0` | 新功能、新系统 | `v0.1.0` |
| Major `x.0.0` | 架构重构、正式发布 | `v1.0.0` |

### 提醒时机

以下情况主动询问是否升级版号：
- 完成一个 Plan 中定义的功能模块
- 阶段性提交超过 5 次
- 用户说"发布/上线/打包"

详细约定参考 .agent/README.md。
