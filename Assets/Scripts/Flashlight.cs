using UnityEngine;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    Light flashlight;

    void Start()
    {
        flashlight = GetComponent<Light>();
    }

    void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
}