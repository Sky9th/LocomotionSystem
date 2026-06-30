using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ⛔ DEPRECATED — 逻辑已内联至 <see cref="SearchState"/>。旧 AbilityExecutor.TryActivate 仍在引用，该旧代码废弃后删除本类。
    /// </summary>
    public class AbilitySearch
    {
        /// <summary>
        /// 执行搜索。按 SearchSO 运行时类型分发。
        /// </summary>
        /// <param name="search">搜索定义（ConeSearchSO / RaySearchSO / CircleSearchSO）</param>
        /// <param name="caster">施法者 GO（排除自身）</param>
        /// <param name="origin">搜索起点世界坐标</param>
        /// <param name="direction">搜索朝向（锥/射线使用）</param>
        /// <returns>命中目标列表。无 Identity 组件的 GO 被过滤。</returns>
        public List<GameObject> Execute(AbilitySearchSO search, GameObject caster, Vector3 origin, Vector3 direction)
        {
            if (search == null || caster == null) return new List<GameObject>();

            return search switch
            {
                ConeSearchSO cone => SearchCone(cone, caster, origin, direction),
                RaySearchSO ray => SearchRay(ray, caster, origin, direction),
                CircleSearchSO circle => SearchCircle(circle, caster, origin),
                _ => new List<GameObject>()
            };
        }

        #region Search Strategies

        private List<GameObject> SearchCone(ConeSearchSO cone, GameObject caster, Vector3 origin, Vector3 direction)
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

        private List<GameObject> SearchRay(RaySearchSO ray, GameObject caster, Vector3 origin, Vector3 direction)
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

        private List<GameObject> SearchCircle(CircleSearchSO circle, GameObject caster, Vector3 origin)
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
    }
}
