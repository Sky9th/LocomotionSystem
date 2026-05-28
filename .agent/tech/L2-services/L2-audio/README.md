# L2_Audio · 音频系统

> 基于 AudioSetSO 的多变体音频系统。AudioManager 管理音量分组，AudioChannel 提供静态 Play 方法，Request/Response 模式分离职责。

**源文件目录**: `Assets/Scripts/Services/L2_Audio/`

## 层级定位

Services 复合 Service。AudioManager 继承 BaseService 由 GameService 管理生命周期，音频数据以 ScriptableObject (AudioSetSO) 形式配置、子模块继承扩展。

- **被 L1 管理**: GameService.Bootstrap() 发现并注册 AudioManager。
- **被 L3 消费**: Character 模块 (CharacterAudio) 构造请求并调用 AudioChannel 播放。
- **依赖 L3 数据**: AudioSetSO 由子模块 (CharacterAudioConfigSO) 继承扩展。

## 调用链

```
播放发起方 (如 CharacterAudio.OnFootstep)
  │
  ├── new AudioRequest(key, set, channel)       ← 组装请求
  │
  ├── TryResolve(request, out response)          ← AudioSetSO 解析为 Clip + Volume + Pitch
  │     └── AudioResponse(clip, volume, pitch)
  │
  └── AudioChannel.Play(in response, source)     ← 通过 AudioSource 播放
        ├── Play(in response, AudioSource)
        │     └── source.pitch = response.Pitch
        │     └── source.PlayOneShot(response.Clip, response.Volume)
        └── Play(in response, worldPosition)     ← 3D 空间音效
              └── AudioSource.PlayClipAtPoint(response.Clip, worldPosition, response.Volume)

AudioManager (BaseService)                       ← 音量分组控制
  ├── SetChannelVolume(channel, volume)
  ├── GetChannelVolume(channel)
  ├── MuteChannel(channel)
  └── SetMasterVolume(volume)
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|------|-----------|------|
| AudioRequest, AudioResponse | CharacterAudio | 发起播放请求并解析为响应 |
| AudioChannel | CharacterAudio | 消费方通过静态 Play 方法播放 |
| AudioSetSO | CharacterAudioConfigSO | CharacterAudioConfigSO 继承 AudioSetSO |
| AudioManager | GameService | 注册为 BaseService，由 Bootstrap 管理生命周期 |
| 本模块 | — | 无其他核心依赖 |

## 设计决策

| 决策 | 原因 |
|------|------|
| AudioChannel 为静态工具类 | 播放不依赖实例，纯函数式调用 |
| AudioSetSO 为抽象基类 | 子模块可扩展自有 AudioSet（脚步声、武器、僵尸），继承多态替代 switch |
| AudioManager 预留为 Service | 未来扩展 AudioSource 池和 3D 空间音频需要实例化管理 |
| Request/Response 模式 | 职责分离：发起方只关心 WHAT，播放层只关心 HOW |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| AudioSource 池化 | 待做 | AudioManager 扩展 | 设计文档 audio-system.md |
| 3D 空间音频支持 | 待做 | AudioSource 池 | 设计文档 audio-system.md |
| 淡入淡出 (FadeIn/FadeOut) | 待做 | AudioManager 扩展 | AudioManager 预留字段 |
| 音量分组独立控制 | 已实现 | AudioManager | AudioChannelType 枚举 |
| 按群组暂停/恢复 | 远期 | AudioManager 扩展 | 设计文档 audio-system.md |
| AudioMixer 集成 | 远期 | Unity AudioMixer | 设计文档 audio-system.md |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [audio-manager.md](audio-manager.md) | 音频总管 — 音量分组控制，预留 AudioSource 池 |
| [L4-data/audio-set-so.md](L4-data/audio-set-so.md) | 音频集基类 — 抽象 SO，子模块继承扩展 |
| [L4-data/audio-channel.md](L4-data/audio-channel.md) | 音频通道 — 静态 Play 方法，AudioSource/3D 播放 |
| [L4-structs/audio-request.md](L4-structs/audio-request.md) | 播放请求 struct — Key/Set/Channel |
| [L4-structs/audio-response.md](L4-structs/audio-response.md) | 播放结果 struct — Clip/Volume/Pitch |
