using UnityEngine;
using System;

namespace CircuitCraft.Data.Events
{
    /// <summary>
    /// Base ScriptableObject event channel for broadcasting payloads of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The payload type published by this channel.</typeparam>
    public abstract class EventChannel<T> : ScriptableObject
    {
        /// <summary>
        /// Raised when this channel publishes an event payload.
        /// </summary>
        public event Action<T> OnEventRaised;

        /// <summary>
        /// Publishes an event payload to all subscribers.
        /// </summary>
        /// <param name="data">The payload to publish.</param>
        public void Raise(T data)
        {
            if (data == null) return;
            OnEventRaised?.Invoke(data);
        }

        /// <summary>
        /// Subscribes a listener to this channel.
        /// </summary>
        /// <param name="listener">The listener callback.</param>
        public void Subscribe(Action<T> listener)
        {
            OnEventRaised += listener;
        }

        /// <summary>
        /// Unsubscribes a listener from this channel.
        /// </summary>
        /// <param name="listener">The listener callback.</param>
        public void Unsubscribe(Action<T> listener)
        {
            OnEventRaised -= listener;
        }
    }
}
