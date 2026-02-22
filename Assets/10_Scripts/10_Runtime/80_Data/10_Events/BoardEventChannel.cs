using UnityEngine;

namespace CircuitCraft.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for board-related string messages.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardEventChannel", menuName = "CircuitCraft/Events/Board Event Channel")]
    public class BoardEventChannel : EventChannel<string>
    {
    }
}
