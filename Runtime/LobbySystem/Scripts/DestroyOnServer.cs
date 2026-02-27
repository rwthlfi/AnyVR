using UnityEngine;

namespace AnyVR.LobbySystem
{
    internal class DestroyOnServer : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_SERVER
            Destroy(gameObject);
#endif
        }
    }
}
