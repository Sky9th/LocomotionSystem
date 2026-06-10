# 2026-06-11 — Properties 接入 Character

## 做了什么

- CharacterActor 直接持有 PropertyAgent，对外暴露 IPropertyReader 只读接口
- CharacterDefSO 重建，放回 L3_Character/Actor/
- Combat (CharacterCombat) 切换：CharacterStats → PropertyAgent，伤害结算/HP 读写
- AbilityExecutor / CostEffectSO：StatDefinitionSO → PropertyDefSO，回调类型安全
- 删除 Physiology 5 个文件（后续用 Modifier 重新实现）
- PropertyComponent 重命名为 PropertyAgent
- FloatSnapshot 删除，改用直接 GetFloat/GetMax
- UI 接入 IPropertyReader：VitalsOverlay 调用 GetFloat/GetMax，UIStatBar 显示小数
- 饥饿 Modifier 验证通过（PerSecond Delta=-0.01）
- IPropertyReader 接口划分读写边界

## 关键决策

- 外部只读 (IPropertyReader)，内部读写 (PropertyAgent)
- `statDef` → `def`（PropertyDefSO 类型）
- `Combat.Combat` → `CharacterCombat`
- 伴生属性推断暂不做

## 相关文件

- `L3_Character/Actor/CharacterActor.cs`
- `L3_Character/Actor/CharacterDefSO.cs`
- `L3_Character/Combat/CharacterCombat.cs`
- `L3_Properties/PropertyAgent.cs`
- `L3_Properties/IPropertyReader.cs`
- `L3_Properties/Instance/EntityProperties.cs`
- `L3_Properties/Instance/FloatState.cs`
- `L3_Properties/Instance/FloatModifier.cs`
- `L3_Ability/AbilityExecutor.cs`
- `L3_Ability/Config/Effect/CostEffectSO.cs`
- `L2_UI/HUD/VitalsOverlay.cs`
- `L2_UI/UIService.cs`
- `L2_UI/Components/UIStatBar.cs`
