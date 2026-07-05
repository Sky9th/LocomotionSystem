using System.Collections.Generic;
using UnityEngine;
using Animancer;
using RedDust.Character.Animation.Drivers;
using RedDust.Character;
using RedDust.Character.Animation;

namespace RedDust.Character.Animation
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
        private bool _skipCompletionThisFrame;

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
            if (driver == activeDriver)
            {
                activeRequest = null;
                activeDriver = null;
                activeCompleted = true;
            }
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

        /// <summary>释放当前活跃 Driver，归还默认 LocomotionDriver。</summary>
        public void ReleaseActive()
        {
            if (activeRequest == null) return;
            activeDriver?.OnInterrupted(null);
            activeRequest = null;
            activeDriver = defaultDriver;
            defaultDriver?.OnResumed();
        }

        // ── 每帧调度 ──

        public void Resolve(in SCharacterFrameContext ctx, float dt)
        {
            EvaluateDrivers(ctx, dt);
            ProcessQueue();
            CheckCompletion();
            activeDriver?.Drive(ctx, dt);
            ActivateDefaultIfNeeded();
        }

        private void EvaluateDrivers(in SCharacterFrameContext ctx, float dt)
        {
            foreach (var driver in drivers)
                driver.Evaluate(ctx, dt);
        }

        private void ProcessQueue()
        {
            if (queue.Count == 0) return;

            var (driver, request) = queue[0];

            if (activeRequest == null)
            {
                // H1: idle — 接受任意 Driver（Locomotion 被抢占 → 技能或受击）
                if (activeDriver != null && activeDriver != driver)
                    activeDriver.OnInterrupted(request);
                AcceptRequest(driver, request);
            }
            else if (request.DriverType == EDriverType.HitReaction)
            {
                // H2: HitReaction 抢占一切（包括其他 HitReaction — 连续受击互相打断）
                // 后续 AbilityEffect 可赋予霸体值 → 阻止抢占
                activeDriver?.OnInterrupted(request);
                activeRequest.OnInterrupt?.Invoke(activeRequest);
                AcceptRequest(driver, request);
            }
            else
            {
                // 拒绝 — Traversal↔Ability 互斥。不清队列，下帧重试。
                return;
            }

            queue.Clear();
        }

        private void AcceptRequest(ICharacterAnimationDriver driver, AnimationRequest request)
        {
            activeDriver = driver;
            activeRequest = request;
            activeCompleted = false;
            _skipCompletionThisFrame = true;
            driver.OnStarted(request);
        }

        private void CheckCompletion()
        {
            if (_skipCompletionThisFrame) { _skipCompletionThisFrame = false; return; }
            if (activeRequest == null || activeCompleted) return;
            float t = layer.CurrentState?.NormalizedTime ?? 0f;
            if (t >= 0.99f)
            {
                activeDriver?.OnCompleted();
                activeRequest = null;
                activeDriver = defaultDriver;
                defaultDriver?.OnResumed();
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
