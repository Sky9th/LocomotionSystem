# UIScreen · 全屏面板基类

> `Assets/Scripts/UI/Core/UIScreen.cs` — 抽象基类。全屏面板，互斥管理。支持 Enter/Exit 渐变动画 + Pause/Resume 交互控制。

## 调用链

```
UIService.ShowScreen(id)
  └── ActivateScreen()
      ├── screen.gameObject.SetActive(true)
      └── screen.PlayEnterSequence(args)
          ├── canvasGroup.DOFade(1f)
          └── OnEnterFinished()
  └── 当前 Screen 被替换时:
      ├── old.PlayExitSequence()
      │   ├── canvasGroup.DOFade(0f)
      │   └── OnExitFinished()
      └── → Destroy(gameObject)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIService | Initialize 时注入 |
| 依赖 | DOTween | Enter/Exit 动画 |
| 被继承 | MainMenuScreen | 主菜单 |
| 被继承 | PauseMenuScreen | 暂停菜单 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `DeltaTime` | float (protected) | Time.unscaledDeltaTime，不受暂停影响 |
| `uiService` | UIService (protected) | Service 引用 |

## 方法

### Initialize()
```csharp
public void Initialize(UIService manager)
```
- **用途**: 注入 UIService 引用，调用 OnInitialize hook
- **参数**: `manager` — UIService 实例
- **调用者**: UIService.TryGetScreen()

### PlayEnterSequence()
```csharp
public virtual Sequence PlayEnterSequence(object args = null)
```
- **用途**: 播放进入动画 — alpha 0→1, EaseOutCubic
- **参数**: `args` — 可选参数 (由子类处理)
- **返回**: DOTween Sequence (UIService 可链式调用)
- **调用者**: UIService.ActivateScreen()
- **备注**: 设置 interactable=true, blocksRaycasts=true

### PlayExitSequence()
```csharp
public virtual Sequence PlayExitSequence()
```
- **用途**: 播放退出动画 — alpha 1→0, EaseInCubic
- **参数**: 无
- **返回**: DOTween Sequence
- **调用者**: UIService.ShowScreen (替换时)
- **备注**: 设置 interactable=false, blocksRaycasts=false

### OnPause()
```csharp
public virtual void OnPause()
```
- **用途**: 暂停时禁用交互
- **调用者**: 预留给外部
- **备注**: canvasGroup.interactable = false

### OnResume()
```csharp
public virtual void OnResume()
```
- **用途**: 恢复时启用交互
- **调用者**: 预留给外部
- **备注**: canvasGroup.interactable = true

### OnInitialize()
```csharp
protected virtual void OnInitialize()
```
- **用途**: 子类覆写，初始化逻辑 (如绑定按钮事件)
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
- **用途**: 清理 DOTween，防止残留动画
- **调用者**: Unity Engine

## 配置参数

| 参数 | 类型 | 默认值 |
|------|------|--------|
| `fadeDuration` | float | 0.3f |

## 未来规划

无。
