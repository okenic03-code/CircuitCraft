using CircuitCraft.Data;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Lightweight cross-scene holder for the currently selected stage.
    /// </summary>
    public static class StageSelectionContext
    {
        public static StageDefinition SelectedStage { get; private set; }

        public static void SetSelectedStage(StageDefinition stage)
        {
            SelectedStage = stage;
        }

        public static void Clear()
        {
            SelectedStage = null;
        }
    }
}
