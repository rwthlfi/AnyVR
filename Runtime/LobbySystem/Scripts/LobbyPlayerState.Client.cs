using System;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState
    {
#region Public API

        /// <summary>
        ///     Singleton instance to the local player's lobby state.
        ///     Is <c>null</c> on the server or if the local player is not a participant of a lobby.
        /// </summary>
        public static LobbyPlayerState Local { get; private set; }

        /// <summary>
        ///     If the player is connected to the LiveKit room of this lobby.
        /// </summary>
        public IReadOnlyObservedVar<bool> IsConnectedToVoice => _isConnectedToVoice;

        /// <summary>
        ///     If the player's microphone is published to the LiveKit room.
        /// </summary>
        public IReadOnlyObservedVar<bool> IsMicrophonePublished => _isMicrophonePublished;

        /// <summary>
        ///     If the player's microphone is muted.
        /// </summary>
        /// <remarks>
        ///     If this is the player state of a remote player which uses the standalone build, <c>IsMicrophoneMuted</c> will always be false due to a bug in the LiveKit package.
        ///     TODO Remove this after LiveKit fix.
        /// <seealso href="https://github.com/livekit/client-sdk-unity/issues/152">LiveKit Issue #152</seealso>
        /// </remarks>
        public IReadOnlyObservedVar<bool> IsMicrophoneMuted => _isMicrophoneMuted;

        /// <summary>
        ///     If the player is speaking at this moment.
        /// </summary>
        public IReadOnlyObservedVar<bool> IsSpeaking => _isSpeaking;

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
