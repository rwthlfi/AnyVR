using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AnyVR.Logging
{
    public enum LogLevel
    {
        Error = 0,
        Warning = 1,
        Debug = 2,
        Verbose = 3
    }

    public class Logger : MonoBehaviour
    {
        [SerializeField] private LogLevel _currentLevel = LogLevel.Warning;

        private void Awake()
        {
            InitSingleton();
        }

        private bool ShouldLog(LogLevel level)
        {
            return level <= _currentLevel;
        }

        internal static void SetLogLevel(LogLevel level)
        {
            _instance._currentLevel = level;
        }

        [Conditional("ANY_VR_LOG")]
        public static void Log(LogLevel level, string tag, object message)
        {
            if (!_instance.ShouldLog(level))
            {
                return;
            }

            string prefixedMessage = $"[AnyVR] [{tag}] {message}";
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(prefixedMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(prefixedMessage);
                    break;
                case LogLevel.Debug:
                    Debug.Log(prefixedMessage);
                    break;
                case LogLevel.Verbose:
                    Debug.Log(prefixedMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        #region Singleton

        private static Logger _instance;

        private void InitSingleton()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            _instance = this;
        }

        #endregion
    }
}
