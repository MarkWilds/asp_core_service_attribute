using System;
using System.Collections.Generic;

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

        public List<Type> Interfaces { get; }

        public ServiceAttribute(ServiceScope scope, params Type[] interfaces)
        {
            Scope = scope;
            Interfaces = new List<Type>(interfaces);
        }
    }
}