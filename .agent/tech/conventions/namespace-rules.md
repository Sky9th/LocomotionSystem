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
RedDust.Core                                      ← L1_Core/
RedDust.Audio                                     ← Services/L2_Audio/
RedDust.CameraService                             ← Services/L2_CameraService/
RedDust.EventDispatcher                           ← Services/L2_EventDispatcher/
RedDust.GameStateService                          ← Services/L2_GameStateService/
RedDust.Input                                     ← Services/L2_Input/
RedDust.PlayerService                             ← Services/L2_PlayerService/
RedDust.SceneService                              ← Services/L2_SceneService/
RedDust.TimeService                               ← Services/L2_TimeService/
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
RedDust.Pathfinding                               ← Services/Modules/L3_Pathfinding/
```

## 注意事项

- L2 Service 的 namespace 名可能与内部主类名相同（如 `RedDust.GameStateService` 包含 `GameStateService` 类），跨 namespace 引用时需用完全限定名
- `RedDust.Input` namespace 与 `UnityEngine.Input` 冲突，引用 Unity Input API 时必须使用 `UnityEngine.Input.xxx`
- 第三方代码（Plugins/, Packages/）的 namespace 不受此约定约束
