using AnyVR.LobbySystem.Internal;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     TODO
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
