# 2026-06-16 — Character ctx 贯彻

## 目标
消除 Character 模块内所有 `GetComponent<CharacterActor>()` 上行引用，数据通过 BuildCtx 逐级下传。

## 产出
- **CharacterBuildContext** 从 10 字段扩展到 22 字段（animation/audio config、masks、root motion flags）
- **ModuleComponent.Awake** 自注册机制——动态 AddComponent 的子模块自动走生命周期
- **AnimationBrain** 自身在 OnWire 取 BuildCtx，不再靠父组件推
- **Drivers/CharacterAudio** 从 `brain.BuildContext` 链路取 config
- **数据树文档**: `.agent/tech/tree/L3-character-function-tree.md`

## 关键决策
- BuildCtx vs FrameCtx 命名区分（buildCtx 字段 / frameCtx 参数）
- 生命周期函数（OnAssemble/OnWire）禁止手动调用
- 子模块自己取依赖（OnWire self-fetch），父组件不推送
