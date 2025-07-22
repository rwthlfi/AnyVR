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

using AnyVR.LobbySystem;
using NUnit.Framework;
using System.Collections;
using Tests.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace AnyVR.Tests.Runtime
{
    internal abstract class RuntimeTest : IPrebuildSetup, IPostBuildCleanup
    {
        private ConnectionManager _connectionManager;
        protected LobbyManager LobbyManager;

        [UnityOneTimeSetUp]
        protected virtual IEnumerator OneTimeSetUp()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync("ANYVR_TEST_OfflineScene");
            Debug.Assert(op != null);
            while (!op.isDone == false)
            {
                yield return null;
            }
            
            const float timeout = 3f;
            float elapsed = 0f;

            while (!ConnectionManager.IsInitialized && elapsed < timeout)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.IsTrue(ConnectionManager.IsInitialized);
            _connectionManager = ConnectionManager.GetInstance();
            Assert.IsNotNull(ConnectionManager.GetInstance());

            yield return null;
        }

        [UnityOneTimeTearDown]
        protected virtual IEnumerator OneTimeTearDown()
        {
            _connectionManager.LeaveServer();
            _connectionManager.StopServer();
            yield return null;
        }

        protected IEnumerator StartServer()
        {
            _connectionManager.StartServer();

            bool globalSceneLoaded = false;
            _connectionManager.OnGlobalSceneLoaded += EventHandler;

            float timeout = 5f;
            while (!globalSceneLoaded && timeout > 0)
            {
                yield return new WaitForFixedUpdate();
                timeout -= Time.fixedDeltaTime;
            }
            Assert.IsFalse(timeout <= 0);
            Assert.IsTrue(globalSceneLoaded);
            
            _connectionManager.OnGlobalSceneLoaded -= EventHandler;

            yield return null;
            
            Assert.IsTrue(_connectionManager.State.HasFlag(ConnectionState.Server));
            LobbyManager = LobbyManager.GetInstance();
            Assert.IsTrue(LobbyManager.IsInitialized);
            Assert.IsNotNull(LobbyManager);

            yield break;

            void EventHandler(bool b)
            {
                globalSceneLoaded = true;
            }
        }

        protected IEnumerator JoinServer()
        {
            const string liveKitServerAddress = "127.0.0.1:7880";
            const string fishnetServerAddress = "127.0.0.1:7777";

            Assert.IsFalse(_connectionManager.State.HasFlag(ConnectionState.Client));
            
            ServerAddressResponse response = new(fishnetServerAddress, liveKitServerAddress);
            
            _connectionManager.OnGlobalSceneLoaded += GlobalSceneEventHandler;
            _connectionManager.OnClientConnectionState += ConnectionStateEventHandler;

            bool success = _connectionManager.ConnectToServer(response, "TestUser", out string error);
            Assert.IsTrue(success, error);
            
            bool globalSceneLoaded = false;
            bool isClient = false;

            float timeout = 5f; // seconds
            while (!(globalSceneLoaded && isClient) && timeout > 0)
            {
                yield return new WaitForFixedUpdate();
                timeout -= Time.fixedDeltaTime;
            }
            
            Assert.IsTrue(globalSceneLoaded);
            Assert.IsTrue(isClient);
            Assert.IsTrue(_connectionManager.State.HasFlag(ConnectionState.Client));
            Assert.IsTrue(LobbyManager.ClientManager.isActiveAndEnabled);

            _connectionManager.OnGlobalSceneLoaded -= GlobalSceneEventHandler;
            _connectionManager.OnClientConnectionState -= ConnectionStateEventHandler;

            yield return null;

            Assert.IsTrue(_connectionManager.State.HasFlag(ConnectionState.Client));
            Assert.IsTrue(LobbyManager.IsInitialized);
            LobbyManager = LobbyManager.GetInstance();
            Assert.IsNotNull(LobbyManager);

            yield return new WaitForSeconds(1);
            yield break;

            void GlobalSceneEventHandler(bool b)
            {
                globalSceneLoaded = true;
            }

            void ConnectionStateEventHandler(ConnectionState state)
            {
                isClient = state.HasFlag(ConnectionState.Client);
            }
        }

        [UnityTearDown]
        public void TearDown()
        {
            _connectionManager.LeaveServer();
            _connectionManager.StopServer();
        }
        
        void IPrebuildSetup.Setup()
        {
            AnyVRTestManager.AddTestScenesToBuildSettings();
        }
        
        void IPostBuildCleanup.Cleanup()
        {
            AnyVRTestManager.RemoveTestScenesFromBuildSettings();
        }
    }
}