# UI System — 技术实现

> 更新: 2026-05-22
> 状态: Service 架构迁移完成
> 方案: uGUI + DOTween + ScriptableObject 配置驱动

## 文件结构

```
Assets/Scripts/UI/
├── UIService.cs                       # BaseService，编排器
├── Core/
│   ├── UIScreenId.cs                  # enum: MainMenu, PauseMenu
│   ├── UIOverlayId.cs                 # enum: VitalsOverlay, StatusOverlay, LoadingOverlay
│   ├── UIModalId.cs                   # enum: (empty, 预留)
│   ├── UIScreen.cs                    # 全屏基类 (CanvasGroup fade + Enter/Exit)
│   └── UIOverlay.cs                   # 叠加基类 (CanvasGroup fade + Enter/Exit)
├── Config/
│   ├── UIThemeSO.cs                   # 颜色/字体/间距/动画参数
│   └── UIPanelConfigSO.cs             # id → prefab + type 注册
├── Components/
│   ├── UIButton.cs                    # DOTween hover/press + 主题色
│   ├── UILabel.cs                     # UITextStyle 枚举驱动主题文本
│   └── UIStatBar.cs                   # 填充条 + 颜色阈值 + DOTween
├── MainMenu/
│   ├── MainMenuScreen.cs             # PZ 风格主菜单
│   └── PauseMenuScreen.cs            # 暂停菜单
└── HUD/
    ├── VitalsOverlay.cs              # HP/Hunger/Thirst/Stamina
    ├── StatusOverlay.cs               # 状态效果（骨架）
    └── LoadingOverlay.cs              # 场景加载过渡
```

无 UIPanel 容器抽象——每个 Screen/Overlay 在自己的 Prefab 里直接用 RectTransform 锚点 + VerticalLayoutGroup 自由布局。

## UIService : BaseService

### 生命周期

```
OnRegister        → context.RegisterService<UIService>(this)
                    → panelConfig.BuildLookup()
OnSubscriptionsActivated
                    → Dispatcher.Subscribe<SGameState>(HandleGameState)
OnServicesReady   → 读 SGameState 快照 → UpdateUIForState
OnDestroy         → Dispatcher.Unsubscribe<SGameState>(HandleGameState)
```

### 公开 API

```csharp
void ShowScreen(string id, object args = null)
void HideScreen(string id)
void ShowOverlay(string id, object args = null)
void HideOverlay(string id)
bool TryGetSnapshot<T>(out T snapshot) where T : struct

// 导航（面板直接调用，不走 EventDispatcher）
void RequestNewGame()
void RequestQuit()

// 状态
bool IsInputBlocked { get; }
```

### Screen 切换流程

```
ShowScreen("MainMenu")
  → TryGetPanel(id, Screen, out UIScreen)
  → 如果已有 currentScreen:
      old.PlayExitSequence().OnComplete(() =>
      {
        Destroy(old.gameObject);
        panelStates.Remove(oldId);
        ActivateScreen(new, id, args);  // SetActive + PlayEnterSequence
      })
  → currentScreen = screen; currentScreenId = id
```

### 场景架构

Persistent Core Scene 模式：
- `Core.unity` (Scene 0) — GameService + EventSystem + UIService，永不卸载
- MainMenuScreen 内嵌于 Core 场景 Canvas，启动即显示主菜单
- `NewGame.unity` — 内容场景，通过 Additive 加载/卸载

UIService 持有两个 Canvas：
- `MainCanvas` (sortOrder=0) — Screen / Overlay 面板
- `LoadingCanvas` (sortOrder=999) — LoadingOverlay 独立 Canvas，永远最顶层

### 场景过渡

**加载 NewGame** — `UIService.RequestNewGame()` → `SLoadSceneRequest("NewGame")`

```
1. loadingCanvasGroup.alpha = 1        // Loading 遮住
2. currentScreen.PlayExitSequence      // MainMenu 淡出
3. SceneService.LoadContentScene()
   a. STimeFreeze → TimeService: timeScale=0
   b. LoadSceneAsync(NewGame, Additive)
   c. 等待 minLoadingDisplayTime (unscaled)
4. SSceneLoadComplete → UIService:
   a. loadingCanvasGroup.alpha = 0
   b. STimeResume → TimeService: timeScale=1
   c. GameStateService.RequestState(Playing)
```

**返回主菜单** — `UIService.RequestMainMenu()` → `SUnloadSceneRequest(null)`

```
1. loadingCanvasGroup.alpha = 1
2. currentScreen.PlayExitSequence      // PauseMenu 淡出
3. SceneService.UnloadContentScene()
   a. STimeFreeze → TimeService: timeScale=0
   b. UnloadSceneAsync(NewGame)
   c. currentContentScene = null
   d. 等待 minLoadingDisplayTime (unscaled)
4. SSceneLoadComplete → UIService:
   a. loadingCanvasGroup.alpha = 0
   b. STimeResume → TimeService: timeScale=1
   c. GameStateService.RequestState(MainMenu)
```

`RequestResume()` 不走场景过渡——同场景内只切状态。

### UpdateUIForState

签名改为 `(SGameState state)`，新增 Paused 分支。Playing 分支用 `PreviousState != Paused` 判断是否建 VitalsOverlay。

### PauseMenuScreen / LoadingOverlay

`PauseMenuScreen : UIScreen` — 四按钮：继续游戏、设置(disabled)、保存(disabled)、返回主菜单。

