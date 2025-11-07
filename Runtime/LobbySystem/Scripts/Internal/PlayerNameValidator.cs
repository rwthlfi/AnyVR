using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem.Internal
{
    public static class PlayerNameValidator
    {
        private const int MinPlayerNameLength = 2;
        private const int MaxPlayerNameLength = 96;

        private static readonly string[] NameBlacklist =
        {
            "admin", "administrator"
        };

        private static bool IsInvalidName(string playerName)
        {
            playerName = playerName.ToLowerInvariant();
            return NameBlacklist.Any(word => playerName.Contains(word));
        }

        internal static PlayerNameUpdateResult ValidatePlayerName(string playerName)
        {
            playerName = Regex.Replace(playerName.Trim(), @"\s+", " ");
            playerName = playerName.ToLowerInvariant();

            switch (playerName.Length)
            {
                case < MinPlayerNameLength:
                    return PlayerNameUpdateResult.TooShort;
                case > MaxPlayerNameLength:
                    return PlayerNameUpdateResult.TooLong;
            }

            if (IsInvalidName(playerName))
            {
                return PlayerNameUpdateResult.InvalidName;
            }

            Assert.IsNotNull(GlobalGameState.Instance);

            bool isUsernameTaken = GlobalGameState.Instance.GetPlayerStates<GlobalPlayerState>().Any(player => player.Name.ToLowerInvariant().Equals(playerName));

            return isUsernameTaken ? PlayerNameUpdateResult.NameTaken : PlayerNameUpdateResult.Success;
        }
    }
}
