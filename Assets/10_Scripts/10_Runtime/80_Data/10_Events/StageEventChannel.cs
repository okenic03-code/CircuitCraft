using UnityEngine;

namespace CircuitCraft.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for stage-related string messages.
    /// </summary>
    [CreateAssetMenu(fileName = "StageEventChannel", menuName = "CircuitCraft/Events/Stage Event Channel")]
    public class StageEventChannel : EventChannel<string>
    {
    }
}
