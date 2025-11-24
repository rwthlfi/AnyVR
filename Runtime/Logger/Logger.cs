using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

[assembly: InternalsVisibleTo("AnyVR.Lobbysystem")]
[assembly: InternalsVisibleTo("AnyVR.Voicechat")]
[assembly: InternalsVisibleTo("AnyVR.PlatformManagement")]
[assembly: InternalsVisibleTo("AnyVR.UserControlSystem")]

namespace AnyVR.Logging
{
    internal enum LogLevel
    {
        Error = 0,
        Warning = 1,
        Debug = 2,
        Verbose = 3
    }

    internal class Logger : MonoBehaviour
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

        internal static void Log(LogLevel level, string tag, object message)
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
