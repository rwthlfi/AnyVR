using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using WebRequests;

namespace AnyVR.WebRequests
{
    public static class WebRequestHandler
    {
        public static async Task<T> GetAsync<T>(string url, int timeoutSeconds = 10) where T : Response, new()
        {
            T response = new();

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
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
            catch (Exception _)
            {
                response.Success = false;
                response.Error = "JSON Parsing Failed";
            }

            return response;
        }
    }
}
