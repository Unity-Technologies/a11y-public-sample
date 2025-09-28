using System;

namespace Unity.Samples.ScreenReader
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class RegisterAccessibilityHandlerAttribute : Attribute
    {
        public Type Type { get; set; }

        public RegisterAccessibilityHandlerAttribute(Type type) => Type = type;
    }
}
