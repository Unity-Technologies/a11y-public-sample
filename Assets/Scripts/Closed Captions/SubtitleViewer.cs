using Unity.Samples.ScreenReader;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Samples.ClosedCaptions
{
    /// <summary>
    /// This class is responsible for displaying subtitles through a UIDocument.
    /// It sets the style of the subtitle view based on the current display settings.
    /// </summary>
    class SubtitleViewer : MonoBehaviour
    {
        public SubtitlePlayer player;
        public UIDocument surface;
        public SubtitleDisplaySettings displaySettings;

        ListView m_SubtitleView;
        VisualElement m_CachedRootVisualElement;
        SubtitlePlayer m_CachedPlayer;

        public SubtitleDisplaySettings currentDisplaySettings => displaySettings == null ?
            SubtitleDisplaySettings.GetDefault() : displaySettings;

        void CreateSubtitleView()
        {
            m_SubtitleView = new ListView
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0, right = 0, bottom = 0,
                    marginBottom = 10
                },
                makeItem = () =>
                {
                    var itemContainer = new VisualElement
                    {
                        style =
                        {
                            justifyContent = Justify.Center,
                            alignItems = Align.Center
                        }
                    };

                    var label = new Label();
                    itemContainer.Add(label);
                    return itemContainer;
                },
                bindItem = (element, i) =>
                {
                    var label = element.Q<Label>();
                    ApplyStyle(label);
                    label.text = m_SubtitleView.itemsSource[i]?.ToString();
                },
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
        }

        void ApplyStyle()
        {
            if (m_SubtitleView == null)
            {
                return;
            }

            SetPickingModeToIgnore(m_CachedRootVisualElement);

            m_SubtitleView.style.opacity = currentDisplaySettings.opacity;

            if (currentDisplaySettings.font)
            {
                m_SubtitleView.style.unityFontDefinition = new FontDefinition
                {
                    fontAsset = displaySettings.font
                };
            }

            m_SubtitleView.style.fontSize = currentDisplaySettings.fontSize;
            m_SubtitleView.style.unityFontStyleAndWeight = currentDisplaySettings.fontStyle;
            m_SubtitleView.style.textShadow = currentDisplaySettings.useDropShadow ?
                new TextShadow
                {
                    color = currentDisplaySettings.dropShadowColor,
                    offset = new Vector2(5, 5)
                } :
                new TextShadow();
        }

        void ApplyStyle(Label label)
        {
            label.style.color = currentDisplaySettings.color;
            label.style.backgroundColor = currentDisplaySettings.backgroundColor;
        }

        void UpdateCachedPlayer()
        {
            if (m_CachedPlayer == player)
            {
                return;
            }

            if (m_CachedPlayer)
            {
                DisconnectFromPlayer();
            }

            m_CachedPlayer = player;

            if (m_CachedPlayer)
            {
                ConnectToPlayer();
            }
        }

        void ConnectToPlayer()
        {
            player.currentItemChanged += OnCurrentItemChanged;
        }

        void OnCurrentItemChanged(SubtitleItem item)
        {
            m_SubtitleView.itemsSource = item?.lines;
            m_SubtitleView.Rebuild();
        }

        void DisconnectFromPlayer()
        {
            player.currentItemChanged -= OnCurrentItemChanged;
        }

        void UpdateRootVisualElement()
        {
            var rootVisualElement = surface != null ? surface.rootVisualElement : null;

            if (m_CachedRootVisualElement == rootVisualElement)
            {
                return;
            }

            if (m_CachedRootVisualElement != null)
            {
                m_SubtitleView?.RemoveFromHierarchy();
            }

            m_CachedRootVisualElement = rootVisualElement;

            if (m_CachedRootVisualElement != null)
            {
                if (m_SubtitleView == null)
                {
                    CreateSubtitleView();
                }
                m_CachedRootVisualElement.GetOrCreateAccessibleProperties().ignored = true;
                m_CachedRootVisualElement.Add(m_SubtitleView);
            }
        }

        static void SetPickingModeToIgnore(VisualElement rootElement)
        {
            if (rootElement == null)
            {
                return;
            }

            foreach (var child in rootElement.hierarchy.Children())
            {
                child.pickingMode = PickingMode.Ignore;

                // Prevent the input interaction with the subtitles UI.
                SetPickingModeToIgnore(child);
            }
        }

        void Update()
        {
            UpdateCachedPlayer();
            UpdateRootVisualElement();
            ApplyStyle();
        }
    }
}
