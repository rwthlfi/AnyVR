using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace LobbySystem.LobbyTests
{
    public class LobbyManagerTests
    {
        private ConnectionManager _connectionManager;
        private LobbyManager _lobbyManager;
        
        [UnitySetUp]
        private IEnumerator Setup()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(0, LoadSceneMode.Additive);
            while (!op.isDone)
            {
                yield return null;
            }

            _connectionManager = ConnectionManager.GetInstance();
            Assert.IsNotNull(_connectionManager);
            _connectionManager.StartServer();
            
            Assert.AreEqual(_connectionManager.State, ConnectionState.Server);

            (string, ushort) liveKitServerAddress = ("127.0.0.1", 7880);
            (string, ushort) fishnetServerAddress = ("127.0.0.1", 7777);
            
            _connectionManager.ConnectToServer(fishnetServerAddress, liveKitServerAddress, "TestUser");

            bool globalSceneLoaded = false;
            
            _connectionManager.GlobalSceneLoaded += _ =>
            {
                globalSceneLoaded = true;
            };
            
            float timeout = 5f;
            // while !(client && loaded) && !timeout
            while (!(_connectionManager.State.HasFlag(ConnectionState.Client) && globalSceneLoaded) && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }
            
            Assert.AreEqual(_connectionManager.State, ConnectionState.Client | ConnectionState.Server);
            _lobbyManager = LobbyManager.GetInstance();
            Assert.IsNotNull(_lobbyManager);
            Assert.IsNotNull(_lobbyManager.ServerManager);
            Assert.IsNotNull(_lobbyManager.ClientManager);
            Assert.AreEqual(_lobbyManager.GetAvailableLobbies().Count, 0);
            yield return new WaitForSeconds(1f);
        }
        
        [UnityTest]
        public IEnumerator CreateLobbyTest()
        {
            const string lobbyName = "TestLobby";
            const string scene = "Assets/Scenes/LobbyScenes/InputModality_DemoScene.unity";
            const ushort lobbyCapacity = 10;
            _lobbyManager.Client_CreateLobby(lobbyName, scene, lobbyCapacity);

            bool receivedCallback = false;
            _lobbyManager.LobbyOpened += lobbyMeta =>
            {
                Assert.AreEqual(lobbyName, lobbyMeta.Name);
                Assert.AreEqual(scene, lobbyMeta.Scene);
                Assert.AreEqual(lobbyCapacity, lobbyMeta.LobbyCapacity);
                receivedCallback = true;
            };
            
            float timeout = 10f;
            while (!receivedCallback && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }
            
            Assert.IsTrue(receivedCallback);
            Assert.AreEqual(_lobbyManager.GetAvailableLobbies().Count, 1);

            KeyValuePair<string, LobbyMetaData> lobbyMetaPair = _lobbyManager.GetAvailableLobbies().First();
            Assert.AreEqual(lobbyName, lobbyMetaPair.Value.Name);
            Assert.AreEqual(scene, lobbyMetaPair.Value.Scene);
            Assert.AreEqual(lobbyCapacity, lobbyMetaPair.Value.LobbyCapacity);

            bool lobbyFound = _lobbyManager.TryGetLobbyIdOfClient(_lobbyManager.ClientManager.Connection.ClientId,
                out string lobbyId);
            
            Assert.IsTrue(lobbyFound);
            Assert.AreEqual(lobbyId, lobbyMetaPair.Key);

            bool lobbyHandlerFound = _lobbyManager.TryGetLobbyHandlerById(lobbyId, out LobbyHandler lobbyHandler);
            Assert.IsTrue(lobbyHandlerFound);

            int creatorId = lobbyMetaPair.Value.CreatorId;
            // The creator should be registered to that lobby in the Lobby Manager
            Assert.IsTrue(_lobbyManager.TryGetLobbyIdOfClient(creatorId, out lobbyId));
            // The creator should NOT have the lobby scene already loaded. I.e the scene should have no client connections.
            Assert.IsTrue(lobbyHandler.GetClients().Length == 0);

            bool creatorJoined = false;
            lobbyHandler.ClientJoin += (i, s) =>
            {
                Assert.AreEqual(creatorId, i);
                creatorJoined = true;
            };
            timeout = 10f;
            while (!creatorJoined && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }
            Assert.IsTrue(creatorJoined);
        }

        [UnityTearDown]
        public void TearDown()
        {
            _connectionManager.LeaveServer();
            _connectionManager.StopServer();
        }
    }
}