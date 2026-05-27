# CharacterAudioConfigSO · 角色音频配置

> `Character/Audio/Config/CharacterAudioConfigSO.cs` — ScriptableObject，角色音频集配置

## 调用链

```
被谁调:
  CharacterAudio → 读取 config.footsteps
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterAudio | 音频配置引用 |
| 依赖 | FootstepSetSO | 脚步音效集 |

## 公开属性

```csharp
public FootstepSetSO footsteps;   // 脚步音效集
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| HitReactSetSO / DeathSetSO / BreathSetSO | 待做 | 代码 TODO/预留 |
