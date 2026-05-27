# LoadingOverlay · 加载遮罩

> `Assets/Scripts/UI/HUD/LoadingOverlay.cs` — 继承 UIOverlay。场景切换时的渐入渐出遮罩，挂载在独立 LoadingCanvas 上。

## 调用链

```
SSceneLoadStart → UIService.HandleSceneLoadStart
  └── loadingCanvasGroup.alpha = 1

SSceneLoadComplete → UIService.HandleSceneLoadComplete
  └── loadingCanvasGroup.alpha = 0
  └── GameStateService.RequestState(targetState)
```

> LoadingOverlay 不通过 UIService.ShowOverlay 管理，而是由 UIService 直接控制 `loadingCanvasGroup.alpha`。

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIOverlay | 基类 |
| 被控制 | UIService | 直接读写 loadingCanvasGroup.alpha |

## 方法

### SetPhase()
```csharp
public void SetPhase(string phase)
```
- **用途**: 设置加载阶段文字 (如 "Loading..." / "Preparing World")
- **参数**: `phase` — 阶段文字
- **调用者**: 预留，供场景加载进度回调

## 配置参数

| 参数 | 类型 | 用途 |
|------|------|------|
| `phaseText` | TMP_Text | 阶段文字显示 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 加载进度条显示 (progressFill Image) | 待做 | SceneService | 代码 TODO |
