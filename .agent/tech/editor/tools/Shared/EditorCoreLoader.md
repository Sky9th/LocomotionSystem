# EditorCoreLoader
> **源文件**: `Assets/Scripts/Shared/Editor/EditorCoreLoader.cs`

进入 Play Mode 时自动将 `playModeStartScene` 设置为 Core.unity，退出时恢复原始设置，确保 Service 层不缺失。

## 调用链

```
被谁调:
  Unity Editor 启动时        → [InitializeOnLoad] 触发静态构造
  EditorApplication.playModeStateChanged → OnPlayModeChanged

调谁:
  PreparePlayFromCore()      → SessionState.SetString / EditorSceneManager.playModeStartScene
  RestoreEditorPlayModeScene() → SessionState.GetString / EditorSceneManager.playModeStartScene (恢复)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | Core 场景 (GameService) | 确保 Core.unity 在 Play Mode 启动场景中被加载 |
| 依赖 | UnityEditor.SceneManagement | playModeStartScene 操作 |

## 方法

### 静态构造函数
```csharp
static EditorCoreLoader()
```
- **用途**: 注册 Play Mode 状态变更回调
- **调用者**: Unity Editor 初始化加载程序集时自动触发 (`[InitializeOnLoad]`)
- **备注**: 只注册事件，不执行场景操作

### OnPlayModeChanged()
```csharp
private static void OnPlayModeChanged(PlayModeStateChange change)
```
- **用途**: Play Mode 状态变更时调度 Prepare / Restore
- **参数**: `change` — PlayModeStateChange 枚举
- **逻辑**:
  - `ExitingEditMode` → `PreparePlayFromCore()`：设置 Core 为启动场景
  - `EnteredEditMode` → `RestoreEditorPlayModeScene()`：恢复原始启动场景

### PreparePlayFromCore()
- **逻辑**:
  1. 如果当前活跃场景已是 Core → 返回（不做任何事）
  2. 否则：通过 `SessionState` 保存当前活跃场景名 + 原始 `playModeStartScene` 路径
  3. 设置 `EditorSceneManager.playModeStartScene` 为 Core.unity 的 SceneAsset

### RestoreEditorPlayModeScene()
- **逻辑**:
  1. 从 `SessionState` 读取之前保存的 `playModeStartScene` 路径
  2. 恢复为原始值（或 null）
  3. 清除 SessionState 中的临时键

## 内部机制

### 条件编译
```csharp
#if UNITY_EDITOR
```
- 整个文件仅在 Editor 环境下编译

### SessionState 键
```csharp
private const string StartupSceneNameKey = "RedDust.Editor.StartupSceneName";
private const string PreviousPlayModeStartScenePathKey = "RedDust.Editor.PreviousPlayModeStartScenePath";
```
- `StartupSceneNameKey` — 保存进入 Play Mode 前的活跃场景名
- `PreviousPlayModeStartScenePathKey` — 保存进入 Play Mode 前 `playModeStartScene` 的原始路径

### 核心路径
```csharp
private const string CoreScenePath = "Assets/Scenes/Core.unity";
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无具体规划。 | — | — |
