using FishNet.Transporting.Multipass;
using UnityEngine;
#if UNITY_WEBGL
using FishNet.Transporting.Bayou;

#else
using FishNet.Transporting.Tugboat;
#endif

namespace LobbySystem
{
    [RequireComponent(typeof(Multipass))]
    public class MultipassHandler : MonoBehaviour
    {
        private void Start()
        {
            Multipass mp = GetComponent<Multipass>();
#if UNITY_WEBGL
            mp.SetClientTransport<Bayou>();
#else
            mp.SetClientTransport<Tugboat>();
#endif
        }
    }
}