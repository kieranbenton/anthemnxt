using System;

namespace AnthemNxt.Core
{
    /// <summary>
    /// Applying this attribute to your public methods makes it possible
    /// to invoke them from JavaScript in your client pages.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MethodAttribute : Attribute { }
}
