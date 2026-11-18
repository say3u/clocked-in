using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 3f;
    public float mouseSensitivity = 0.1f;
    public Transform cam;

    CharacterController controller;
    float lookUp;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
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
        controller.SimpleMove(move.normalized * speed);
    }
}