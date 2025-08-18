using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellPad : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                    // Camera Main
    public Transform strokeContainer;     // child: StrokeContainer
    public SimpleFPSController player;    // your FPS controller
    public Material lineMaterial;         // Unlit/Color (white)

    [Header("Combos & Casting")]
    public SpellLibrary library;          // on SpellcastingManager
    public Transform vfxAnchor;           // usually Camera Main
    public float sequenceTimeout = 0.6f;  // seconds between strokes in a combo
    public bool autoHideOnSuccess = true;

    // Set by your hotkey script when opening the pad (IDs must match library combos)
    [HideInInspector] public string[] allowedCombos;

    [Header("Drawing")]
    public float minPixelStep = 6f;       // screen pixels between samples
    public float lineWidth = 0.01f;       // LineRenderer width
    public float zEpsilon = 0.001f;       // push stroke toward camera to avoid z-fighting

    private Plane drawPlane;
    private LineRenderer currentLine;
    private readonly List<Vector3> worldPoints = new List<Vector3>();
    private readonly List<Vector2> localPoints = new List<Vector2>();

    // recent gesture buffer (with timestamps) to allow combos of any length
    private struct GestureStamp { public PrimitiveGesture g; public float t; }
    private readonly List<GestureStamp> recentStamped = new List<GestureStamp>();
    private readonly List<PrimitiveGesture> recent = new List<PrimitiveGesture>(); // scratch list

    void Awake()
    {
        drawPlane = new Plane(-transform.forward, transform.position);
    }

    void OnEnable()
    {
        // refresh plane in case pad moved
        drawPlane.SetNormalAndPosition(-transform.forward, transform.position);
        ClearPad();
        ClearRecent();
        LockLook(true); // free mouse, freeze camera look
    }

    void OnDisable()
    {
        LockLook(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) BeginStroke();
        if (Input.GetMouseButton(0) && currentLine != null) ContinueStroke();
        if (Input.GetMouseButtonUp(0) && currentLine != null) EndStroke();

        // prune expired gestures so combos must be quick
        PruneOldGestures(Time.time - sequenceTimeout);
    }

    // ----- drawing -----

    void BeginStroke()
    {
        worldPoints.Clear();
        localPoints.Clear();

        currentLine = new GameObject("Stroke").AddComponent<LineRenderer>();
        currentLine.transform.SetParent(strokeContainer, false);
        currentLine.useWorldSpace = true;
        currentLine.material = lineMaterial;
        currentLine.widthMultiplier = lineWidth;
        currentLine.numCornerVertices = 4;
        currentLine.numCapVertices = 4;
        currentLine.alignment = LineAlignment.View;
        currentLine.sortingOrder = 1000;
        currentLine.positionCount = 0;

        AddPointFromMouse(true);
    }

    void ContinueStroke()
    {
        AddPointFromMouse(false);
    }

    void EndStroke()
    {
        PrimitiveGesture g = PrimitiveRecognizer.Classify(localPoints);
        if (g != PrimitiveGesture.Unknown)
        {
            recentStamped.Add(new GestureStamp { g = g, t = Time.time });
            BuildRecentList();

            if (library != null && library.TryMatch(recent, out var matched, out int consumed))
            {
                // check per-slot whitelist
                bool allowed = true;
                if (allowedCombos != null && allowedCombos.Length > 0)
                {
                    allowed = System.Array.Exists(allowedCombos, id => id == matched.id);
                }

                if (allowed)
                {
                    // optional VFX
                    if (matched.vfxPrefab != null && vfxAnchor != null)
                    {
                        Instantiate(matched.vfxPrefab,
                            vfxAnchor.position + vfxAnchor.forward * 1.0f,
                            vfxAnchor.rotation);
                    }

                    ConsumeTail(consumed);

                    if (autoHideOnSuccess)
                    {
                        gameObject.SetActive(false); // put pad away
                        currentLine = null;
                        return;
                    }
                }

                // success but staying open OR not allowed for this slot:
                ClearPad();
            }
            else
            {
                // no match yet; clear visual but keep buffer
                ClearPad();
            }
        }
        else
        {
            // not a primitive; clear visual
            ClearPad();
        }

        currentLine = null;
    }

    void AddPointFromMouse(bool force)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (!drawPlane.Raycast(ray, out enter)) return;

        // world point on the plane
        Vector3 world = ray.GetPoint(enter);

        // to local (pad) space
        Vector3 local3 = transform.InverseTransformPoint(world);
        Vector2 local = new Vector2(local3.x, local3.y);

        // clamp to quad bounds so strokes never leave the pad
        ClampToPad(ref local);

        // rebuild clamped world position and lift it slightly toward camera
        world = transform.TransformPoint(new Vector3(local.x, local.y, 0f));
        world += -transform.forward * zEpsilon;

        if (force || ShouldAdd(world))
        {
            worldPoints.Add(world);
            localPoints.Add(local);
            currentLine.positionCount = worldPoints.Count;
            currentLine.SetPosition(worldPoints.Count - 1, world);
        }
        else if (worldPoints.Count > 0)
        {
            currentLine.SetPosition(worldPoints.Count - 1, world);
        }
    }

    bool ShouldAdd(Vector3 world)
    {
        Vector2 a = cam.WorldToScreenPoint(world);
        Vector2 b = (worldPoints.Count > 0)
            ? cam.WorldToScreenPoint(worldPoints[worldPoints.Count - 1])
            : a;
        return (a - b).sqrMagnitude >= (minPixelStep * minPixelStep);
    }

    public void ClearPad()
    {
        for (int i = strokeContainer.childCount - 1; i >= 0; i--)
            Destroy(strokeContainer.GetChild(i).gameObject);
    }

    void LockLook(bool unlockMouseForDrawing)
    {
        if (player != null) player.SetLookEnabled(!unlockMouseForDrawing);
        Cursor.lockState = unlockMouseForDrawing ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlockMouseForDrawing;
    }

    // quad local space spans roughly [-0.5, 0.5] in X and Y
    bool ClampToPad(ref Vector2 local)
    {
        float x = Mathf.Clamp(local.x, -0.5f, 0.5f);
        float y = Mathf.Clamp(local.y, -0.5f, 0.5f);
        bool inside = (local.x >= -0.5f && local.x <= 0.5f && local.y >= -0.5f && local.y <= 0.5f);
        local = new Vector2(x, y);
        return inside;
    }

    // ----- combo buffer utils -----

    void PruneOldGestures(float cutoff)
    {
        int i = 0;
        for (; i < recentStamped.Count; i++)
            if (recentStamped[i].t >= cutoff) break;
        if (i > 0) recentStamped.RemoveRange(0, i);
    }

    void BuildRecentList()
    {
        recent.Clear();
        for (int i = 0; i < recentStamped.Count; i++)
            recent.Add(recentStamped[i].g);
    }

    void ConsumeTail(int count)
    {
        for (int i = 0; i < count && recentStamped.Count > 0; i++)
            recentStamped.RemoveAt(recentStamped.Count - 1);
        BuildRecentList();
    }

    void ClearRecent()
    {
        recentStamped.Clear();
        recent.Clear();
    }
}
