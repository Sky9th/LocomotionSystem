# UI System — 技术实现

> 更新: 2026-05-19
> 状态: Phase 3.5 完成，UIPanelId 拆分为三枚举，完整游戏循环闭环
> 方案: uGUI + DOTween + ScriptableObject 配置驱动

## 文件结构

```
Assets/Scripts/UI/
├── UIManager.cs                       # BaseService，编排器
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

## UIManager : BaseService

### 生命周期

```
OnRegister        → context.RegisterService<UIManager>(this)
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

### 场景过渡

统一入口 `TransitionWithLoading(sceneName, targetState)`，替换原 `TransitionToGameplay`：

```
1. ShowOverlay(LoadingOverlay)       // Loading 盖上来
2. currentScreen.PlayExitSequence    // 当前屏幕淡出（在 Loading 下面）
3. SceneManager.LoadSceneAsync       // 异步加载场景
4. HideOverlay(LoadingOverlay)       // Loading 退出（须在 RequestState 前）
5. GameState.RequestState            // SGameState 事件 → UpdateUIForState
```

`RequestNewGame()` `RequestMainMenu()` 走同一协程。`RequestResume()` 不走——同场景内只切状态。

### UpdateUIForState

签名改为 `(SGameState state)`，新增 Paused 分支。Playing 分支用 `PreviousState != Paused` 判断是否建 VitalsOverlay。

### PauseMenuScreen / LoadingOverlay

`PauseMenuScreen : UIScreen` — 四按钮：继续游戏、设置(disabled)、保存(disabled)、返回主菜单。继续调 `uiManager.RequestResume()`，返回主菜单调 `uiManager.RequestMainMenu()`。

`LoadingOverlay : UIOverlay` — `phaseText` + `SetPhase(string)`。`SetProgress` 预留。MVP 写死 "Loading..." Label。

## UIScreen / UIOverlay

### 共同点

- `[SerializeField] CanvasGroup canvasGroup`
- `[SerializeField] float fadeDuration`
- `Initialize(UIManager)` → `OnInitialize()` hook
- `PlayEnterSequence()` → alpha 0→1 (EaseOutCubic) → OnEnterFinished
- `PlayExitSequence()` → alpha 1→0 (EaseInCubic) → OnExitFinished
- 返回 DOTween `Sequence`，UIManager 可 `yield return seq.WaitForCompletion()`

### 差异

| | UIScreen | UIOverlay |
|---|---|---|
| Pause/Resume | 有 | 无 |
| 管理方式 | 互斥，UIManager.currentScreen | 并存，List<UIOverlay> |

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

- `SGameState` 通过 EventDispatcher 发布 → UIManager 订阅，驱动 UI 切换
- `SCharacterSnapshot.Stats` 字典 → VitalsOverlay 每 0.1s 读取，路径可配
- UIManager 持久化（GameManager `DontDestroyOnLoad`），场景切换不丢失
- MainMenuScreen → UIManager 导航用直接方法调用，不用 EventDispatcher

## Unity Editor 待办

1. DOTween 已安装（`Assets/Plugins/Demigiant/`），在 Utility Panel 确认 Setup
2. 创建 `Assets/Settings/UI/DefaultTheme.asset`（Create → Game → UI → Theme）
3. 创建 `Assets/Settings/UI/PanelConfig.asset`（Create → Game → UI → Panel Config）
4. GameManager 预制体下添加 UIManager 子节点，内建 Canvas + ScreenContainer/OverlayContainer/ModalContainer
5. 制作 MainMenuScreen.prefab 和 VitalsOverlay.prefab
6. 连线配置
