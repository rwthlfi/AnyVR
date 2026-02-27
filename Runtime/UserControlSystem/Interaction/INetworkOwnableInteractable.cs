using FishNet.Connection;
using FishNet.Object;

namespace AnyVR.UserControlSystem
{
    public interface INetworkOwnableInteractable
    {
        public NetworkObject NetworkObject { get; }

        public NetworkConnection Owner { get; }

        public void RequestOwnership(NetworkConnection conn = null);

        public void ReleaseOwnership(NetworkConnection conn = null);
    }
}
