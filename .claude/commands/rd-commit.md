---
description: 提交代码并归档文档
argument-hint: [type: summary]
allowed-tools: Read, Write, Edit, Glob, Bash, Grep
---

提交当前改动并自动归档。提交信息使用英文，**必须严格遵守以下格式**。

## 提交信息模板（强约束）

提交信息必须恰好包含 **三部分**，由两个空行分隔：

```
type(scope): imperative summary, max 72 chars
                         ← 空行 1（必填）
- verb description 1
- verb description 2
                         ← 空行 2（必填）
vX.X.X
```

## 结构约束

### 开头行（强制格式）

格式：`type(scope): description`

```
^(feat|refactor|fix|docs|chore)\([a-z0-9-]+\): [a-z].+$
```

| 规则 | 说明 |
|------|------|
| `type` 五选一 | `feat` / `refactor` / `fix` / `docs` / `chore` |
| `(scope)` 必填 | 括号内有实际模块名，小写字母+数字+连字符 |
| `: ` 冒号空格 | 冒号后**必须有一个空格** |
| `description` 小写开头 | 首个字母小写，祈使动词开头，≤72 字符 |
| 整行 ≤72 字符 | 含 type(scope): 前缀 |

**非法开头示例**：
```
refactor: summary          ← 缺 scope
refactor(UI): summary      ← scope 不能大写
refactor(ui) : summary     ← 冒号后不能有空格
refactor(ui):Summary       ← description 不能大写开头
Refactor(ui): summary      ← type 不能大写
```

### 结尾行（强制格式）

格式：`v<major>.<minor>.<patch>`

```
^v\d+\.\d+\.\d+$
```

| 规则 | 说明 |
|------|------|
| `v` 前缀 | 小写 `v` |
| 三段数字 | 用 `.` 分隔，每段至少一位数字 |
| 最后一行 | 之前必须有一个空行，之后无内容 |

**非法结尾示例**：
```
V0.2.0         ← 大写 V
v0.2           ← 缺 patch 段
v0.2.0-beta    ← 不能用 pre-release 标签
ver0.2.0       ← 不能用 ver 前缀
```

### 空行（强制）

- 开头行和 body 之间：**一个空行**
- body 和结尾行之间：**一个空行**
- 不得有多余空行

## 约束规则

| # | 规则 | 说明 |
|----|------|------|
| 1 | **type 必选** | `feat` / `refactor` / `fix` / `docs` / `chore` |
| 2 | **scope 必填** | 主要改动的模块名，如 `character`, `input`, `audio`, `stats`, `ui`, `core`, `shared` |
| 3 | **summary 祈使语气** | 用动词原形开头，描述做了什么（不用过去式），≤72 字符 |
| 4 | **body 每条一行 `-` 开头** | 每行描述一个逻辑独立的改动，≤100 字符 |
| 5 | **body 动词开头** | 每条用 `add`, `fix`, `remove`, `rename`, `extract`, `merge`, `split`, `move`, `switch`, `replace`, `simplify`, `change`, `migrate` 等祈使动词开头 |
| 6 | **不用 "update" 模糊词** | 用具体的动词代替泛泛的 "update"（如 `rename`/`migrate`/`switch` 而非 `update namespace`） |
| 7 | **尾行版本号** | 最后一行单独写本次升级到的版号 `vX.X.X` |
| 8 | **英文** | 全部英文 |
| 9 | **开头行校验** | 必须通过正则 `^(feat\|refactor\|fix\|docs\|chore)\([a-z0-9-]+\): [a-z].+$` |
| 10 | **结尾行校验** | 必须通过正则 `^v\d+\.\d+\.\d+$` |
| 11 | **两空行** | 开头行↔body 之间一个空行，body↔版号之间一个空行，不能多不能少 |

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
refactor(namespace): migrate all namespaces from Game.* to RedDust.*

- rename root namespace Game to RedDust across 155 files
- flatten non-L-prefixed directories into parent namespace (Actor, Config, Structs)
- move L1 Core to RedDust.Core, Shared to RedDust.Shared
- reassign 9 L2 services to RedDust.{ServiceName} namespaces
- assign 5 L4 subsystems under RedDust.Character.* hierarchy
- fix namespace/type collisions with fully-qualified names in 5 services
- resolve RedDust.Input namespace conflict with UnityEngine.Input

v0.2.0
```

```
fix(character): restore ground detection after kinematic refactor

- add missing ground probe layer mask to CharacterGroundDetection
- fix SGroundContact.IsGrounded returning false when standing on terrain

v0.2.1
```

## 流程

1. `git diff --stat` — 确认改动范围，确定 type 和 scope
2. **先做版本总结和版号升级**：
   - 读取 `.agent/VERSION.md` 确定当前版号
   - 根据改动量决定升级级别（patch/minor/major）
   - 创建 `.agent/versions/vX.X.X.md` 写版本 changelog
   - 更新 `.agent/VERSION.md`
3. **生成提交信息并校验**，必须通过以下检查才能 commit：
   - 开头行匹配 `^(feat|refactor|fix|docs|chore)\([a-z0-9-]+\): [a-z].+$`
   - 开头行 ≤72 字符
   - 结尾行匹配 `^v\d+\.\d+\.\d+$`
   - 恰好两个空行（开头↔body ↔结尾）
   - body 每行 `- ` 开头，≤100 字符
4. `git commit`
5. 执行 `/rd-doc` 三层归档：
   - `sessions/YYYY-MM-DD-主题.md` — 会话记录
   - `tech/modules/<模块>.md` — 技术改动
   - `design/<子系统>.md` — 设计决策

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