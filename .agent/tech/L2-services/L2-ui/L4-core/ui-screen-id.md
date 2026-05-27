# UIScreenId
> **源文件**: `Assets/Scripts/UI/Core/UIScreenId.cs`

全屏面板标识枚举。

## 值

| 枚举值 | 用途 |
|--------|------|
| `MainMenu` | 主菜单 |
| `PauseMenu` | 暂停菜单 |

## 调用链

枚举类型，被 UIScreen、UIService 等引用以标识界面。

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被引用 | UIService | HandleGameState 状态路由 |
| 被引用 | UIPanelConfigSO.ScreenEntry | id→Prefab 映射 Key |
| 被引用 | UIService.screenStates | PanelState 缓存 Key |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 新增 SettingsScreen | 待做 | 旧 ui-system.md |
| 新增 SaveLoadScreen | 待做 | 旧 ui-system.md |
