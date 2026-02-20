namespace CircuitCraft.Managers
{
    public static class ServiceRegistry
    {
        public static GameManager GameManager { get; private set; }
        public static SimulationManager SimulationManager { get; private set; }

        public static void Register(GameManager gameManager)
        {
            GameManager = gameManager;
        }

        public static void Register(SimulationManager simulationManager)
        {
            SimulationManager = simulationManager;
        }

        public static void Unregister(GameManager gameManager)
        {
            if (GameManager == gameManager)
                GameManager = null;
        }

        public static void Unregister(SimulationManager simulationManager)
        {
            if (SimulationManager == simulationManager)
                SimulationManager = null;
        }
    }
}
