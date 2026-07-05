# 2026-07-06 — 站姿/蹲姿切换 + 动画资产补全

## Background

S5 动画系统补完进入实施阶段。PROTOFACTOR 外部资产集中有大量未导入的 Crouch/Air/Land/Turn 动画，2H_Fist（徒手）姿态全缺。Head Look IK 和 Footstep 事件桥接对俯视角游戏优先级非常低——`headLookMixer` 从未赋值，运行时始终死路径。

输入系统已有 Crouch/Stand 按键绑定（C/X），`PlayerService.SetPosture()` 已接线，但下游动画和移动系统不响应 Posture 变化。本次实现站立↔蹲姿动画切换，跳过俯视角无意义的趴姿。

## Changes

### 动画资产补全
- 从 External `Ultimate Animation Collection` 导入 24 个 FBX + .meta（Humanoid rig 一致）：2H_Fist x11, 1H_Blade Relax x9, 1H_Sidearm Relax x4
- 覆盖 CrouchIdle / CrouchMixer(4方向) / AirLight/Hard / LandLight/Hard / Turn90L/R
- 2H_Fist/Locomotion/ 扁平目录重构为 Combat/ 子目录（WalkMixer/RunMixer/CrouchMixer），与 1H_Blade 和 1H_Sidearm 结构一致
- animation_all.json: Node.js 脚本全量更新 GUID 引用（air/land/turn/crouch 共 5 个 set）

### Head Look 清理
- 删除 `CharacterHeadLook.cs`（49行静态类）+ .meta
- AnimationBrain: TotalLayerCount 7→6, 移除 HeadLook=5 层/headLookLayer/headLookMixer/UpdateHeadLook/FreezeHeadLookChildren，Footstep 重编号为 5
- CharacterKinematic: `CharacterHeadLook.Evaluate()` → `Vector2.zero`
- LocomotionAnimationConfigSO + AnimationImportExport: 移除 `headLookSmoothingSpeed`
- 所有移除处保留 IK 占位注释

### Footstep 桥接延后
- AnimationBrain.Start(): 注释 `FootstepCallback` 桥接线
- CharacterAudio.OnWire()/OnDestroy(): 注释订阅/取消订阅
- 保留 `OnFootstep` 事件签名 + `HandleFootstep()` 方法占位

### 站姿/蹲姿切换
- LocomotionAnimationSetSO: +`crouchIdle` (ClipTransition) + `crouchMixer` (MixerTransition2D)
- GroundLocomotion: Posture==Crouching → 移动时 Crawl gait（crawlAnimNativeSpeed=1m/s），静止时 Idle gait
- BaseIdleState: +`ResolveIdle()` — Posture==Crouching ∧ crouchIdle 有 clip → crouchIdle，fallback idleL
- BaseMovingState: gait switch +`Crawl → crouchMixer`；蹲姿 fallback 用 crouchMixer（防站立闪烁）
- AnimationImportExport: DTO/Export/Import 增加 crouchIdle/crouchMixer/crawlAnimNativeSpeed

### 短期计划更新
- S5.1 Footstep: 🔒 延后
- S5.2 Head Look IK: 🔒 延后
- S5.3 Crawl mixer: ✅ 完成（以 crouchMixer 形式落地）
- S5 总耗时: ~2.5天 → ~0.5天（剩余 AirLand + Traversal）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 蹲姿静止 gait=Idle，移动 gait=Crawl | A: 蹲姿固定 Crawl → 静止时 crawlAnimNativeSpeed>0 导致 Motor 计算非零速度，Phase 不进入 Idle，BaseIdleState 无法触发 | 分情况处理：静止=Crawling+Idle→crouchIdle，移动=Crawling+Crawl→crouchMixer |
| Head Look 全删，留注释占位 | A: 注释保留死代码 → 徒增代码量，不如干净删除 | headLookMixer 从未赋值（运行时第一行 return），删除比注释更清晰 |
| Footstep 注释桥接而非删除事件体系 | A: 完全删除 OnFootstep+FootstepCallback+InjectFootstepEvents → 牵连 BaseLayer 内部基础设施 | 保留基础设施，将来机器人等特殊角色取消注释即可 |

## Known Issues

- [ ] Zombie sets 的 walkMixer 使用 8方向无中心点格式，蹲姿切换未在 Zombie 测试 — P2
- [ ] crawlAnimNativeSpeed 默认 1.0m/s（慢于 walk 1.5m/s），后续可能需要属性系统接入 — P2
- [ ] 2H_Fist Relax set 无 crouch 动画（未导入），蹲姿时 fallback 到站立 idleL — P2
- [ ] crouchIdle/crouchMixer 字段新增后，Zombie SO 自动获得空字段（Unity 序列化），静态值无害 — P3

## Cross-References

### Related Sessions
- [2026-07-06-headlook-footstep-deferral.md](2026-07-06-headlook-footstep-deferral.md) — Head Look + Footstep 延后决策
- [2026-07-06-ability-old-code-cleanup.md](2026-07-06-ability-old-code-cleanup.md) — 同天旧代码清理
- [2026-07-06-passive-pipeline-runtime.md](2026-07-06-passive-pipeline-runtime.md) — 被动管线运行时

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — S5.1/S5.2 延后，S5.3 完成

### Related Tech Docs
- LocomotionAnimationSetSO: 新增 crouchIdle/crouchMixer 字段
- BaseIdleState/BaseMovingState: 调用链更新（蹲姿动画选择）

### Flag for Design Doc Creation
- [x] No design doc needed — crouch input already designed (C/X keys), animation is implementation detail.
