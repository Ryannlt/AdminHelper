using BepInEx.Logging;

namespace AdminHelper
{
    // The only logging route, so no other file has to name the mod loader. One swap at port time.
    internal static class Log
    {
        // A static class cannot reach BaseUnityPlugin's instance logger, so it registers its own source.
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
