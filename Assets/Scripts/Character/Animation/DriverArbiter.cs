using System.Collections.Generic;
using UnityEngine;
using Animancer;
using Game.Character.Animation.Drivers;
using Game.Character.Animation.Requests;

namespace Game.Character.Animation
{
    internal sealed class DriverArbiter
    {
        private readonly AnimancerLayer layer;

        private readonly List<ICharacterAnimationDriver> drivers = new();
        private readonly List<(ICharacterAnimationDriver driver, AnimationRequest request)> queue = new();

        private ICharacterAnimationDriver defaultDriver;
        private AnimationRequest activeRequest;
        private ICharacterAnimationDriver activeDriver;
        private bool activeCompleted;

        public AnimationRequest ActiveRequest => activeRequest;

        internal DriverArbiter(AnimancerLayer layer)
        {
            this.layer = layer;
        }

        // ── Driver 管理 ──

        public void RegisterDriver(ICharacterAnimationDriver driver)
        {
            if (driver == null || drivers.Contains(driver)) return;
            drivers.Add(driver);
            if (defaultDriver == null) defaultDriver = driver;
        }

        public void UnregisterDriver(ICharacterAnimationDriver driver)
        {
            drivers.Remove(driver);
            if (driver == activeDriver) { activeRequest = null; activeCompleted = true; }
            if (driver == defaultDriver) defaultDriver = drivers.Count > 0 ? drivers[0] : null;
        }

        // ── 请求提交 ──

        public void SubmitRequest(ICharacterAnimationDriver driver, AnimationRequest request)
        {
            if (request == null) return;
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].driver == driver)
                { queue[i] = (driver, request); return; }
            }
            queue.Add((driver, request));
        }

        public void Release(ICharacterAnimationDriver driver)
        {
            if (driver == activeDriver)
            {
                activeRequest = null;
                activeDriver = defaultDriver;
                defaultDriver?.OnResumed();
            }
        }

        // ── 每帧调度 ──

        public void Resolve(in SCharacterSnapshot snapshot, float dt)
        {
            ProcessQueue();
            CheckCompletion();
            activeDriver?.Drive(snapshot, dt);
            ActivateDefaultIfNeeded();
        }

        private void ProcessQueue()
        {
            if (queue.Count == 0) return;

            queue.Sort((a, b) => b.request.Resistance.CompareTo(a.request.Resistance));

            foreach (var (driver, request) in queue)
            {
                if (activeRequest == null)
                {
                    AcceptRequest(driver, request);
                }
                else if (request.Resistance >= activeRequest.Resistance && driver != activeDriver)
                {
                    activeDriver?.OnInterrupted(request);
                    AcceptRequest(driver, request);
                }
            }
            queue.Clear();
        }

        private void AcceptRequest(ICharacterAnimationDriver driver, AnimationRequest request)
        {
            if (activeDriver != null && activeDriver != driver)
                activeDriver.OnInterrupted(request);

            activeDriver = driver;
            activeRequest = request;
            activeCompleted = false;

            if (request.HasClip) layer.Play(request.Clip, request.FadeIn);
            else if (request.HasAlias) layer.TryPlay(request.Alias);
        }

        private void CheckCompletion()
        {
            if (activeRequest == null || activeCompleted) return;
            float t = layer.CurrentState?.NormalizedTime ?? 0f;
            if (t >= 0.99f)
            {
                if (activeRequest.OnComplete == OnCompleteBehavior.Resume)
                {
                    layer.Stop();
                    activeRequest = null;
                    activeDriver = defaultDriver;
                    defaultDriver?.OnResumed();
                }
                activeCompleted = true;
            }
        }

        private void ActivateDefaultIfNeeded()
        {
            if (activeRequest == null && activeDriver != defaultDriver)
            {
                activeDriver = defaultDriver;
                defaultDriver?.OnResumed();
            }
        }
    }
}
