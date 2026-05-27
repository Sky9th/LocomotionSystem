# CharacterAudio · 角色音频

> `Character/Audio/CharacterAudio.cs` — MonoBehaviour，脚步触发音效

## 调用链

```
被谁调:
  Unity 生命周期 → Start()

调谁:
  LocomotionDriver.BaseLayer.FootstepCallback = OnFootstep  → 注册脚步回调
  OnFootstep → AudioRequest + AudioChannel.Play()           → 播放音效
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | LocomotionDriver | 注册脚步事件回调 |
| 依赖 | CharacterAudioConfigSO | 音频配置（FootstepSet） |
| 依赖 | AudioChannel | 音频播放通道 |
| 依赖 | AudioRequest/AudioResponse | 播放请求/响应 |

## 方法

### Start()
```csharp
private void Start()
```
- **用途**: 查找 LocomotionDriver 并注册脚步事件回调
- **调用者**: Unity 生命周期

### OnFootstep()
```csharp
private void OnFootstep()
```
- **用途**: 脚步事件触发时播放音效
- **调用者**: BaseLayer.InjectFootstepEvents() 注入的 Animancer 事件

### TryResolve()
```csharp
private static bool TryResolve(in AudioRequest request, out AudioResponse response)
```
- **用途**: 解析音频请求 — 从 FootstepSetSO 中读取 clip/volume/pitch
- **调用者**: OnFootstep
- **备注**: pitch 在 baseVolume 基础上加上随机变化

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 受击音效 | 待做 | 代码 TODO |
| 死亡音效 | 待做 | 代码 TODO |
| 呼吸音效 | 待做 | 代码预留 |
