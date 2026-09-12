using BepInEx.Logging;

namespace AdminHelper
{
    internal static class Log
    {
        private static readonly ManualLogSource Source = Logger.CreateLogSource("AdminHelper");

        public static void Info(string message)
        {
            Source.LogInfo(message);
        }

        public static void Warn(string message)
        {
            Source.LogWarning(message);
        }

        public static void Error(string message)
        {
            Source.LogError(message);
        }
    }
}
