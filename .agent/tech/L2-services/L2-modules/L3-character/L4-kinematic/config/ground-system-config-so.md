# GroundSystemConfigSO

> L4 Kinematic / Config — 世界级地面系统参数，所有角色共享。

## 职责

定义地面探测、锁地、障碍物 LayerMask 等**与角色身份无关**的物理模拟参数。Player 和 NPC 共用同一份资产。

## 字段

| 分组 | 字段 | 默认值 | 说明 |
|------|------|--------|------|
| Ground Probe | `probeHeight` | 0.5 | 地面探测球体起点(脚底上方) |
| | `probeRadius` | 0.25 | 探测球体半径 |
| Ground Layer | `groundLayerMask` | ~0 | 地面层遮罩 |
| Obstacle Layer | `obstacleLayerMask` | ~0 | 障碍物层遮罩 |
| Ground Lock | `enableGroundLocking` | true | 是否开启锁地 |
| | `groundLockMaxDistance` | 0.15 | 锁地最大距离 |
| | `groundLockVerticalOffset` | 0 | 锁地垂直偏移 |
| Debounce | `groundReacquireDebounceDuration` | 0 | 离地后重新识别地面的防抖时间 |

## 消费者

- `CharacterKinematic` — 地面探测、障碍物检测、锁地逻辑
- `CharacterGroundDetection` — 接收 `probeHeight`/`probeRadius`/`groundLayerMask`
- `CharacterObstacleDetection` — 接收 `obstacleLayerMask`

## 来源

从 `KinematicProfileSO` 拆分而来——原 17 字段中 8 个系统级参数独立为此 SO。
