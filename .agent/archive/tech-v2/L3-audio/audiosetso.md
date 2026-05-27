# AudioSetSO · 音频集基类

> `Audio/AudioSetSO.cs` — 抽象 ScriptableObject，供子模块继承扩展不同种类的音频集

## 调用链

```
被谁调:
  TryResolve(request, out response)   ← 将 AudioSetSO 向下转型为具体子类
  子类 (CharacterAudioConfigSO)       ← 继承并添加具体字段

调谁: (无 — 抽象基类)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AudioRequest | 请求中持有 AudioSetSO 引用 |
| 被依赖 | 02-character CharacterAudioConfigSO | 继承自 AudioSetSO |
| 被依赖 | 02-character FootstepSetSO | 继承自 AudioSetSO |

## 公开属性

无。

```csharp
public abstract class AudioSetSO : ScriptableObject { }
```

## 方法

无。

## 使用规则

- 抽象类，不能直接实例化
- 子模块继承此类并添加具体字段（如 FootstepSetSO 的 clip/baseVolume/pitchVariation）
- TryResolve 中使用 `is` / `as` 向下转型到具体子类

## 继承体系

```
AudioSetSO (abstract)
├── CharacterAudioConfigSO   { FootstepSetSO footsteps }
│   └── FootstepSetSO        { AudioClip clip, float baseVolume, float pitchVariation }
├── WeaponAudioSetSO (预留)
└── ZombieAudioConfigSO (预留)
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| WeaponAudioSetSO 实现 | 远期 | 设计文档 audio-system.md |
| ZombieAudioConfigSO 实现 | 远期 | 设计文档 audio-system.md |
