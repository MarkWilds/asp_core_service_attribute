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
        private const int AssemblyRecursiveDepth = 1;
        
        /// <summary>
        /// Find any class with the ServiceAttribute and adds it as a service
        /// </summary>
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

            CheckAssembly(serviceCollection, assembly);
        }

        private static void CheckAssembly(IServiceCollection serviceCollection, Assembly assembly, int depth = 0)
        {
            if (depth++ == AssemblyRecursiveDepth)
                return;

            IEnumerable<TypeInfo> types = assembly.DefinedTypes;
            CheckTypesForAttribute(serviceCollection, types);

            AssemblyName[] assemblies = assembly.GetReferencedAssemblies();
            foreach (AssemblyName subAssembly in assemblies)
            {
                CheckAssembly(serviceCollection, Assembly.Load(subAssembly), depth);    
            }
        }

        private static void CheckTypesForAttribute(IServiceCollection serviceCollection, IEnumerable<TypeInfo> types)
        {
            foreach (TypeInfo type in types)
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

        private static void AddService(string methodName, IServiceCollection serviceCollection, Type serviceType,
            ServiceAttribute attribute)
        {
            try
            {
                List<Type> servicesTypes = new List<Type>();
                if (attribute.Interfaces.Any())
                {
                    foreach (Type @interface in attribute.Interfaces)
                    {
                        servicesTypes.Add(@interface);
                        servicesTypes.Add(serviceType);

                        MethodInfo genericExtensionMethod =
                            GetGenericExtensionMethod(methodName, 2, serviceCollection, servicesTypes.ToArray());
                        genericExtensionMethod.Invoke(serviceCollection, new object[] {serviceCollection});

                        servicesTypes.Clear();

                        Console.WriteLine(
                            $"Registered service: {serviceType.Namespace}.{serviceType.Name} with interface " +
                            $"{@interface.Namespace}.{@interface.Name}");
                    }
                }
                else
                {
                    servicesTypes.Add(serviceType);
                    MethodInfo genericExtensionMethod =
                        GetGenericExtensionMethod(methodName, 1, serviceCollection, servicesTypes.ToArray());
                    genericExtensionMethod.Invoke(serviceCollection, new object[] {serviceCollection});

                    Console.WriteLine($"Registered service: {serviceType.Namespace}.{serviceType.Name}");
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"Failed to register service : {serviceType.Namespace}.{serviceType.Name}", ex);
            }
        }

        private static MethodInfo GetGenericExtensionMethod(string methodName, int generics,
            IServiceCollection serviceCollection, params Type[] types)
        {
            MethodInfo extensionMethod = (from method in GetExtensionMethods(serviceCollection)
                where method.Name == methodName && method.IsGenericMethod
                                                && method.GetGenericArguments().Length == generics
                select method).First();

            return extensionMethod.MakeGenericMethod(types);
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