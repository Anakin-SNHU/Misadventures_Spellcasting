using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellPad : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                    // Camera providing screen-to-world raycasts for sampling cursor positions.
    public Transform strokeContainer;     // Parent transform under which LineRenderer stroke objects are spawned.
    public SimpleFPSController player;    // First-person controller whose look input is toggled while drawing.
    public Material lineMaterial;         // Material assigned to LineRenderers for strokes (e.g., Unlit/Color).

    [Header("Combos & Casting")]
    public SpellLibrary library;          // Lookup/matcher for gesture sequences; resolves combos to spell entries.
    public Transform vfxAnchor;           // Transform used as an origin/forward for spawned VFX when a combo matches.
    public float sequenceTimeout = 0.6f;  // Time window (seconds); gestures older than this are pruned from the buffer.
    public bool autoHideOnSuccess = true; // When true, disables the pad after a successful combo match.

    // Populated externally by a hotkey/slot system; identifiers must correspond to SpellLibrary combo ids.
    [HideInInspector] public string[] allowedCombos; // Optional whitelist restricting which combos can fire in this context.

    [Header("Drawing")]
    public float minPixelStep = 6f;       // Minimum screen-space distance (pixels) required to append a new stroke sample.
    public float lineWidth = 0.01f;       // LineRenderer width (world units) for drawn strokes.
    public float zEpsilon = 0.001f;       // Offset along -forward to bias the stroke toward the camera, avoiding z-fighting.

    private Plane drawPlane;              // World-space plane used as the drawing surface (aligned to this transform).
    private LineRenderer currentLine;     // Active stroke LineRenderer while the mouse button is held.
    private readonly List<Vector3> worldPoints = new List<Vector3>(); // Stroke points in world space (for rendering).
    private readonly List<Vector2> localPoints = new List<Vector2>(); // Stroke points in local pad space (for recognition).

    // Time-stamped gesture buffer enabling variable-length combos with temporal pruning.
    private struct GestureStamp { public PrimitiveGesture g; public float t; }
    private readonly List<GestureStamp> recentStamped = new List<GestureStamp>(); // Ordered history of recognized primitives.
    private readonly List<PrimitiveGesture> recent = new List<PrimitiveGesture>(); // Scratch list mirroring gesture ids only.

    void Awake()
    {
        drawPlane = new Plane(-transform.forward, transform.position); // Plane facing camera relative to this pad.
    }

    void OnEnable()
    {
        drawPlane.SetNormalAndPosition(-transform.forward, transform.position); // Refresh plane if transform changed.
        ClearPad();                                                   // Remove any previous stroke GameObjects.
        ClearRecent();                                                // Reset gesture buffers for a fresh session.
        LockLook(true);                                               // Enable cursor and disable FPS look while drawing.
    }

    void OnDisable()
    {
        LockLook(false);                                              // Restore FPS look and lock cursor state.
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) BeginStroke();               // Start a new stroke at the current cursor position.
        if (Input.GetMouseButton(0) && currentLine != null) ContinueStroke(); // Append/interpolate stroke samples.
        if (Input.GetMouseButtonUp(0) && currentLine != null) EndStroke();    // Finalize stroke and attempt recognition.

        PruneOldGestures(Time.time - sequenceTimeout);                // Enforce rolling time window for combo assembly.
    }

    // ----- drawing -----

    void BeginStroke()
    {
        worldPoints.Clear();
        localPoints.Clear();

        currentLine = new GameObject("Stroke").AddComponent<LineRenderer>(); // One renderer per stroke for simple cleanup.
        currentLine.transform.SetParent(strokeContainer, false);
        currentLine.useWorldSpace = true;
        currentLine.material = lineMaterial;
        currentLine.widthMultiplier = lineWidth;
        currentLine.numCornerVertices = 4;                          // Rounded corners for visual smoothness.
        currentLine.numCapVertices = 4;                             // Rounded caps for stroke endpoints.
        currentLine.alignment = LineAlignment.View;                 // Billboarded alignment toward the viewing camera.
        currentLine.sortingOrder = 1000;                            // Render priority above most scene geometry.
        currentLine.positionCount = 0;

        AddPointFromMouse(true);                                    // Seed with an initial sample.
    }

    void ContinueStroke()
    {
        AddPointFromMouse(false);                                   // Append conditionally based on pixel spacing.
    }

    void EndStroke()
    {
        PrimitiveGesture g = PrimitiveRecognizer.Classify(localPoints); // Convert sampled local points to a primitive.
        if (g != PrimitiveGesture.Unknown)
        {
            recentStamped.Add(new GestureStamp { g = g, t = Time.time }); // Record recognized primitive with timestamp.
            BuildRecentList();                                            // Update contiguous gesture-id list.

            if (library != null && library.TryMatch(recent, out var matched, out int consumed))
            {
                // Check optional whitelist for the active slot/context.
                bool allowed = true;
                if (allowedCombos != null && allowedCombos.Length > 0)
                {
                    allowed = System.Array.Exists(allowedCombos, id => id == matched.id);
                }

                if (allowed)
                {
                    // Spawn optional VFX at the anchor with a small forward offset.
                    if (matched.vfxPrefab != null && vfxAnchor != null)
                    {
                        Instantiate(matched.vfxPrefab,
                            vfxAnchor.position + vfxAnchor.forward * 1.0f,
                            vfxAnchor.rotation);
                    }

                    ConsumeTail(consumed);                           // Remove the matched suffix from the gesture buffer.

                    if (autoHideOnSuccess)
                    {
                        gameObject.SetActive(false);                 // Close the pad after a successful cast.
                        currentLine = null;
                        return;
                    }
                }

                ClearPad();                                          // Successful match but staying open, or not allowed: reset stroke visuals.
            }
            else
            {
                ClearPad();                                          // No match yet: clear visuals but retain buffered gestures.
            }
        }
        else
        {
            ClearPad();                                              // Unrecognized primitive: clear visuals.
        }

        currentLine = null;                                          // Mark stroke as finished.
    }

    void AddPointFromMouse(bool force)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);         // Ray from camera through cursor.
        float enter;
        if (!drawPlane.Raycast(ray, out enter)) return;              // Skip if no intersection with the drawing plane.

        Vector3 world = ray.GetPoint(enter);                         // World-space hit on the plane.

        // Convert to pad-local space for recognition and clamping.
        Vector3 local3 = transform.InverseTransformPoint(world);
        Vector2 local = new Vector2(local3.x, local3.y);

        ClampToPad(ref local);                                       // Constrain to pad bounds in local space.

        // Rebuild clamped world position and bias slightly toward the camera to avoid coplanar artifacts.
        world = transform.TransformPoint(new Vector3(local.x, local.y, 0f));
        world += -transform.forward * zEpsilon;

        if (force || ShouldAdd(world))
        {
            worldPoints.Add(world);
            localPoints.Add(local);
            currentLine.positionCount = worldPoints.Count;
            currentLine.SetPosition(worldPoints.Count - 1, world);   // Append new vertex to the line.
        }
        else if (worldPoints.Count > 0)
        {
            currentLine.SetPosition(worldPoints.Count - 1, world);   // Update last vertex for smoother interpolation.
        }
    }

    bool ShouldAdd(Vector3 world)
    {
        Vector2 a = cam.WorldToScreenPoint(world);                   // Current sample in screen space.
        Vector2 b = (worldPoints.Count > 0)
            ? cam.WorldToScreenPoint(worldPoints[worldPoints.Count - 1])
            : a;
        return (a - b).sqrMagnitude >= (minPixelStep * minPixelStep); // Enforce minimum pixel spacing between samples.
    }

    public void ClearPad()
    {
        for (int i = strokeContainer.childCount - 1; i >= 0; i--)
            Destroy(strokeContainer.GetChild(i).gameObject);         // Remove all stroke renderers under the container.
    }

    void LockLook(bool unlockMouseForDrawing)
    {
        if (player != null) player.SetLookEnabled(!unlockMouseForDrawing); // Toggle FPS look input.
        Cursor.lockState = unlockMouseForDrawing ? CursorLockMode.None : CursorLockMode.Locked; // Control cursor mode.
        Cursor.visible = unlockMouseForDrawing;                   // Show cursor while drawing on the pad.
    }

    // Pad local space spans approximately [-0.5, 0.5] in both X and Y.
    bool ClampToPad(ref Vector2 local)
    {
        float x = Mathf.Clamp(local.x, -0.5f, 0.5f);
        float y = Mathf.Clamp(local.y, -0.5f, 0.5f);
        bool inside = (local.x >= -0.5f && local.x <= 0.5f && local.y >= -0.5f && local.y <= 0.5f); // Indicates original inclusion.
        local = new Vector2(x, y);
        return inside;
    }

    // ----- combo buffer utils -----

    void PruneOldGestures(float cutoff)
    {
        int i = 0;
        for (; i < recentStamped.Count; i++)
            if (recentStamped[i].t >= cutoff) break;                 // Find first non-expired entry.
        if (i > 0) recentStamped.RemoveRange(0, i);                  // Drop all gestures older than the cutoff.
    }

    void BuildRecentList()
    {
        recent.Clear();
        for (int i = 0; i < recentStamped.Count; i++)
            recent.Add(recentStamped[i].g);                          // Mirror only gesture types for matching.
    }

    void ConsumeTail(int count)
    {
        for (int i = 0; i < count && recentStamped.Count > 0; i++)
            recentStamped.RemoveAt(recentStamped.Count - 1);         // Remove most-recent gestures comprising the match.
        BuildRecentList();
    }

    void ClearRecent()
    {
        recentStamped.Clear();
        recent.Clear();
    }
}
