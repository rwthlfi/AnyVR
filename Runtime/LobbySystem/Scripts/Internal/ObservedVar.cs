using System;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem.Internal
{
    public interface IReadOnlyObservedVar<T>
    {
        T Value { get; }
        event Action<T> OnValueChanged;
    }

    [Serializable]
    internal class ObservedSyncVar<T> : SyncVar<T>, IReadOnlyObservedVar<T>
    {
        public ObservedSyncVar()
        {
            OnChange += (_, next, _) =>
            {
                OnValueChanged?.Invoke(next);
            };
        }
        public event Action<T> OnValueChanged;

        public void InvokeCallback()
        {
            OnValueChanged?.Invoke(Value);
        }
    }
}
