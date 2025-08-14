using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellcastingUI : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public string slotName = "Spell 1";
        public KeyCode key = KeyCode.Alpha1;   // 1..0 = Alpha1..Alpha0
        public bool assigned = false;          // toggle to “assign” a spell
        public Sprite runeSprite;              // shown when opened
    }

    public Slot[] slots = new Slot[]
    {
        new Slot(){ slotName="Spell 1", key=KeyCode.Alpha1, assigned=true },
        new Slot(){ slotName="Spell 2", key=KeyCode.Alpha2, assigned=true },
        new Slot(){ slotName="Spell 3", key=KeyCode.Alpha3, assigned=true },
        // add more if you want up to Alpha0
    };

    [Header("Overlay refs")]
    public Canvas overlayCanvas;        // the Canvas you made
    public CanvasGroup overlayGroup;    // CanvasGroup on the panel
    public Image runeImage;             // Image that shows the rune

    [Header("Player refs")]
    public SimpleFPSController playerController;

    int activeIndex = -1;

    void Start()
    {
        HideOverlay();
    }

    void Update()
    {
        // open/cancel via hotkeys
        for (int i = 0; i < slots.Length; i++)
        {
            if (Input.GetKeyDown(slots[i].key))
            {
                if (activeIndex == i)
                {
                    // pressing the same key cancels
                    HideOverlay();
                }
                else if (slots[i].assigned)
                {
                    ShowOverlay(i);
                }
                // if not assigned, ignore
            }
        }

        // global cancel
        if (activeIndex != -1 && Input.GetKeyDown(KeyCode.Escape))
            HideOverlay();
    }

    void ShowOverlay(int index)
    {
        activeIndex = index;
        var s = slots[index];

        if (runeImage) runeImage.sprite = s.runeSprite;
        if (overlayCanvas) overlayCanvas.enabled = true;
        if (overlayGroup)
        {
            overlayGroup.alpha = 1f;
            overlayGroup.interactable = true;
            overlayGroup.blocksRaycasts = true;
        }

        if (playerController) playerController.SetLookEnabled(false); // lock camera, free mouse
    }

    void HideOverlay()
    {
        activeIndex = -1;
        if (overlayGroup)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
        }
        if (overlayCanvas) overlayCanvas.enabled = false;

        if (playerController) playerController.SetLookEnabled(true);  // restore look + lock cursor
    }
}
