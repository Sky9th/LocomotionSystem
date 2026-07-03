using UnityEngine;
using RedDust.Character;
using RedDust.Character.Kinematic;
using RedDust.Character.Pathfinding;

namespace RedDust.Character.Locomotion
{
    internal sealed class Motor
    {
        private Vector2 currentLocalVelocity;

        internal SCharacterMotor Evaluate(
            in SCharacterKinematic kin, PathfindingAgent pf,
            float desiredSpeed, float acceleration, float dt)
        {
            var turnAngle = SignedAngle(kin.BodyForward, kin.LocomotionHeading);

            if (pf != null && pf.HasActivePath)
            {
                var externalVel = pf.DesiredVelocity;
                externalVel.y = 0f;
                var localVel = ConvertToLocal(externalVel, kin.LocomotionHeading);
                currentLocalVelocity = localVel;
                return new SCharacterMotor(localVel, localVel, externalVel, turnAngle);
            }

            var desired = new Vector2(0f, desiredSpeed);
            currentLocalVelocity = Smooth(currentLocalVelocity, desired, acceleration, dt);
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

        private static Vector2 ConvertToLocal(Vector3 world, Vector3 heading)
        {
            var fwd = heading;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < Mathf.Epsilon) fwd = Vector3.forward;
            fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd).normalized;
            return new Vector2(Vector3.Dot(world, right), Vector3.Dot(world, fwd));
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
