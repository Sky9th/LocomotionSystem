---
description: 归档设计/技术文档到 .agent 目录
argument-hint: [主题描述]
allowed-tools: Read, Write, Edit, Glob, Bash, Grep
---

将用户描述的内容归档到 `.agent/` 目录的正确位置。

## 分类规则

根据内容性质选择目录：
- **design/** — 设计决策、系统定位、玩家体验（WHY）
- **tech/** — 技术实现、调用链、API 文档（HOW），按 L1→L5 架构层级组织，**编写时使用 rd-tech-doc skill**
- **tech/conventions/** — 命名规范、代码风格
- **plans/** — 长期/短期开发计划
- **sessions/** — 会话归档（YYYY-MM-DD-主题.md）
- **references/** — 截图、外部资料

> `archive/tech-v1/` 和 `archive/tech-v2/` 为旧文档归档，不再新增内容。所有新技术文档写入 `tech/`。

## 子系统子目录（按 L1→L5 层级）

| 层级 | 目录 | 系统 |
|------|------|------|
| L1 | `L1-core/` | GameManager 根 — GameContext, GameService, BaseService |
| L2 | `L2-services/` | Service 层 — EventDispatcher, Scene, Time, GameState, Player, Camera |
| L2 | `L2-services/L2-input/` | 输入系统 (复合 Service) |
| L2 | `L2-services/L2-ui/` | UI 系统 (复合 Service) |
| L2 | `L2-services/L2-audio/` | 音频系统 (复合 Service) |
| L2 | `L2-services/L2-modules/L3-character/` | 角色系统 (独立模块) |
| L2 | `L2-services/L2-modules/L3-stats/` | Stat 数值框架 (独立模块) |
| L2 | `L2-services/L2-modules/L3-pathfinding/` | 寻路系统 (独立模块) |
| — | `shared/` | 全局 Helper — 日志、编辑器、工具 |

## 命名

- 中文，清晰描述内容
- design: `子系统名-主题.md`
- sessions: `YYYY-MM-DD-主题.md`
- tech: 遵循 rd-tech-doc skill 的命名和结构约定（kebab-case + L 前缀目录）

## 流程

1. 确认用户想记录的内容属于哪个分类和层级
2. 如果是 tech 文档 → 调用 **rd-tech-doc skill** 完成写入
3. design/sessions/plans → 直接写入对应目录
4. 必要时更新 `tech/README.md` 根总领目录树

## 代码改动后归档（重要）

如果本次会话修改了代码，**必须同时归档三层**：

| 层 | 目录 | 内容 | 示例 |
|----|------|------|------|
| 会话 | `sessions/` | 本次改了什么、为什么、已知问题 | `YYYY-MM-DD-主题.md` |
| 技术 | `tech/` | 模块实现细节、调用链、API | 使用 rd-tech-doc skill 更新 |
| 设计 | `design/` | 设计决策、为什么这样改 | 俯视角 → 更新 `game-overview.md` 或新建设计文档 |

**流程**：
1. 通过 `git diff --stat` 确定改动了哪些模块
2. 创建/更新 session 文件（会话归档）
3. 调用 **rd-tech-doc skill** 更新 `tech/` 对应模块文档
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
