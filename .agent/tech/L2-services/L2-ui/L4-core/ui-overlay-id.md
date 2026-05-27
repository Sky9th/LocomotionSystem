# UIOverlayId
> **源文件**: `Assets/Scripts/UI/Core/UIOverlayId.cs`

叠加层标识枚举。

## 值

| 枚举值 | 用途 |
|--------|------|
| `VitalsOverlay` | 生命体征 HUD (HP/Hunger/Thirst/Stamina) |
| `StatusOverlay` | 状态效果 HUD (Buff/Debuff) |
| `LoadingOverlay` | 加载遮罩 |

## 使用位置

- `UIPanelConfigSO.OverlayEntry` — id→Prefab 映射 Key
- `UIService` — ShowOverlay / HideOverlay 创建管理

## 未来规划

无。
