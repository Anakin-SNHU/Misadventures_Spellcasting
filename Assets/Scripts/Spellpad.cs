using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellPad : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                   // Camera Main
    public Transform strokeContainer;    // StrokeContainer (empty child)
    public SimpleFPSController player;   // Your FPS controller
    public Material lineMaterial;        // Unlit/Color (white)

    [Header("Drawing")]
    public float minPixelStep = 6f;      // pixels between samples
    public float lineWidth = 0.01f;      // LineRenderer width
    public float sequenceTimeout = 0.6f; // reserved for combos later
    public float zEpsilon = 0.001f;      // push stroke toward camera

    private Plane drawPlane;
    private LineRenderer currentLine;
    private readonly List<Vector3> worldPoints = new List<Vector3>();
    private readonly List<Vector2> localPoints = new List<Vector2>();
    private readonly List<PrimitiveGesture> recentGestures = new List<PrimitiveGesture>();
    private float lastStrokeTime = -999f;

    void Awake()
    {
        // Plane that faces the camera (normal points away from camera)
        drawPlane = new Plane(-transform.forward, transform.position);
    }

    void OnEnable()
    {
        // Refresh the plane in case the pad moved
        drawPlane.SetNormalAndPosition(-transform.forward, transform.position);
        ClearPad();
        LockLook(true); // free mouse, freeze camera look
    }

    void OnDisable()
    {
        LockLook(false); // restore look + cursor lock
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) BeginStroke();
        if (Input.GetMouseButton(0) && currentLine != null) ContinueStroke();
        if (Input.GetMouseButtonUp(0) && currentLine != null) EndStroke();
    }

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
        currentLine.sortingOrder = 1000; // ensure on top
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
        recentGestures.Add(g);
        lastStrokeTime = Time.time;

        // For the snapshot: clear pad on any valid primitive
        if (g != PrimitiveGesture.Unknown)
            ClearPad();

        currentLine = null;
    }

    void AddPointFromMouse(bool force)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (!drawPlane.Raycast(ray, out enter)) return;

        // World position on the pad plane
        Vector3 world = ray.GetPoint(enter);

        // Convert to pad local space (so recognizer is resolution independent)
        Vector3 local3 = transform.InverseTransformPoint(world);
        Vector2 local = new Vector2(local3.x, local3.y);

        // Clamp to the quad bounds so strokes cannot leave the pad
        ClampToPad(ref local);

        // Rebuild a clamped world position and nudge toward camera to avoid z-fighting
        world = transform.TransformPoint(new Vector3(local.x, local.y, 0f));
        world += -transform.forward * zEpsilon;

        if (force || ShouldAdd(world))
        {
            worldPoints.Add(world);
            localPoints.Add(local);
            currentLine.positionCount = worldPoints.Count;
            currentLine.SetPosition(worldPoints.Count - 1, world);
        }
        else
        {
            // Smoothly update the last point while moving between samples
            if (worldPoints.Count > 0)
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

    // Quad local space spans roughly [-0.5, 0.5] in X and Y
    bool ClampToPad(ref Vector2 local)
    {
        float x = Mathf.Clamp(local.x, -0.5f, 0.5f);
        float y = Mathf.Clamp(local.y, -0.5f, 0.5f);
        bool inside = (local.x >= -0.5f && local.x <= 0.5f && local.y >= -0.5f && local.y <= 0.5f);
        local = new Vector2(x, y);
        return inside;
    }
}
