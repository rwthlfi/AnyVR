using System.Runtime.InteropServices;
using UnityEngine;

namespace AnyVR.Voicechat
{
    internal class TokenServiceFfi : MonoBehaviour
    {
        [DllImport("livekit_tokengen_ffi", EntryPoint = "create_token")]
        internal static extern string CreateToken(string room, string userName, string userIdentity); //TODO: Add TTL parameter
    }
}
