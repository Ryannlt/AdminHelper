using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    // Every entry is read live, so edits to the cfg apply without a restart.
    internal static class Settings
    {
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> TickHz;
        public static ConfigEntry<bool> RequireAdminLogin;

        public static ConfigEntry<float> ClusterNearMetres;
        public static ConfigEntry<float> ClusterFarMetres;

        public static ConfigEntry<float> RiseSeconds;
        public static ConfigEntry<float> RecoverMultiplier;

        public static ConfigEntry<float> EnemyRadius;
        public static ConfigEntry<int> EnemyCrowd;

        public static ConfigEntry<float> FormationSuppression;
        public static ConfigEntry<float> LineRadius;
        public static ConfigEntry<int> LineMinMates;
        public static ConfigEntry<int> LineMaxMates;
        public static ConfigEntry<float> LineResidual;
        public static ConfigEntry<int> ClusterMinMates;
        public static ConfigEntry<float> ClusterFormationRadius;

        public static ConfigEntry<int> RingThreshold;
        public static ConfigEntry<int> RamboThreshold;
        public static ConfigEntry<float> RamboHoldSeconds;
        public static ConfigEntry<bool> ScoreCavalry;
        public static ConfigEntry<string> ExemptClasses;

        public static ConfigEntry<bool> ShowRings;
        public static ConfigEntry<bool> ShowLabels;
        public static ConfigEntry<bool> ShowCornerList;
        public static ConfigEntry<bool> ShowOwnScore;
        public static ConfigEntry<int> MaxLabels;
        public static ConfigEntry<string> ToggleKey;
        public static ConfigEntry<bool> StartHudVisible;

        private static readonly HashSet<PlayerClass> ExemptSet = new HashSet<PlayerClass>();
        private static string _exemptSource;
        private static string _toggleKeySource;
        private static KeyCode _toggleKey = KeyCode.F6;

        private static ConfigFile _config;
        private static DateTime _stamp;
        private static float _nextCheck;

        public static void Create(ConfigFile config)
        {
            _config = config;

            Enabled = config.Bind("General", "Enabled", true,
                "Master switch. Turning this off stops the scorer as well as the HUD.");
            TickHz = config.Bind("General", "TickHz", 5f,
                "Scoring ticks per second. Higher costs more and buys nothing.");
            RequireAdminLogin = config.Bind("General", "RequireAdminLogin", true,
                "Only show other players once the server has authenticated an 'rc login'. Turning this off makes the mod a wallhack.");

            ClusterNearMetres = config.Bind("Isolation", "ClusterNearMetres", 10f,
                "Distance from the midpoint of your two nearest mates at which isolation starts counting.");
            ClusterFarMetres = config.Bind("Isolation", "ClusterFarMetres", 30f,
                "Distance at which isolation is fully saturated.");

            RiseSeconds = config.Bind("Scoring", "RiseSeconds", 6f,
                "Time constant for the score climbing. Larger is slower to flag.");
            RecoverMultiplier = config.Bind("Scoring", "RecoverMultiplier", 3f,
                "How much faster the score falls than it climbs when a player rejoins.");

            EnemyRadius = config.Bind("Danger", "EnemyRadius", 30f,
                "How close an enemy has to be before any isolation counts as danger.");
            EnemyCrowd = config.Bind("Danger", "EnemyCrowd", 3,
                "Enemies inside the radius that count as being fully inside their formation.");

            FormationSuppression = config.Bind("Formation", "FormationSuppression", 0.9f,
                "Fraction of the raw isolation signal removed while a player is in formation.");
            LineRadius = config.Bind("Formation", "LineRadius", 10f,
                "Radius searched for formation mates.");
            LineMinMates = config.Bind("Formation", "LineMinMates", 2,
                "Mates needed before the line fit is attempted.");
            LineMaxMates = config.Bind("Formation", "LineMaxMates", 6,
                "Most mates fed into the line fit.");
            LineResidual = config.Bind("Formation", "LineResidual", 2f,
                "Metres of spread either side of the best-fit line still counted as a line.");
            ClusterMinMates = config.Bind("Formation", "ClusterMinMates", 3,
                "Mates within ClusterFormationRadius that count as a square or skirmisher knot.");
            ClusterFormationRadius = config.Bind("Formation", "ClusterFormationRadius", 8f,
                "Radius for the tight cluster test.");

            RingThreshold = config.Bind("Flagging", "RingThreshold", 40,
                "Isolation score at which a marker appears. Tracks the live score, so it clears as a player returns.");
            RamboThreshold = config.Bind("Flagging", "RamboThreshold", 75,
                "Isolation score at which the dwell timer starts running.");
            RamboHoldSeconds = config.Bind("Flagging", "RamboHoldSeconds", 5f,
                "Seconds above the threshold before a player is flagged.");
            ScoreCavalry = config.Bind("Flagging", "ScoreCavalry", false,
                "Score cavalry too. Off by default since cavalry operating apart is not a rambo.");
            ExemptClasses = config.Bind("Flagging", "ExemptClasses", "",
                "Comma-separated PlayerClass names that are never flagged, e.g. Surgeon,Sapper.");

            ShowRings = config.Bind("Display", "ShowRings", true,
                "Draw a ground ring under each watched player.");
            ShowLabels = config.Bind("Display", "ShowLabels", true,
                "Draw the floating name and score label over each watched player.");
            ShowCornerList = config.Bind("Display", "ShowCornerList", true,
                "List watched players in the corner with their scores and distance.");
            ShowOwnScore = config.Bind("Display", "ShowOwnScore", true,
                "Your own scores plus the raw distances behind them. The fastest way to pick thresholds.");
            MaxLabels = config.Bind("Display", "MaxLabels", 12,
                "Most floating labels drawn at once, nearest first.");
            ToggleKey = config.Bind("Display", "ToggleKey", "F6",
                "Key that hides and shows the HUD. Any UnityEngine.KeyCode name.");
            StartHudVisible = config.Bind("Display", "StartHudVisible", false,
                "Whether the HUD starts visible when the game launches. After that the toggle sticks until you quit.");

            _stamp = Stamp();
        }

        // BepInEx never watches its own cfg, so an external edit only lands if the mod goes looking for it.
        public static void PollForExternalEdits()
        {
            if (_config == null || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 1f;

            DateTime stamp = Stamp();
            if (stamp == _stamp) return;

            _stamp = stamp;
            _config.Reload();
        }

        private static DateTime Stamp()
        {
            try
            {
                return File.GetLastWriteTimeUtc(_config.ConfigFilePath);
            }
            catch (Exception)
            {
                // A locked or half-written file just means no reload this second.
                return _stamp;
            }
        }

        public static bool IsExempt(PlayerClass playerClass)
        {
            RefreshExemptSet();
            return ExemptSet.Contains(playerClass);
        }

        // Reparsed only when the preference string actually changes.
        private static void RefreshExemptSet()
        {
            string source = ExemptClasses.Value ?? string.Empty;
            if (source == _exemptSource) return;

            _exemptSource = source;
            ExemptSet.Clear();

            string[] parts = source.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string name = parts[i].Trim();
                if (name.Length == 0) continue;

                try
                {
                    ExemptSet.Add((PlayerClass)Enum.Parse(typeof(PlayerClass), name, true));
                }
                catch (Exception)
                {
                    Log.Warn("Unknown PlayerClass in ExemptClasses: " + name);
                }
            }
        }

        // Cached because this is read every frame and Enum.Parse allocates.
        public static KeyCode ResolveToggleKey()
        {
            string source = ToggleKey.Value ?? string.Empty;
            if (source == _toggleKeySource) return _toggleKey;

            _toggleKeySource = source;
            _toggleKey = KeyCode.F6;

            string name = source.Trim();
            if (name.Length == 0) return _toggleKey;

            try
            {
                _toggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), name, true);
            }
            catch (Exception)
            {
                Log.Warn("ToggleKey '" + name + "' is not a KeyCode name. Falling back to F6.");
            }

            return _toggleKey;
        }
    }
}
