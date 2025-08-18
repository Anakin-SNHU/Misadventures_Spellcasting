using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpellCombo
{
    public string id;                          // e.g., "FireSlash", "ShieldV"
    public PrimitiveGesture[] pattern;         // e.g., [Slash, Backslash]
    public GameObject vfxPrefab;               // optional: simple success VFX
}
