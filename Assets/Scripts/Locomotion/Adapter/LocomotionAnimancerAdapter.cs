using UnityEngine;
using Animancer;
using Locomotion.Animation;
using Animancer.FSM;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

/// <summary>
/// Animancer-based presentation adapter for <see cref="LocomotionAgent"/>.
/// Consumes SPlayerLocomotion snapshots and a LocomotionAnimationProfile
/// to drive idle / move / turn-in-place / airborne animation states.
/// </summary>
[DisallowMultipleComponent]
public class LocomotionAnimancerAdapter : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private LocomotionAgent agent;
    [SerializeField] private LocomotionAdapter adapter;
    [SerializeField] private AnimancerComponent animancer;
    [SerializeField] private LocomotionAnimationProfile profile;

    [Header("Header Look")]
    [SerializeField] private AvatarMask headerMask;
    [SerializeField] private MixerTransition2D headerLookMixer;

    [Header("Head Look Smoothing")]
    [SerializeField] private float headYawSpeed = 5f;
    [SerializeField] private float headPitchSpeed = 5f;

    private AnimancerLayer baseLayer;
    private AnimancerLayer headLayer;
    private bool headLookMixerInitialized;

    private float smoothedYaw;
    private float smoothedPitch;

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponentInParent<LocomotionAgent>();
        }
        if (adapter == null)
        {
            adapter = GetComponentInParent<LocomotionAdapter>();
        }
        if (animancer == null)
        {
            animancer = GetComponentInChildren<AnimancerComponent>();
        }

        baseLayer = animancer.Layers[0];

        headLayer = animancer.Layers[1];

        headLayer.Mask = headerMask;

        if (headerLookMixer == null)
        {
            headerLookMixer = new MixerTransition2D();
        }

        headerLookMixer.Type = MixerTransition2D.MixerType.Directional;

        headerLookMixer.Animations = new Object[]
        {
            profile.lookUp.Clip,
            profile.lookDown.Clip,
            profile.lookLeft.Clip,
            profile.lookRight.Clip
        };

        headerLookMixer.Thresholds = new Vector2[]
        {
            new Vector2(0f,  -1f),
            new Vector2(0f, 1f),
            new Vector2(-1f, 0f),
            new Vector2( 1f, 0f)
        };

        headerLookMixer.DefaultParameter = Vector2.zero;
    }

    private void OnEnable()
    {
        animancer.Play(profile.idle);
    }

    void Update()
    {
        if (agent == null || adapter == null || animancer == null || profile == null)
        {
            return;
        }

        UpdateLookDirection();
    }

    void UpdateLookDirection()
    {
        Vector2 headLook = adapter.HeadLook;
        Debug.Log($"Head Look: {headLook}");
        float maxYaw = 90f;
        float maxPitch = 90f;

        float normalizedYaw = Mathf.Clamp(headLook.x / maxYaw, -1f, 1f);
        float normalizedPitch = Mathf.Clamp(headLook.y / maxPitch, -1f, 1f);

        smoothedYaw = Mathf.MoveTowards(smoothedYaw, normalizedYaw, headYawSpeed * Time.deltaTime);
        smoothedPitch = Mathf.MoveTowards(smoothedPitch, normalizedPitch, headPitchSpeed * Time.deltaTime);

        var mixerState = (Vector2MixerState)headLayer.Play(headerLookMixer);

        if (!headLookMixerInitialized)
        {
            for (int i = 0; i < mixerState.ChildCount; i++)
            {
                var child = mixerState.GetChild(i);
                child.Speed = 0f; 
            }

            headLookMixerInitialized = true;
        }

        float upAmount = Mathf.Clamp01(Mathf.Max(0f,  smoothedPitch));  
        float downAmount = Mathf.Clamp01(Mathf.Max(0f, -smoothedPitch)); 
        float leftAmount = Mathf.Clamp01(Mathf.Max(0f, -smoothedYaw));   
        float rightAmount = Mathf.Clamp01(Mathf.Max(0f,  smoothedYaw));  

        mixerState.GetChild(0).NormalizedTime = upAmount;
        mixerState.GetChild(1).NormalizedTime = downAmount;
        mixerState.GetChild(2).NormalizedTime = leftAmount;
        mixerState.GetChild(3).NormalizedTime = rightAmount;

        mixerState.Parameter = new Vector2(smoothedYaw, -smoothedPitch);
    }

}
