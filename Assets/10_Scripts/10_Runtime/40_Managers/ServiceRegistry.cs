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
    }
}
