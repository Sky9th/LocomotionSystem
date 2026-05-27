# UIPanelConfigSO · 面板配置

> `Assets/Scripts/UI/Config/UIPanelConfigSO.cs` — ScriptableObject。维护 Screen/Overlay/Modal 的 id→Prefab 映射。

## 调用链

```
UIService.OnRegister()
  └── panelConfig.BuildLookup()

UIService.TryGetScreen(id)
  └── panelConfig.TryGetScreen(id, out prefab)
      ├── 先查 lookup dictionary
      └── 未构建时调用 BuildLookup()

UIService.TryGetOverlay(id)
  └── panelConfig.TryGetOverlay(id, out prefab)

(预留) UIService.TryGetModal(id)
  └── panelConfig.TryGetModal(id, out prefab)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被消费 | UIService | 查找所有 Panel Prefab |
| 引用 | UIScreenId / UIOverlayId / UIModalId | 枚举作为查找 Key |

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
- **用途**: 构建 Dictionary<object, GameObject> 查找表，合并 Screen/Overlay/Modal
- **调用者**: UIService.OnRegister()
- **备注**: 必须在 UIService 使用前调用，否则 TryGet 方法会自动触发

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
- **用途**: 根据 ModalId 查找 Prefab (预留)
- **调用者**: 预留给 UIService Modal 系统

## 未来规划

无。
