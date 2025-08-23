using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PrimitiveRecognizer
{
    const float MIN_LENGTH = 0.05f;              // Minimum required end-to-end distance for a stroke to be considered valid.
    const float STRAIGHTNESS_MIN = 0.85f;        // Minimum straightness ratio (end-to-end / total path length).
    const float ANGLE_TOL = 20f;                 // Angular tolerance (degrees) when classifying line orientation.

    // Attempts to classify a sequence of sampled 2D points into one of the primitive gesture types.
    public static PrimitiveGesture Classify(List<Vector2> pts)
    {
        if (pts == null || pts.Count < 2) return PrimitiveGesture.Unknown;

        // Compute total path length along the sampled stroke.
        float pathLen = 0f;
        for (int i = 1; i < pts.Count; i++)
            pathLen += Vector2.Distance(pts[i - 1], pts[i]);

        // Measure end-to-end distance from first to last point.
        float endToEnd = Vector2.Distance(pts[0], pts[pts.Count - 1]);
        if (endToEnd < MIN_LENGTH) return PrimitiveGesture.Unknown;

        // Straightness ratio close to 1 indicates a nearly straight stroke.
        float straightness = endToEnd / Mathf.Max(0.0001f, pathLen);
        if (straightness < STRAIGHTNESS_MIN) return PrimitiveGesture.Unknown;

        // Compute stroke direction vector and corresponding angle in degrees.
        Vector2 dir = (pts[pts.Count - 1] - pts[0]).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // Result is in range -180..180.

        // Compare angle to canonical directions with symmetry: horizontal, vertical, slash, backslash.
        float d0 = MinAngle180(angle, 0f);     // Horizontal (0° or 180°).
        float d90 = MinAngle180(angle, 90f);   // Vertical (90° or -90°).
        float d45 = MinAngle180(angle, 45f);   // Diagonal slash (45° or -135°).
        float d135 = MinAngle180(angle, 135f); // Diagonal backslash (135° or -45°).

        if (d0 <= ANGLE_TOL) return PrimitiveGesture.Horizontal;
        if (d90 <= ANGLE_TOL) return PrimitiveGesture.Vertical;
        if (d45 <= ANGLE_TOL) return PrimitiveGesture.Slash;
        if (d135 <= ANGLE_TOL) return PrimitiveGesture.Backslash;

        return PrimitiveGesture.Unknown;       // No match within angular tolerance.
    }

    // Normalizes an angle to the range [-180, 180].
    static float NormAngle(float deg)
    {
        deg %= 360f;
        if (deg < -180f) deg += 360f;
        if (deg > 180f) deg -= 360f;
        return deg;
    }

    // Computes the minimal absolute angular difference to a target, treating opposite directions as equivalent.
    static float MinAngle180(float a, float target)
    {
        float diff = Mathf.Abs(NormAngle(a - target));
        diff = Mathf.Min(diff, Mathf.Abs(diff - 180f)); // 0° and 180° are treated as identical orientations.
        return diff;
    }
}

public enum PrimitiveGesture
{
    Unknown = 0,    // Gesture could not be classified.
    Horizontal = 1, // Left-to-right or right-to-left straight line.
    Vertical = 2,   // Upward or downward straight line.
    Slash = 3,      // Diagonal from lower-left to upper-right (or opposite).
    Backslash = 4   // Diagonal from upper-left to lower-right (or opposite).
}
