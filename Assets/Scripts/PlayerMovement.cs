using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 3f;
    public float sprintSpeed = 6f;
    public float mouseSensitivity = 0.1f;
    public Transform cam;

    [Header("Stamina")]
    public float maxStamina = 5f;      // seconds of sprinting
    public float staminaRegen = 1f;    // how fast it refills

    CharacterController controller;
    float lookUp;
    float stamina;
    bool tired;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        stamina = maxStamina;
    }

    void Update()
    {
        // look around
        Vector2 mouse = Mouse.current.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(0, mouse.x, 0);
        lookUp = Mathf.Clamp(lookUp - mouse.y, -80, 80);
        cam.localRotation = Quaternion.Euler(lookUp, 0, 0);

        // walk
        float x = 0, z = 0;
        if (Keyboard.current.wKey.isPressed) z += 1;
        if (Keyboard.current.sKey.isPressed) z -= 1;
        if (Keyboard.current.dKey.isPressed) x += 1;
        if (Keyboard.current.aKey.isPressed) x -= 1;
        Vector3 move = transform.right * x + transform.forward * z;

        // sprint
        bool wantsToSprint = Keyboard.current.leftShiftKey.isPressed && move != Vector3.zero;
        bool sprinting = wantsToSprint && !tired;

        if (sprinting)
        {
            stamina -= Time.deltaTime;
            if (stamina <= 0)
            {
                stamina = 0;
                tired = true;   // ran out, have to wait before sprinting again
            }
        }
        else
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRegen * Time.deltaTime);
            if (tired && stamina >= maxStamina * 0.5f)
                tired = false;  // got half back, can sprint again
        }

        float currentSpeed = sprinting ? sprintSpeed : speed;
        controller.SimpleMove(move.normalized * currentSpeed);
    }

    // temporary stamina display, Isabelle can replace with real UI
    void OnGUI()
    {
        GUI.Label(new Rect(20, 80, 300, 30), "Stamina: " + stamina.ToString("0.0") + (tired ? "  (tired)" : ""));
    }
}