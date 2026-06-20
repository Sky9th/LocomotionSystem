# 2026-06-20 — Grip 切换链路 + 动画资产重组

## 做了什么

### Grip 切换运行时
- `LocomotionAnimationSetSO` 新增 `HasFullLocomotion`（检查 walkMixer/runMixer 是否有动画）
- `CharacterActor.Update()` 在 director 之后解析 grip，animSet 传入 Simulate
- `LocomotionDriver.Evaluate()` 检测 grip 变化：Full → swap BaseLayer，Partial → Arm 层叠武器 idle
- `AnimationBrain` 新增 Arm 层（index 2, armMask），公开 `ArmLayer`
- `GroundLocomotion` / `ILocomotionSimulator` 签名加 animSet 参数
- `BaseLayer.AnimSet` setter 从 private 改为可写
- `PlayerDirector` 加 debug 1/2/3 键切换 grip tag
- `animancerTransitions` 字段从 CharacterActor 删除

### 动画资产重组
- 整理 PROTOFACTOR 2Handed Gun + 1Handed Melee 动画
- 新目录: `1H_Sidearm/`, `1H_Blade/` → Locomotion → Relax/Combat
- 4 套新 locomotion set: Sidearm Relax, Sidearm Combat (Aiming), Blade Relax, Blade Combat
- Relax = partial grip（无 WalkMixer/RunMixer → Base 层承担）
- Combat = full grip（4 方向）
- `Human.json` 更新: 5 个 sets, gripTable 用 Relax 作默认
- `tags_all.json`: `Pistol` → `1H_Sidearm`, `Knife` → `1H_Blade`
- 攀爬动画导入 `Character/Traversal/`

### 修复
- `TraversalAnimationSetSO`: StringAsset → ClipTransition
- `AnimationImportExport`: Traversal 导入导出适配 ClipTransition
- 攀爬动画 GUID 写入 JSON
- CharacterActor `animancerTransitions` 字段及引用删除
