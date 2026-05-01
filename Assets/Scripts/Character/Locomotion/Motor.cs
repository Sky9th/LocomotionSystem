using UnityEngine;
using Game.Character.Input;

namespace Game.Character.Locomotion
{
    internal sealed class Motor
    {
        private Vector2 currentLocalVelocity;

        internal SCharacterMotor Evaluate(
            in SCharacterKinematic kin, in SCharacterInputActions inp,
            LocomotionProfile profile, float dt)
        {
            var move = inp.MoveAction.HasInput ? inp.MoveAction : inp.LastMoveAction;
            var desired = ComputeDesired(move, profile.moveSpeed);
            currentLocalVelocity = Smooth(currentLocalVelocity, desired, profile.acceleration, dt);
            var planar = ConvertToWorld(currentLocalVelocity, kin.LocomotionHeading);
            var turnAngle = SignedAngle(kin.BodyForward, kin.LocomotionHeading);
            return new SCharacterMotor(desired, currentLocalVelocity, planar, turnAngle);
        }

        // ── Static Helpers ──

        private static Vector2 ComputeDesired(SMoveIAction action, float speed)
        {
            if (!action.HasInput || speed <= 0f) return Vector2.zero;
            var input = action.RawInput;
            var intensity = Mathf.Clamp01(input.magnitude);
            if (input.sqrMagnitude > Mathf.Epsilon) input = input.normalized;
            return input * (intensity * speed);
        }

        private static Vector2 Smooth(Vector2 cur, Vector2 des, float accel, float dt)
        {
            if (accel <= 0f || dt <= 0f) return des;
            return Vector2.MoveTowards(cur, des, accel * dt);
        }

        private static Vector3 ConvertToWorld(Vector2 local, Vector3 heading)
        {
            var f = heading; f.y = 0f;
            if (f.sqrMagnitude <= Mathf.Epsilon) f = Vector3.forward;
            f.Normalize();
            var r = Vector3.Cross(Vector3.up, f);
            return f * local.y + r * local.x;
        }

        private static float SignedAngle(Vector3 body, Vector3 heading)
        {
            var b = body; b.y = 0f;
            var h = heading; h.y = 0f;
            if (b.sqrMagnitude <= Mathf.Epsilon || h.sqrMagnitude <= Mathf.Epsilon) return 0f;
            b.Normalize(); h.Normalize();
            return Mathf.Clamp(Vector3.SignedAngle(b, h, Vector3.up), -180f, 180f);
        }
    }
}
