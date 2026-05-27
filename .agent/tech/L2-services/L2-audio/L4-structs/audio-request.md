# AudioRequest
> **源文件**: `Assets/Scripts/Audio/AudioRequest.cs`

readonly struct，组装播放所需信息。

## 调用链

```
被谁调:
  CharacterAudio.OnFootstep()       ← new AudioRequest("Foot", config.footsteps, AudioChannelType.SFX)
  外部系统                          ← 构造请求传入 TryResolve

调谁:
  TryResolve(in request, out response)  ← 请求作为参数传入解析函数
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterAudio | 构造请求实例 |
| 依赖 | AudioSetSO | Set 字段持有 AudioSetSO 引用 |
| 依赖 | AudioChannelType | 标识目标通道 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Key` | `string` | 音效标识 Key（如 "Foot"） |
| `Set` | `AudioSetSO` | 音频集引用（子模块具体类型） |
| `Channel` | `AudioChannelType` | 目标通道 (Master/Music/SFX/Ambience/UI/Voice/Alert) |

## 方法

### AudioRequest()
```csharp
public AudioRequest(string key, AudioSetSO set, AudioChannelType channel)
```
- **用途**: 构造播放请求
- **参数**: `key` — 音效标识；`set` — 音频集；`channel` — 目标通道
- **调用者**: 发起方

### None
```csharp
public static AudioRequest None => default;
```
- **用途**: 空请求常量
- **备注**: 用于初始化或失败返回

## 使用规则

- readonly struct，创建后不可变
- Channel 用于 AudioManager 音量分组控制（当前由解析方决定使用方式）
- Set 在 TryResolve 中被向下转型为具体子类读取音频数据

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 增加 Volume/Pitch/FadeIn 字段 | 待做 | 设计文档 audio-system.md |
