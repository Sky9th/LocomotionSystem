# 2026-05-23 会话层显式化重构

## 背景

返回主菜单时 UI Overlay 未隐藏、GameContext 快照残留、CameraService 继续跟随过时位置。
根因：会话级状态散落在 DontDestroyOnLoad 各 Service 中，无显式创建/销毁生命周期。

## 决策

采用 D 方案（状态放场景的正确理解版）：
- DontDestroyOnLoad 分基础设施层和会话层
- 会话层对象由各 Service 显式拥有、显式销毁
- GameService 作为会话协调者，Boot 和 Teardown 统一
- 使用 `IGameplaySessionHandler` 接口而非 BaseService virtual（不耦合基类）

对比了 A/B/C/D 四个方案，D 在约束力(5/5)、一致性(4/5)、扩展性(5/5)上碾压。

## 改动清单

新建：
- `IGameplaySessionHandler.cs`
- `EditorCoreLoader.cs`

修改：
- `GameService.cs` — 会话协调，TeardownSession()，Editor 直开
- `GameContext.cs` — ClearSnapshots()
- `PlayerService.cs` — 拥有 Player，Instantiate(transform)
- `CameraService.cs` — OnGameplaySessionEnd，防御性 pivot 重建
- `UIService.cs` — HideAllOverlays()，pendingTargetState 守卫
- `SceneService.cs` — SetCurrentContentScene()，移除 STimeFreeze/Resume
- `TimeService.cs` — 移除自循环，isSceneLoading/isGamePaused 分开追踪
- `GameStateService.cs` — Editor 直开 initialState=Playing

删除：
- `STimeFreeze.cs` / `STimeResume.cs` — 死代码

Bug 修复：
- `InputService.OnDestroy` 缺少 `actionsConfigured` 守卫（重复 GameManager 实例破坏共享 handler 回调）
- 调试日志清理

## 后续

- 门过渡（内容场景间切换）能力已就绪，Player 在会话层不受影响
