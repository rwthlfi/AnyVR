using System.Runtime.InteropServices;
using UnityEngine;

namespace AnyVR.Voicechat
{
    public class TokenServiceFfi : MonoBehaviour
    {
        [DllImport("livekit_tokengen_ffi", EntryPoint = "create_token")]
        public static extern string CreateToken(string room, string userName, string userIdentity);
    }
}
