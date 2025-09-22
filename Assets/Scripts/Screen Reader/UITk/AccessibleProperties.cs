using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [UxmlObject]
    public partial class AccessibleProperties
    {
        internal struct Value<T>
        {
            private T m_Value;
            private bool m_Defined;

            public bool defined => m_Defined;

            public Value(T value)
            {
                m_Value = value;
                m_Defined = false;
            }

            public T Get() => m_Value;

            public void Set(AccessibleProperties acc, T value)
            {
                m_Defined = true;

                if (EqualityComparer<T>.Default.Equals(m_Value, value))
                    return;
                m_Value = value;
                acc?.owner?.MarkDirtyRepaint();
                //acc?.owner.IncrementVersion(VersionChangeType.Accessibility);
            }
        }

        internal Value<bool> m_IsActive = new Value<bool>(true);
        internal Value<string> m_Label;
        internal Value<AccessibilityRole> m_Role;
        internal Value<bool> m_Ignored;
        internal Value<bool> m_Modal;
        internal Value<string> m_Value;
        internal Value<string> m_Hint;
        internal Value<bool> m_AllowsDirectInteraction;
        
        public VisualElement owner { get; internal set; }

        [UxmlAttribute, CreateProperty]
        public bool ignored
        {
            get => m_Ignored.Get();
            set
            {
                m_Ignored.Set(this, value);
            }
        }
        
        [UxmlAttribute, CreateProperty]
        public bool modal
        {
            get => m_Modal.Get();
            set
            {
                m_Modal.Set(this, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public bool active
        {
            get => m_IsActive.Get();
            set
            {
                m_IsActive.Set(this, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public string label
        {
            get => m_Label.Get();
            set
            {
                m_Label.Set(this, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public AccessibilityRole role
        {
            get => m_Role.Get();
            set
            {
                m_Role.Set(this, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public string value
        {
            get => m_Value.Get();
            set
            {
                m_Value.Set(this, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public string hint
        {
            get => m_Hint.Get();
            set
            {
                m_Hint.Set(this, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public bool allowsDirectInteraction
        {
            get => m_AllowsDirectInteraction.Get();
            set
            {
                m_AllowsDirectInteraction.Set(this, value);
            }
        }

        public event Func<bool> selected;
        public Action incremented;
        public Action decremented;

        internal bool InvokeSelected()
        {
            return selected?.Invoke() ?? false;
        }

        internal void InvokeIncremented()
        {
            incremented?.Invoke();
        }

        internal void InvokeDecremented()
        {
            decremented?.Invoke();
        }
    }

    public static class AccessibilityVisualElementExtensions
    {
        static Dictionary<VisualElement, AccessibleProperties> m_AttachedAccessibleProperties = new();

        static AccessibleProperties GetAttachedAccessibleProperties(VisualElement ve)
        {
            if (m_AttachedAccessibleProperties.TryGetValue(ve, out var acc))
            {
                return acc;
            }

            return null;
        }

        static AccessibleProperties AttachAccessibleProperties(VisualElement ve)
        {
            var accessible = new AccessibleProperties();
            accessible.owner = ve;
            m_AttachedAccessibleProperties[ve] = accessible;
            return accessible;
        }

        public static AccessibleProperties GetAccessibleProperties(this VisualElement ve)
        {
            var acc = GetAttachedAccessibleProperties(ve);

            if (acc != null)
                return acc;

            var accElement = ve as AccessibleVisualElement;
            return accElement?.accessible;
        }

        public static AccessibleProperties GetOrCreateAccessibleProperties(this VisualElement ve)
        {
            return GetAccessibleProperties(ve) ?? AttachAccessibleProperties(ve);
        }
    }
}