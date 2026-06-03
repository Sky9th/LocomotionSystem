# 2026-06-03 — 目录重构：去掉所有 L4/L5 前缀

## 目标

移除 `L3_Character/` 下所有 L4/L5 目录名前缀。Namespace 已是干净名（如 `RedDust.Character.Combat`），目录名却多出 `L4_`/`L5_` 前缀，不一致且影响可读性。

## 改动

### 目录改名（10 个）

| 旧名 | 新名 |
|------|------|
| `L4_Animation/` | `Animation/` |
| `L4_Audio/` | `Audio/` |
| `L4_Combat/` | `Combat/` |
| `L4_Director/` | `Director/` |
| `L4_Kinematic/` | `Kinematic/` |
| `L4_Locomotion/` | `Locomotion/` |
| `L4_Pathfinding/` | `Pathfinding/` |
| `L4_Stats/` | `Stats/` |
| `L4_Animation/L5_Drivers/` | `Animation/Drivers/` |
| `L4_Combat/L5_Drivers/` | `Combat/Drivers/` |

### 技术细节

- 149 文件全部以 git rename 追踪，零 .cs 代码改动
- L4_Combat 因 IDE 文件锁，用 cp + git rm --cached + git add 替代 git mv
- 目录 `.meta` 文件需手动同步改名（git mv 目录不会自动改 sibling .meta）
- `.agent/tech/README.md` 命名规则 + 目录树同步更新

### 设计决策

- L4/L5 去前缀，L1/L2/L3 保留：L1-L3 是顶层模块入口，保留前缀便于识别；L4/L5 的层级由嵌套表达
- 不改 doc 目录命名（`.agent/tech/` 下 kebab-case 目录），与代码目录解耦

## 关联

- Tech: `.agent/tech/README.md`（命名规则 + 目录树更新）
- Versions: `v0.6.9`
- Previous session: `2026-06-03-combat-config-structs.md`
