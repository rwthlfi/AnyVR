using System.Collections;
using UnityEngine.TestTools;

namespace AnyVR.Tests.Runtime
{
    internal class ServerTests : RuntimeTest
    {
        [UnityTest]
        public IEnumerator StartServerTest()
        {
            yield return StartServer();
            yield return null;
        }
    }
}