`LoadingOverlay` 挂载在 `LoadingCanvas` 下，不通过 Instantiate 创建——从 Core 场景开始就存在。通过 `loadingCanvasGroup.alpha` 0/1 切换。`SetPhase`/`SetProgress` API 保留，供未来复杂加载流程直接调用实例方法。

## UIScreen / UIOverlay

### 共同点

- `[SerializeField] CanvasGroup canvasGroup`
- `[SerializeField] float fadeDuration`
- `Initialize(UIService)` → `OnInitialize()` hook
- `PlayEnterSequence()` → alpha 0→1 (EaseOutCubic) → OnEnterFinished
- `PlayExitSequence()` → alpha 1→0 (EaseInCubic) → OnExitFinished
- 返回 DOTween `Sequence`，UIService 可 `yield return seq.WaitForCompletion()`

### 差异

| | UIScreen | UIOverlay |
|---|---|---|
| Pause/Resume | 有 | 无 |
| 管理方式 | 互斥，UIService.currentScreen | 并存，List<UIOverlay> |

## 颜色风格系统

### UIColorSet（9 色结构体）

```csharp
[Serializable]
public struct UIColorSet
{
    public Color primary;          // 按钮背景
    public Color primaryHover;     // 按钮悬浮
    public Color primaryPressed;   // 按钮按下
    public Color onPrimary;        // 按钮文字
    public Color surface;          // 面板背景
    public Color surfaceAlt;       // 交替行
    public Color onSurface;        // 面板文字
    public Color onSurfaceMuted;   // 弱化文字
    public Color border;           // 描边
}
```

### UIColorStyle（全局风格枚举）

`Normal / Primary / Danger / Warning / Success`

### UIThemeSO.GetColorSet(style)

返回对应 `UIColorSet`，UIButton 的 ApplyTheme 和 Pointer 方法通过此方法取色。组件加 `[SerializeField] private UIColorStyle style` 字段，Inspector 下拉切换。

### 组件色彩角色映射

| 组件 | 使用的 UIColorSet 字段 |
|------|----------------------|
| UIButton | bg=primary, hover=primaryHover, press=primaryPressed, text=onPrimary |
| UIPanel | bg=surface | TODO: onSurfaceMuted, border, drag, resize, close |
| UILabel（后续） | 在按钮上=onPrimary，在面板内=onSurface |

## 配置：UIThemeSO

集中管理所有视觉参数：面板背景色、按钮 Normal/Hover/Press/Disabled 四态色、文字色（title/body/subtitle/accent/danger）、StatBar 三色阈值、TMP 字体和字号映射、间距、按钮尺寸、动画时长。

`[CreateAssetMenu(fileName = "UITheme", menuName = "Game/UI/Theme")]`

## 配置：UIPanelConfigSO

```csharp
[Serializable] public struct ScreenEntry {
    public UIScreenId id;
    public GameObject prefab;
}
[Serializable] public struct OverlayEntry {
    public UIOverlayId id;
    public GameObject prefab;
}
[Serializable] public struct ModalEntry {
    public UIModalId id;
    public GameObject prefab;
}
public ScreenEntry[] screens;
public OverlayEntry[] overlays;
public ModalEntry[] modals;
```

`BuildLookup()` 构建三个 `Dictionary<object, GameObject>`，`TryGetScreen/TryGetOverlay/TryGetModal` 类型安全查询。

## 组件

### UIButton

继承 `IPointerEnter/Exit/Down/UpHandler`。Hover→scale 1.05+亮色，Exit→scale 1.0+正常色，Down→scale 0.97+暗色。全部 DOTween，0.1s EaseOutQuad。

Awake 时关闭 Unity Button 原生 transition（设 `Transition.None`），防止和 DOTween 叠动。

暴露 `event Action OnClicked` 和 `bool Interactable` 属性。

### UILabel

`UITextStyle` 枚举驱动：Title/Subtitle/Body/Button/Small。Awake 时从 UIThemeSO 拉字体/字号/颜色应用。`SetStyle()` 支持运行时切换。

### UIStatBar

水平 Filled Image，默认 fillMethod=Horizontal。`SetValue(current, max)` 计算归一化值，DOTween 驱动 fillAmount 平滑过渡。

颜色阈值在 `Update()` 中跟随 `targetFill`（不是 `fillImage.fillAmount`——否则 DOTween 补间期间颜色闪错）。

max ≤ 0 时显示 "--"，角色未生成时安全降级。

### UIPanel

`[ExecuteAlways]`。Awake 时从 `theme.GetColorSet(style).surface` 设 Image.color，提供统一暗色面板背景。

| TODO | 说明 |
|------|------|
| drag | 标题栏拖拽，UIPanelDragHandler 组件 |
| resize | 右下角缩放，UIPanelResizeHandler 组件 |
| close | 关闭按钮 + OnClose event |

## 集成点

- `SGameState` 通过 EventDispatcher 发布 → UIService 订阅，驱动 UI 切换
- `SCharacterSnapshot.Stats` 字典 → VitalsOverlay 每 0.1s 读取，路径可配
- UIService 持久化（GameService `DontDestroyOnLoad`），场景切换不丢失
- MainMenuScreen / PauseMenuScreen → UIService 导航用直接方法调用，不用 EventDispatcher

## Unity Editor 待办

1. Core.unity 设为 Build Settings Scene 0
2. MainMenuScreen prefab 内嵌于 Core 场景 Canvas
3. 连线 GameService → 各 Service 子节点
