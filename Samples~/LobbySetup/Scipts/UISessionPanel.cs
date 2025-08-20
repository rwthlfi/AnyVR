using System.Collections.Generic;
using System.Linq;
using AnyVR.LobbySystem;
using UnityEngine;

namespace AnyVR.Sample.LobbySetup
{
    public class UISessionPanel : MonoBehaviour
    {
        private LobbyHandler _lobbyHandler;

        private readonly Dictionary<int, UIUserListEntry> _players = new();
        
        [SerializeField] private UIUserListEntry _entryPrefab;
        
        [SerializeField] private RectTransform _entryParent;

        private void OnEnable()
        {
            _lobbyHandler = LobbyHandler.GetInstance();
            if (_lobbyHandler == null)
            {
                Debug.LogWarning("LobbyHandler not found. Disabling UISessionPanel.");
                gameObject.SetActive(false);
                return;
            }
            
            _lobbyHandler.OnPlayerJoin += AddPlayerEntry;
            _lobbyHandler.OnPlayerLeave += RemovePlayerEntry;

            RemoveAllEntries();

            foreach (PlayerState player in _lobbyHandler.PlayerStates)
            {
                AddPlayerEntry(player);
            }
        }
        private void RemoveAllEntries()
        {
            foreach (UIUserListEntry entry in _players.Values)
            {
                Destroy(entry.gameObject);
            }
            _players.Clear();
        }

        private void AddPlayerEntry(PlayerState playerState)
        {
            if (_players.ContainsKey(playerState.GetID()))
                return;
            
            UIUserListEntry entry = Instantiate(_entryPrefab, _entryParent);
            entry.SetPlayerInfo(playerState);
            entry.OnKickButtonPressed += _lobbyHandler.KickPlayer;
            
            _players.Add(playerState.GetID(), entry);
        }
        
        private void RemovePlayerEntry(PlayerState playerState)
        {
            _players.Remove(playerState.GetID(), out UIUserListEntry entry);

            if (entry != null)
                Destroy(entry.gameObject);
        }

#if !UNITY_SERVER
        private GUIStyle _style;
        private void OnGUI()
        {
            if (_lobbyHandler == null)
                return;

            if (!gameObject.activeSelf)
                return;
            
            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.yellow
                },
            };

            const float x = 10f;
            const float y = 25f;
            const float width = 500f;
            const float height = 20f;
            Rect labelRect = new(x, y, width, height);

            string debugMsg = $"Capacity: {_lobbyHandler.PlayerStates.Count()} / {_lobbyHandler.MetaData.LobbyCapacity}";
            GUI.Label(labelRect, debugMsg, _style);
        }
#endif
    }
}
