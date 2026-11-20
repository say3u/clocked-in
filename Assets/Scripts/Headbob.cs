using UnityEngine;

public class HeadBob : MonoBehaviour
{
    public float bobSpeed = 10f;     // how fast it bobs
    public float bobAmount = 0.05f;  // how far it moves up and down

    CharacterController controller;
    float startY;
    float timer;

    void Start()
    {
        controller = GetComponentInParent<CharacterController>();
        startY = transform.localPosition.y;
    }

    void Update()
    {
        Vector3 pos = transform.localPosition;
        Vector3 v = controller.velocity;
        float moving = new Vector2(v.x, v.z).magnitude;

        if (moving > 0.1f)
        {
            // walking: bob up and down
            timer += Time.deltaTime * bobSpeed;
            pos.y = startY + Mathf.Sin(timer) * bobAmount;
        }
        else
        {
            // standing still: smoothly go back to normal
            timer = 0;
            pos.y = Mathf.Lerp(pos.y, startY, Time.deltaTime * 8f);
        }

        transform.localPosition = pos;
    }
}