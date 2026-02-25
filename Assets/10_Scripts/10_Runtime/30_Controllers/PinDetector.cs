using System.Collections.Generic;
using CircuitCraft.Core;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Pure logic for detecting nearest pins on components by grid proximity.
    /// </summary>
    public static class PinDetector
    {
        /// <summary>
        /// Finds the nearest pin on a single component within the maximum Manhattan distance.
        /// </summary>
        /// <param name="component">Component to evaluate.</param>
        /// <param name="mouseGridPos">Mouse position in grid coordinates.</param>
        /// <param name="pinRef">Nearest pin when found.</param>
        /// <param name="maxDistance">Maximum allowed Manhattan distance from the mouse position.</param>
        /// <returns>True when a nearest pin is found within max distance; otherwise false.</returns>
        public static bool TryGetNearestPin(PlacedComponent component, GridPosition mouseGridPos, out PinReference pinRef, int maxDistance = 1)
        {
            pinRef = default;
            if (component is null)
                return false;

            PinReference? bestPin = null;
            int bestDistance = int.MaxValue;

            foreach (var pin in component.Pins)
            {
                GridPosition pinWorld = component.GetPinWorldPosition(pin.PinIndex);
                int distance = pinWorld.ManhattanDistance(mouseGridPos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPin = new PinReference(component.InstanceId, pin.PinIndex, pinWorld);
                }
            }

            if (!bestPin.HasValue)
                return false;

            if (bestDistance > maxDistance)
                return false;

            pinRef = bestPin.Value;
            return true;
        }

        /// <summary>
        /// Finds the nearest pin across all components within the maximum Manhattan distance.
        /// </summary>
        /// <param name="components">Components to evaluate.</param>
        /// <param name="mouseGridPos">Mouse position in grid coordinates.</param>
        /// <param name="pinRef">Nearest pin when found.</param>
        /// <param name="maxDistance">Maximum allowed Manhattan distance from the mouse position.</param>
        /// <returns>True when a nearest pin is found within max distance; otherwise false.</returns>
        public static bool TryGetNearestPinFromAll(IReadOnlyList<PlacedComponent> components, GridPosition mouseGridPos, out PinReference pinRef, int maxDistance = 1)
        {
            pinRef = default;
            if (components is null || components.Count == 0)
                return false;

            PinReference? bestPin = null;
            int bestDistance = int.MaxValue;

            foreach (var component in components)
            {
                if (component is null)
                    continue;

                foreach (var pin in component.Pins)
                {
                    GridPosition pinWorld = component.GetPinWorldPosition(pin.PinIndex);
                    int distance = pinWorld.ManhattanDistance(mouseGridPos);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPin = new PinReference(component.InstanceId, pin.PinIndex, pinWorld);
                    }
                }
            }

            if (!bestPin.HasValue)
                return false;

            if (bestDistance > maxDistance)
                return false;

            pinRef = bestPin.Value;
            return true;
        }
    }
}
