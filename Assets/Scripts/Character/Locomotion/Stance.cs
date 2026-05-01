using UnityEngine;
using Game.Character.Input;

namespace Game.Character.Locomotion
{
    internal sealed class Stance
    {
        private EMovementGait currentGait = EMovementGait.Idle;
        private EPosture currentPosture = EPosture.Standing;
        private bool isTurning;
        private float lastDesiredYaw;
        private float lookStabilityTimer;

        internal SCharacterDiscrete Evaluate(
            in SCharacterMotor motor, in SCharacterKinematic kin,
            in SCharacterInputActions inp, LocomotionProfile profile, float dt)
        {
            var phase = EvaluatePhase(in kin, in motor);
            var gait = EvaluateGait(in inp, profile);
            var posture = EvaluatePosture(in inp, profile);
            var turning = EvaluateTurning(in motor, in kin, profile, dt, phase);
            return new SCharacterDiscrete(phase, posture, gait, turning);
        }

        // ── Phase ──

        private static ELocomotionPhase EvaluatePhase(in SCharacterKinematic kin, in SCharacterMotor motor)
        {
            if (!kin.GroundContact.IsGrounded) return ELocomotionPhase.Airborne;
            var v = motor.ActualPlanarVelocity; v.y = 0f;
            return v.sqrMagnitude <= Vector3.kEpsilon
                ? ELocomotionPhase.GroundedIdle : ELocomotionPhase.GroundedMoving;
        }

        // ── Gait ──

        private EMovementGait EvaluateGait(in SCharacterInputActions inp, LocomotionProfile profile)
        {
            if (!inp.MoveAction.HasInput)
            { currentGait = EMovementGait.Idle; return currentGait; }

            var g = currentGait == EMovementGait.Idle ? EMovementGait.Run : currentGait;
            if (inp.SprintAction.Button.IsRequested && profile.canSprint)
                g = g == EMovementGait.Sprint ? EMovementGait.Run : EMovementGait.Sprint;
            currentGait = g;
            return g;
        }

        // ── Posture ──

        private EPosture EvaluatePosture(in SCharacterInputActions inp, LocomotionProfile profile)
        {
            if (inp.StandAction.Button.IsRequested)
            { currentPosture = EPosture.Standing; return currentPosture; }
            if (inp.ProneAction.Button.IsRequested && profile.canProne)
            { currentPosture = EPosture.Prone; return currentPosture; }
            if (inp.CrouchAction.Button.IsRequested && profile.canCrouch)
            { currentPosture = EPosture.Crouching; return currentPosture; }
            return currentPosture;
        }

        // ── Turning ──

        private bool EvaluateTurning(in SCharacterMotor motor, in SCharacterKinematic kin,
            LocomotionProfile profile, float dt, ELocomotionPhase phase)
        {
            if (phase != ELocomotionPhase.GroundedIdle && phase != ELocomotionPhase.GroundedMoving)
            { isTurning = false; return false; }

            var yaw = Mathf.Atan2(kin.LocomotionHeading.x, kin.LocomotionHeading.z) * Mathf.Rad2Deg;
            var yawDelta = Mathf.Abs(Mathf.DeltaAngle(yaw, lastDesiredYaw));
            lastDesiredYaw = yaw;

            if (yawDelta <= profile.lookStabilityAngle) lookStabilityTimer += dt;
            else lookStabilityTimer = 0f;

            var absAngle = Mathf.Abs(motor.TurnAngle);
            var wantsTurn = absAngle >= profile.turnEnterAngle;
            var lookStable = lookStabilityTimer >= profile.lookStabilityDuration;
            var turnDone = absAngle <= profile.turnCompletionAngle;

            if (!isTurning && wantsTurn && lookStable) isTurning = true;
            else if (isTurning && turnDone) isTurning = false;

            return isTurning;
        }
    }
}
