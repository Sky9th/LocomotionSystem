using UnityEngine;
using RedDust.Character;
using RedDust.Character.Director;
using RedDust.Character.Kinematic;

namespace RedDust.Character.Locomotion
{
    internal sealed class Motor
    {
        private Vector2 currentLocalVelocity;

        internal SCharacterMotor Evaluate(
            in SCharacterKinematic kin, in SCharacterIntent intent,
            LocomotionProfile profile, float dt)
        {
            var turnAngle = SignedAngle(kin.BodyForward, kin.LocomotionHeading);

            var speed = intent.HasMovement
                ? profile.GetSpeedForGait(intent.DesiredGait) * intent.MovementSpeedMultiplier
                : 0f;

            var desired = new Vector2(0f, speed);
            currentLocalVelocity = Smooth(currentLocalVelocity, desired, profile.acceleration, dt);
            var planar = ConvertToWorld(currentLocalVelocity, kin.LocomotionHeading);
            return new SCharacterMotor(desired, currentLocalVelocity, planar, turnAngle);
        }

        private static Vector2 Smooth(Vector2 cur, Vector2 des, float accel, float dt)
        {
            if (accel <= 0f || dt <= 0f) return des;
            return Vector2.MoveTowards(cur, des, accel * dt);
        }

        private static Vector3 ConvertToWorld(Vector2 local, Vector3 heading)
        {
            var fwd = heading;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < Mathf.Epsilon) fwd = Vector3.forward;
            fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd).normalized;
            return right * local.x + fwd * local.y;
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
