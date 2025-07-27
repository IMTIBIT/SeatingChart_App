using System;
using System.Collections.Generic;
using SeatingChartApp.Runtime.Systems; // For DebugManager

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Implements a Service Locator pattern. This static class holds references
    /// to all major manager instances, allowing for a decoupled architecture
    /// where systems can request dependencies without needing a direct reference.
    /// </summary>
    public static class ServiceProvider
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// Registers a service instance, making it available to the rest of the application.
        /// </summary>
        /// <typeparam name="T">The type of the service to register.</typeparam>
        /// <param name="service">The instance of the service.</param>
        public static void Register<T>(T service)
        {
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                DebugManager.LogWarning(LogCategory.General, $"Service of type {type.Name} is already registered.");
                return;
            }
            services[type] = service;
            DebugManager.Log(LogCategory.General, $"Service registered: {type.Name}");
        }

        /// <summary>
        /// Retrieves a registered service instance.
        /// </summary>
        /// <typeparam name="T">The type of the service to retrieve.</typeparam>
        /// <returns>The registered service instance, or null if not found.</returns>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (!services.TryGetValue(type, out var service))
            {
                DebugManager.LogError(LogCategory.General, $"Service of type {type.Name} not found.");
                return null;
            }
            return service as T;
        }

        /// <summary>
        /// Unregisters a service, removing it from the locator.
        /// </summary>
        /// <typeparam name="T">The type of service to unregister.</typeparam>
        public static void Unregister<T>()
        {
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                services.Remove(type);
                DebugManager.Log(LogCategory.General, $"Service unregistered: {type.Name}");
            }
        }
    }
}
