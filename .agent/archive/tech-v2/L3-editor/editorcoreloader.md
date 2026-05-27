# EditorCoreLoader · Core 场景加载器

> `Editor/EditorCoreLoader.cs` — 进入 Play Mode 时自动加载 Core.unity，确保 Service 层不缺失

## 调用链

```
被谁调:
  Unity Editor 启动时        → [InitializeOnLoad] 触发静态构造
  EditorApplication.playModeStateChanged → OnPlayModeChanged

调谁:
  OnPlayModeChanged()        → EditorSceneManager.OpenScene(corePath, Additive)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | 01-core (GameService) | 确保 Core.unity 场景在 Play Mode 开始前已加载 |
| 依赖 | UnityEditor.SceneManagement | OpenScene / GetSceneByPath 操作场景 |

## 方法

### 静态构造函数
```csharp
static EditorCoreLoader()
```
- **用途**: 注册 Play Mode 状态变更回调
- **调用者**: Unity Editor 在初始化加载程序集时自动触发 (`[InitializeOnLoad]`)
- **备注**: 不在这里执行任何场景操作，只注册事件

### OnPlayModeChanged()
```csharp
private static void OnPlayModeChanged(PlayModeStateChange change)
```
- **用途**: 检测进入 Play Mode 前的场景状态，确保 Core 已加载
- **参数**: `change` — Play Mode 状态变更枚举
- **调用者**: `EditorApplication.playModeStateChanged` 事件
- **逻辑**:
  - 仅响应 `PlayModeStateChange.ExitingEditMode` (进入 Play Mode 前一刻)
  - 获取当前活跃场景
  - 如果活跃场景为 `Core` → 跳过 (直接进 Play Mode)
  - 否则 → 以 Additive 模式打开 `Assets/Scenes/Core.unity`
- **备注**: 如果开发者在非 Core 场景下进入 Play Mode，Core 会作为叠加场景加载，GameService.Bootstrap() 检测到 activeScene != Core 时会自动适配

## 内部机制

### 条件编译
```csharp
#if UNITY_EDITOR
```
- 整个文件仅在 Editor 环境下编译，不影响 Runtime 构建

### 关键常量
```csharp
var corePath = "Assets/Scenes/Core.unity";  // Core 场景路径
```
- 硬编码路径，未来可迁移到 GameProfile 配置

## 依赖的 Unity Editor API

| API | 用途 |
|-----|------|
| `InitializeOnLoadAttribute` | 程序集加载时自动运行静态构造 |
| `EditorApplication.playModeStateChanged` | 监听 Play Mode 状态切换 |
| `PlayModeStateChange.ExitingEditMode` | 捕获即将进入 Play Mode 的时机 |
| `EditorSceneManager.OpenScene(Additive)` | 以叠加模式加载场景，不改变当前场景 |
| `EditorSceneManager.GetSceneByPath()` | 按路径检查场景是否已加载 |
| `SceneManager.GetActiveScene()` | 获取当前活跃场景 |
