# UIOverlay
> **源文件**: `Assets/Scripts/UI/Core/UIOverlay.cs`

叠加层抽象基类。可多个同时存在，不阻塞其他 UI。支持 Enter/Exit 渐变动画。

## 调用链

```
UIService.ShowOverlay(id)
  ├── overlay.gameObject.SetActive(true)
  └── overlay.PlayEnterSequence(args)
      ├── canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutCubic)
      └── OnEnterFinished()

UIService.HideOverlay(id)
  └── overlay.PlayExitSequence()
      ├── canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InCubic)
      └── → Destroy(gameObject)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIService | Initialize 时注入 |
| 依赖 | DOTween | Enter/Exit 动画 |
| 被继承 | VitalsOverlay | 生命体征 HUD |
| 被继承 | StatusOverlay | 状态效果 HUD |
| 被继承 | LoadingOverlay | 加载遮罩 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `DeltaTime` | float (protected) | `Time.unscaledDeltaTime` |
| `uiService` | UIService (protected) | Service 引用 |

## 方法

### Initialize()
```csharp
public void Initialize(UIService manager)
```
- **用途**: 注入 UIService 引用，调用 `OnInitialize()` hook
- **调用者**: UIService.TryGetOverlay()

### PlayEnterSequence()
```csharp
public virtual Sequence PlayEnterSequence(object args = null)
```
- **用途**: 播放进入动画 — alpha 0→1, EaseOutCubic
- **参数**: `args` — 可选参数（由子类处理）
- **返回**: DOTween Sequence
- **调用者**: UIService.ShowOverlay()
- **备注**: 设置 interactable=true, blocksRaycasts=true

### PlayExitSequence()
```csharp
public virtual Sequence PlayExitSequence()
```
- **用途**: 播放退出动画 — alpha 1→0, EaseInCubic
- **返回**: DOTween Sequence
- **调用者**: UIService.HideOverlay()
- **备注**: 设置 interactable=false, blocksRaycasts=false

### OnInitialize()
```csharp
protected virtual void OnInitialize()
```
- **用途**: 子类覆写初始化 hook
- **调用者**: Initialize()

### OnEnterFinished()
```csharp
protected virtual void OnEnterFinished()
```
- **用途**: 进入动画完成回调
- **调用者**: PlayEnterSequence OnComplete

### OnExitFinished()
```csharp
protected virtual void OnExitFinished()
```
- **用途**: 退出动画完成回调
- **调用者**: PlayExitSequence OnComplete

### OnDestroy()
```csharp
protected virtual void OnDestroy()
```
- **用途**: 清理 DOTween
- **调用者**: Unity Engine

## 内部机制

- **MonoBehaviour**: 抽象基类，通过 `[SerializeField]` 暴露 CanvasGroup / fadeDuration
- **动画返回 Sequence**: 让 UIService 可通过 OnComplete 链式控制销毁时机
- **DOTween 清理**: OnDestroy 中调用 `DOTween.Kill(transform)`，防止残留动画

## 与 UIScreen 的差异

| 特性 | UIScreen | UIOverlay |
|------|----------|-----------|
| Pause/Resume | 有 | 无 |
| 管理方式 | 互斥 (currentScreen) | 并存 (List) |
| 默认 fadeDuration | 0.3s | 0.2s |

## 未来规划

无。
