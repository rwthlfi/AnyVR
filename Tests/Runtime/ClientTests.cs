using AnyVR.LobbySystem;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace AnyVR.Tests.Runtime
{
    internal class ClientTests : RuntimeTest
    {

        private static LobbyMetaData _lmd;
        
        [UnityOneTimeSetUp]
        protected override IEnumerator OneTimeSetUp()
        {
            yield return base.OneTimeSetUp();
            yield return StartServer();
            yield return JoinServer();
        }

        [UnityTest, Order(0)]
        public IEnumerator CreateLobby()
        {
            const string lobbyName = "ANYVR_TEST_LobbyScene1";

            EditorBuildSettingsScene lobbyScene = EditorBuildSettings.scenes.First(s => s.path.Contains(lobbyName));
            Assert.IsNotNull(lobbyScene);
            const ushort lobbyCapacity = 10;
            
            LobbySceneMetaData sceneMetaData = ScriptableObject.CreateInstance<LobbySceneMetaData>();
            sceneMetaData._scenePath = lobbyScene.path;
            sceneMetaData._sceneName = lobbyName;
            sceneMetaData._recommendedUsers = new LobbySceneMetaData.MinMaxRange(0, lobbyCapacity);
            
            LobbyManager.Client_CreateLobby(lobbyName, string.Empty, sceneMetaData, lobbyCapacity);

            LobbyManager.OnLobbyOpened += EventHandler;
            
            bool receivedCallback = false;
            float timeout = 5f;
            
            while (!receivedCallback && timeout > 0)
            {
                yield return new WaitForFixedUpdate();
                timeout -= Time.fixedDeltaTime;
            }

            LobbyManager.OnLobbyOpened -= EventHandler;

            Assert.IsTrue(receivedCallback);
            Assert.AreEqual(LobbyManager.GetLobbies().Count, 1);

            KeyValuePair<Guid, LobbyMetaData> lobbyMetaPair = LobbyManager.GetLobbies().First();
            Assert.AreEqual(lobbyName, lobbyMetaPair.Value.Name);
            Assert.AreEqual(lobbyScene.path, lobbyMetaPair.Value.ScenePath);
            Assert.AreEqual(lobbyCapacity, lobbyMetaPair.Value.LobbyCapacity);

            _lmd = lobbyMetaPair.Value;
            
            yield break;
            void EventHandler(Guid lobbyId)
            {
                receivedCallback = true;
            }
        }

        
        [UnityTest, Order(1)]
        public IEnumerator JoinLobby()
        {
            Assert.IsNotNull(_lmd, "s_lmd is null. Call CreateLobby() first.");
            
            LobbyManager.Client_JoinLobby(_lmd.LobbyId);
            
            LobbyManager.OnLobbyJoined += EventHandler;
            
            bool receivedCallback = false;
            float timeout = 5f;
            
            while (!receivedCallback && timeout > 0)
            {
                yield return new WaitForFixedUpdate();
                timeout -= Time.fixedDeltaTime;
            }

            LobbyManager.OnLobbyJoined -= EventHandler;
            
            bool lobbyFound = LobbyManager.TryGetLobbyIdOfClient(LobbyManager.ClientManager.Connection.ClientId,
                out Guid lobbyId);

            Assert.IsTrue(lobbyFound);
            Assert.AreEqual(lobbyId, _lmd.LobbyId);

            bool lobbyHandlerFound = LobbyManager.TryGetLobbyHandlerById(lobbyId, out LobbyHandler lobbyHandler);
            Assert.IsTrue(lobbyHandlerFound);

            int creatorId = _lmd.CreatorId;
            // The creator should be registered to that lobby in the Lobby Manager
            Assert.IsTrue(LobbyManager.TryGetLobbyIdOfClient(creatorId, out lobbyId));
            // The creator should NOT have the lobby scene already loaded. I.e. the scene should have no client connections.
            Assert.IsTrue(lobbyHandler.GetPlayers().Count == 0);

            bool creatorJoined = false;
            lobbyHandler.OnPlayerJoined += clientId =>
            {
                Assert.AreEqual(creatorId, clientId);
                creatorJoined = true;
            };
            timeout = 5;
            while (!creatorJoined && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            Assert.IsTrue(creatorJoined);
            LobbyMetaData currentLobby = LobbyManager.Client_GetCurrentLobby();
            Assert.IsNotNull(currentLobby);
            Assert.AreEqual(currentLobby, _lmd);

            LobbyManager.LeaveLobby(LobbyManager.ClientManager.Connection);
            // TODO
            yield break;

            void EventHandler(Guid _)
            {
                receivedCallback = true;
            }
        }
        
        [UnityTest, Order(2)]
        public IEnumerator CloseLobby()
        {
            LobbyManager.Client_CloseLobby();
            yield return null;
        }
    }
}