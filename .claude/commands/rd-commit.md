---
description: 提交代码并归档文档
argument-hint: [type: summary]
allowed-tools: Read, Write, Edit, Glob, Bash, Grep
---

提交当前改动并自动归档。type 用英文，描述用中文。

## 提交信息模板（强约束）

提交信息必须恰好包含 **三部分**，由两个空行分隔：

```
type(scope): 中文描述
                         ← 空行 1（必填）
- 改动点 1
- 改动点 2
                         ← 空行 2（必填）
vX.X.X
```

## 结构约束

### 开头行（强制格式）

格式：`type(scope): 中文描述`

| 规则 | 说明 |
|------|------|
| `type` 五选一 | `feat` / `refactor` / `fix` / `docs` / `chore` |
| `(scope)` 必填 | 括号内有实际模块名，小写英文，如 `character`、`animation`、`pathfinding`、`camera`、`ui`、`core` |
| `: ` 冒号空格 | 冒号后**必须有一个空格** |
| `描述` | 中文，祈使语气，≤30 字符 |
| 整行 ≤72 字符 | 含 type(scope): 前缀 |

### 结尾行（强制格式）

格式：`v<major>.<minor>.<patch>`

| 规则 | 说明 |
|------|------|
| `v` 前缀 | 小写 `v` |
| 三段数字 | 用 `.` 分隔，每段至少一位数字 |
| 最后一行 | 之前必须有一个空行，之后无内容 |

### 空行（强制）

- 开头行和 body 之间：**一个空行**
- body 和结尾行之间：**一个空行**
- 不得有多余空行

## 约束规则

| # | 规则 | 说明 |
|----|------|------|
| 1 | **type 必选** | `feat` / `refactor` / `fix` / `docs` / `chore` |
| 2 | **scope 必填** | 主要改动的模块名，小写英文 |
| 3 | **描述中文祈使语气** | 中文动词开头，≤30 字符 |
| 4 | **body 每条一行 `- ` 开头** | 每行描述一个逻辑独立的改动，≤80 字符 |
| 5 | **尾行版本号** | 最后一行单独写本次升级到的版号 `vX.X.X` |
| 6 | **两空行** | 开头行↔body 之间一个空行，body↔版号之间一个空行 |

## type 说明

| type | 场景 |
|------|------|
| `feat` | 新功能 |
| `refactor` | 重构（不改功能行为） |
| `fix` | 修复 bug |
| `docs` | 仅文档改动 |
| `chore` | 配置、依赖、清理 |

## 合法示例

```
refactor(character): 重构移动速度架构，改为按步态配置

- LocomotionProfile 拆分单一 moveSpeed 为 walk/run/sprint/crawl 四档
- 新增 LocomotionModeProfile.animNativeSpeed 记录动画原生速度
- 新增 AnimationBrain.ApplySpeedMultiplier 计算步态乘积
- 新增 PathfindingAgent.DesiredSpeedMultiplier 接入 A* 减速
- Motor.ConvertToWorld 修复 heading 旋转

v0.4.0
```

```
fix(character): 修复地面检测失效导致角色悬浮

- 补充 CharacterGroundDetection 缺失的 ground probe layer mask
- 修复 SGroundContact.IsGrounded 站在地形上返回 false

v0.4.1
```

## 流程

1. `git diff --stat` — 确认改动范围，确定类型和范围
2. **先做版本总结和版号升级**：
   - 读取 `.agent/VERSION.md` 确定当前版号
   - 根据改动量决定升级级别（patch/minor/major）
   - 创建 `.agent/versions/vX.X.X.md` 写版本 changelog
   - 更新 `.agent/VERSION.md`
3. **归档文档**（版本文件已写入，一并提交）：
   - `sessions/YYYY-MM-DD-主题.md` — 会话记录
   - `tech/` — 有新增/修改模块时更新技术文档
   - `design/` — 有设计决策时更新
4. **生成提交信息并展示**，body 每行 `- ` 开头，≤80 字符。**用 `AskUserQuestion` 弹出确认按钮**：
   - question: "确认提交？"
   - header: "Commit"
   - 两个选项: "提交" (label: "提交") / "取消" (label: "取消")
   - 点"提交" → 执行 `git add -A && git commit -m "<message>"`
   - 点"取消" → 终止，不做任何操作

## 版号

- 当前版号见 `.agent/VERSION.md`
- 版本文件在 `.agent/versions/vX.X.X.md`，每个版本一个文件
- **版号必须先升级再提交，commit message 中提及新版号**
- 规则见 `rd-doc` 版本控制章节

### 版号规则

| 级别 | 触发条件 | 示例 |
|------|---------|------|
| Patch `0.0.x` | bug 修复、小调整 | `v0.0.1` → `v0.0.2` |
| Minor `0.x.0` | 新功能、新系统 | `v0.1.0` |
| Major `x.0.0` | 架构重构、正式发布 | `v1.0.0` |
