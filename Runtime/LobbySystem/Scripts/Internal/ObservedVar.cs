using System;
using AnyVR.Logging;
using FishNet.Object.Synchronizing;
using Logger = AnyVR.Logging.Logger;

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
        public event Action<T> OnValueChanged;

        public ObservedSyncVar()
        {
            OnChange += (_, next, _) => OnValueChanged?.Invoke(next);
        }
    }
}
