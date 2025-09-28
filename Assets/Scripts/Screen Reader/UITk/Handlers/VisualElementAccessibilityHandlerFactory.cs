using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    public static class VisualElementAccessibilityHandlerFactory
    {
        internal interface ICreator
        {
            bool CanCreate(VisualElement element);

            VisualElementAccessibilityHandler Create(VisualElement element);
        }

        class Creator : ICreator
        {
            Type m_VisualElementType;
            Type m_AccessibleElementType;

            public bool CanCreate(VisualElement element) => m_VisualElementType.IsAssignableFrom(element.GetType());

            public VisualElementAccessibilityHandler Create(VisualElement element)
            {
                return CreateInternal();
            }

            public Creator(Type visualElementType, Type accessibleElementType)
            {
                m_VisualElementType = visualElementType;
                m_AccessibleElementType = accessibleElementType;
            }

            VisualElementAccessibilityHandler CreateInternal()
            {
                return Activator.CreateInstance(m_AccessibleElementType) as VisualElementAccessibilityHandler;
            }
        }

        class Creator<T> : ICreator where T: VisualElement
        {
            public Func<VisualElement, VisualElementAccessibilityHandler> func;

            public bool CanCreate(VisualElement element) => element is T;

            public VisualElementAccessibilityHandler Create(VisualElement element)
            {
                return func.Invoke(element);
            }

            public Creator(Func<VisualElement, VisualElementAccessibilityHandler> func)
            {
                this.func = func;
            }
        }

        class GenericCreator : ICreator
        {
            Type m_VisualElementGenericTypeDefinition;
            Type m_AccessibleElementGenericTypeDefinition;

            public GenericCreator(Type visualElementTypeDef, Type accessibleElemTypeDef)
            {
                m_VisualElementGenericTypeDefinition = visualElementTypeDef;
                m_AccessibleElementGenericTypeDefinition = accessibleElemTypeDef;
            }

            public bool CanCreate(VisualElement element)
            {
                var type = element.GetType();

                // Find the exact BaseField<> class type.
                while (true)
                {
                    if (type == typeof(VisualElement))
                    {
                        break;
                    }

                    if (type.IsGenericType && type.GetGenericTypeDefinition() == m_VisualElementGenericTypeDefinition)
                    {
                        return true;
                    }

                    type = type.BaseType;
                }

                return false;
            }

            public VisualElementAccessibilityHandler Create(VisualElement element)
            {
                var type = element.GetType();

                // Find the exact BaseField<> class type is
                while (!(type.IsGenericType && type.GetGenericTypeDefinition() == m_VisualElementGenericTypeDefinition))
                {
                    type = type.BaseType;
                }

                var args = type.GetGenericArguments();
                var accessibleElementType = m_AccessibleElementGenericTypeDefinition.MakeGenericType(args);

                return Activator.CreateInstance(accessibleElementType) as VisualElementAccessibilityHandler;
            }
        }

        static List<ICreator> s_Factories = new();
        public static void RegisterFactory<TVISUELEMENT_TYPE, TELEMENT_TYPE>()
            where TVISUELEMENT_TYPE: VisualElement
            where TELEMENT_TYPE : VisualElementAccessibilityHandler,
            new()
        {
            RegisterFactory(new Creator<TVISUELEMENT_TYPE>(_ => new TELEMENT_TYPE()));
        }

        public static void RegisterFactory(Type visualElementType, Type accessibleElementType)
        {
            RegisterFactory(new Creator(visualElementType, accessibleElementType));
        }

        public static void RegisterGenericFactory(Type visualElementTypeDefinition, Type accessibleElementTypeDefinition)
        {
            RegisterFactory(new GenericCreator(visualElementTypeDefinition, accessibleElementTypeDefinition));
        }

        internal static void RegisterFactory(ICreator creator)
        {
            s_Factories.Add(creator);
        }

        public static bool HasFactory(VisualElement element)
        {
            return FindFactory(element) != null;
        }

        static ICreator FindFactory(VisualElement element)
        {
            RegisterBuiltinAccessibleElementFactories();

            // Look for factory in reverse order because factories registered the last have priority.
            for (var i = s_Factories.Count - 1; i >= 0; --i)
            {
                var factory = s_Factories[i];

                if (factory.CanCreate(element))
                {
                    return factory;
                }
            }

            return null;
        }

        public static VisualElementAccessibilityHandler Create(VisualElement element)
        {
            var factory = FindFactory(element);
            VisualElementAccessibilityHandler acc;

            if (factory == null && element.GetAccessibleProperties() != null)
            {
                acc = new VisualElementAccessibilityHandler();
            }
            else
            {
                acc = factory?.Create(element);
            }

            if (acc != null)
            {
                acc.ownerElement = element;
            }

            return acc;
        }

        public static void RegisterBuiltinAccessibleElementFactories()
        {
            if (s_Factories.Count > 0)
            {
                return;
            }

            RegisterFactory<Label, LabelHandler>();
            RegisterFactory<Button, ButtonHandler>();
            RegisterGenericFactory(typeof(BaseField<>), typeof(BaseFieldHandler<>));
            RegisterGenericFactory(typeof(BaseSlider<>), typeof(BaseSliderHandler<>));
            RegisterFactory<TextField, TextFieldFieldHandler>();
            RegisterFactory<DropdownField, DropdownFieldHandler>();
            RegisterFactory<ListView, ListViewHandler>();
            RegisterFactory<ScrollView, ScrollViewHandler>();
            RegisterFactory(new ListViewItemHandlerCreator());
            RegisterFactory(new TabHandlerCreator());
            RegisterFactoriesFromUserAssemblies();
        }

        static void RegisterFactoriesFromUserAssemblies()
        {
            // Get all user assemblies (ones that are not part of Unity by default)
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // In the Player, we filter assemblies to only introspect types of user assemblies
                // which will exclude Unity builtin assemblies (i.e. runtime modules).
                if (assembly.FullName.StartsWith("Unity") ||
                    assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("mscorlib") ||
                    assembly.FullName.StartsWith("netstandard"))
                {
                    continue;
                }

                RegisterAllFactoriesInAssembly(assembly);
            }
        }

        static void RegisterAllFactoriesInAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(VisualElementAccessibilityHandler).IsAssignableFrom(type))
                {
                    continue;
                }

                var regAttributes = (RegisterAccessibilityHandlerAttribute[])type.GetCustomAttributes(typeof(RegisterAccessibilityHandlerAttribute), true);

                foreach (var regAttr in regAttributes)
                {
                    RegisterFactory(regAttr.Type, type);
                }
            }
        }
    }
}
