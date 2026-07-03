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

        public void Release(ICharacterAnimationDriver driver)
        {
            if (driver == activeDriver)
            {
                activeDriver.OnInterrupted(null);
                activeRequest = null;
                activeDriver = defaultDriver;
                defaultDriver?.OnResumed();
            }
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

            // H2: 稳定排序 — 先 Resistance 降序，再按类型名保证确定性
            var sorted = new List<(ICharacterAnimationDriver, AnimationRequest)>(queue);
            sorted.Sort((a, b) =>
            {
                int cmp = b.Item2.Resistance.CompareTo(a.Item2.Resistance);
                if (cmp != 0) return cmp;
                return string.CompareOrdinal(a.Item1.GetType().Name, b.Item1.GetType().Name);
            });

            // H3: snapshot 防止 OnStarted 内 SubmitRequest 并发修改
            foreach (var (driver, request) in sorted)
            {
                if (activeRequest == null)
                {
                    // H1: 默认驱动被抢占时通知
                    if (activeDriver != null && activeDriver != driver)
                        activeDriver.OnInterrupted(request);
                    AcceptRequest(driver, request);
                }
                else if (request.Resistance >= activeRequest.Resistance && CanInterrupt(activeDriver, driver))
                {
                    activeDriver.OnInterrupted(request);
                    AcceptRequest(driver, request);
                }
                else
                {
                    Debug.LogWarning($"[DriverArbiter] Request skipped — " +
                        $"incoming={driver.GetType().Name} (R={request.Resistance}) vs " +
                        $"active={activeDriver?.GetType().Name} (R={activeRequest.Resistance}) — " +
                        $"resistance too low or cannot interrupt");
                }
            }
            queue.Clear();
        }

        /// <summary>
        /// 同类 Driver 可互相打断，异类互斥（Ability ↔ Traversal 不可打断）。
        /// Locomotion（默认驱动，无 activeRequest）不在此检查——已在 activeRequest==null 分支处理。
        /// </summary>
        private static bool CanInterrupt(ICharacterAnimationDriver active, ICharacterAnimationDriver incoming)
        {
            if (active.GetType() == incoming.GetType()) return true;
            // 默认驱动（LocomotionDriver）不提交 Request，不会出现在 active 位置
            // 异类（Ability vs Traversal）：互不可打断
            return false;
        }

        private void AcceptRequest(ICharacterAnimationDriver driver, AnimationRequest request)
        {
            activeDriver = driver;
            activeRequest = request;
            activeCompleted = false;
            driver.OnStarted(request);
            // 不播放 — Driver 在 OnStarted() 中自行处理 layer。
            // OnInterrupted 由 ProcessQueue 在调用 AcceptRequest 前统一处理。
        }

        private void CheckCompletion()
        {
            if (activeRequest == null || activeCompleted) return;
            float t = layer.CurrentState?.NormalizedTime ?? 0f;
            if (t >= 0.99f)
            {
                activeDriver?.OnCompleted();

                if (activeRequest.OnComplete == OnCompleteBehavior.Resume)
                {
                    activeRequest = null;
                    activeDriver = defaultDriver;
                    defaultDriver?.OnResumed();
                }
                // Stay: activeRequest/activeDriver 保持，同 Driver 新请求可替换，外部 Release 可归还

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
