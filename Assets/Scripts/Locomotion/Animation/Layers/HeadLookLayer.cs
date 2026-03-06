using UnityEngine;
using Animancer;
using Game.Locomotion.Animation.Core;

namespace Game.Locomotion.Animation.Layers
{
    /// <summary>
    /// Animation layer that drives a head-look mixer based on the
    /// look direction stored in the locomotion snapshot.
    ///
    /// It mirrors the behaviour of the legacy LocomotionAnimancerAdapter
    /// head look implementation, but uses the shared animation context.
    /// </summary>
    internal sealed class HeadLookLayer : ILocomotionAnimationLayer
    {
        private const string HeadLayerName = "HeadLook";
        public int LayerIndex => 1;
        public AnimancerLayer Layer { get; set; }

        private bool mixerInitialized;
        private float smoothedYaw;
        private float smoothedPitch;

        private SLocomotionAnimationLayerSnapshot lastSnapshot;

        public string LayerName => HeadLayerName;

        public SLocomotionAnimationLayerSnapshot AnimationSnapshot => lastSnapshot;

        public HeadLookLayer(AnimancerLayer layer)
        {
            Layer = layer;
        }

        public void Update(in LocomotionAnimationContext context)
        {
            NamedAnimancerComponent animancer = context.Animancer;
            var alias = context.Alias;
            var profile = context.Profile;

            if (animancer == null || alias == null || profile == null)
            {
                return;
            }

            // Ensure we have at least two layers: base (0) + head (1).
            if (animancer.Layers.Count < 2)
            {
                return;
            }

            // Head look uses a Vector2 mixer where X = yaw, Y = pitch.
            var state = Layer.TryPlay(alias.lookMixer) as Vector2MixerState;
            if (state == null)
            {
                return;
            }

            Vector2 look = context.Snapshot.Motor.LookDirection;

            // Prefer locomotion capability limits so animation mapping
            // stays consistent with simulation. Fall back to small
            // non-zero defaults to avoid divide-by-zero.
            float maxYaw = 1e-3f;
            float maxPitch = 1e-3f;

            if (context.LocomotionProfile != null)
            {
                maxYaw = Mathf.Max(maxYaw, context.LocomotionProfile.maxHeadYawDegrees);
                maxPitch = Mathf.Max(maxPitch, context.LocomotionProfile.maxHeadPitchDegrees);
            }

            float targetYaw = Mathf.Clamp(look.x / maxYaw, -1f, 1f);
            float targetPitch = Mathf.Clamp(look.y / maxPitch, -1f, 1f);

            float smoothing = Mathf.Max(0f, profile.headLookSmoothingSpeed);
            float step = smoothing * context.DeltaTime;

            smoothedYaw = Mathf.MoveTowards(smoothedYaw, targetYaw, step);
            smoothedPitch = Mathf.MoveTowards(smoothedPitch, targetPitch, step);

            if (!mixerInitialized)
            {
                for (int i = 0; i < state.ChildCount; i++)
                {
                    var child = state.GetChild(i);
                    child.Speed = 0f;
                    child.Weight = 1f;
                    child.NormalizedTime = 1f;
                }

                mixerInitialized = true;
            }

            state.Parameter = new Vector2(smoothedYaw, smoothedPitch);

            // Head look currently does not distinguish turn animations so
            // IsTurnAnimation is always false and we don't track clip alias.
            lastSnapshot = new SLocomotionAnimationLayerSnapshot(
                layerName: HeadLayerName,
                alias: alias.lookMixer,
                normalizedTime: 0f);
        }
    }
}
