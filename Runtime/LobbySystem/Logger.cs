using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AnyVR.LobbySystem
{
    internal enum LogLevel
    {
        k_error = 0,
        k_warning = 1,
        k_debug = 2,
        k_verbose = 3
    }

    public class Logger : MonoBehaviour
    {
        [SerializeField] private LogLevel _currentLevel = LogLevel.k_warning;

        private void Awake()
        {
            InitSingleton();
        }

        private bool ShouldLog(LogLevel level)
        {
            return level <= _currentLevel;
        }

        public static void LogError(object message)
        {
            Log(message.ToString(), LogLevel.k_error);
        }

        public static void LogWarning(object message)
        {
            Log(message.ToString(), LogLevel.k_warning);
        }

        public static void LogDebug(object message)
        {
            Log(message.ToString(), LogLevel.k_debug);
        }

        public static void LogVerbose(object message)
        {
            Log(message.ToString(), LogLevel.k_verbose);
        }

        internal static void SetLogLevel(LogLevel level)
        {
            s_instance._currentLevel = level;
        }

        private static void Log(string message, LogLevel level)
        {
            if (!s_instance.ShouldLog(level))
            {
                return;
            }

            string prefixedMessage = $"[AnyVR] {message}";
            switch (level)
            {
                case LogLevel.k_error:
                    Debug.LogError(prefixedMessage);
                    break;
                case LogLevel.k_warning:
                    Debug.LogWarning(prefixedMessage);
                    break;
                case LogLevel.k_debug:
                    Debug.Log(prefixedMessage);
                    break;
                case LogLevel.k_verbose:
                    Debug.Log(prefixedMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        #region Singleton

        internal static Logger s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            s_instance = this;
        }

        #endregion
    }
}