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

        public static ConfigEntry<float> ClusterNearMeters;
        public static ConfigEntry<float> ClusterFarMeters;

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
        public static ConfigEntry<string> ExemptClasses;

        public static ConfigEntry<bool> ShowRings;
        public static ConfigEntry<bool> ShowLabels;
        public static ConfigEntry<bool> ShowGlow;
        public static ConfigEntry<int> MaxLabels;
        public static ConfigEntry<RamboUiMode> RamboUi;
        public static ConfigEntry<string> ToggleKey;
        public static ConfigEntry<bool> CompactLabels;

        public static ConfigEntry<string> SpectatePrevKey;
        public static ConfigEntry<string> SpectateNextKey;

        public static ConfigEntry<bool> ClassFilterEnabled;
        public static ConfigEntry<bool> RegimentSearchEnabled;
        public static ConfigEntry<bool> FactionSearchEnabled;
        public static ConfigEntry<bool> RowActionsEnabled;
        public static ConfigEntry<bool> ShowRulesTab;

        public static ConfigEntry<bool> MeleeGraceEnabled;

        public static ConfigEntry<bool> FlagHighlightEnabled;
        public static ConfigEntry<bool> FlagMinimapMarkers;
        public static ConfigEntry<bool> FlagMinimapAlways;
        public static ConfigEntry<bool> ShowFlagLabels;

        public static ConfigEntry<bool> AfkMarkEnabled;
        public static ConfigEntry<float> AfkSeconds;
        public static ConfigEntry<float> AfkMoveMeters;

        public static ConfigEntry<bool> MeleeMarkerEnabled;
        public static ConfigEntry<bool> TeamkillFilterEnabled;
        public static ConfigEntry<float> MeleeChainMeters;
        public static ConfigEntry<float> MeleeWindowSeconds;
        public static ConfigEntry<bool> ColorKey;
        public static ConfigEntry<bool> KillLogColorsWithoutUi;

        public static ConfigEntry<bool> CavalryRules;
        public static ConfigEntry<float> CompassRangeMeters;
        public static ConfigEntry<float> CavalryFarMultiplier;
        public static ConfigEntry<float> CavalryChainMeters;
        public static ConfigEntry<float> CavalryEngageMeters;
        public static ConfigEntry<bool> MultichargeCheck;
        public static ConfigEntry<int> MultichargeMinRiders;

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

            ClusterNearMeters = config.Bind("Isolation", "ClusterNearMeters", 10f,
                "Distance from the midpoint of your two nearest mates at which isolation starts counting.");
            ClusterFarMeters = config.Bind("Isolation", "ClusterFarMeters", 30f,
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
                "Meters of spread either side of the best-fit line still counted as a line.");
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
            ExemptClasses = config.Bind("Flagging", "ExemptClasses", "",
                "Comma-separated PlayerClass names that are never flagged, e.g. Surgeon,Sapper.");
            MeleeGraceEnabled = config.Bind("Flagging", "MeleeGrace", true,
                "Do not flag a player who was not already ramboing when their melee started, until the melee window has run out. Covers the last one or two left in a fight their mates started.");

            FlagHighlightEnabled = config.Bind("CTF", "FlagHighlightEnabled", false,
                "Track flags. Admins get a glow on every flag in its faction's color, and flags are drawn on the Map tab. ShowFlagLabels and FlagMinimapMarkers add labels and minimap markers.");
            FlagMinimapMarkers = config.Bind("CTF", "FlagMinimapMarkers", false,
                "Mark flags on the minimap by the game's own rules: blue on your side, red on the enemy's, and the enemy's only while their faction is revealed. A carried flag counts as the carrier's. Needs FlagHighlightEnabled.");
            ShowFlagLabels = config.Bind("CTF", "ShowFlagLabels", false,
                "Label each flag with its faction, or with who is carrying it. Separate from the player labels. Needs FlagHighlightEnabled.");
            FlagMinimapAlways = config.Bind("CTF", "FlagMinimapAlways", false,
                "Admins see every flag on the minimap whatever the reveal rules. Needs FlagMinimapMarkers and an rc login.");

            AfkMarkEnabled = config.Bind("AFK", "AfkMarkEnabled", true,
                "Gray out players who have not moved for a while, so a parked player is not read as someone worth watching.");
            AfkSeconds = config.Bind("AFK", "AfkSeconds", 90f,
                "Seconds without moving before a player counts as AFK.");
            AfkMoveMeters = config.Bind("AFK", "AfkMoveMeters", 0.75f,
                "Meters a player has to move to reset their AFK timer.");

            ShowRings = config.Bind("Display", "ShowRings", true,
                "Draw a ground ring under each watched player.");
            ShowLabels = config.Bind("Display", "ShowLabels", true,
                "Draw the floating name and score label over each watched player. Flag labels have their own switch, ShowFlagLabels.");
            ShowGlow = config.Bind("Display", "ShowGlow", true,
                "Glow around watched players and flags in their color, visible through walls and terrain.");
            MaxLabels = config.Bind("Display", "MaxLabels", 12,
                "Most floating labels drawn at once, worst first.");
            RamboUi = config.Bind("Display", "RamboUi", RamboUiMode.Off,
                "When the rambo overlay shows, like the game's Admin Raygun: On, FreeflightOnly or Off. ToggleKey and the P menu button both change it, and it is remembered between launches.");
            ToggleKey = config.Bind("Display", "ToggleKey", "F6",
                "Key that hides the overlay and brings it back in the mode it was in. Any UnityEngine.KeyCode name.");
            CompactLabels = config.Bind("Display", "CompactLabels", false,
                "Labels show only the player's name, in their state color, with no state or scores.");

            SpectatePrevKey = config.Bind("Spectate", "SpectatePrevKey", "LeftBracket",
                "Spectate the previous watched player. The list runs worst first: RAMBO, then CAV CHARGE, then ISOLATED and MELEE GRACE by danger, then AFK. Any UnityEngine.KeyCode name.");
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
            ShowRulesTab = config.Bind("PMenu", "ShowRulesTab", false,
                "Show the game's Rules tab in the P menu.");

            MeleeMarkerEnabled = config.Bind("KillLog", "MeleeMarkerEnabled", true,
                "Mark kills in the admin kill log with crossed swords when the victim was in a melee.");
            TeamkillFilterEnabled = config.Bind("KillLog", "TeamkillFilterEnabled", true,
                "Add a teamkills only toggle to the kill log tab, and accept 'tk' in its search box.");
            MeleeChainMeters = config.Bind("KillLog", "MeleeChainMeters", 10f,
                "How close a player must be to someone fighting to count as being in that melee themselves.");
            MeleeWindowSeconds = config.Bind("KillLog", "MeleeWindowSeconds", 10f,
                "Seconds without a hit or a block anywhere in the melee before it is treated as over.");
            ColorKey = config.Bind("KillLog", "ColorKey", true,
                "Show a key under the kill log title that explains the name colors and icons.");
            KillLogColorsWithoutUi = config.Bind("KillLog", "KillLogColorsWithoutUi", false,
                "Color kill log names, and list the colors in the key, while Rambo UI is Off. With Rambo UI on they always show.");

            CavalryRules = config.Bind("Cavalry", "CavalryRules", true,
                "Score mounted players against the other mounted players on their side, at compass range. Off leaves mounted players unscored. A dismounted cavalryman is scored like any other class.");
            CompassRangeMeters = config.Bind("Cavalry", "CompassRangeMeters", 0f,
                "Compass range in meters. 0 reads it from the game: the distance at which a friendly drops off the compass.");
            CavalryFarMultiplier = config.Bind("Cavalry", "CavalryFarMultiplier", 2f,
                "A rider's isolation reaches full at compass range times this, measured to the nearest mounted teammate.");
            CavalryChainMeters = config.Bind("Cavalry", "CavalryChainMeters", 40f,
                "How close a mounted player must be to a mounted player in a melee to count as in it too.");
            CavalryEngageMeters = config.Bind("Cavalry", "CavalryEngageMeters", 30f,
                "A mounted player in a melee stays in it while any enemy is this close.");
            MultichargeCheck = config.Bind("Cavalry", "MultichargeCheck", true,
                "Mark a mounted player's kill in the kill log when fewer than MultichargeMinRiders riders were chained together at compass range.");
            MultichargeMinRiders = config.Bind("Cavalry", "MultichargeMinRiders", 3,
                "Riders, the killer included, that must be chained together at compass range for a mounted kill to be valid.");

            MapEnabled = config.Bind("Map", "MapEnabled", true,
                "Add a Minimap tab to the P menu: an overhead picture of the battle with every player drawn by class in their faction's color, and admin actions on them. Rambo outlines are drawn on it while the overlay is showing, and flags with FlagHighlightEnabled.");

            Migrate(config, "Isolation", "ClusterNearMetres", ClusterNearMeters);
            Migrate(config, "Isolation", "ClusterFarMetres", ClusterFarMeters);
            Migrate(config, "AFK", "AfkMoveMetres", AfkMoveMeters);
            Migrate(config, "KillLog", "MeleeChainMetres", MeleeChainMeters);
            Drop(config, "Flagging", "ScoreCavalry");
            config.Save();

            _toggleKey = new KeyBinding(ToggleKey, KeyCode.F6);
            _spectatePrevKey = new KeyBinding(SpectatePrevKey, KeyCode.LeftBracket);
            _spectateNextKey = new KeyBinding(SpectateNextKey, KeyCode.RightBracket);

            _stamp = Stamp();
        }

        public static void ResetAll()
        {
            if (_config == null) return;

            bool save = _config.SaveOnConfigSet;
            _config.SaveOnConfigSet = false;
            try
            {
                foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in _config) Reset(pair.Value);
            }
            finally
            {
                _config.SaveOnConfigSet = save;
            }

            _config.Save();
            _stamp = Stamp();

            Reset(RyLib.GlowStyle.ScaleWithDistance);
            Reset(RyLib.GlowStyle.OutlineWidth);
            Reset(RyLib.GlowStyle.Brightness);
            Reset(RyLib.GlowStyle.Glow);
            Reset(RyLib.GlowStyle.GlowWidth);
            Reset(RyLib.MapStyle.IconSize);

            Log.Info("settings reset to their defaults.");
        }

        private static void Reset(ConfigEntryBase entry)
        {
            if (entry != null) entry.BoxedValue = entry.DefaultValue;
        }

        private static void Migrate(ConfigFile config, string section, string oldKey, ConfigEntry<float> target)
        {
            ConfigEntry<float> old = config.Bind(section, oldKey, float.NaN, string.Empty);
            if (!float.IsNaN(old.Value)) target.Value = old.Value;
            config.Remove(old.Definition);
        }

        private static void Drop(ConfigFile config, string section, string oldKey)
        {
            ConfigEntry<bool> old = config.Bind(section, oldKey, false, string.Empty);
            config.Remove(old.Definition);
        }

        public static bool KillLogColors
        {
            get
            {
                if (!AdminHelperMod.CanReveal()) return false;
                return RamboUi.Value != RamboUiMode.Off || (KillLogColorsWithoutUi != null && KillLogColorsWithoutUi.Value);
            }
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
