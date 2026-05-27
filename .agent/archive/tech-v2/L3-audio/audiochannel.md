# AudioChannel · 音频通道

> `Audio/AudioChannel.cs` — 静态工具类，提供 AudioResponse 的播放能力

## 调用链

```
被谁调:
  CharacterAudio.OnFootstep()         ← 脚步音效
  外部系统 (任何持有 AudioResponse 的代码)  ← 播放音效

调谁:
  AudioSource.PlayOneShot()           ← 通过现有 AudioSource 播放
  AudioSource.PlayClipAtPoint()       ← 3D 空间播放
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 02-character CharacterAudio | 消费方，调用 Play 方法 |
| 被依赖 | 任何外部系统 | 只要有 AudioResponse 即可播放 |
| 依赖 | AudioResponse | 接收 response 参数，读取 Clip/Volume/Pitch |

## 方法

### Play(AudioSource)
```csharp
public static void Play(in AudioResponse response, AudioSource source)
```
- **用途**: 通过指定 AudioSource 播放音效
- **参数**: `response` — 播放响应（Clip + Volume + Pitch）；`source` — 目标 AudioSource
- **调用者**: CharacterAudio.OnFootstep() 等
- **备注**: response.IsValid 为 false 或 source 为 null 时直接返回

### Play(3D)
```csharp
public static void Play(in AudioResponse response, Vector3 worldPosition)
```
- **用途**: 在世界坐标位置播放 3D 空间音效
- **参数**: `response` — 播放响应；`worldPosition` — 世界坐标
- **调用者**: 外部系统（爆炸、环境音等）
- **备注**: 使用 Unity 内置 AudioSource.PlayClipAtPoint，自动创建临时 AudioSource

## 使用规则

- 纯静态类，无实例状态
- 播放前检查 `response.IsValid`（Clip != null）
- 3D 播放使用 PlayClipAtPoint，自动管理临时 AudioSource 生命周期

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 播放完成后回调 | 远期 | 设计文档 audio-system.md |
