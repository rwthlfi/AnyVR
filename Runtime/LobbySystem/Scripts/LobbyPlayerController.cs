using AnyVR.LobbySystem.Internal;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the controller of a specific player in a lobby.
    ///     Each instance of this component is owned by the corresponding player and is only replicated to that player.
    ///     The LobbyPlayerController can be used to invoke operations on the server by sending RPCs.
    ///     Override and RPCs as needed. Each lobby can be configured with its own custom lobby player controller.
    ///     The default implementation exposes some client-side lobby actions (promote, kick, leave, etc.).
    ///     Also manages voice–chat integration.
    /// </summary>
    public partial class LobbyPlayerController : PlayerController
    {
#region Serialized Properties

        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [FormerlySerializedAs("_playerPrefab")]
        [SerializeField] private NetworkObject _playerAvatarPrefab;

#endregion

#region Private Fields

        private readonly RpcAwaiter<PlayerKickResult> _playerKickUpdateAwaiter = new(PlayerKickResult.Timeout, PlayerKickResult.Cancelled);

        private readonly RpcAwaiter<PlayerPromotionResult> _playerPromoteUpdateAwaiter = new(PlayerPromotionResult.Timeout, PlayerPromotionResult.Cancelled);

#endregion

#region Singleton

        public static LobbyPlayerController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

#endregion
    }
}
