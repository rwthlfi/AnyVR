using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voicechat;

namespace LobbySystem
{
    public class LobbyRoomHandler : NetworkBehaviour
    {
        private readonly SyncDictionary<int, string> _sids = new();

        private LobbyMetaData _lmd;

        private void Awake()
        {
            InitSingleton();
        }

        public static event Action<int[]> ClientListReceived;
        public static event Action<int> ClientJoined;
        public static event Action<int> ClientLeft;

        public override void OnStartClient()
        {
            if (IsServerInitialized)
            {
                return;
            }

            LobbyMetaData? temp = LobbyManager.s_instance.GetCurrentLobby();

            if (temp == null)
            {
                Debug.LogWarning("Could not get current lobby data!");
                return;
            }

            _lmd = (LobbyMetaData)temp;
            LobbyManager.s_instance.RequestLobbyClientListRpc(_lmd);

            LiveKitManager.s_instance.ConnectedToRoom += () =>
            {
                RegisterLocalSid(LiveKitManager.s_instance.LocalParticipant.Sid);
            };
            if (LiveKitManager.s_instance.TryGetAvailableMicrophoneNames(out string[] micNames))
            {
                string msg = micNames.Aggregate("Available Microphones:\n",
                    (current, micName) => current + "\t" + micName + "\n");
                Debug.Log(msg);
                LiveKitManager.s_instance.SetActiveMicrophone(micNames[1]);
            }

            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("UIScene", LoadSceneMode.Additive);

            LobbyManager.s_instance.LobbyClientListReceived += list =>
            {
                ClientListReceived?.Invoke(list);
            };
            LobbyManager.s_instance.ClientJoined += clientId =>
            {
                ClientJoined?.Invoke(clientId);
            };
            LobbyManager.s_instance.ClientLeft += clientId =>
            {
                ClientLeft?.Invoke(clientId);
            };
        }

        [ServerRpc(RequireOwnership = false)]
        private void RegisterLocalSid(string sid, NetworkConnection conn = null)
        {
            if (conn != null)
            {
                _sids.TryAdd(conn.ClientId, sid);
            }
        }

        public static bool IsLocalClientAdmin()
        {
            return s_instance._lmd.Creator == s_instance.ClientManager.Connection.ClientId;
        }

        public static int GetAdminId()
        {
            return s_instance._lmd.Creator;
        }

        public static void SetMicrophoneActive(bool micActive)
        {
            LiveKitManager.s_instance.SetMicrophoneEnabled(micActive);
        }

        #region Singleton

        private static LobbyRoomHandler s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Debug.LogWarning("Instance of LobbyRoomHandler already exists!");
                return;
            }

            s_instance = this;
        }

        #endregion
    }
}