using UnityEngine;
using Animancer;

using Game.Locomotion.Adapter;

namespace Game.Locomotion.Adapter
{
    public partial class LocomotionAnimancerAdapter : MonoBehaviour
    {
        private AnimancerLayer headLayer;
        private bool headLookMixerInitialized;

        private float smoothedYaw;
        private float smoothedPitch;

        private void UpdateLookDirection()
        {
        Vector2 headLook = agent.Snapshot.LookDirection;
        float maxYaw = 90f;
        float maxPitch = 90f;

        float normalizedYaw = Mathf.Clamp(headLook.x / maxYaw, -1f, 1f);
        float normalizedPitch = Mathf.Clamp(headLook.y / maxPitch, -1f, 1f);

        smoothedYaw = Mathf.MoveTowards(smoothedYaw, normalizedYaw, headYawSpeed * GameTime.Delta);
        smoothedPitch = Mathf.MoveTowards(smoothedPitch, normalizedPitch, headPitchSpeed * GameTime.Delta);

        var mixerState = (Vector2MixerState)headLayer.TryPlay(alias.lookMixer);

        if (!headLookMixerInitialized)
        {
            Debug.Log(mixerState.ChildCount);
            for (int i = 0; i < mixerState.ChildCount; i++)
            {
                var child = mixerState.GetChild(i);
                child.Speed = 0f;
                child.Weight = 1f;
                child.NormalizedTime = 1f;
            }

            headLookMixerInitialized = true;
        }

        mixerState.Parameter = new Vector2(smoothedYaw, smoothedPitch);
        }
    }
}
