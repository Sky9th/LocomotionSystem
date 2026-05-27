# UIPanelConfigSO
> **源文件**: `Assets/Scripts/UI/Config/UIPanelConfigSO.cs`

ScriptableObject。维护 Screen/Overlay/Modal 的 id→Prefab 映射。

## 调用链

```
UIService.OnRegister()
  └── panelConfig.BuildLookup()

UIService.TryGetScreen(id)
  └── panelConfig.TryGetScreen(id, out prefab)
      ├── 先查 lookup dictionary
      └── 未构建时自动调用 BuildLookup()

UIService.TryGetOverlay(id)
  └── panelConfig.TryGetOverlay(id, out prefab)

(预留) UIService.TryGetModal(id)
  └── panelConfig.TryGetModal(id, out prefab)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被消费 | UIService | 查找所有 Panel Prefab |
| 引用 | UIScreenId | Screen 查找 Key |
| 引用 | UIOverlayId | Overlay 查找 Key |
| 引用 | UIModalId | Modal 查找 Key（预留） |

## 公开属性

| 字段 | 类型 | 用途 |
|------|------|------|
| `screens` | ScreenEntry[] | Screen id→Prefab 映射表 |
| `overlays` | OverlayEntry[] | Overlay id→Prefab 映射表 |
| `modals` | ModalEntry[] | Modal id→Prefab 映射表（预留） |

## 数据结构

```csharp
[Serializable]
public struct ScreenEntry  { public UIScreenId id; public GameObject prefab; }
[Serializable]
public struct OverlayEntry { public UIOverlayId id; public GameObject prefab; }
[Serializable]
public struct ModalEntry   { public UIModalId id; public GameObject prefab; }
```

## 方法

### BuildLookup()
```csharp
public void BuildLookup()
```
- **用途**: 构建 `Dictionary<object, GameObject>` 查找表，合并 Screen/Overlay/Modal
- **调用者**: UIService.OnRegister()
- **备注**: 懒加载 — 若 TryGet 方法被调用时 lookup 为 null 则自动触发

### TryGetScreen()
```csharp
public bool TryGetScreen(UIScreenId id, out GameObject prefab)
```
- **用途**: 根据 ScreenId 查找 Prefab
- **调用者**: UIService.TryGetScreen()

### TryGetOverlay()
```csharp
public bool TryGetOverlay(UIOverlayId id, out GameObject prefab)
```
- **用途**: 根据 OverlayId 查找 Prefab
- **调用者**: UIService.TryGetOverlay()

### TryGetModal()
```csharp
public bool TryGetModal(UIModalId id, out GameObject prefab)
```
- **用途**: 根据 ModalId 查找 Prefab（预留）
- **调用者**: 预留给 UIService Modal 系统

## 内部机制

无特殊 Unity 生命周期方法。

## 未来规划

无。
