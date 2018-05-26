using System;

namespace shared
{
    public enum ServiceScope
    {
        Singleton,
        Scoped,
        Transient
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        public ServiceScope Scope { get; }

        public bool ComponentOnly { get; }

        public ServiceAttribute(ServiceScope scope, bool componentOnly)
        {
            Scope = scope;
            ComponentOnly = componentOnly;
        }
    }
}