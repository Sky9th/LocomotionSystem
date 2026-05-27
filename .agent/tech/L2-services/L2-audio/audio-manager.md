# AudioManager
> **源文件**: `Assets/Scripts/Audio/AudioManager.cs`

BaseService，提供音频通道音量分组控制，预留 AudioSource 池化和 3D 音频扩展。

## 调用链

```
被谁调:
  GameService.Bootstrap()           ← 自动发现并注册为 Service
  GameContext                       ← 通过 TryResolveService<AudioManager>() 查找
  外部系统                          ← 调用 SetChannelVolume / GetChannelVolume / MuteChannel / SetMasterVolume

调谁:
  GameContext.RegisterService()     ← OnRegister 时注册自身
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext | 通过 RegisterService 注册 |
| 依赖 | BaseService | 继承自 BaseService |
| 被依赖 | 外部系统 | 通过 GameContext 查找并控制音量 |
| 依赖 | AudioChannelType | 管理各通道音量 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `MasterVolume` | `float` | Master 通道当前音量（只读） |

## 方法

### OnRegister()
```csharp
protected override bool OnRegister(GameContext context)
```
- **用途**: 注册自身到 GameContext
- **返回**: true — 注册成功
- **调用者**: `GameService.Bootstrap()` Step 2

### OnServicesReady()
```csharp
protected override void OnServicesReady()
```
- **用途**: 服务就绪回调（当前为空实现）
- **调用者**: `GameService.Bootstrap()` Step 5

### SetChannelVolume()
```csharp
public void SetChannelVolume(AudioChannelType channel, float volume)
```
- **用途**: 设置指定通道的音量
- **参数**: `channel` — 通道类型；`volume` — 音量 [0, 1]
- **调用者**: 外部系统（设置菜单、游戏逻辑）
- **备注**: 自动 Mathf.Clamp01

### GetChannelVolume()
```csharp
public float GetChannelVolume(AudioChannelType channel)
```
- **用途**: 获取指定通道的当前音量
- **参数**: `channel` — 通道类型
- **返回**: 音量值，默认 1f（TryGetValue 降级）
- **调用者**: 外部系统

### MuteChannel()
```csharp
public void MuteChannel(AudioChannelType channel)
```
- **用途**: 将指定通道静音（音量设为 0）
- **参数**: `channel` — 通道类型
- **调用者**: 外部系统（快捷键静音等）

### SetMasterVolume()
```csharp
public void SetMasterVolume(float volume)
```
- **用途**: 设置 Master 通道音量（全局总音量）
- **参数**: `volume` — 音量 [0, 1]
- **调用者**: 外部系统（设置菜单）
- **备注**: 自动 Mathf.Clamp01；实质为 channelVolumes[Master] = volume

## 内部机制

- Awake/Start/Update 无逻辑（预留扩展）
- 当前无 Teardown 逻辑

### 数据存储

```csharp
Dictionary<AudioChannelType, float> channelVolumes
// 构造时初始化所有通道为 1f
// 通道枚举: Master / Music / SFX / Ambience / UI / Voice / Alert
```

## 使用规则

- 通过 `GameContext.TryResolveService<AudioManager>()` 获取引用
- 音量值始终保持在 [0, 1] 范围
- 当前 AudioSource 池未实现，Play 请求直接由 AudioChannel 静态方法处理

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| AudioSource 对象池 | 待做 | 设计文档 audio-system.md |
| 3D 空间音频混音 | 待做 | 设计文档 audio-system.md |
| 淡入淡出控制 | 待做 | 设计文档 audio-system.md |
| 按群组暂停/恢复 | 远期 | 设计文档 audio-system.md |
| Unity AudioMixer 集成 | 远期 | 设计文档 audio-system.md |
