using UnityEngine;

namespace CircuitCraft.Data.Events
{
    [CreateAssetMenu(fileName = "BoardEventChannel", menuName = "CircuitCraft/Events/Board Event Channel")]
    public class BoardEventChannel : EventChannel<string>
    {
    }
}
