# PlayerDirector · 玩家意图控制器

> **Last Verified**: 2026-06-22 | **Verification**: All referenced files exist, signatures match code

> `Character/Director/Player/PlayerDirector.cs` — ICharacterDirector 实现，组合输入 + 寻路

## 调用链

```
被谁调:
  CharacterActor.Update() → director.Evaluate()

调谁:
  PlayerInput → 读取鼠标/按键/Equip/Skill 状态
  PathfindingAgent → SetDestination / DesiredVelocity / HasPath / HasReachedDestination
  AbilityExecutor → TryActivate (技能激活)
  GameplayTagContainer → AddTag / RemoveTag (grip 标签)
  SCharacterIntent → 构造并返回
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterActor | 调用 Evaluate() |
| 依赖 | PlayerInput | 聚合输入事件（移动/姿态/Equip/Skill） |
| 依赖 | PathfindingAgent | 设置目的地 + 读取速度/状态 |
| 依赖 | AbilityExecutor | 技能激活 |
| 依赖 | GameplayTagContainer | Grip 标签写入（过渡方案） |
| 依赖 | GripAnimationTableSO | entries 读取 gripTag |
| 创建 | SCharacterIntent | 返回结构体 |

## 公开属性

```csharp
// ICharacterDirector 实现
SCharacterIntent Evaluate();
```

## 内部状态

```csharp
// 持久步态/姿态缓存（逐帧 Evaluate 读取并可能被 ProcessClickToMove 修改）
private EMovementGait currentGait;
private EPosture currentPosture;

// Equip 装备状态（Equip1→[0], Equip2→[1], Equip3→[2]）
private readonly bool[] equippedSlots = new bool[3];
```

## 方法

### Evaluate()
```csharp
public SCharacterIntent Evaluate()
```
- **用途**: 每帧评估玩家意图，返回 SCharacterIntent
- **流程**:
  1. `ProcessEquipInput()` — Equip 事件驱动 grip 标签 + BodyForm
  2. `ProcessClickToMove()` — 右键时调用 `agent.SetDestination(mousePos)`
  3. `ProcessSkillInput()` — Skill1/2 事件驱动技能激活
  4. 计算 hasActivePath，构造 Intent
  5. 清除帧信号
- **调用者**: `CharacterActor.Update()`

### ProcessEquipInput()
```csharp
private void ProcessEquipInput()
```
- **用途**: Equip1/2/3 事件 → toggle GripTable entries 对应 grip tag
- **逻辑**:
  - EquipN press + 已装备 → 卸下（RemoveTag, slot=false）
  - EquipN press + 未装备 → 清除所有已有 grip tag + 装备新 slot（武器互斥）
  - 每帧只处理一个 Equip 输入（break）
- **空值守卫**: GripTable==null, entries==null, ownedTags==null, gripTag==null, slot 索引 < entries.Length
- **备注**: TODO — 当前 Director 直接写 OwnedTags 是过渡方案，装备系统完成后由 GripSwitchEvent 替代

### ProcessSkillInput()
```csharp
private void ProcessSkillInput()
```
- **用途**: 聚合 Skill1/Skill2 输入 → TryActivateSkill
- **调用者**: `Evaluate()`

### TryActivateSkill()
```csharp
private void TryActivateSkill(RedDust.Ability.AbilityDefSO def, string slotName)
```
- **用途**: 统一空值检查和日志的技能激活
- **逻辑**: AbilityExecutor 或 def 为 null → LogWarning 并 return；否则 TryActivate
- **备注**: TODO — 临时方案，技能树/装备系统完成后由 AbilitySlotManager 替代

### ResolveBodyForm()
```csharp
private EBodyForm ResolveBodyForm()
```
- **用途**: 派生 BodyForm —— `equippedSlots` 任意 true → Combat，否则 Relax
- **逻辑**: 遍历 equippedSlots，找到任意 true 即返回 Combat

### ComputeHeading()
```csharp
private Vector3 ComputeHeading()
```
- **用途**: 计算 LocomotionHeading
- **逻辑**: 当寻路激活时返回 `agent.DesiredVelocity.normalized`，否则返回 `modelRoot.forward`

### ProcessClickToMove()
```csharp
private void ProcessClickToMove()
```
- **用途**: 右键点击地面 → `agent.SetDestination(mouseGroundPosition)`，步态设为 Run

### ResolveGait()
```csharp
private EMovementGait ResolveGait()
```
- **用途**: 根据寻路状态和输入切换步态（Idle/Run/Sprint）

### ComputeAim()
```csharp
private Vector3 ComputeAim()
```
- **用途**: 返回鼠标地面位置方向（供 HeadLook 使用）

### ResolvePosture()
```csharp
private EPosture ResolvePosture()
```
- **用途**: 根据 Stand/Crouch/Prone 输入切换姿态

## 设计决策

| 决策 | 原因 |
|------|------|
| heading 用 `desiredVelocity.normalized` 替代 `PathDirection` | 速度方向经 AIPath 平滑，转向时自然过渡 |
| BodyForm 由装备状态派生而非独立 toggle | 单一事实源（装备状态），无需同步两个独立状态 |
| Equip1→entries[0] 映射约定 | GripTable entries 按序排列，隐式映射在 debug 代码中已验证 |
| Director 直接写 OwnedTags | 装备系统未就绪，GripSwitchEvent 过早抽象。保留 TODO 后续迁移 |
| Equip 输入不加空值守卫（fail-fast） | NRE 提前暴露 EventHub 配置缺失，与现有事件行为一致 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Director 直接写 OwnedTags → GripSwitchEvent | 待做 | 装备系统完成后 |
| Skill 激活 → AbilitySlotManager | 待做 | 技能树/装备系统完成后 |
| WASD 移动输入支持 | 待做 | Phase 4+ |
