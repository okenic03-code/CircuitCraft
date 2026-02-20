using System;
using System.Collections.Generic;

namespace CircuitCraft.Managers
{
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static GameManager GameManager => Resolve<GameManager>();
        public static SimulationManager SimulationManager => Resolve<SimulationManager>();

        /// <summary>
        /// Registers a service instance. Prefer calling in Awake.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            Type serviceType = typeof(T);

            if (service == null)
            {
                _services.Remove(serviceType);
                return;
            }

            _services[serviceType] = service;
        }

        /// <summary>
        /// Resolves a previously registered service. Prefer calling in Start or later.
        /// </summary>
        public static T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return service as T;
            }

            return null;
        }

        public static void Unregister<T>(T service) where T : class
        {
            Type serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out object registeredService) && ReferenceEquals(registeredService, service))
            {
                _services.Remove(serviceType);
            }
        }

        public static void Register(GameManager gameManager)
        {
            Register<GameManager>(gameManager);
        }

        public static void Register(SimulationManager simulationManager)
        {
            Register<SimulationManager>(simulationManager);
        }

        public static void Unregister(GameManager gameManager)
        {
            Unregister<GameManager>(gameManager);
        }

        public static void Unregister(SimulationManager simulationManager)
        {
            Unregister<SimulationManager>(simulationManager);
        }
    }
}
