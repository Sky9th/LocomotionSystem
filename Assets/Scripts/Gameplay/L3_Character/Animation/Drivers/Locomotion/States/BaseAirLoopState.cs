using UnityEngine;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseAirLoopState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseAirLoopState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
        {
            get
            {
                var contact = Owner.Ctx.Kinematic.GroundContact;
                var minFall = Owner.AnimProfile?.landMinFallDistance ?? 0.2f;
                return !contact.IsGrounded
                    && contact.DistanceToGround >= minFall;
            }
        }

        // TODO: 按 fall 落差选择 AirLight / AirHard（需新增阈值字段）
        public override void OnEnterState()
        {
            var air = Owner.AnimSet?.airLight;
            if (air == null || air.Animations.Length == 0)
            {
                Debug.LogWarning($"[BaseAirLoop] {Owner.AnimSet?.name}: airLight is empty, skipping to ground.");
            }
            else
            {
                Owner.Play(air);
            }
            Owner.AirborneStartY = Owner.Ctx.Kinematic.Position.y;
            Owner.MaxFallDistance = 0f;
            Owner.Rig?.SetSuppressGroundLock(true);
        }

        public override void Tick()
        {
            float fall = Owner.AirborneStartY - Owner.Ctx.Kinematic.Position.y;
            if (fall > Owner.MaxFallDistance) Owner.MaxFallDistance = fall;

            if (Owner.TrySetState(BaseStateKey.AirLand)) return;
            // 空动画无着陆过渡，直接落地
            if (!Owner.Ctx.Kinematic.GroundContact.IsGrounded) return;
            Owner.Rig?.SetSuppressGroundLock(false);
            Owner.ForceSetState(BaseStateKey.Idle);
        }
    }
}
