using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;

public class FontManager : MonoBehaviour
{
    public TMP_FontAsset defaultFont;
    public TMP_FontAsset boldFont;

    const float k_DefaultLetterCardFontSize = 132f;
    const float k_DefaultHeaderFontSize = 75f;
    const float k_DefaultFontSize = 64f;

    List<TMP_Text> m_LetterCardTextComponents = new();
    List<TMP_Text> m_HeaderTextComponents = new();
    List<TMP_Text> m_TextComponents = new();
    
    void Start()
    {
        FindAllTextComponents();
        
        // Note: On Android, AccessibilitySettings.isBoldTextEnabled requires at least Android 12 (API level 31).
        UpdateFontStyle(AccessibilitySettings.isBoldTextEnabled ? boldFont : defaultFont);
        UpdateFontScale(AccessibilitySettings.fontScale);
    }

    void OnEnable()
    {
        // Note: AccessibilitySettings.boldTextStatusChanged is only available on iOS.
        // On Android, the app restarts when AccessibilitySettings.isBoldTextEnabled changes.
        AccessibilitySettings.boldTextStatusChanged += OnBoldTextStatusChanged;
        AccessibilitySettings.fontScaleChanged += OnFontScaleChanged;
    }

    void OnDisable()
    {
        AccessibilitySettings.boldTextStatusChanged -= OnBoldTextStatusChanged;
        AccessibilitySettings.fontScaleChanged -= OnFontScaleChanged;
    }

    void FindAllTextComponents()
    {
        var texts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);

        foreach (var text in texts)
        {
            if (text.fontSize == k_DefaultLetterCardFontSize)
            {
                m_LetterCardTextComponents.Add(text);
            }
            else if (text.fontSize == k_DefaultHeaderFontSize)
            {
                m_HeaderTextComponents.Add(text);
            }
            else
            {
                m_TextComponents.Add(text);
            }
        }
    }

    void OnBoldTextStatusChanged(bool status)
    {
        UpdateFontStyle(status ? boldFont : defaultFont);
    }

    void UpdateFontStyle(TMP_FontAsset font)
    {
        foreach (var text in m_LetterCardTextComponents)
        {
            text.font = font;
        }
        
        foreach (var text in m_HeaderTextComponents)
        {
            text.font = font;
        }

        foreach (var text in m_TextComponents)
        {
            text.font = font;
        }
    }

    void OnFontScaleChanged(float fontScale)
    {
        UpdateFontScale(fontScale);
    }

    void UpdateFontScale(float fontScale)
    {
        foreach (var tmpText in m_LetterCardTextComponents)
        {
            tmpText.fontSize = fontScale * k_DefaultLetterCardFontSize;
        }
        
        foreach (var tmpText in m_HeaderTextComponents)
        {
            tmpText.fontSize = fontScale * k_DefaultHeaderFontSize;
        }

        foreach (var tmpText in m_TextComponents)
        {
            tmpText.fontSize = fontScale * k_DefaultFontSize;
        }
    }
}
