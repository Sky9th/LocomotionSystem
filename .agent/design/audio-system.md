# 音效系统设计

> 日期: 2026-05-09
> 状态: Phase 1.5 已实现（Animancer 事件 → BaseLayer 回调 → CharacterAudio）

## 职责边界

- **子模块** → WHAT + WHEN：持有 AudioSetSO，在适宜时机触发声音
- **音效系统** → HOW：音量/变调/通道；不关心业务语义

## 数据流（脚步）

```
BaseLayer.Play(mixer)
  → InjectFootstepEvents()
    → child.Events.Add(0.12f, callback)
       callback = baseLayer.FootstepCallback?.Invoke()

CharacterAudio.Start()
  → locoDriver.BaseLayer.FootstepCallback = OnFootstep

CharacterAudio.OnFootstep()
  → AudioRequest("Foot", set, SFX)
  → TryResolve → AudioResponse
  → AudioChannel.Play(response, source)
```

## 关键设计点

| 决策 | 说明 |
|------|------|
| 回调代替事件链 | `FootstepCallback` 一个委托，Animancer 事件直调，不绕多层转发 |
| 去重注入 | `injectedMixer` 记下上次的 mixer，避免 Play() 重复调用累积重复事件 |
| Timing | Start() 中设回调，确保 LocomotionDriver.OnEnable 已完成 baseLayer 初始化 |

## 契约

```csharp
AudioRequest  { Key, AudioSetSO Set, AudioChannelType Channel }
AudioResponse { AudioClip Clip, float Volume, float Pitch }
AudioChannel.Play(in AudioResponse, AudioSource)   // static pure function
```

## AudioSetSO 继承

```
AudioSetSO (abstract)
├── CharacterAudioConfigSO        { FootstepSetSO footsteps }
│   └── FootstepSetSO             { AudioClip clip, float baseVolume, pitchVariation }
├── WeaponAudioSetSO (预留)
└── ZombieAudioConfigSO (预留)
```

## 文件清单

```
Assets/Scripts/Audio/
  AudioSetSO.cs          abstract base
  AudioRequest.cs        契约 Request
  AudioResponse.cs       契约 Response
  AudioChannel.cs        static Play()
  AudioManager.cs        BaseService（预留）

Assets/Scripts/Character/Audio/
  CharacterAudio.cs      MB, Start中设 FootstepCallback
  Config/
    CharacterAudioConfigSO.cs
    FootstepSetSO.cs
```

## 技术债

| 项目 | 现状 | 改进方向 |
|------|------|---------|
| `InjectFootstepEvents` | `Play()` 时遍历 mixer children 注入 Animancer 事件 | 改用 TransitionAsset `_Events` 预配；或收敛到 AnimationBrain 统一管理 |
| 仅 Walk/Run Mixer 注入 | 当前对所有 mixer 注入 | 按动画类型过滤（Idle 不注入） |
| `FootstepCallback` 散落 | 挂在 BaseLayer 上 | 未来可能收敛到 AnimationBrain |
