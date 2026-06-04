# 2026-06-05 Ability 模块提取 + 全量重命名

> v0.7.1

## 动机

地面火焰等非角色实体需要相同的搜索+效果管道。Combat/ 在 L3_Character 下，环境模块不应依赖角色模块。提取为独立 L3_Ability。

## 关键决策

- AbilityComponent 改为 MonoBehaviour — 挂 Prefab 用，跟 Collider/Rigidbody 一样
- 不引入 IEffectTarget — 组件自己持 tags + stats 引用
- 火陷阱不需要额外脚本 — cooldownDuration 就是 tick 间隔
- 三 Agent 交叉审查命名一致性、实体无关性、字段清晰度，全部采纳
