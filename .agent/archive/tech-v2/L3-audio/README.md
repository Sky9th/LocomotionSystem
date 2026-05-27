# 06-audio · 音频系统

> 基于 AudioSetSO 的多变体音频系统。AudioManager 管理 AudioSource 池和音量分组，AudioChannel 提供静态 Play 方法。

## 调用链

```
播放发起方 (如 CharacterAudio.OnFootstep)
  │
  ├── new AudioRequest(key, set, channel)     ← 组装请求
  │
  ├── TryResolve(request, out response)       ← AudioSetSO 解析为 AudioClip + Volume + Pitch
  │     └── AudioResponse(clip, volume, pitch)
  │
  └── AudioChannel.Play(in response, source)  ← 通过 AudioSource 播放
        ├── AudioChannel.Play(in response, AudioSource)
        │     └── source.pitch = response.Pitch
        │     └── source.PlayOneShot(response.Clip, response.Volume)
        └── AudioChannel.Play(in response, worldPosition)   ← 3D 空间音效
              └── AudioSource.PlayClipAtPoint(response.Clip, worldPosition, response.Volume)

AudioManager (BaseService)                     ← 预留的总管，当前提供音量控制
  ├── SetChannelVolume(channel, volume)
  ├── GetChannelVolume(channel)
  ├── MuteChannel(channel)
  └── SetMasterVolume(volume)
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|------|-----------|------|
| AudioRequest, AudioResponse | 02-character CharacterAudio | 发起播放请求并解析为响应 |
| AudioChannel | 02-character CharacterAudio | 消费方通过静态 Play 方法播放 |
| AudioSetSO | 02-character CharacterAudioConfigSO | CharacterAudioConfigSO 继承 AudioSetSO |
| AudioManager | 01-core BaseService | 注册为 Service，提供音量控制 |
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
| [audiomanager.md](audiomanager.md) | 音频总管 — 音量分组控制，预留 AudioSource 池 |
| [audiosetso.md](audiosetso.md) | 音频集基类 — 抽象 SO，子模块继承扩展 |
| [audiochannel.md](audiochannel.md) | 音频通道 — 静态 Play 方法，AudioSource / 3D 播放 |
| [audiorequest.md](audiorequest.md) | 播放请求 struct — Key / Set / Channel |
| [audioresponse.md](audioresponse.md) | 播放结果 struct — Clip / Volume / Pitch |
