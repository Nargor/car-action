using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RacerOverheadUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Canvas worldCanvas;
    public RawImage avatarImage;
    public TextMeshProUGUI nameText;
    public Image nameBadgeBg;
    public GameObject nitroBadge;
    public GameObject prankBadge;
    public TextMeshProUGUI prankText;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0f, 1.85f, 0f);

    private Transform targetCar;
    private Camera mainCam;

    void Start()
    {
        targetCar = transform.parent;
        mainCam = Camera.main;

        if (nitroBadge != null) nitroBadge.SetActive(false);
        if (prankBadge != null) prankBadge.SetActive(false);
    }

    void LateUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // Position above car
        if (targetCar != null)
        {
            transform.position = targetCar.position + offset;
        }

        // Always billboard face the camera
        transform.rotation = mainCam.transform.rotation;
    }

    public void SetRacer(string username, Color carColor, Texture2D avatarTex = null)
    {
        if (nameText != null)
            nameText.text = username;

        if (nameBadgeBg != null)
        {
            Color c = carColor;
            c.a = 0.88f;
            nameBadgeBg.color = c;
        }

        if (avatarImage != null)
        {
            if (avatarTex != null)
            {
                avatarImage.texture = avatarTex;
            }
            else
            {
                // Generate a colored circle avatar with initials if no photo
                avatarImage.texture = GenerateInitialAvatar(username, carColor);
            }
        }
    }

    public void ShowNitro(bool active)
    {
        if (nitroBadge != null)
            nitroBadge.SetActive(active);
    }

    public void ShowPrank(string prankType, bool active)
    {
        if (prankBadge != null)
        {
            prankBadge.SetActive(active);
            if (prankText != null && active)
            {
                prankText.text = prankType.ToUpper() + "!";
            }
        }
    }

    private Texture2D GenerateInitialAvatar(string name, Color bgCol)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    // Gradient inside circle
                    float factor = 0.8f + 0.2f * (y / (float)size);
                    cols[y * size + x] = new Color(bgCol.r * factor, bgCol.g * factor, bgCol.b * factor, 1f);
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }
}
