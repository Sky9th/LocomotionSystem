using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 搜索执行工具。将 AbilitySearchSO 纯数据转为物理查询。
    /// </summary>
    public static class AbilitySearchUtility
    {
        public static List<GameObject> Execute(
            AbilitySearchSO search,
            GameObject caster,
            Vector3 origin,
            Vector3 direction)
        {
            var results = new List<GameObject>();
            if (search == null || caster == null) return results;

            int max = search.maxTargets > 0 ? search.maxTargets : int.MaxValue;

            switch (search.searchType)
            {
                case ESearchType.Cone when search is ConeSearchSO cone:
                    ExecuteCone(results, cone, caster, origin, direction, max);
                    break;
                case ESearchType.RayLine when search is RaySearchSO ray:
                    ExecuteRay(results, ray, caster, origin, direction);
                    break;
                case ESearchType.Circle when search is CircleSearchSO circle:
                    ExecuteCircle(results, circle, caster, origin, max);
                    break;
            }

            return results;
        }

        private static void ExecuteCone(
            List<GameObject> results, ConeSearchSO cone,
            GameObject caster, Vector3 origin, Vector3 direction, int max)
        {
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
        }

        private static void ExecuteRay(
            List<GameObject> results, RaySearchSO ray,
            GameObject caster, Vector3 origin, Vector3 direction)
        {
            if (!Physics.Raycast(origin, direction, out var hit, ray.range, ray.targetMask))
                return;

            var go = hit.collider.gameObject;
            if (go == caster) return;
            if (go.GetComponent<Identity>() == null) return;

            results.Add(go);
        }

        private static void ExecuteCircle(
            List<GameObject> results, CircleSearchSO circle,
            GameObject caster, Vector3 origin, int max)
        {
            var hits = Physics.OverlapSphere(origin, circle.range, circle.targetMask);

            for (int i = 0; i < hits.Length && results.Count < max; i++)
            {
                var go = hits[i].gameObject;
                if (go == caster) continue;
                if (go.GetComponent<Identity>() == null) continue;

                if (!results.Contains(go))
                    results.Add(go);
            }
        }
    }
}
