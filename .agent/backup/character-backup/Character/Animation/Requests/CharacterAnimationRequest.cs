using System;
using Animancer;
using UnityEngine;

namespace Game.Character.Animation.Requests
{
    public sealed class CharacterAnimationRequest
    {
        private CharacterAnimationRequest(
            string requestId,
            ECharacterAnimationSource source,
            ECharacterAnimationChannel channel,
            EAnimationInterruption priority,
            AnimationClip clip,
            StringAsset alias,
            float fadeDuration,
            float crossFadeDuration,
            Action onCompleted)
        {
            RequestId = requestId;
            Source = source;
            Channel = channel;
            Priority = priority;
            Clip = clip;
            Alias = alias;
            FadeDuration = fadeDuration;
            CrossFadeDuration = crossFadeDuration;
            OnCompleted = onCompleted;
        }

        public string RequestId { get; }
        public ECharacterAnimationSource Source { get; }
        public ECharacterAnimationChannel Channel { get; }
        public EAnimationInterruption Priority { get; }
        public AnimationClip Clip { get; }
        public StringAsset Alias { get; }
        public float FadeDuration { get; }
        public float CrossFadeDuration { get; }
        public Action OnCompleted { get; }

        public bool HasClip => Clip != null;
        public bool HasAlias => Alias != null;

        public static CharacterAnimationRequest CreateClip(
            string requestId,
            ECharacterAnimationSource source,
            AnimationClip clip,
            ECharacterAnimationChannel channel = ECharacterAnimationChannel.FullBody,
            EAnimationInterruption priority = EAnimationInterruption.Ability,
            float fadeDuration = 0.1f,
            float crossFadeDuration = 0.15f,
            Action onCompleted = null)
        {
            return new CharacterAnimationRequest(
                requestId,
                source,
                channel,
                priority,
                clip,
                alias: null,
                fadeDuration,
                crossFadeDuration,
                onCompleted);
        }

        public static CharacterAnimationRequest CreateAlias(
            string requestId,
            ECharacterAnimationSource source,
            StringAsset alias,
            ECharacterAnimationChannel channel = ECharacterAnimationChannel.FullBody,
            EAnimationInterruption priority = EAnimationInterruption.Ability,
            float fadeDuration = 0.1f,
            float crossFadeDuration = 0.15f,
            Action onCompleted = null)
        {
            return new CharacterAnimationRequest(
                requestId,
                source,
                channel,
                priority,
                clip: null,
                alias,
                fadeDuration,
                crossFadeDuration,
                onCompleted);
        }
    }
}