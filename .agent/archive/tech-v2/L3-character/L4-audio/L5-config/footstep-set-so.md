# FootstepSetSO · 脚步音效集

> `Character/Audio/Config/FootstepSetSO.cs` — ScriptableObject，不同材质脚步音效 Clip 配置

## 调用链

```
被谁调:
  CharacterAudio.TryResolve() → 读取 clip/baseVolume/pitchVariation
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterAudio | 脚步音效读取 |
| 继承 | AudioSetSO | 音频集基类 |

## 公开属性

```csharp
public AudioClip clip;                        // 脚步音效 Clip
[Range(0, 1)] public float baseVolume = 0.8f; // 基础音量
[Range(0, 0.5f)] public float pitchVariation;  // 音调随机变化范围
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 多材质脚步（草地/沙地/木板/金属等不同 Clip 集合） | 待做 | 代码目前仅单 Clip |
