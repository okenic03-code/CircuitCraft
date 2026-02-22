using CircuitCraft.Core;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Provides pure placement validation helpers for board coordinates.
    /// </summary>
    public static class PlacementValidator
    {
        /// <summary>
        /// Determines whether a component can be placed at the specified board position.
        /// </summary>
        /// <param name="boardState">Current board state to validate against.</param>
        /// <param name="position">Candidate grid position for placement.</param>
        /// <returns>True when the position is available or board state is unavailable; otherwise false.</returns>
        public static bool IsValidPlacement(BoardState boardState, GridPosition position)
        {
            if (boardState is null)
            {
                return true;
            }

            return !boardState.IsPositionOccupied(position);
        }
    }
}
