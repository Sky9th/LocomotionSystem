# 2026-06-11 — Stats 模块清理

## 做了什么

删除废弃的 L3_Stats 模块（已由 L3_Properties 完全替代）。

## 改动范围

| 模块 | 操作 | 原因 |
|------|------|------|
| `L3_Stats/` 全部代码 | 删除 | 无外部消费者，Properties 已接管 |
| `CharacterStats.cs` | 删除 | 桥接层，无调用方 |
| `Assets/Data/Stats/` | 删除 | 全量 .asset + JSON |
| `TagPicker.TestOpen()` | 删除 | 调试菜单入口 |

## 验证方法

- `grep -r "using RedDust.Stats\|StatsTreeSO\|StatInstance\|StatDefinitionSO" Assets/Scripts/` → 无匹配
- CharacterActor:73 走 `PropertyAgent`，CharacterCombat:18 构造函数接收 `PropertyAgent`

## 已知问题

- `Player.prefab:80` 残留孤立 `statsTree` 序列化字段，已移出代码但 prefab 需重新保存清理
