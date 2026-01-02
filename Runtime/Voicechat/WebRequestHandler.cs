using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AnyVR.LobbySystem.Internal
{
    internal abstract class Response
    {
        [NonSerialized]
        internal string Error;
        [NonSerialized]
        internal bool Success;
    }

    internal static class WebRequestHandler
    {
        internal static async Task<T> GetAsync<T>(Uri uri, int timeoutSeconds = 10) where T : Response, new()
        {
            T response = new();

            using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            webRequest.timeout = timeoutSeconds;

            await webRequest.SendWebRequest();

            response.Success = webRequest.result == UnityWebRequest.Result.Success;

            if (!response.Success)
            {
                response.Error = webRequest.error ?? "Unknown error occurred";
                return response;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(webRequest.downloadHandler.text, response);
            }
            catch (Exception)
            {
                response.Success = false;
                response.Error = "JSON Parsing Failed";
            }

            return response;
        }
    }
}
