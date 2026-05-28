#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only gizmo drawing helpers.
/// Keep these utilities out of gameplay code paths.
/// </summary>
internal static class GizmoDebugUtility
{
    public static void DrawArrowLine(Vector3 from, Vector3 to, Color color, string label = null)
    {
        if (from == to)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawLine(from, to);

        Vector3 direction = to - from;
        float length = direction.magnitude;
        if (length <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 dirNormalized = direction / length;
        const float arrowSize = 0.15f;
        const float arrowAngle = 20f;

        Quaternion rotLeft = Quaternion.AngleAxis(arrowAngle, Vector3.up);
        Quaternion rotRight = Quaternion.AngleAxis(-arrowAngle, Vector3.up);

        Vector3 leftDir = rotLeft * -dirNormalized;
        Vector3 rightDir = rotRight * -dirNormalized;

        Gizmos.DrawLine(to, to + leftDir * arrowSize);
        Gizmos.DrawLine(to, to + rightDir * arrowSize);

        if (!string.IsNullOrEmpty(label))
        {
            Handles.color = Color.white;
            Handles.Label((from + to) * 0.5f, label);
        }
    }

    public static void DrawWireBox(Vector3 center, Vector3 size, Color color, string label = null)
    {
        if (size.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawWireCube(center, size);

        if (!string.IsNullOrEmpty(label))
        {
            Handles.color = Color.white;
            Handles.Label(center, label);
        }
    }

    public static void DrawSphere(Vector3 center, float radius, Color color, string label = null)
    {
        if (radius <= Mathf.Epsilon)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawSphere(center, radius);

        if (!string.IsNullOrEmpty(label))
        {
            Handles.color = Color.white;
            Handles.Label(center, label);
        }
    }
}
#endif
