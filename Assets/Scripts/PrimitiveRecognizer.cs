using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PrimitiveRecognizer
{
    const float MIN_LENGTH = 0.05f;
    const float STRAIGHTNESS_MIN = 0.85f;
    const float ANGLE_TOL = 20f;

    public static PrimitiveGesture Classify(List<Vector2> pts)
    {
        if (pts == null || pts.Count < 2) return PrimitiveGesture.Unknown;

        float pathLen = 0f;
        for (int i = 1; i < pts.Count; i++)
            pathLen += Vector2.Distance(pts[i - 1], pts[i]);

        float endToEnd = Vector2.Distance(pts[0], pts[pts.Count - 1]);
        if (endToEnd < MIN_LENGTH) return PrimitiveGesture.Unknown;

        float straightness = endToEnd / Mathf.Max(0.0001f, pathLen);
        if (straightness < STRAIGHTNESS_MIN) return PrimitiveGesture.Unknown;

        Vector2 dir = (pts[pts.Count - 1] - pts[0]).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // -180..180

        // helper returns the smallest absolute difference in degrees, wrapping at 180
        float d0 = MinAngle180(angle, 0f);   // horizontal both directions
        float d90 = MinAngle180(angle, 90f);   // vertical both directions
        float d45 = MinAngle180(angle, 45f);   // slash both directions
        float d135 = MinAngle180(angle, 135f);   // backslash both directions

        if (d0 <= ANGLE_TOL) return PrimitiveGesture.Horizontal;
        if (d90 <= ANGLE_TOL) return PrimitiveGesture.Vertical;
        if (d45 <= ANGLE_TOL) return PrimitiveGesture.Slash;
        if (d135 <= ANGLE_TOL) return PrimitiveGesture.Backslash;

        return PrimitiveGesture.Unknown;

    }

    static float NormAngle(float deg)
    {
        deg %= 360f;
        if (deg < -180f) deg += 360f;
        if (deg > 180f) deg -= 360f;
        return deg;
    }

    static float MinAngle180(float a, float target)
    {
        // normalize both to [0,180) symmetry by folding 180-degree opposites together
        float diff = Mathf.Abs(NormAngle(a - target));
        diff = Mathf.Min(diff, Mathf.Abs(diff - 180f)); // treat 0 and 180 as equivalent
        return diff;
    }

}

public enum PrimitiveGesture
{
    Unknown = 0,
    Horizontal = 1,
    Vertical = 2,
    Slash = 3,
    Backslash = 4
}
