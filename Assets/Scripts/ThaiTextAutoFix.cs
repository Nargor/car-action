using UnityEngine;
using TMPro;

/// <summary>
/// Automatically fixes floating Thai vowels on TextMeshProUGUI components.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
[ExecuteAlways]
public class ThaiTextAutoFix : MonoBehaviour
{
    private TextMeshProUGUI tmpText;
    private string lastRawText;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        ApplyAdjustment();
    }

    void OnEnable()
    {
        ApplyAdjustment();
    }

    void Update()
    {
        if (tmpText == null) return;
        if (tmpText.text != lastRawText)
        {
            ApplyAdjustment();
        }
    }

    public void ApplyAdjustment()
    {
        if (tmpText == null) tmpText = GetComponent<TextMeshProUGUI>();
        if (tmpText == null || string.IsNullOrEmpty(tmpText.text)) return;

        if (System.Text.RegularExpressions.Regex.IsMatch(tmpText.text, @"[\u0E00-\u0E7F]"))
        {
            string adjusted = ThaiFontAdjuster.Adjust(tmpText.text);
            if (adjusted != tmpText.text)
            {
                lastRawText = adjusted;
                tmpText.text = adjusted;
            }
            else
            {
                lastRawText = tmpText.text;
            }
        }
        else
        {
            lastRawText = tmpText.text;
        }
    }
}
