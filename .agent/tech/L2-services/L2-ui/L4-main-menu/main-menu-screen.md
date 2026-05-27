# MainMenuScreen
> **源文件**: `Assets/Scripts/UI/MainMenu/MainMenuScreen.cs`

继承 UIScreen。PZ 风格主菜单：新游戏 / 加载存档 / 设置 / 退出。

## 调用链

```
SGameState{MainMenu} → UIService.HandleGameState
  └── ShowScreen(MainMenu)
      └── ActivateScreen → PlayEnterSequence()
          └── OnInitialize()
              ├── newGameButton  → "新游戏" + OnClicked → HandleNewGame
              ├── loadGameButton → "加载存档" + SetInteractable(false)
              ├── settingsButton → "设置" + SetInteractable(false)
              ├── quitButton     → "退出游戏" + OnClicked → HandleQuit
              └── versionText    → Application.version

按钮事件:
  ├── HandleNewGame → uiService.RequestNewGame()
  │   └── StartSceneTransition → 淡出 → SLoadSceneRequest → Playing
  └── HandleQuit → uiService.RequestQuit()
      └── Application.Quit()

销毁:
  └── OnDestroy → 取消 newGameButton/quitButton 的 OnClicked 事件绑定
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIScreen | 基类 |
| 依赖 | UIService | 导航 (RequestNewGame / RequestQuit) |
| 依赖 | UIButton | 4 个按钮组件 |
| 依赖 | UILabel | 版本号文字 |

## 公开属性

无公开属性。所有字段通过 `[SerializeField]` 在 Inspector 中配置。

## 方法

### OnInitialize()
```csharp
protected override void OnInitialize()
```
- **用途**: 配置按钮文字和点击事件
- **调用者**: UIScreen.Initialize()
- **备注**: loadGameButton 和 settingsButton 当前为 disabled

### HandleNewGame()
```csharp
private void HandleNewGame()
```
- **用途**: 开始新游戏
- **调用者**: newGameButton.OnClicked

### HandleQuit()
```csharp
private void HandleQuit()
```
- **用途**: 退出游戏
- **调用者**: quitButton.OnClicked

### OnDestroy()
```csharp
protected override void OnDestroy()
```
- **用途**: 取消事件绑定，防止残留引用
- **调用者**: Unity Engine

## 内部机制

- **MonoBehaviour**: UIScreen → MonoBehaviour，受 Unity 生命周期管理
- **事件清理**: `OnDestroy` 中退订 `OnClicked` 事件，防止对象销毁后回调触发

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| LoadGame 按钮启用（存档系统就绪后） | 待做 | 旧 ui-system.md |
| Settings 按钮启用（设置面板就绪后） | 待做 | 旧 ui-system.md |
