using Animancer;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Locomotion/Animancer/Alias String Profile")]
public class AnimancerStringProfile : ScriptableObject
{
    [Header("Clips")]
    public StringAsset idleL;
    public StringAsset idleR;
    public StringAsset idleToRun180L;
    public StringAsset idleToRun180R;
    public StringAsset walkMixer;
    public StringAsset runMixer;
    public StringAsset runUp;
    public StringAsset runDown;
    public StringAsset sprint;
    public StringAsset walkForward;
    public StringAsset walkLeft;
    public StringAsset walkRight;
    public StringAsset walkBackward;
    public StringAsset turnInWalk180L;
    public StringAsset turnInWalk180R;
    public StringAsset turnInRun180L;
    public StringAsset turnInRun180R;
    public StringAsset turnInSprint180L;
    public StringAsset turnInSprint180R;
    public StringAsset turnInPlace90L;
    public StringAsset turnInPlace90R; 
    public StringAsset turnInPlace180L;
    public StringAsset turnInPlace180R;
    public StringAsset lookMixer;
    public StringAsset lookUp;
    public StringAsset lookDown;
    public StringAsset lookLeft;
    public StringAsset lookRight;

    [Header("Parameters")]
    public StringAsset HeadLookX;
    public StringAsset HeadLookY;
    public StringAsset VelocityX;
    public StringAsset VelocityY;
}
