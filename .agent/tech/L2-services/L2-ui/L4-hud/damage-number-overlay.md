# DamageNumberOverlay
> **源文件**: `Assets/Scripts/Services/L2_UI/HUD/DamageNumberOverlay.cs`
> **Last Verified**: 2026-07-04 | **Verification**: All referenced files exist, signatures match code

继承 UIOverlay。订阅 HitEvent，收到伤害事件后通过 `WorldToScreenPoint` + `RectTransformUtility.ScreenPointToLocalPointInRectangle` 将世界命中点转换为 overlay 本地坐标，从对象池取出 DamageNumberWidget 播放飘字动画。

尸潮场景：单 Screen Space Canvas 合批，maxPoolSize=30 上限静默丢弃。

## 调用链

```
UIService.HandleGameState(Playing)
  └── ShowOverlay(DamageNumberOverlay)
      └── OnInitialize()
          ├── CanvasGroup disable (interactable/blocksRaycasts)
          ├── GameContext.Instance.TryResolveService<EventHub>()
          │   └── _eventHub.Get<HitEvent>().Register(OnHitReceived)
          └── 预暖 initialPoolSize 个 DamageNumberWidget

HitEvent.Raise(SDamageInfo)
  └── OnHitReceived(hit)
      ├── if Amount <= 0 → return        // 过滤零伤害/回避
      ├── worldCamera.WorldToScreenPoint(hit.HitPoint)
      ├── if z < 0 → return              // 相机后方
      ├── RectTransformUtility.ScreenPointToLocalPointInRectangle(
      │     _rectTransform, screenPos + offsetY, null, out localPos)
      ├── pool.Get() → CreateWidget()    // 池空时动态分配
      └── widget.Play(hit.Amount, localPos)

Update()
  └── 倒序 _active[] → w.IsIdle → ReturnToPool(w)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承 | UIOverlay | 基类，UIService 管理生命周期 |
| 依赖 | GameContext | TryResolveService<EventHub> |
| 依赖 | EventHub | Get<HitEvent>.Register/Unregister |
| 依赖 | HitEvent | GameEvent<SDamageInfo> 广播通道 |
| 依赖 | DamageNumberWidget | 对象池管理，Play/Recycle |
| 依赖 | Camera | WorldToScreenPoint 坐标转换 |

## 公开属性

无公开属性。`override PlayEnterSequence => null` 跳过基类的 CanvasGroup 交互开启。

## 方法

### OnInitialize()
```csharp
protected override void OnInitialize()
```
- **用途**: 获取 EventHub → 订阅 HitEvent，预暖对象池，禁用 CanvasGroup 交互
- **调用者**: UIOverlay.Initialize()

### OnHitReceived()
```csharp
private void OnHitReceived(SDamageInfo hit)
```
- **用途**: 过滤无效伤害 → 坐标转换 → 取 widget → 播放动画
- **参数**: `hit` — 原始伤害信息（预减免 Amount）
- **过滤**: Amount <= 0（零伤害/完全回避），screenPos.z < 0（相机后方）
- **调用者**: HitEvent.Raise() → Action<SDamageInfo> delegate

### GetWidget()
```csharp
private DamageNumberWidget GetWidget()
```
- **用途**: 从对象池取 widget（pool.Pop()），池空时动态 CreateWidget()，达 maxPoolSize 返回 null
- **返回**: widget 或 null

### ReturnToPool()
```csharp
private void ReturnToPool(DamageNumberWidget widget)
```
- **用途**: widget.Recycle() 中止动画 → 推回 pool

### CreateWidget()
```csharp
private DamageNumberWidget CreateWidget()
```
- **用途**: Instantiate(widgetPrefab, _rectTransform)，直接返回（不 push pool——仅 OnInitialize 预暖时 push）

### TestDamage() (Editor Only)
```csharp
[ContextMenu("Test Damage Number")]
private void TestDamage()
```
- **用途**: 屏幕中央生成测试伤害数字，方便 Prefab 位置验证

## 内部机制

- **MonoBehaviour**: 继承 UIOverlay，Update() 每帧回收已完成动画的 widget
- **PlayEnterSequence override**: 返回 null 跳过 CanvasGroup fade + 交互开启
- **对象池**: Stack<DamageNumberWidget>，总实例上限 maxPoolSize=30
- **静默降级**: 池耗尽时 GetWidget() 返回 null，丢弃此次伤害不报错

## 使用方法

- 配置前提：EventHub 的 abilityEvents 数组必须包含 HitEvent.asset
- 游戏启动后 UIService 自动创建 overlay，无需手动管理
- Editor 测试：Play 模式下右键组件 → "Test Damage Number"

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `widgetPrefab` | DamageNumberWidget | — | 池模板 Prefab |
| `initialPoolSize` | int | 10 | 预暖 widget 数量 |
| `maxPoolSize` | int | 30 | 最大实例数（active + pooled） |
| `screenOffsetY` | float | 50 | 命中点上方屏幕像素偏移 |
| `worldCamera` | Camera | — | 世界坐标→屏幕坐标的参考相机 |

## 未来规划

| 计划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 终伤显示（当前显示预减免） | 待定 | 扩展 SDamageInfo 携带 finalAmount | session 2026-07-04-damage-number-hint |
| DOTween 预分配优化 | 待定 | profiling 确认 GC 热点 | 同上 |
| 伤害类型颜色区分（物/火/毒等） | 待定 | EffectTag 映射 | — |
