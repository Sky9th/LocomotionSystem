---
description: 提交代码并归档文档
argument-hint: [type: summary]
allowed-tools: Read, Write, Edit, Glob, Bash, Grep
---

提交当前改动并自动归档。提交信息使用英文。

## 提交信息模板

```
type: short summary

- change 1
- change 2
```

## 实际例子

```
refactor: top-down camera and mouse-based heading

- rename SCameraContext to SCameraSnapshot, CharacterInputModule to CharacterEventReceiver
- switch CameraService to Cinemachine Transposer + HardLookAt for top-down view
- fix Motor ConvertToWorld to screen-relative (WASD → +Z/-Z/-X/+X)
- simplify Stance turning to use TurnAngle directly, remove lookStability
- change CharacterKinematic viewForward parameter to heading
- compute heading from mouse ground position in CharacterActor
- set GameStateService Playing cursor to Confined + visible
- remove lookStabilityAngle/Duration from LocomotionProfile
```

## type 说明

| type | 场景 |
|------|------|
| `feat` | 新功能 |
| `refactor` | 重构（不改功能行为） |
| `fix` | 修复 bug |
| `docs` | 仅文档改动 |
| `chore` | 配置、依赖、清理 |

概述不超过 70 字符，改动点每条一行 `-` 开头。

## 流程

1. `git diff --stat` — 确认改动范围，确定 type
2. 按模板生成英文提交信息，`git commit`
3. 执行 `/rd-doc` 三层归档：
   - `sessions/YYYY-MM-DD-主题.md` — 会话记录
   - `tech/modules/<模块>.md` — 技术改动
   - `design/<子系统>.md` — 设计决策
4. 更新 `.agent/versions/vX.X.X.md`，追加本次改动条目

## 版号

- 当前版号见 `.agent/VERSION.md`
- 版本文件在 `.agent/versions/vX.X.X.md`，每个版本一个文件
- 规则见 `rd-doc` 版本控制章节