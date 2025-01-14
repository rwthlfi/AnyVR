using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tests.Runtime;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace AnyVr.LobbySystem.LobbyTests
{
    public class LobbyManagerTests : LobbyTest
    {
        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return base.Setup();
            yield return StartServer();
            yield return JoinServer();
            Assert.AreEqual(LobbyManager.GetLobbies().Count, 0);
        }

        /// <summary>
        ///     The client creates a lobby and connects to it.
        ///     The client then leaves the lobby.
        /// </summary>
        [UnityTest]
        public IEnumerator SimpleLobbyCreationTest()
        {
            const string lobbyName = "TestLobby";
            const string scene = "TestLobbyScene";
            const ushort lobbyCapacity = 10;
            LobbyManager.Client_CreateLobby(lobbyName, scene, lobbyCapacity);

            bool receivedCallback = false;

            LobbyManager.LobbyOpened += EventHandler;
            float timeout = 5f;
            while (!receivedCallback && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            LobbyManager.LobbyOpened -= EventHandler;

            Assert.IsTrue(receivedCallback);
            Assert.AreEqual(LobbyManager.GetLobbies().Count, 1);

            KeyValuePair<Guid, LobbyMetaData> lobbyMetaPair = LobbyManager.GetLobbies().First();
            Assert.AreEqual(lobbyName, lobbyMetaPair.Value.Name);
            Assert.AreEqual(scene, lobbyMetaPair.Value.Scene);
            Assert.AreEqual(lobbyCapacity, lobbyMetaPair.Value.LobbyCapacity);

            bool lobbyFound = LobbyManager.TryGetLobbyIdOfClient(LobbyManager.ClientManager.Connection.ClientId,
                out Guid lobbyId);

            Assert.IsTrue(lobbyFound);
            Assert.AreEqual(lobbyId, lobbyMetaPair.Key);

            bool lobbyHandlerFound = LobbyManager.TryGetLobbyHandlerById(lobbyId, out LobbyHandler lobbyHandler);
            Assert.IsTrue(lobbyHandlerFound);

            int creatorId = lobbyMetaPair.Value.CreatorId;
            // The creator should be registered to that lobby in the Lobby Manager
            Assert.IsTrue(LobbyManager.TryGetLobbyIdOfClient(creatorId, out lobbyId));
            // The creator should NOT have the lobby scene already loaded. I.e the scene should have no client connections.
            Assert.IsTrue(lobbyHandler.GetClients().Length == 0);

            bool creatorJoined = false;
            lobbyHandler.ClientJoin += (i, s) =>
            {
                Assert.AreEqual(creatorId, i);
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
            Assert.AreEqual(currentLobby, lobbyMetaPair.Value);

            // LobbyManager.LeaveLobby(LobbyManager.ClientManager.Connection);
            // TODO
            yield break;

            void EventHandler(LobbyMetaData lmd)
            {
                Assert.AreEqual(lobbyName, lmd.Name);
                Assert.AreEqual(scene, lmd.Scene);
                Assert.AreEqual(lobbyCapacity, lmd.LobbyCapacity);
                receivedCallback = true;
            }
        }
    }
}