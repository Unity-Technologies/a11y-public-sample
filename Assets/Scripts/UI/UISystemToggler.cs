using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class UISystemToggler : MonoBehaviour
{
    public Canvas uguiCanvas;
    public UIDocument uitkDocument;

    public bool useUIToolkit;

    public const string useUIToolkitPreference = "UseUIToolkit";

    bool m_Initialized;

    void Awake()
    {
        m_Initialized = true;

#if UNITY_EDITOR
        // Load the saved preference when a scene is opened.
        useUIToolkit = PlayerPrefs.GetInt(useUIToolkitPreference) == 1;
#else
        PlayerPrefs.SetInt(useUIToolkitPreference, useUIToolkit ? 1 : 0);
#endif

        ToggleUISystem();
    }

    void OnValidate()
    {
        // Only save the preference when the user changes it in the Inspector, not during script load.
        if (!m_Initialized)
        {
            return;
        }

        PlayerPrefs.SetInt(useUIToolkitPreference, useUIToolkit ? 1 : 0);

#if UNITY_EDITOR
        // Delay canvas hierarchy changes to avoid issues during OnValidate.
        EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                ToggleUISystem();
            }
        };
#endif
    }

    void ToggleUISystem()
    {
        if (uguiCanvas == null)
        {
            Debug.LogError($"{nameof(uguiCanvas)} is not assigned for {GetType().Name}.");
            return;
        }

        if (uitkDocument == null)
        {
            Debug.LogError($"{nameof(uitkDocument)} is not assigned for {GetType().Name}.");
            return;
        }

        uguiCanvas.gameObject.SetActive(!useUIToolkit);
        uitkDocument.gameObject.SetActive(useUIToolkit);
    }
}
