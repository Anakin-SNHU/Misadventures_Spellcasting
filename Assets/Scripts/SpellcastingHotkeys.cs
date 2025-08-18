using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellcastingHotkeys : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public string name = "Slot";
        public KeyCode key = KeyCode.Alpha1;
        public bool assigned = true;
        // IDs from SpellLibrary.combos[i].id that this slot allows
        public string[] allowedComboIds;
    }

    public SpellPad spellPad;        // drag the SpellPad component here
    public Transform padTransform;   // drag the SpellPad transform here
    public Camera playerCam;         // Camera Main
    public float padDistance = 0.6f;
    public Vector2 padScale = new Vector2(0.6f, 0.6f);

    // Configure your slots in the Inspector
    public Slot[] slots = new Slot[]
    {
        new Slot { name="1", key=KeyCode.Alpha1, assigned=true,  allowedComboIds = new []{ "Spell_H" } },
        new Slot { name="2", key=KeyCode.Alpha2, assigned=true,  allowedComboIds = new []{ "Spell_V" } },
        new Slot { name="3", key=KeyCode.Alpha3, assigned=true,  allowedComboIds = new []{ "Spell_Slash" } },
        // add more as needed; e.g. a combo slot:
        // new Slot { name="4", key=KeyCode.Alpha4, assigned=true, allowedComboIds = new []{ "Spell_H_S_V" } },
    };

    int activeIndex = -1;

    void Start()
    {
        if (spellPad != null) spellPad.gameObject.SetActive(false);
    }

    void Update()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (Input.GetKeyDown(slots[i].key))
            {
                if (!slots[i].assigned) return;

                if (activeIndex == i)
                {
                    HidePad();
                }
                else
                {
                    ShowPad(i);
                }
            }
        }

        if (activeIndex != -1 && Input.GetKeyDown(KeyCode.Escape))
            HidePad();
    }

    void ShowPad(int index)
    {
        activeIndex = index;

        if (padTransform != null && playerCam != null)
        {
            padTransform.SetParent(playerCam.transform, false);
            padTransform.localPosition = new Vector3(0f, 0f, padDistance);
            padTransform.localRotation = Quaternion.identity;
            padTransform.localScale = new Vector3(padScale.x, padScale.y, 1f);
        }

        if (spellPad != null)
        {
            // pass the whitelist to the pad
            spellPad.allowedCombos = slots[index].allowedComboIds;
            spellPad.gameObject.SetActive(true);
        }
    }

    void HidePad()
    {
        activeIndex = -1;
        if (spellPad != null) spellPad.gameObject.SetActive(false);
    }
}
