# PauseMenuScreen · 暂停菜单

> `Assets/Scripts/UI/MainMenu/PauseMenuScreen.cs` — 继承 UIScreen。暂停菜单：继续游戏 / 设置 / 保存 / 返回主菜单。

## 调用链

```
SGameState{Paused} → UIService.HandleGameState
  └── ShowScreen(PauseMenu)
      └── OnInitialize()
          ├── continueBtn  → "继续游戏" + OnClicked → HandleContinue
          ├── settingsBtn  → "设置" + SetInteractable(false)
          ├── saveBtn      → "保存" + SetInteractable(false)
          └── mainMenuBtn  → "返回主菜单" + OnClicked → HandleMainMenu

按钮事件:
  ├── HandleContinue → uiService.RequestResume()
  │   └── GameStateService.RequestState(Playing)
  └── HandleMainMenu → uiService.RequestMainMenu()
      └── 淡出 → SUnloadSceneRequest → MainMenu
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIScreen | 基类 |
| 依赖 | UIService | 导航 (RequestResume / RequestMainMenu) |
| 依赖 | UIButton | 4 个按钮组件 |

## 方法

### OnInitialize()
```csharp
protected override void OnInitialize()
```
- **用途**: 配置按钮文字和点击事件
- **调用者**: UIScreen.Initialize()
- **备注**: settingsBtn 和 saveBtn 当前为 disabled

### HandleContinue()
```csharp
void HandleContinue()
```
- **用途**: 恢复游戏
- **调用者**: continueBtn.OnClicked
- **备注**: 通过 UIService.RequestResume → GameStateService.RequestState(Playing)

### HandleMainMenu()
```csharp
void HandleMainMenu()
```
- **用途**: 返回主菜单
- **调用者**: mainMenuBtn.OnClicked
- **备注**: 触发场景卸载和状态切换

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Save 按钮启用 (存档系统就绪后) | 待做 | 旧 ui-system.md |
| Settings 按钮启用 (设置面板就绪后) | 待做 | 旧 ui-system.md |
