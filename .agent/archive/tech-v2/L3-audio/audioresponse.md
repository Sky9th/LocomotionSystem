# AudioResponse · 播放结果

> `Audio/AudioResponse.cs` — readonly struct，TryResolve 的产出，包含播放所需的具体音频数据

## 调用链

```
被谁调:
  TryResolve(in request, out response)  ← 在解析函数中构造并输出
  AudioChannel.Play(in response, ...)   ← 读取 Clip/Volume/Pitch 播放

调谁: (无 — 纯数据载体)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 02-character CharacterAudio | TryResolve 产出 response |
| 被依赖 | AudioChannel | Play 方法消费 response |
| 依赖 | AudioClip | 持有 Unity AudioClip 引用 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Clip` | `AudioClip` | 要播放的音频片段 |
| `Volume` | `float` | 播放音量 |
| `Pitch` | `float` | 播放音调 |
| `IsValid` | `bool` | 是否有效（Clip != null），计算属性 |

## 方法

### AudioResponse()
```csharp
public AudioResponse(AudioClip clip, float volume, float pitch)
```
- **用途**: 构造播放响应
- **参数**: `clip` — 音频片段；`volume` — 音量；`pitch` — 音调
- **调用者**: `TryResolve()` 解析函数

### None
```csharp
public static AudioResponse None => default;
```
- **用途**: 空响应常量
- **备注**: IsValid = false

## 使用规则

- readonly struct，创建后不可变
- 播放前必须检查 `IsValid`，否则跳过播放
- Volume 和 Pitch 在 TryResolve 中由具体 AudioSetSO 决定（如 FootstepSetSO 的 baseVolume + pitchVariation 随机）

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 增加持续时间/Duration 字段 | 远期 | 设计文档 audio-system.md |
