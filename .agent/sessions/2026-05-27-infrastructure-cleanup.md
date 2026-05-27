# 2026-05-27 基础设施清理与寻路搭建

## 改动

### Git LFS 配置重组
- `.gitattributes` 按资产类型分组并添加区段注释（3D models / textures / audio / video / compiled / fonts / archives）
- 新增 `.ttf`、`.otf`、`.tar`、`.gz` 等文件类型到 LFS 追踪

### A* ExampleScenes 清理
- 从版本控制移除 `Packages/com.arongranberg.astar/ExampleScenes~/` 下 2973 个示例文件（121 MB）
- 添加到 `.gitignore`，避免重新提交
- 节省 LFS 存储空间

### PathFinding 测试场景
- 新建 `Assets/Scenes/PathFinding.unity`，用于 A* grid agent 开发测试
- 更新 Core 场景

### PolygonPrototype 迁移
- 从 `Assets/External/Synty/` 移动到 `Assets/Art/PolygonPrototype/`
- 与 Prototype Builder 工具配合使用，表示"正在使用中"的资产

## 已知问题

### 文档架构重组
- 移除 `.agent/tech/` 旧 v2 文档（36 个文件），迁移到 `.agent/archive/tech-v1/` 和 `.agent/archive/tech-v2/`
- 新增 `rd-tech-doc` skill，按 L1→L5 架构层级编写技术文档
- 更新 `rd-doc` 命令：tech 文档写入委托给 `rd-tech-doc` skill
- 移除 `.agent/backup/character-backup/` 下 196 个旧角色模块备份文件
- 新增 `.agent/archive/` 作为旧版本文档存放目录
- 新增 `.agent/README.md` 作为文档目录说明

## 已知问题

- 转身 180° 时有概率速度异常慢，未定位根因（来自上一 session）
