using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpellCombo
{
    public string id;                          // Unique identifier string for this combo (e.g., "FireSlash").
    public PrimitiveGesture[] pattern;         // Ordered sequence of gestures that define this combo (e.g., [Slash, Backslash]).
    public GameObject vfxPrefab;               // Optional visual effect prefab to instantiate upon a successful match.
}
