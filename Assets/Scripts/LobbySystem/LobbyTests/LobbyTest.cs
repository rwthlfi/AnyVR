// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2024 Engineering Hydrology, RWTH Aachen University.
// 
// AnyVR is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// AnyVR is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AnyVR.
// If not, see <https://www.gnu.org/licenses/>.

using FishNet;
using FishNet.Connection;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LobbySystem.LobbyTests
{
    public abstract class LobbyTest
    {
        private ConnectionManager _connectionManager;
        protected LobbyManager LobbyManager;

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(0, LoadSceneMode.Additive);
            while (!op.isDone)
            {
                yield return null;
            }

            _connectionManager = ConnectionManager.GetInstance();
            Assert.IsNotNull(_connectionManager);
        }

        protected IEnumerator StartServer()
        {
            _connectionManager.StartServer();

            bool globalSceneLoaded = false;
            _connectionManager.GlobalSceneLoaded += EventHandler;

            float timeout = 5f;
            while (!globalSceneLoaded && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            _connectionManager.GlobalSceneLoaded -= EventHandler;

            Assert.IsFalse(timeout <= 0);
            Assert.IsTrue(globalSceneLoaded);
            Assert.IsTrue(_connectionManager.State.HasFlag(ConnectionState.Server));
            LobbyManager = LobbyManager.GetInstance();
            Assert.IsNotNull(LobbyManager);

            yield break;

            void EventHandler(bool b)
            {
                globalSceneLoaded = true;
            }
        }

        protected IEnumerator JoinServer()
        {
            (string, ushort) liveKitServerAddress = ("127.0.0.1", 7880);
            (string, ushort) fishnetServerAddress = ("127.0.0.1", 7777);

            _connectionManager.ConnectToServer(fishnetServerAddress, liveKitServerAddress, "TestUser");

            bool globalSceneLoaded = false;

            _connectionManager.GlobalSceneLoaded += EventHandler;

            float timeout = 5f;
            // while !(client && loaded) && !timeout
            while (!globalSceneLoaded && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            _connectionManager.GlobalSceneLoaded -= EventHandler;

            Assert.IsFalse(timeout <= 0);
            Assert.IsTrue(globalSceneLoaded);
            Assert.IsTrue(_connectionManager.State.HasFlag(ConnectionState.Client));

            LobbyManager = LobbyManager.GetInstance();
            Assert.IsNotNull(LobbyManager);
            yield return new WaitForSecondsRealtime(1);

            yield break;

            void EventHandler(bool b)
            {
                globalSceneLoaded = true;
            }
        }

        [UnityTearDown]
        public void TearDown()
        {
            _connectionManager.LeaveServer();
            _connectionManager.StopServer();
        }
    }
}