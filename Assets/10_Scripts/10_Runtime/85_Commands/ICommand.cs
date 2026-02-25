namespace CircuitCraft.Commands
{
    /// <summary>
    /// Defines reversible board mutation behavior for undo/redo workflows.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Applies the command mutation.
        /// </summary>
        void Execute();

        /// <summary>
        /// Reverts the mutation applied by <see cref="Execute"/>.
        /// </summary>
        void Undo();

        /// <summary>
        /// Gets a user-facing description of the command.
        /// </summary>
        string Description { get; }
    }
}
