using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellLibrary : MonoBehaviour
{
    public List<SpellCombo> combos = new List<SpellCombo>(); // Collection of available combos, each with a gesture pattern and metadata.

    // Attempts to match the most recent subsequence of gestures against any known combo pattern.
    // Matches are tested longest-first so that longer, more specific combos take priority over shorter ones.
    public bool TryMatch(IList<PrimitiveGesture> recent, out SpellCombo matched, out int consumed)
    {
        matched = null;                                     // Output combo reference when found.
        consumed = 0;                                       // Number of gestures consumed by the matched combo.
        if (recent == null || recent.Count == 0) return false;

        // Determine the maximum combo length among all entries to limit search depth.
        int maxLen = 0;
        for (int i = 0; i < combos.Count; i++)
            if (combos[i].pattern != null && combos[i].pattern.Length > maxLen)
                maxLen = combos[i].pattern.Length;

        // Only need to test up to the minimum of recent length and max known combo length.
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
                    // Compare gesture-by-gesture with the suffix of the recent list.
                    if (recent[recent.Count - len + i] != combo.pattern[i])
                    {
                        equal = false; break;
                    }
                }

                if (equal)
                {
                    matched = combo;                        // Report the matched combo.
                    consumed = len;                         // Number of gestures matched and consumed.
                    return true;
                }
            }
        }
        return false;                                       // No combo matched in the provided sequence.
    }
}
