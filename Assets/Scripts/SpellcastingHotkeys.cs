using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellcastingHotkeys : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public string name = "Slot";                // Display name or identifier for this hotkey slot.
        public KeyCode key = KeyCode.Alpha1;        // Keyboard key bound to activate this slot.
        public bool assigned = true;                // Indicates whether this slot is available for use.
        public string[] allowedComboIds;            // Array of SpellLibrary combo IDs that can be cast from this slot.
    }

    public SpellPad spellPad;                       // Reference to the SpellPad component used for gesture input.
    public Transform padTransform;                  // Transform of the SpellPad object (used for positioning in front of the camera).
    public Camera playerCam;                        // Camera representing the player's view.
    public float padDistance = 0.6f;                // Forward distance (in world units) to place the pad from the camera.
    public Vector2 padScale = new Vector2(0.6f, 0.6f); // Local scale applied to the pad when displayed.

    // Initial hotkey slots with configured key bindings and allowed combos.
    public Slot[] slots = new Slot[]
    {
        new Slot { name="1", key=KeyCode.Alpha1, assigned=true,  allowedComboIds = new []{ "Spell_H" } },
        new Slot { name="2", key=KeyCode.Alpha2, assigned=true,  allowedComboIds = new []{ "Spell_V" } },
        new Slot { name="3", key=KeyCode.Alpha3, assigned=true,  allowedComboIds = new []{ "Spell_Slash" } },
        // Example: new Slot { name="4", key=KeyCode.Alpha4, assigned=true, allowedComboIds = new []{ "Spell_H_S_V" } },
    };

    int activeIndex = -1;                           // Index of the currently active slot, or -1 if none.

    void Start()
    {
        if (spellPad != null) spellPad.gameObject.SetActive(false); // Ensure the pad is hidden at startup.
    }

    void Update()
    {
        // Listen for key presses to toggle slots.
        for (int i = 0; i < slots.Length; i++)
        {
            if (Input.GetKeyDown(slots[i].key))
            {
                if (!slots[i].assigned) return;

                if (activeIndex == i)
                {
                    HidePad();                      // Hide if pressing the currently active slot again.
                }
                else
                {
                    ShowPad(i);                     // Show the pad for the newly selected slot.
                }
            }
        }

        // Allow escape key to close the active pad.
        if (activeIndex != -1 && Input.GetKeyDown(KeyCode.Escape))
            HidePad();
    }

    void ShowPad(int index)
    {
        activeIndex = index;

        if (padTransform != null && playerCam != null)
        {
            // Parent the pad to the camera and set its position, rotation, and scale relative to view.
            padTransform.SetParent(playerCam.transform, false);
            padTransform.localPosition = new Vector3(0f, 0f, padDistance);
            padTransform.localRotation = Quaternion.identity;
            padTransform.localScale = new Vector3(padScale.x, padScale.y, 1f);
        }

        if (spellPad != null)
        {
            // Provide the whitelist of allowed combos for this slot.
            spellPad.allowedCombos = slots[index].allowedComboIds;
            spellPad.gameObject.SetActive(true);
        }
    }

    void HidePad()
    {
        activeIndex = -1;
        if (spellPad != null) spellPad.gameObject.SetActive(false); // Disable the pad until next activation.
    }
}
