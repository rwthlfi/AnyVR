using System;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState
    {
#region Public API

        /// <summary>
        ///     If this is the local player's state.
        /// </summary>
        public bool IsLocalPlayer => IsController;

        /// <summary>
        ///     Singleton instance to the local player's lobby state.
        ///     Is <c>null</c> on the server or if the local player is not a participant of a lobby.
        /// </summary>
        public static LobbyPlayerState Local { get; private set; }

#endregion

#region Lifecycle

        public override void OnStartClient()
        {
            base.OnStartClient();
            Assert.IsFalse(_lobbyId.Value == Guid.Empty);
        }

        private void OnDestroy()
        {
            if (Local == this)
            {
                Local = null;
            }
        }

#endregion
    }
}
