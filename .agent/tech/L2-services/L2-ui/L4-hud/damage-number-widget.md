# DamageNumberWidget
> **源文件**: `Assets/Scripts/Services/L2_UI/HUD/DamageNumberWidget.cs`
> **Last Verified**: 2026-07-04 | **Verification**: All referenced files exist, signatures match code

单个浮动伤害数字，由 DamageNumberOverlay 对象池管理。接收 overlay 本地坐标（来自 `RectTransformUtility.ScreenPointToLocalPointInRectangle`），设置 `anchoredPosition` + DOTween 上飘淡出，动画完成后标记 `IsIdle=true` 供 Update 回收。

## 调用链

```
DamageNumberOverlay.OnHitReceived()
  └── widget.Play(amount, localPos)
      ├── DOTween.Kill(transform)         // 清理残留动画
      ├── SetActive(true)
      ├── anchoredPosition = localPos + randomOffset
      ├── label.text = RoundToInt(amount)
      ├── label.alpha = 1
      ├── DOTween.Sequence()
      │   ├── Join: DOAnchorPosY(+riseDistance, duration, OutQuad)
      │   └── Insert(fadeDelay): DOFade(0, fadeDuration, InQuad)
      └── OnComplete: IsIdle = true, SetActive(false)

DamageNumberOverlay.Update()
  └── if w.IsIdle → ReturnToPool(w)
      └── widget.Recycle()
          ├── DOTween.Kill(transform)
          ├── IsIdle = true
          └── SetActive(false)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被管理 | DamageNumberOverlay | 对象池所有者和唯一调用方 |
| 依赖 | TMP_Text | 文本组件，设置 text + alpha 动画 |
| 依赖 | DOTween | DOAnchorPosY / DOFade 动画 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `IsIdle` | bool | 动画完成标志，Update 轮询后回收 |

## 方法

### Awake()
```csharp
private void Awake()
```
- **用途**: 缓存 `_rt = transform as RectTransform`
- **调用者**: Unity Engine

### Play()
```csharp
public void Play(float amount, Vector2 localPos)
```
- **用途**: 播放飘字动画：重置位置+文本+alpha → DOAnchorPosY 上飘 + DOFade 淡出
- **参数**: `amount` — 预减免伤害值；`localPos` — overlay 本地坐标
- **动画时长**: duration=0.8s，淡出延迟 fadeDelay=0.3s
- **调用者**: DamageNumberOverlay.OnHitReceived()

### Recycle()
```csharp
public void Recycle()
```
- **用途**: 立即中止动画并标记 Idle，供池回收
- **调用者**: DamageNumberOverlay.ReturnToPool()

### OnDestroy()
```csharp
private void OnDestroy()
```
- **用途**: DOTween.Kill(transform) 清理残留 tween
- **调用者**: Unity Engine

## 内部机制

- **RectTransform 锚点要求**: Anchor=(0.5,0.5), Pivot=(0.5,0.5) — 与 `RectTransformUtility` 返回的 overlay 中心坐标系对齐
- **随机偏移**: Play 时在 localPos 上叠加 Random.Range(-randomX, randomX) 防止多数字完全重叠
- **动画序列**: DOTween.Sequence，OnComplete 自动标记 Idle + SetActive(false)
- **默认不可见**: Prefab 初始 SetActive(false)，池中待用

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `riseDistance` | float | 70 | 上飘像素距离 |
| `duration` | float | 0.8 | 总动画时长 |
| `fadeDelay` | float | 0.3 | 淡出开始延迟 |
| `fadeDuration` | float | 0.5 | 淡出持续时长 |
| `randomX` | float | 15 | 水平随机偏移 |
| `randomY` | float | 5 | 垂直随机偏移 |
| `label` | TMP_Text | — | 文字组件引用 |

## 未来规划

| 计划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| Sequence 预分配（减少 GC） | 待定 | profiling | code-review |
| 伤害类型颜色映射 | 待定 | EffectTag → 颜色表 | — |
