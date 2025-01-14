using System.Collections;
using Tests.Runtime;
using UnityEngine.TestTools;

namespace AnyVr.LobbySystem.LobbyTests
{
    public class ServerTests : LobbyTest
    {
        [UnityTest]
        public IEnumerator StartServerTest()
        {
            // yield return StartServer();
            // yield return JoinServer();
            yield return null;
        }
    }
}