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
    public StringAsset idle;
    public StringAsset walkMixer;
    public StringAsset walkForward;
    public StringAsset walkLeft;
    public StringAsset walkRight;
    public StringAsset walkBackward;
    public StringAsset turnLeft90;
    public StringAsset turnRight90; 
    public StringAsset turnLeft180;
    public StringAsset turnRight180;
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
