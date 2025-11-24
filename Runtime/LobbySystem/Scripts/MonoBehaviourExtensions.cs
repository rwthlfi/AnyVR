using AnyVR.LobbySystem.Internal;
using UnityEngine;

namespace AnyVR.LobbySystem
{
    public static class MonoBehaviourExtensions
    {
        public static GameStateBase GetGameState(this MonoBehaviour behaviour)
        {
            return GameStateBase.GetInstance(behaviour.gameObject.scene);
        }

        public static T GetGameState<T>(this MonoBehaviour behaviour) where T : GameStateBase
        {
            return GameStateBase.GetInstance(behaviour.gameObject.scene) as T;
        }
    }
}
