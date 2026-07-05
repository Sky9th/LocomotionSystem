using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ⛔ DEPRECATED — 物理查询已内联至 ExecutionState（Fire 帧碰撞）。
    ///   链变更为 Gating → Cost → Activation → Execution。保留供参考。
    /// </summary>
    public class SearchState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Search;

        private const float MinDuration = 0.5f;
        private float _elapsed;
        private bool _searched;

        public override void OnEnter(ref SActiveAbilityContext ctx)
        {
            _elapsed = 0f;
            _searched = false;
        }

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var active = a as ActiveAbilitySO;

            // ── 首帧：执行搜索 ──
            if (!_searched)
            {
                _searched = true;
                var caster = ctx.Executor.gameObject;
                ctx.Targets = ExecuteSearch(active?.search, caster, ctx.Origin, ctx.Direction);
            }

            // ── 每帧绘制 Debug 形状 ──
            DrawDebugSearch(active?.search, ctx.Origin, ctx.Direction, 0.5f);

            // ── 最少停留 MinDuration ──
            _elapsed += dt;
            if (_elapsed < MinDuration)
                return this;

            return new CostState();
        }

        #region Search Strategies

        private static List<GameObject> ExecuteSearch(AbilitySearchSO search, GameObject caster, Vector3 origin, Vector3 direction)
        {
            if (search == null || caster == null) return new List<GameObject>();

            return search switch
            {
                ConeSearchSO cone     => SearchCone(cone, caster, origin, direction),
                RaySearchSO ray       => SearchRay(ray, caster, origin, direction),
                CircleSearchSO circle => SearchCircle(circle, caster, origin),
                _                     => new List<GameObject>()
            };
        }

        private static List<GameObject> SearchCone(ConeSearchSO cone, GameObject caster, Vector3 origin, Vector3 direction)
        {
            var results = new List<GameObject>();
            int max = cone.maxTargets > 0 ? cone.maxTargets : int.MaxValue;
            var hits = Physics.OverlapSphere(origin, cone.range, cone.targetMask);
            float halfAngle = cone.angle * 0.5f;

            for (int i = 0; i < hits.Length && results.Count < max; i++)
            {
                var go = hits[i].gameObject;
                if (go == caster) continue;
                if (go.GetComponent<Identity>() == null) continue;

                var toTarget = hits[i].transform.position - origin;
                if (Vector3.Angle(direction, toTarget) > halfAngle) continue;

                if (!results.Contains(go))
                    results.Add(go);
            }

            return results;
        }

        private static List<GameObject> SearchRay(RaySearchSO ray, GameObject caster, Vector3 origin, Vector3 direction)
        {
            var results = new List<GameObject>();

            if (Physics.Raycast(origin, direction, out var hit, ray.range, ray.targetMask))
            {
                var go = hit.collider.gameObject;
                if (go != caster && go.GetComponent<Identity>() != null)
                    results.Add(go);
            }

            return results;
        }

        private static List<GameObject> SearchCircle(CircleSearchSO circle, GameObject caster, Vector3 origin)
        {
            var results = new List<GameObject>();
            int max = circle.maxTargets > 0 ? circle.maxTargets : int.MaxValue;
            var hits = Physics.OverlapSphere(origin, circle.range, circle.targetMask);

            for (int i = 0; i < hits.Length && results.Count < max; i++)
            {
                var go = hits[i].gameObject;
                if (go == caster) continue;
                if (go.GetComponent<Identity>() == null) continue;

                if (!results.Contains(go))
                    results.Add(go);
            }

            return results;
        }

        #endregion

        #region Debug Draw

        private static void DrawDebugSearch(AbilitySearchSO search, Vector3 origin, Vector3 direction, float duration)
        {
            // ── 原点球体：始终可见，确认 SearchState 在运行 ──
            DrawDebugSphere(origin, 0.15f, Color.white, duration);

            // 兜底方向
            var dir = direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction;

            if (search == null) return;

            switch (search)
            {
                case ConeSearchSO cone:
                    DrawDebugCone(origin, dir, cone.range, cone.angle, duration);
                    break;
                case RaySearchSO ray:
                    DrawDebugRay(origin, dir, ray.range, duration);
                    break;
                case CircleSearchSO circle:
                    DrawDebugCircle(origin, circle.range, duration);
                    break;
            }
        }

        /// <summary>原点参考球 — 确认 SearchState 运行中，始终可见。</summary>
        private static void DrawDebugSphere(Vector3 center, float radius, Color color, float duration)
        {
            // 三个正交圆环
            int seg = 16;
            for (int ring = 0; ring < 3; ring++)
            {
                var prev = center + (ring switch
                {
                    0 => new Vector3(radius, 0, 0),
                    1 => new Vector3(0, radius, 0),
                    _ => new Vector3(0, 0, radius),
                });
                for (int i = 1; i <= seg; i++)
                {
                    float a = (float)i / seg * Mathf.PI * 2f;
                    var pt = center + ring switch
                    {
                        0 => new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0),
                        1 => new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius),
                        _ => new Vector3(0, Mathf.Cos(a) * radius, Mathf.Sin(a) * radius),
                    };
                    Debug.DrawLine(prev, pt, color, duration);
                    prev = pt;
                }
            }
        }

        private static void DrawDebugCone(Vector3 origin, Vector3 direction, float range, float angle, float duration)
        {
            float halfAngle = angle * 0.5f;
            var forward = direction.normalized * range;

            // 中心线
            Debug.DrawRay(origin, forward, Color.yellow, duration);

            // 左右边缘
            var left = Quaternion.Euler(0, -halfAngle, 0) * forward;
            var right = Quaternion.Euler(0, halfAngle, 0) * forward;
            Debug.DrawRay(origin, left, Color.yellow, duration);
            Debug.DrawRay(origin, right, Color.yellow, duration);

            // 远端弧线 (用线段逼近)
            int arcSegments = 16;
            var prev = origin + left;
            for (int i = 1; i <= arcSegments; i++)
            {
                float t = (float)i / arcSegments;
                float a = Mathf.Lerp(-halfAngle, halfAngle, t);
                var arcDir = Quaternion.Euler(0, a, 0) * forward;
                var pt = origin + arcDir;
                Debug.DrawLine(prev, pt, Color.yellow, duration);
                prev = pt;
            }
        }

        private static void DrawDebugRay(Vector3 origin, Vector3 direction, float range, float duration)
        {
            Debug.DrawRay(origin, direction.normalized * range, Color.red, duration);
        }

        private static void DrawDebugCircle(Vector3 origin, float range, float duration)
        {
            int segments = 32;
            var prev = origin + new Vector3(range, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                var pt = origin + new Vector3(Mathf.Cos(a) * range, 0, Mathf.Sin(a) * range);
                Debug.DrawLine(prev, pt, Color.cyan, duration);
                prev = pt;
            }
        }

        #endregion
    }
}
