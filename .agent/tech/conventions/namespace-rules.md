# Namespace 约定

## 根

`RedDust`

## 映射规则

```
namespace = "RedDust" + "." + 从 Scripts/ 起, 每个 L#_ 目录名去前缀后, 用 "." 连接
```

- **L#_ 前缀目录**: 去 `L#_` 后作为 namespace 段（`L3_Character` → `Character`）
- **非 L#_ 前缀目录**: 组织性容器，不产生 namespace 段，文件归入父级（`Actor/`, `Config/`, `Structs/`, `Data/`, `Rules/`, `States/`, `Requests/` 等）
- **容器目录**: `Services/` 和 `Modules/` 跳过，不出现在 namespace 中
- **Shared/**: 作为 Scripts 根级目录，映射为 namespace 段 `Shared`

## 完整映射

```
RedDust.Shared                                    ← Shared/
RedDust.Core                                      ← L1_Core/ (含 EventDispatcherService)
RedDust.Audio                                     ← Services/L2_Audio/
RedDust.GameCamera                                ← Services/L2_CameraService/
RedDust.GameState                                 ← Services/L2_GameStateService/
RedDust.GameInput                                 ← Services/L2_Input/
RedDust.Player                                    ← Services/L2_PlayerService/
RedDust.GameScene                                 ← Services/L2_SceneService/
RedDust.GameTime                                  ← Services/L2_TimeService/
RedDust.UI                                        ← Services/L2_UI/
RedDust.Character                                 ← Services/Modules/L3_Character/ (Actor, Config, Input)
RedDust.Character.Animation                       ← L3_Character/L4_Animation/ (Config, Requests)
RedDust.Character.Animation.Drivers               ← L5_Drivers/ (根目录)
RedDust.Character.Animation.Drivers.Locomotion    ← L5_Drivers/L5_Locomotion/ (States)
RedDust.Character.Audio                           ← L3_Character/L4_Audio/ (Config)
RedDust.Character.Kinematic                       ← L3_Character/L4_Kinematic/ (Structs)
RedDust.Character.Locomotion                      ← L3_Character/L4_Locomotion/ (Structs)
RedDust.Character.Stats                           ← L3_Character/L4_Stats/ (Rules)
RedDust.Stats                                     ← Services/Modules/L3_Stats/
RedDust.Stats.Editor                              ← Services/Modules/L3_Stats/Editor/
RedDust.Pathfinding                               ← Services/Modules/L3_Pathfinding/ (stub, 无代码)
```

## 层级豁免

以下类型不适用 L1-L5 单向依赖约束，可被任意层引用：

- **契约定义枚举**（如 `EGameState`）—— 纯数据，定义游戏状态协议
- **事件驱动 Struct**（如 `SSceneLoadComplete`、`SIActionMove`）—— 通过 EventDispatcher 分发，是跨层共享数据载体

## 注意事项

- `EventDispatcherService` 已从 L2_EventDispatcher 移至 L1_Core，namespace 合并到 `RedDust.Core`
- `RedDust.GameInput`、`RedDust.GameTime`、`RedDust.GameCamera` 跳过 `Service` 后缀并添加 `Game` 前缀以避免 namespace/class 同名和 Unity 类型冲突（`Input`、`Time`、`Camera`）
- 第三方代码（Plugins/, Packages/）的 namespace 不受此约定约束
