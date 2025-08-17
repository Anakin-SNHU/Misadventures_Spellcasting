using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellcastingHotkeys : MonoBehaviour
{
    public SpellPad spellPad;          // drag the SpellPad component here
    public Transform padTransform;     // drag the SpellPad transform here
    public Camera playerCam;           // Camera Main
    public float padDistance = 0.6f;   // meters in front of camera
    public Vector2 padScale = new Vector2(0.6f, 0.6f);

    // mark which slots are "assigned" (true = enabled)
    public bool[] slotAssigned = new bool[10] { true, true, true, false, false, false, false, false, false, false };

    KeyCode[] keys = new KeyCode[]
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
    };

    int activeIndex = -1;

    void Start()
    {
        if (spellPad != null) spellPad.gameObject.SetActive(false);
    }

    void Update()
    {
        // handle 1..0
        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
            {
                if (!slotAssigned[i]) return;

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

        // position and orient the pad in front of the camera
        if (padTransform != null && playerCam != null)
        {
            padTransform.SetParent(playerCam.transform, false);
            padTransform.localPosition = new Vector3(0f, 0f, padDistance);
            padTransform.localRotation = Quaternion.identity;
            padTransform.localScale = new Vector3(padScale.x, padScale.y, 1f);
        }

        if (spellPad != null) spellPad.gameObject.SetActive(true);
    }

    void HidePad()
    {
        activeIndex = -1;
        if (spellPad != null) spellPad.gameObject.SetActive(false);
    }
}
