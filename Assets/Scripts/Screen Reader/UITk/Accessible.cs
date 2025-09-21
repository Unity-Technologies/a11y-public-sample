using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    public class Accessible
    {
        internal enum ValueSpecificity : byte
        {
            Default,
            Inline,
            OverriddenInUxml
        }
        
        internal struct Value<T>
        {
            private T m_Value;
            private ValueSpecificity m_Spec;

            public ValueSpecificity specificity => m_Spec;

            public bool defined => specificity != ValueSpecificity.Default;

            public Value(T value)
            {
                m_Value = value;
                m_Spec = ValueSpecificity.Default;
            }

            public T Get() => m_Value;

            public void Set(Accessible acc, T value, ValueSpecificity spec = ValueSpecificity.Inline)
            {
                m_Spec = spec;

                if (EqualityComparer<T>.Default.Equals(m_Value, value))
                    return;
                m_Value = value;
                acc?.owner.MarkDirtyRepaint();
                //acc?.owner.IncrementVersion(VersionChangeType.Accessibility);
            }
        }

        internal Value<bool> m_IsActive = new Value<bool>(true);
        internal Value<string> m_Label;
        internal Value<AccessibilityRole> m_Role;
        internal Value<bool> m_Ignored;
        internal Value<string> m_Value;
        internal Value<string> m_Hint;
        internal Value<bool> m_AllowsDirectInteraction;
        
        public VisualElement owner { get; internal set; }

        [CreateProperty]
        public bool ignored
        {
            get => m_Ignored.Get();
            set
            {
                m_Ignored.Set(this, value);
            }
        }

        [CreateProperty]
        public bool active
        {
            get => m_IsActive.Get();
            set
            {
                m_IsActive.Set(this, value);
            }
        }

        [CreateProperty]
        public string label
        {
            get => m_Label.Get();
            set
            {
                m_Label.Set(this, value);
            }
        }

        [CreateProperty]
        public AccessibilityRole role
        {
            get => m_Role.Get();
            set
            {
                m_Role.Set(this, value);
            }
        }

        [CreateProperty]
        public string value
        {
            get => m_Value.Get();
            set
            {
                m_Value.Set(this, value);
            }
        }

        [CreateProperty]
        public string hint
        {
            get => m_Hint.Get();
            set
            {
                m_Hint.Set(this, value);
            }
        }

        [CreateProperty]
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
        
        [Serializable]
        public new class UxmlSerializedData : UnityEngine.UIElements.UxmlSerializedData
        {
#pragma warning disable 649
            [SerializeField] private bool active = true;
            [SerializeField, UxmlIgnore, HideInInspector] UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags active_UxmlAttributeFlags;
            [SerializeField] private bool ignored;
            [SerializeField, UxmlIgnore, HideInInspector] UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags ignored_UxmlAttributeFlags;
            [SerializeField] private string label;
            [SerializeField, UxmlIgnore, HideInInspector] UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags label_UxmlAttributeFlags;
            [SerializeField] private AccessibilityRole role;
            [SerializeField, UxmlIgnore, HideInInspector] UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags role_UxmlAttributeFlags;
            [SerializeField] private string value;
            [SerializeField, UxmlIgnore, HideInInspector] UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags value_UxmlAttributeFlags;
            [SerializeField] private string hint;
            [SerializeField, UxmlIgnore, HideInInspector] UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags hint_UxmlAttributeFlags;
            [SerializeField] private bool allowsDirectInteraction;
            [SerializeField, UxmlIgnore, HideInInspector] UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags allowsDirectInteraction_UxmlAttributeFlags;
#pragma warning restore 649
            public override object CreateInstance() => new Accessible();

            void WriteField<T>(Accessible acc, ref Value<T> field, UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags flags, T value)
            {
                // Do not override inline value
                if (field.specificity == ValueSpecificity.Inline)
                    return;

                if (ShouldWriteAttributeValue(flags))
                    field.Set(acc, value, ValueSpecificity.OverriddenInUxml);
            }
            public override void Deserialize(object obj)
            {
                var acc = obj as Accessible;

                WriteField(acc, ref acc.m_IsActive, active_UxmlAttributeFlags, active);
                WriteField(acc, ref acc.m_Ignored, ignored_UxmlAttributeFlags, ignored);
                WriteField(acc, ref acc.m_Label, label_UxmlAttributeFlags, label);
                WriteField(acc, ref acc.m_Role, role_UxmlAttributeFlags, role);
                WriteField(acc, ref acc.m_Value, value_UxmlAttributeFlags, value);
                WriteField(acc, ref acc.m_Hint, hint_UxmlAttributeFlags, hint);
                WriteField(acc, ref acc.m_AllowsDirectInteraction, allowsDirectInteraction_UxmlAttributeFlags, allowsDirectInteraction);
            }
        }
    }

    public static class AccessibilityVisualElementExtensions
    {
        static Dictionary<VisualElement, Accessible> m_AttachedAccessibles = new();

        static Accessible GetAttachedAccessible(VisualElement ve)
        {
            if (m_AttachedAccessibles.TryGetValue(ve, out var acc))
            {
                return acc;
            }

            return null;
        }

        static Accessible AttachAccessible(VisualElement ve)
        {
            var accessible = new Accessible();
            accessible.owner = ve;
            m_AttachedAccessibles[ve] = accessible;
            return accessible;
        }

        public static Accessible GetAccessible(this VisualElement ve)
        {
            var acc = GetAttachedAccessible(ve);

            if (acc != null)
                return acc;

            var accElement = ve as AccessibleVisualElement;
            return accElement?.accessible;
        }

        public static Accessible GetOrCreateAccessible(this VisualElement ve)
        {
            return GetAccessible(ve) ?? AttachAccessible(ve);
        }
    }
}