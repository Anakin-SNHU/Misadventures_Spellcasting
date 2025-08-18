using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellLibrary : MonoBehaviour
{
    public List<SpellCombo> combos = new List<SpellCombo>();

    // Try to match the tail of 'recent' against any combo pattern (longest-first)
    public bool TryMatch(IList<PrimitiveGesture> recent, out SpellCombo matched, out int consumed)
    {
        matched = null;
        consumed = 0;
        if (recent == null || recent.Count == 0) return false;

        int maxLen = 0;
        for (int i = 0; i < combos.Count; i++)
            if (combos[i].pattern != null && combos[i].pattern.Length > maxLen)
                maxLen = combos[i].pattern.Length;

        int tryLen = Mathf.Min(maxLen, recent.Count);
        for (int len = tryLen; len >= 1; len--)
        {
            for (int c = 0; c < combos.Count; c++)
            {
                var combo = combos[c];
                if (combo.pattern == null || combo.pattern.Length != len) continue;

                bool equal = true;
                for (int i = 0; i < len; i++)
                {
                    if (recent[recent.Count - len + i] != combo.pattern[i])
                    {
                        equal = false; break;
                    }
                }

                if (equal)
                {
                    matched = combo;
                    consumed = len;
                    return true;
                }
            }
        }
        return false;
    }
}
