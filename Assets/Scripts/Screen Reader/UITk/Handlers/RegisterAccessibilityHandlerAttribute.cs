using System;
using System.Collections.Generic;
using UnityEngine.Accessibility;

namespace Unity.Samples.ScreenReader
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class RegisterAccessibilityHandlerAttribute : Attribute
    {
        private Type type;

        public Type Type
        {
            get => type;
            set => type = value;
        }
        public RegisterAccessibilityHandlerAttribute(Type type) => this.type = type;
    }
}