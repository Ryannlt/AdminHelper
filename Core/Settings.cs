using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
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
        public static ConfigEntry<bool> ShowGlow;
        public static ConfigEntry<int> MaxLabels;
        public static ConfigEntry<RamboUiMode> RamboUi;
        public static ConfigEntry<string> ToggleKey;

        public static ConfigEntry<string> SpectatePrevKey;
        public static ConfigEntry<string> SpectateNextKey;

        public static ConfigEntry<bool> ClassFilterEnabled;
        public static ConfigEntry<bool> RegimentSearchEnabled;
        public static ConfigEntry<bool> FactionSearchEnabled;
        public static ConfigEntry<bool> RowActionsEnabled;

        public static ConfigEntry<bool> MeleeGraceEnabled;

        public static ConfigEntry<bool> FlagHighlightEnabled;
        public static ConfigEntry<bool> FlagMinimapMarkers;
        public static ConfigEntry<bool> FlagMinimapAlways;
        public static ConfigEntry<bool> ShowFlagLabels;

        public static ConfigEntry<bool> AfkMarkEnabled;
        public static ConfigEntry<float> AfkSeconds;
        public static ConfigEntry<float> AfkMoveMetres;

        public static ConfigEntry<bool> MeleeMarkerEnabled;
        public static ConfigEntry<bool> TeamkillFilterEnabled;
        public static ConfigEntry<float> MeleeChainMetres;
        public static ConfigEntry<float> MeleeWindowSeconds;

        public static ConfigEntry<bool> MapEnabled;

        private static readonly HashSet<PlayerClass> ExemptSet = new HashSet<PlayerClass>();
        private static string _exemptSource;

        private static KeyBinding _toggleKey;
        private static KeyBinding _spectatePrevKey;
        private static KeyBinding _spectateNextKey;

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
            MeleeGraceEnabled = config.Bind("Flagging", "MeleeGrace", true,
                "Do not flag a player who was not already ramboing when their melee started, until the melee window has run out. Covers the last one or two left in a fight their mates started.");

            FlagHighlightEnabled = config.Bind("CTF", "FlagHighlightEnabled", false,
                "Track flags. Admins get a glow and a label on every flag in its faction's colour, and flags go on the minimap (see FlagMinimapMarkers). Off by default, since it only matters on a flag game mode.");
            FlagMinimapMarkers = config.Bind("CTF", "FlagMinimapMarkers", true,
                "Mark flags on the minimap by the game's own rules: blue on your side, red on the enemy's, and the enemy's only while their faction is revealed. A carried flag counts as the carrier's. Needs FlagHighlightEnabled.");
            ShowFlagLabels = config.Bind("CTF", "ShowFlagLabels", true,
                "Label each flag with its faction, or with who is carrying it. Separate from the player labels.");
            FlagMinimapAlways = config.Bind("CTF", "FlagMinimapAlways", true,
                "Admins see every flag on the minimap whatever the reveal rules. Needs FlagHighlightEnabled and an rc login.");

            AfkMarkEnabled = config.Bind("AFK", "AfkMarkEnabled", true,
                "Grey out players who have not moved for a while, so a parked player is not read as someone worth watching.");
            AfkSeconds = config.Bind("AFK", "AfkSeconds", 90f,
                "Seconds without moving before a player counts as AFK.");
            AfkMoveMetres = config.Bind("AFK", "AfkMoveMetres", 0.75f,
                "Metres a player has to move to reset their AFK timer.");

            ShowRings = config.Bind("Display", "ShowRings", true,
                "Draw a ground ring under each watched player.");
            ShowLabels = config.Bind("Display", "ShowLabels", true,
                "Draw the floating name and score label over each watched player. Flag labels have their own switch, ShowFlagLabels.");
            ShowGlow = config.Bind("Display", "ShowGlow", true,
                "Glow around watched players and flags in their colour, visible through walls and terrain.");
            MaxLabels = config.Bind("Display", "MaxLabels", 12,
                "Most floating labels drawn at once, worst first.");
            RamboUi = config.Bind("Display", "RamboUi", RamboUiMode.Off,
                "When the rambo overlay shows, like the game's Admin Raygun: On, FreeflightOnly or Off. ToggleKey and the P menu button both change it, and it is remembered between launches.");
            ToggleKey = config.Bind("Display", "ToggleKey", "F6",
                "Key that hides the overlay and brings it back in the mode it was in. Any UnityEngine.KeyCode name.");

            SpectatePrevKey = config.Bind("Spectate", "SpectatePrevKey", "LeftBracket",
                "Spectate the previous watched player. The list runs worst first: RAMBO, then ISOLATED and FIGHT by danger, then AFK. Any UnityEngine.KeyCode name.");
            SpectateNextKey = config.Bind("Spectate", "SpectateNextKey", "RightBracket",
                "Spectate the next watched player, worst first. Any UnityEngine.KeyCode name.");

            ClassFilterEnabled = config.Bind("PMenu", "ClassFilterEnabled", true,
                "Let the P menu player search box filter by class, e.g. 'rifleman', 'surgeon' or 'cavalry'.");
            RegimentSearchEnabled = config.Bind("PMenu", "RegimentSearchEnabled", true,
                "Also match regiment tags in that box, ignoring punctuation and accents, so [45e] is found by 45.");
            RowActionsEnabled = config.Bind("PMenu", "RowActionsEnabled", true,
                "Add 'Reg TP' and 'Rez + Reg TP' to a player's action buttons. They send the player to the middle of their regiment's players of the same class, or to the closest friendly player if there are none.");
            FactionSearchEnabled = config.Bind("PMenu", "FactionSearchEnabled", true,
                "Also match factions and sides there, e.g. 'french', 'allied', 'attackers' or 'defenders'. Combines with a class, e.g. 'french cav'.");

            MeleeMarkerEnabled = config.Bind("KillLog", "MeleeMarkerEnabled", true,
                "Mark kills in the admin kill log with crossed swords when the victim was in a melee.");
            TeamkillFilterEnabled = config.Bind("KillLog", "TeamkillFilterEnabled", true,
                "Add a teamkills only toggle to the kill log tab, and accept 'tk' in its search box.");
            MeleeChainMetres = config.Bind("KillLog", "MeleeChainMetres", 10f,
                "How close a player must be to someone fighting to count as being in that melee themselves.");
            MeleeWindowSeconds = config.Bind("KillLog", "MeleeWindowSeconds", 10f,
                "Seconds without a hit or a block anywhere in the melee before it is treated as over.");

            MapEnabled = config.Bind("Map", "MapEnabled", false,
                "Add a Map tab to the P menu: an overhead picture of the battle with every player drawn by class in their faction's colour, and admin actions on them. Rambo outlines and flags are drawn on it while those are showing. Off by default.");

            _toggleKey = new KeyBinding(ToggleKey, KeyCode.F6);
            _spectatePrevKey = new KeyBinding(SpectatePrevKey, KeyCode.LeftBracket);
            _spectateNextKey = new KeyBinding(SpectateNextKey, KeyCode.RightBracket);

            _stamp = Stamp();
        }

        public static KeyCode ToggleKeyCode
        {
            get { return _toggleKey.Key; }
        }

        public static KeyCode SpectatePrevKeyCode
        {
            get { return _spectatePrevKey.Key; }
        }

        public static KeyCode SpectateNextKeyCode
        {
            get { return _spectateNextKey.Key; }
        }

        public static void Write<T>(ConfigEntry<T> entry, T value)
        {
            entry.Value = value;
            _stamp = Stamp();
        }

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
                return _stamp;
            }
        }

        public static bool IsExempt(PlayerClass playerClass)
        {
            RefreshExemptSet();
            return ExemptSet.Contains(playerClass);
        }

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

        private sealed class KeyBinding
        {
            private readonly ConfigEntry<string> _entry;
            private readonly KeyCode _fallback;

            private string _source;
            private KeyCode _key;

            public KeyBinding(ConfigEntry<string> entry, KeyCode fallback)
            {
                _entry = entry;
                _fallback = fallback;
                _key = fallback;
            }

            public KeyCode Key
            {
                get
                {
                    string source = _entry.Value ?? string.Empty;
                    if (source == _source) return _key;

                    _source = source;
                    _key = _fallback;

                    string name = source.Trim();
                    if (name.Length == 0) return _key;

                    try
                    {
                        _key = (KeyCode)Enum.Parse(typeof(KeyCode), name, true);
                    }
                    catch (Exception)
                    {
                        Log.Warn(_entry.Definition.Key + " '" + name + "' is not a KeyCode name. Falling back to " + _fallback + ".");
                    }

                    return _key;
                }
            }
        }
    }
}
