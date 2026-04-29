# 数据结构

```
SCharacterSnapshot                          ← 对外唯一快照 (替代当前 SLocomotion)
│
├── Kinematic: SCharacterKinematic          ← Character 层, 始终有效
│   ├── Position                  Vector3
│   ├── BodyForward               Vector3
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
├── Motor: SLocomotionMotor                  ← Locomotion 仿真: 速度/Heading/Turn
│   ├── DesiredLocalVelocity      Vector2
│   ├── DesiredPlanarVelocity     Vector3
│   ├── ActualLocalVelocity       Vector2
│   ├── ActualPlanarVelocity      Vector3
│   ├── ActualSpeed               float
│   ├── LocomotionHeading         Vector3
│   ├── TurnAngle                 float
│   └── IsLeftFootOnFront         bool
│
├── DiscreteState: SLocomotionDiscrete       ← Locomotion 仿真: 状态
│   ├── Phase           ELocomotionPhase       GroundedIdle / Moving / Airborne / Landing
│   ├── Posture         EPosture               Standing / Crouching / Prone
│   ├── Gait            EMovementGait          Idle / Walk / Run / Sprint
│   ├── Condition       ELocomotionCondition   Normal / InjuredLight / InjuredHeavy
│   └── IsTurning       bool
│
└── Traversal: SLocomotionTraversal           ← Locomotion 仿真: 穿越
    ├── Type             ELocomotionTraversalType   None / Climb / Vault / StepOver
    ├── Stage            ELocomotionTraversalStage  Idle / Requested / Committed / Completed / Canceled
    ├── ObstacleHeight   float
    ├── ObstaclePoint    Vector3
    ├── TargetPoint      Vector3
    └── FacingDirection  Vector3


[内部] CharacterFrameContext                    ← CharacterActor 同帧内逐级传递
│
├── Input        SCharacterInputActions         ← Step 1: ReadActions
│   ├── MoveAction             SMoveIAction
│   ├── LastMoveAction         SMoveIAction
│   ├── LookAction             SLookIAction
│   ├── CrouchAction           SCrouchIAction
│   ├── ProneAction            SProneIAction
│   ├── WalkAction             SWalkIAction
│   ├── RunAction              SRunIAction
│   ├── SprintAction           SSprintIAction
│   ├── JumpAction             SJumpIAction
│   └── StandAction            SStandIAction
│
├── Kinematic    SCharacterKinematic            ← Step 3: Evaluate (内容同上)
├── Motor        SLocomotionMotor               ← Step 4-5: Simulate (内容同上)
├── Discrete     SLocomotionDiscrete
└── Traversal    SLocomotionTraversal
```
