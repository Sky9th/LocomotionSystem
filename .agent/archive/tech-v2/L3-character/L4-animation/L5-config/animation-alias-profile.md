# AnimationAliasProfile · 动画别名配置

> `Character/Animation/Config/AnimationAliasProfile.cs` — ScriptableObject，Animancer StringAsset 别名映射

## 调用链

```
被谁调:
  AnimationBrain.Awake()        → 读取 lookMixer
  LocomotionDriver.OnEnable()   → 传给 BaseLayer
  BaseLayer → FSM States        → 通过 Owner.Alias 读取具体别名
  TraversalDriver.Evaluate()    → 读取 ClimbUp 别名
```

## 公开属性

```csharp
[Header("Clips")]
public StringAsset idleL;                    // 待机左
public StringAsset idleR;                    // 待机右
public StringAsset idleToRun180L;            // 待机→跑步 左转 180
public StringAsset idleToRun180R;            // 待机→跑步 右转 180
public StringAsset walkMixer;                // 行走 Mixer
public StringAsset runMixer;                 // 跑步 Mixer
public StringAsset sprint;                   // 冲刺
public StringAsset walkForward;              // 行走前进
public StringAsset walkLeft;                 // 行走左移
public StringAsset walkRight;                // 行走右移
public StringAsset walkBackward;             // 行走后退
public StringAsset turnInWalk180L;           // 行走中左转 180
public StringAsset turnInWalk180R;           // 行走中右转 180
public StringAsset turnInRun180L;            // 跑步中左转 180
public StringAsset turnInRun180R;            // 跑步中右转 180
public StringAsset turnInSprint180L;         // 冲刺中左转 180
public StringAsset turnInSprint180R;         // 冲刺中右转 180
public StringAsset turnInPlace90L;           // 原地左转 90
public StringAsset turnInPlace90R;           // 原地右转 90
public StringAsset turnInPlace180L;          // 原地左转 180
public StringAsset turnInPlace180R;          // 原地右转 180
public StringAsset lookMixer;                // 头部注视 Mixer
public StringAsset lookUp;                   // 抬头
public StringAsset lookDown;                 // 低头
public StringAsset lookLeft;                 // 左看
public StringAsset lookRight;                // 右看
public StringAsset ClimbUp1meter;            // 攀爬 1 米
public StringAsset ClimbUp2meter;            // 攀爬 2 米
public StringAsset ClimbUpHalfMeter;         // 攀爬 0.5 米
public StringAsset ClimbDown1meter;          // 爬下 1 米
public StringAsset ClimbDown2meter;          // 爬下 2 米
public StringAsset LandLight;                // 轻落地
public StringAsset LandMedium;               // 中落地
public StringAsset LandHard;                 // 重落地
public StringAsset LandFromWall;             // 墙壁落地
public StringAsset AirLoop;                  // 空中循环
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AnimationBrain | 读取 lookMixer |
| 被依赖 | LocomotionDriver | 传 BaseLayer 作动画别名 |
| 被依赖 | TraversalDriver | 读取 ClimbUp 别名 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Vault/StepOver 动画别名 | 待做 | 代码预留 |
| Crawl 动画别名 | 待做 | 枚举已定义 |
