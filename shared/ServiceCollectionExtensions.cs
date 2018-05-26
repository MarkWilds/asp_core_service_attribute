using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using shared;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void ScanAssembly(this IServiceCollection serviceCollection)
        {
            ScanAssembly(serviceCollection, Assembly.GetEntryAssembly());
        }

        /// <summary>
        /// Find any class with the ServiceAttribute and adds it as a service
        /// </summary>
        public static void ScanAssembly(this IServiceCollection serviceCollection, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            foreach (Type type in assembly.GetTypes())
            {
                ServiceAttribute serviceAttribute =
                    type.GetCustomAttribute<ServiceAttribute>();

                if (serviceAttribute != null)
                {
                    switch (serviceAttribute.Scope)
                    {
                        case ServiceScope.Singleton:
                            AddService("AddSingleton", serviceCollection, type, serviceAttribute);
                            break;
                        case ServiceScope.Scoped:
                            AddService("AddScoped", serviceCollection, type, serviceAttribute);
                            break;
                        case ServiceScope.Transient:
                            AddService("AddTransient", serviceCollection, type, serviceAttribute);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the serviceType as service
        /// </summary>
        private static void AddService(string methodName, IServiceCollection serviceCollection, Type serviceType,
            ServiceAttribute attribute)
        {
            try
            {
                List<Type> servicesTypes = new List<Type>();
                Type[] interfaces = serviceType.GetInterfaces();
                int genericArguments = attribute.ComponentOnly ? 1 : 2;

                if (interfaces.Any() && !attribute.ComponentOnly)
                    servicesTypes.AddRange(interfaces);
                servicesTypes.Add(serviceType);

                IEnumerable<MethodInfo> methodInfos = from method in GetExtensionMethods(serviceCollection)
                    where method.Name == methodName && method.IsGenericMethod 
                          && method.GetGenericArguments().Length == genericArguments 
                    select method;

                MethodInfo extensionMethod = methodInfos.First();
                MethodInfo genericExtensionMethod = extensionMethod.MakeGenericMethod(servicesTypes.ToArray());
                genericExtensionMethod.Invoke(serviceCollection, new object[] {serviceCollection});

                Console.WriteLine($"Registered service: {serviceType.Namespace}.{serviceType.Name}");
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"Failed to register service : {serviceType.Namespace}.{serviceType.Name}");
            }
        }

        /// <summary>
        /// This method extends the System.Type-type to get all extended methods.
        /// It searches hereby in all assemblies
        /// </summary>
        /// <returns>returns MethodInfo[] with the extended method</returns>
        private static MethodInfo[] GetExtensionMethods(IServiceCollection serviceCollection)
        {
            Type t = serviceCollection.GetType();
            List<Type> assTypes = new List<Type>();
            assTypes.AddRange(Assembly
                .GetEntryAssembly()
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .SelectMany(x => x.DefinedTypes));

            // only find extension methods in assemblies
            IEnumerable<MethodInfo> query = from type in assTypes
                where type.IsSealed && !type.IsGenericType && !type.IsNested
                from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                where method.IsDefined(typeof(ExtensionAttribute), false) &&
                      typeof(IServiceCollection).IsAssignableFrom(t)
                select method;

            return query.ToArray();
        }
    }
}