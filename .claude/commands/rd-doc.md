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

详细约定参考 .agent/README.md。
