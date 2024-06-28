using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using UnityEngine;

#if UNITY_WEBGL
using FishNet.Transporting.Bayou;
#else
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