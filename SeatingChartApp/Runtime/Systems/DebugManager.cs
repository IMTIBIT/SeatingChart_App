using UnityEngine;

namespace SeatingChartApp.Runtime.Systems
{

    public enum LogCategory
    {
        General,
        Saving,
        UI,
        Analytics,
        Authentication,
        Spawning
    }

    public static class DebugManager
    {
        public static void Log(LogCategory category, string message)
        {
            Debug.Log($"[{System.DateTime.Now:HH:mm:ss}][{category}] {message}");
        }

        public static void LogWarning(LogCategory category, string message)
        {
            Debug.LogWarning($"[{System.DateTime.Now:HH:mm:ss}][{category}] {message}");
        }

        public static void LogError(LogCategory category, string message)
        {
            Debug.LogError($"[{System.DateTime.Now:HH:mm:ss}][{category}] {message}");
        }
    }
}