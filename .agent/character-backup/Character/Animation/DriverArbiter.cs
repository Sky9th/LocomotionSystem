using System;
using System.Collections.Generic;
using Animancer;
using Game.Character.Animation.Drivers;
using Game.Character.Animation.Requests;

namespace Game.Character.Animation
{
    internal sealed class DriverArbiter
    {
        private readonly ECharacterAnimationChannel channel;
        private readonly AnimancerLayer layer;

        private ICharacterAnimationDriver defaultDriver;
        private ICharacterAnimationDriver activeDriver;

        private CharacterAnimationRequest activeRequest;
        private ECharacterAnimationPlaybackState playbackState;

        private readonly ICharacterAnimationDriver[] driverBuffer = new ICharacterAnimationDriver[4];
        private int driverCount;

        public ECharacterAnimationChannel Channel => channel;
        public ICharacterAnimationDriver ActiveDriver => activeDriver;
        public ECharacterAnimationPlaybackState PlaybackState => playbackState;

        public DriverArbiter(ECharacterAnimationChannel channel, AnimancerLayer layer)
        {
            this.channel = channel;
            this.layer = layer;
            playbackState = ECharacterAnimationPlaybackState.None;
        }

        public void RegisterDriver(ICharacterAnimationDriver driver)
        {
            if (driver == null)
            {
                return;
            }

            if (driver.Channel != channel)
            {
                return;
            }

            if (driverCount >= driverBuffer.Length)
            {
                return;
            }

            driverBuffer[driverCount] = driver;
            driverCount++;

            Array.Sort(driverBuffer, 0, driverCount, DriverPriorityComparer.Instance);

            if (driver.Mode == ECharacterAnimationRequestMode.Continuous && defaultDriver == null)
            {
                defaultDriver = driver;
            }
        }

        public void Update(float deltaTime)
        {
            switch (playbackState)
            {
                case ECharacterAnimationPlaybackState.None:
                    EvaluatePending();
                    ActiveDriver?.Update(deltaTime);
                    break;

                case ECharacterAnimationPlaybackState.Playing:
                    EvaluatePlaying(deltaTime);
                    break;

                case ECharacterAnimationPlaybackState.Completed:
                case ECharacterAnimationPlaybackState.Rejected:
                case ECharacterAnimationPlaybackState.Interrupted:
                    TransitionToDefault();
                    break;
            }
        }

        private void EvaluatePending()
        {
            for (int i = driverCount - 1; i >= 0; i--)
            {
                ICharacterAnimationDriver driver = driverBuffer[i];
                if (driver == null || driver.Mode == ECharacterAnimationRequestMode.Continuous)
                {
                    continue;
                }

                CharacterAnimationRequest request = driver.BuildRequest();
                if (request == null)
                {
                    continue;
                }

                if (request.Channel != channel)
                {
                    continue;
                }

                AcceptRequest(driver, request);
                return;
            }

            if (defaultDriver != null)
            {
                ActivateDefault();
            }
        }

        private void EvaluatePlaying(float deltaTime)
        {
            if (activeRequest == null)
            {
                return;
            }

            for (int i = driverCount - 1; i >= 0; i--)
            {
                ICharacterAnimationDriver driver = driverBuffer[i];
                if (driver == null || driver == activeDriver)
                {
                    continue;
                }

                CharacterAnimationRequest request = driver.BuildRequest();
                if (request == null)
                {
                    continue;
                }

                if (request.Priority > activeRequest.Priority)
                {
                    InterruptActive();
                    AcceptRequest(driver, request);
                    return;
                }
            }

            if (layer.CurrentState != null && layer.CurrentState.NormalizedTime >= 0.99f)
            {
                CompleteActive();
            }
        }

        private void AcceptRequest(ICharacterAnimationDriver driver, CharacterAnimationRequest request)
        {
            if (activeDriver != null && activeDriver != driver)
            {
                InterruptActive();
            }

            activeDriver = driver;
            activeRequest = request;
            playbackState = ECharacterAnimationPlaybackState.Playing;

            if (request.HasClip)
            {
                layer.Play(request.Clip, request.FadeDuration);
            }
            else if (request.HasAlias)
            {
                layer.TryPlay(request.Alias);
            }
        }

        private void ActivateDefault()
        {
            if (defaultDriver == null)
            {
                return;
            }

            if (activeDriver != defaultDriver)
            {
                activeDriver = defaultDriver;
                defaultDriver.OnResumed();
            }

            playbackState = ECharacterAnimationPlaybackState.None;
            activeRequest = null;
        }

        private void InterruptActive()
        {
            if (activeDriver == null)
            {
                return;
            }

            activeDriver.OnInterrupted();
            playbackState = ECharacterAnimationPlaybackState.Interrupted;
            activeRequest = null;
        }

        private void CompleteActive()
        {
            activeRequest?.OnCompleted?.Invoke();
            playbackState = ECharacterAnimationPlaybackState.Completed;
            activeRequest = null;
        }

        private void TransitionToDefault()
        {
            playbackState = ECharacterAnimationPlaybackState.None;
            activeRequest = null;
            ActivateDefault();
        }

        private sealed class DriverPriorityComparer : IComparer<ICharacterAnimationDriver>
        {
            public static readonly DriverPriorityComparer Instance = new();

            public int Compare(ICharacterAnimationDriver x, ICharacterAnimationDriver y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return ((int)x.Priority).CompareTo((int)y.Priority);
            }
        }
    }
}