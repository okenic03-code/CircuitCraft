using UnityEngine;
using System;

namespace CircuitCraft.Data.Events
{
    public abstract class EventChannel<T> : ScriptableObject
    {
        public event Action<T> OnEventRaised;

        public void Raise(T data)
        {
            if (data == null) return;
            OnEventRaised?.Invoke(data);
        }

        public void Subscribe(Action<T> listener)
        {
            OnEventRaised += listener;
        }

        public void Unsubscribe(Action<T> listener)
        {
            OnEventRaised -= listener;
        }
    }
}
