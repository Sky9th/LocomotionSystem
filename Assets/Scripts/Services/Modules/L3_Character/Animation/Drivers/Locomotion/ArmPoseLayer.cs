using Animancer;
using RedDust.Character;
using RedDust.Character.Locomotion;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    /// <summary>
    /// Arm 层武器姿态管理。仅在 LocomotionDriver 活跃时工作。
    /// Partial grip 移动时 Arm 叠 CombatSet idle 补武器 pose；
    /// Full grip / 静止 / 非活跃时 Arm 淡出。
    /// </summary>
    internal sealed class ArmPoseLayer
    {
        private readonly AnimancerLayer _layer;
        private readonly CharacterBuildContext _buildCtx;
        private LocomotionAnimationSetSO _lastAnimSet;
        private bool _lastWasIdle;

        internal ArmPoseLayer(AnimancerLayer layer, CharacterBuildContext buildCtx)
        {
            _layer = layer;
            _buildCtx = buildCtx;
        }

        internal void Update(in CharacterFrameContext ctx)
        {
            if (_layer == null) return;
            var animSet = _buildCtx?.ResolvedLocoAnimSet;
            bool isIdle = ctx.Discrete.Gait == EMovementGait.Idle;

            if (animSet == _lastAnimSet && isIdle == _lastWasIdle) return;
            _lastAnimSet = animSet;
            _lastWasIdle = isIdle;

            if (animSet == null || animSet.HasFullLocomotion || isIdle)
            {
                _layer.StartFade(0, 0.25f);
            }
            else
            {
                var idle = animSet.idleL;
                if (idle != null)
                    _layer.Play(idle);
                else
                    _layer.StartFade(0, 0.25f);
            }
        }

        /// <summary>LocomotionDriver 被抢占时立即淡出 Arm。</summary>
        internal void FadeOut()
        {
            _layer?.StartFade(0, 0.25f);
        }

        /// <summary>LocomotionDriver 恢复时强制下一帧重新评估。</summary>
        internal void Invalidate()
        {
            _lastAnimSet = null;
        }
    }
}
