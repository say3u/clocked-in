using UnityEngine;
using UnityEngine.UI;

// Animated TV static with horizontal streaks. Put on a RawImage that stretches over the whole screen.
// Pre-generates a few noise frames and flips between them.
[RequireComponent(typeof(RawImage))]
public class StaticNoise : MonoBehaviour
{
    [SerializeField] int width = 256;
    [SerializeField] int height = 270;
    [SerializeField] int frameCount = 6;
    [SerializeField] float fps = 12f;
    [SerializeField, Range(0f, 0.98f)] float streakLength = 0.9f; // 0 = plain grain, higher = longer horizontal streaks

    RawImage image;
    Texture2D[] frames;
    float timer;
    int index;

    void Awake()
    {
        image = GetComponent<RawImage>();
        frames = new Texture2D[frameCount];

        for (int f = 0; f < frameCount; f++)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; // chunky pixels, more retro
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                byte v = (byte)Random.Range(0, 256);
                for (int x = 0; x < width; x++)
                {
                    // Mostly repeat the previous pixel in the row, sometimes pick a new brightness
                    if (Random.value > streakLength) v = (byte)Random.Range(0, 256);
                    pixels[y * width + x] = new Color32(v, v, v, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            frames[f] = tex;
        }
        image.texture = frames[0];
    }

    void Update()
    {
        // unscaled so it keeps animating when the game is paused (reusable for the pause menu)
        timer += Time.unscaledDeltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            index = (index + 1) % frames.Length;
            image.texture = frames[index];
        }
    }

    void OnDestroy()
    {
        if (frames == null) return;
        foreach (var t in frames) if (t != null) Destroy(t);
    }
}