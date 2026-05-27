# UIOverlay · 叠加层基类

> `Assets/Scripts/UI/Core/UIOverlay.cs` — 抽象基类。叠加层，可多个同时存在，不阻塞其他 UI。支持 Enter/Exit 渐变动画。

## 调用链

```
UIService.ShowOverlay(id)
  ├── overlay.gameObject.SetActive(true)
  └── overlay.PlayEnterSequence(args)
      ├── canvasGroup.DOFade(1f)
      └── OnEnterFinished()

UIService.HideOverlay(id)
  └── overlay.PlayExitSequence()
      ├── canvasGroup.DOFade(0f)
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
| `DeltaTime` | float (protected) | Time.unscaledDeltaTime |
| `uiService` | UIService (protected) | Service 引用 |

## 方法

### Initialize()
```csharp
public void Initialize(UIService manager)
```
- **用途**: 注入 UIService 引用，调用 OnInitialize hook
- **调用者**: UIService.TryGetOverlay()

### PlayEnterSequence()
```csharp
public virtual Sequence PlayEnterSequence(object args = null)
```
- **用途**: 播放进入动画 — alpha 0→1, EaseOutCubic
- **调用者**: UIService.ShowOverlay()

### PlayExitSequence()
```csharp
public virtual Sequence PlayExitSequence()
```
- **用途**: 播放退出动画 — alpha 1→0, EaseInCubic
- **调用者**: UIService.HideOverlay()

### OnInitialize() / OnEnterFinished() / OnExitFinished()
```csharp
protected virtual void OnInitialize()
protected virtual void OnEnterFinished()
protected virtual void OnExitFinished()
```
- **用途**: 子类覆写 hook

### OnDestroy()
```csharp
protected virtual void OnDestroy()
```
- **用途**: 清理 DOTween
- **调用者**: Unity Engine

## 与 UIScreen 的差异

| 特性 | UIScreen | UIOverlay |
|------|----------|-----------|
| Pause/Resume | 有 | 无 |
| 管理方式 | 互斥 (currentScreen) | 并存 (List) |
| 默认 fadeDuration | 0.3s | 0.2s |

## 未来规划

无。
