# Namespace 约定

## 根

`RedDust`

## 映射规则

```
namespace = "RedDust" + "." + 从 Scripts/ 起, 逐段拼接:
  容器目录（Core, Services, Gameplay）→ 保留
  L#_ 前缀目录 → 去 L#_ 后作为 namespace 段
  Shared/ → 保留为 Shared
  非 L#_ 前缀目录 → 组织性容器，不产生 namespace 段（Actor/, Config/, Structs/ 等）
```

### 示例

| 路径 | namespace |
|------|-----------|
| `Core/L1_Events/EventHub.cs` | `RedDust.Core.Events` |
| `Core/L1_GameService/GameService.cs` | `RedDust.Core.GameService` |
| `Core/L1_Structs/MetaStruct.cs` | `RedDust.Core.Structs` |
| `Services/L2_Audio/AudioService.cs` | `RedDust.Services.Audio` |
| `Services/L2_EntityService/EntityService.cs` | `RedDust.Services.EntityService` |
| `Gameplay/L3_Character/Actor/CharacterActor.cs` | `RedDust.Gameplay.Character` |
| `Gameplay/L3_Ability/AbilityForest.cs` | `RedDust.Gameplay.Ability` |
| `Shared/Logging/LogManager.cs` | `RedDust.Shared.Logging` |

## 完整映射（v0.45.4 目录结构）

```
RedDust.Shared                                    ← Shared/
RedDust.Shared.Logging                            ← Shared/Logging/
RedDust.Shared.Utility                            ← Shared/Utility/

RedDust.Core.GameService                          ← Core/L1_GameService/
RedDust.Core.GameContext                          ← Core/L1_GameContext/
RedDust.Core.Modules                              ← Core/L1_Modules/
RedDust.Core.Events                               ← Core/L1_Events/
RedDust.Core.RdTag                                ← Core/L1_RdTag/
RedDust.Core.Structs                              ← Core/L1_Structs/

RedDust.Services.Audio                            ← Services/L2_Audio/
RedDust.Services.AssetService                     ← Services/L2_AssetService/
RedDust.Services.AI                               ← Services/L2_AIService/
RedDust.Services.Camera                           ← Services/L2_CameraService/
RedDust.Services.EntityService                    ← Services/L2_EntityService/
RedDust.Services.GameState                        ← Services/L2_GameStateService/
RedDust.Services.Input                            ← Services/L2_Input/
RedDust.Services.ModService                       ← Services/L2_ModService/
RedDust.Services.Pathfinding                      ← Services/L2_Pathfinding/
RedDust.Services.Player                           ← Services/L2_PlayerService/
RedDust.Services.Scene                            ← Services/L2_SceneService/
RedDust.Services.Time                             ← Services/L2_TimeService/
RedDust.Services.UI                               ← Services/L2_UI/

RedDust.Gameplay.Ability                          ← Gameplay/L3_Ability/
RedDust.Gameplay.Ammo                             ← Gameplay/L3_Ammo/
RedDust.Gameplay.Building                         ← Gameplay/L3_Building/
RedDust.Gameplay.Character                        ← Gameplay/L3_Character/
RedDust.Gameplay.Character.Animation              ← Gameplay/L3_Character/Animation/
RedDust.Gameplay.Character.Animation.Drivers      ← .../Animation/Drivers/
RedDust.Gameplay.Character.Animation.Drivers.Locomotion ← .../Drivers/Locomotion/
RedDust.Gameplay.Character.Audio                  ← .../Audio/
RedDust.Gameplay.Character.Kinematic              ← .../Kinematic/
RedDust.Gameplay.Character.Locomotion             ← .../Locomotion/
RedDust.Gameplay.Consumable                       ← Gameplay/L3_Consumable/
RedDust.Gameplay.Container                        ← Gameplay/L3_Container/
RedDust.Gameplay.Equipment                        ← Gameplay/L3_Equipment/
RedDust.Gameplay.Identity                         ← Gameplay/L3_Identity/
RedDust.Gameplay.Properties                       ← Gameplay/L3_Properties/
RedDust.Gameplay.SceneItem                        ← Gameplay/L3_SceneItem/
```

## 层级豁免

以下类型不适用 L1-L5 单向依赖约束，可被任意层引用：

- **契约定义枚举**（如 `EGameState`）—— 纯数据，定义游戏状态协议
- **事件驱动 Struct**（如 `SSceneLoadComplete`、`SIActionMove`）—— 通过 EventDispatcher 分发，是跨层共享数据载体

## 注意事项

- `Camera`、`Input`、`Time` 等 namespace 段与 Unity 内置类型可能冲突，但 C# 的 `using` 别名或全限定名可规避
- 第三方代码（Plugins/, Packages/）的 namespace 不受此约定约束
- Editor/ 下代码遵循相同规则，附加 `.Editor` 后缀
