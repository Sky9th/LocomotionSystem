# 数据结构

## SCharacterSnapshot（对外唯一快照）

```
SCharacterSnapshot
│
├── Kinematic: SCharacterKinematic          ← Character 层, 始终有效
│   ├── Position                  Vector3
│   ├── BodyForward               Vector3
│   ├── LocomotionHeading         Vector3    ← 运动方向 (相机→水平投影)
│   ├── LookDirection             Vector2
│   ├── GroundContact: SGroundContact
│   │   ├── IsGrounded            bool
│   │   ├── DistanceToGround      float
│   │   ├── IsWalkableSlope       bool
│   │   ├── ContactPoint          Vector3
│   │   ├── ContactNormal         Vector3
│   │   └── StateDuration         float
│   └── ForwardObstacleDetection: SForwardObstacleDetection
│       ├── HasHit / HasTopSurface    bool
│       ├── IsSlope / IsObstacle      bool
│       ├── CanClimb / CanVault / CanStepOver  bool
│       ├── Distance / ObstacleHeight  float
│       ├── Point / Normal / TopPoint / TopNormal / Direction  Vector3
│       ├── SurfaceAngle               float
│       └── Collider                   Collider
│
└── Locomotion: SLocomotionState             ← Locomotion 仿真
    ├── Motor: SCharacterMotor               ← 运动数据 (连续值)
    │   ├── DesiredLocalVelocity      Vector2
    │   ├── ActualLocalVelocity       Vector2
    │   ├── ActualPlanarVelocity      Vector3
    │   └── TurnAngle                 float
    │
    └── Discrete: SCharacterDiscrete         ← 状态标签 (离散值)
        ├── Phase           ELocomotionPhase
        ├── Posture         EPosture
        ├── Gait            EMovementGait
        └── IsTurning       bool


[内部] CharacterFrameContext                    ← CharacterActor 同帧内逐级传递

├── Input        SCharacterInputActions         ← Step 1: ReadActions
├── Kinematic    SCharacterKinematic            ← Step 3: Evaluate
├── Motor        SCharacterMotor                ← Step 4: Motor.Evaluate
└── Discrete     SCharacterDiscrete             ← Step 4: Stance.Evaluate
