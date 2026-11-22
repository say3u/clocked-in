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

    [Header("Crouch")]
    public float crouchSpeed = 1.5f;
    public float crouchHeight = 1.1f;
    public float crouchTransitionSpeed = 8f;

    // HeadBob uses this to lower the camera
    public float CameraDrop { get; private set; }

    CharacterController controller;
    float lookUp;
    float stamina;
    bool tired;
    bool crouching;
    float standHeight;
    float standCenterY;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        stamina = maxStamina;
        standHeight = controller.height;
        standCenterY = controller.center.y;
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

        UpdateCrouch();

        // sprint
        bool wantsToSprint = Keyboard.current.leftShiftKey.isPressed && move != Vector3.zero && !crouching;
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

        float currentSpeed = speed;
        if (crouching) currentSpeed = crouchSpeed;
        else if (sprinting) currentSpeed = sprintSpeed;

        controller.SimpleMove(move.normalized * currentSpeed);
    }

    void UpdateCrouch()
    {
        if (Keyboard.current.cKey.isPressed)
            crouching = true;
        else if (crouching && CanStandUp())
            crouching = false;

        float targetHeight = crouching ? crouchHeight : standHeight;
        float height = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // keep the player's feet on the floor while the height changes
        controller.height = height;
        controller.center = new Vector3(controller.center.x, standCenterY - (standHeight - height) / 2f, controller.center.z);
        CameraDrop = standHeight - height;
    }

    // stops you from standing up into a desk or ceiling
    bool CanStandUp()
    {
        float radius = controller.radius * 0.95f;
        Vector3 origin = transform.TransformPoint(controller.center);
        float distance = standHeight - controller.height / 2f - radius;
        return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance, ~0, QueryTriggerInteraction.Ignore);
    }

    // temporary stamina display, Isabelle can replace with real UI
    void OnGUI()
    {
        GUI.Label(new Rect(20, 80, 300, 30), "Stamina: " + stamina.ToString("0.0") + (tired ? "  (tired)" : ""));
    }
}