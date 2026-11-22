using UnityEngine;
using UnityEngine.UI;

// Soft translucent band that slowly scrolls top to bottom and loops.
// Setup: anchors top-stretch (min 0,1 / max 1,1), pivot (0.5, 1), Left/Right = 0, Height ~200.
// Must be a direct child of a full-screen RectTransform (e.g. the canvas).
// Put it on an Image with Source Image: None. The soft gradient is generated automatically.
public class ScanLine : MonoBehaviour
{
    [SerializeField] float secondsPerPass = 7f;   // higher = slower and creepier
    [SerializeField] bool softEdges = true;       // fades the top and bottom so it blends in
    [SerializeField] int gradientHeight = 128;

    RectTransform rt;
    RectTransform parent;
    float t;

    void Awake()
    {
        rt = (RectTransform)transform;
        parent = (RectTransform)transform.parent;

        var img = GetComponent<Image>();
        if (softEdges && img != null)
        {
            // 1px-wide vertical gradient: transparent -> opaque in the middle -> transparent
            var tex = new Texture2D(1, gradientHeight, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < gradientHeight; y++)
            {
                float a = Mathf.Sin(Mathf.PI * y / (gradientHeight - 1f)); // 0 -> 1 -> 0
                tex.SetPixel(0, y, new Color(1f, 1f, 1f, a * a));          // squared = softer falloff
            }
            tex.Apply();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, 1, gradientHeight), new Vector2(0.5f, 0.5f), 100f);
            img.type = Image.Type.Simple;
        }
    }

    void Update()
    {
        t += Time.unscaledDeltaTime / secondsPerPass;
        if (t > 1f) t -= 1f;

        float barHeight = rt.rect.height;
        float y = Mathf.Lerp(barHeight, -parent.rect.height, t); // starts just above screen, ends fully below
        rt.anchoredPosition = new Vector2(0f, y);
    }
}