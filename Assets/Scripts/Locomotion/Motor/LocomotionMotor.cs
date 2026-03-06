using UnityEngine;
using Game.Locomotion.Computation;
using Game.Locomotion.Config;
using Game.Locomotion.Input;
using Game.Locomotion.Discrete.Structs;

namespace Game.Locomotion.Agent
{
    /// <summary>
    /// Motor module for <see cref="LocomotionAgent"/>.
    ///
    /// Owns per-agent kinematic state (smoothed planar velocity) and applies
    /// Transform-level corrections such as root-motion alignment and
    /// ground locking.
    /// </summary>
    internal sealed class LocomotionMotor
    {
        private Vector3 currentVelocity;
        private Vector2 currentLocalVelocity;

        internal Vector3 CurrentVelocity => currentVelocity;
        internal Vector2 CurrentLocalVelocity => currentLocalVelocity;

        internal LocomotionMotor()
        {
            Reset();
        }

        internal void Reset()
        {
            currentVelocity = Vector3.zero;
            currentLocalVelocity = Vector2.zero;
        }

        internal void UpdateKinematics(
            in SLocomotionInputActions inputActions,
            LocomotionProfile profile,
            Vector3 locomotionHeading,
            float deltaTime,
            out Vector2 desiredLocalVelocity,
            out Vector3 desiredWorldVelocity)
        {
            float moveSpeed = profile != null ? profile.moveSpeed : 0f;
            SMoveIAction moveAction = inputActions.MoveAction.Equals(SMoveIAction.None)
                ? inputActions.LastMoveAction
                : inputActions.MoveAction;

            desiredLocalVelocity = LocomotionKinematics.ComputeDesiredPlanarVelocity(moveAction, moveSpeed);

            float acceleration = profile != null ? profile.acceleration : 0f;
            currentLocalVelocity = LocomotionKinematics.SmoothVelocity(
                currentLocalVelocity,
                desiredLocalVelocity,
                acceleration,
                deltaTime);

            desiredWorldVelocity = LocomotionKinematics.ConvertLocalToWorldPlanarVelocity(
                desiredLocalVelocity,
                locomotionHeading);

            currentVelocity = LocomotionKinematics.ConvertLocalToWorldPlanarVelocity(
                currentLocalVelocity,
                locomotionHeading);
        }

        internal SLocomotionMotor Evaluate(
            Transform agentTransform,
            Transform followAnchor,
            Transform modelRoot,
            LocomotionProfile profile,
            in SLocomotionInputActions inputActions,
            float deltaTime)
        {
            // 1) Apply look input to rotate the follow anchor so that
            // heading and head-look evaluation are based on updated camera orientation.
            LocomotionCameraAnchor.UpdateRotation(
                followAnchor,
                inputActions.LookAction,
                profile);

            Vector3 position = agentTransform.position;
            Vector3 bodyForward = agentTransform.forward;
            Vector3 locomotionHeading = LocomotionHeading.Evaluate(followAnchor, agentTransform);

            float maxSlopeAngle = profile != null ? profile.maxGroundSlopeAngle : 0f;
            float groundRayLength = profile != null ? profile.groundRayLength : 0f;
            LayerMask groundLayerMask = profile != null ? profile.groundLayerMask : ~0;
            SGroundContact groundContact = LocomotionGroundDetection.SampleGround(
                position,
                groundRayLength,
                groundLayerMask,
                maxSlopeAngle);

            // 2) Kinematics: desired + smoothed velocities.
            UpdateKinematics(
                in inputActions,
                profile,
                locomotionHeading,
                deltaTime,
                out Vector2 desiredLocalVelocity,
                out Vector3 desiredVelocity);

            // 3) Derived probes.
            float turnAngle = LocomotionKinematics.ComputeSignedPlanarTurnAngle(bodyForward, locomotionHeading);

            Vector2 lookDirection = LocomotionHeadLook.Evaluate(
                followAnchor,
                modelRoot,
                agentTransform,
                profile);

            return new SLocomotionMotor(
                position,
                desiredLocalVelocity,
                desiredVelocity,
                currentLocalVelocity,
                currentVelocity,
                currentVelocity.magnitude,
                locomotionHeading,
                bodyForward,
                lookDirection,
                groundContact,
                turnAngle,
                isLeftFootOnFront: false);
        }

    }
}
