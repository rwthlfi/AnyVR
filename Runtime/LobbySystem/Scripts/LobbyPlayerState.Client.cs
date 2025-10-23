using System;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState
    {
        public bool IsLocalPlayer => IsController;

        public static LobbyPlayerState Local { get; private set; }

        private void OnDestroy()
        {
            if (Local == this)
            {
                Local = null;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Assert.IsFalse(_lobbyId.Value == Guid.Empty);
        }
    }
}
