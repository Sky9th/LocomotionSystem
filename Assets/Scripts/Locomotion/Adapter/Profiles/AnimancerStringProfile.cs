using System;
using System.Collections;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Locomotion/Animancer/Alias String Profile")]
public class AnimancerStringProfile : ScriptableObject
{
    [Header("Clips")]
    public StringAsset idleL;
    public StringAsset idleR;
    public StringAsset walkMixer;
    public StringAsset walkForward;
    public StringAsset walkLeft;
    public StringAsset walkRight;
    public StringAsset walkBackward;
    public StringAsset turnInWalk180L;
    public StringAsset turnInWalk180R;
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
