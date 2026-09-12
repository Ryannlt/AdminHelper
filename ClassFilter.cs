using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using HoldfastGame;

namespace AdminHelper
{
    internal static class ClassFilter
    {
        public static readonly List<PlayerClass> NoMatch = new List<PlayerClass>();

        private static readonly Dictionary<string, PlayerClass> Aliases =
            new Dictionary<string, PlayerClass>(StringComparer.OrdinalIgnoreCase)
            {
                { "line", PlayerClass.ArmyLineInfantry },
                { "linf", PlayerClass.ArmyLineInfantry },
                { "lineinfantry", PlayerClass.ArmyLineInfantry },
                { "officer", PlayerClass.ArmyInfantryOfficer },
                { "off", PlayerClass.ArmyInfantryOfficer },
                { "sarge", PlayerClass.Sergeant },
                { "sgt", PlayerClass.Sergeant },
                { "light", PlayerClass.LightInfantry },
                { "medic", PlayerClass.Surgeon },
                { "doc", PlayerClass.Surgeon },
                { "engineer", PlayerClass.Sapper },
                { "flag", PlayerClass.FlagBearer },
                { "bearer", PlayerClass.FlagBearer },
                { "drummer", PlayerClass.Musician },
                { "fifer", PlayerClass.Musician },
                { "piper", PlayerClass.Musician },
                { "marine", PlayerClass.NavalMarine },
                { "captain", PlayerClass.NavalCaptain },
                { "gren", PlayerClass.Grenadier },
                { "cannon", PlayerClass.Cannoneer },
                { "gunner", PlayerClass.Cannoneer }
            };

        private static readonly Dictionary<string, PlayerClass[]> Groups =
            new Dictionary<string, PlayerClass[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "cav", new[] { PlayerClass.Hussar, PlayerClass.Dragoon } },
                { "cavalry", new[] { PlayerClass.Hussar, PlayerClass.Dragoon } },
                { "arty", new[] { PlayerClass.Cannoneer, PlayerClass.Deprecated_Rocketeer } },
                { "artillery", new[] { PlayerClass.Cannoneer, PlayerClass.Deprecated_Rocketeer } },
                { "sailor", new[] { PlayerClass.NavalSailor, PlayerClass.NavalSailor2 } },
                { "officers", new[] { PlayerClass.ArmyInfantryOfficer, PlayerClass.Sergeant, PlayerClass.NavalCaptain } },
                {
                    "naval", new[]
                    {
                        PlayerClass.NavalMarine, PlayerClass.NavalCaptain,
                        PlayerClass.NavalSailor, PlayerClass.NavalSailor2
                    }
                },
                {
                    "navy", new[]
                    {
                        PlayerClass.NavalMarine, PlayerClass.NavalCaptain,
                        PlayerClass.NavalSailor, PlayerClass.NavalSailor2
                    }
                }
            };

        private static readonly PlayerClass[] AllClasses = (PlayerClass[])Enum.GetValues(typeof(PlayerClass));
        private static readonly string[] AllNames = BuildNames();

        private static readonly List<PlayerClass> Found = new List<PlayerClass>();

        public static List<PlayerClass> Resolve(string text)
        {
            Found.Clear();

            string token = (text == null) ? string.Empty : text.Trim();

            if (token.Length < 3) return Found;

            PlayerClass[] group;
            if (Groups.TryGetValue(token, out group))
            {
                Found.AddRange(group);
                return Found;
            }

            PlayerClass alias;
            if (Aliases.TryGetValue(token, out alias))
            {
                Found.Add(alias);
                return Found;
            }

            AddByName(token, Match.Exact);
            if (Found.Count == 0) AddByName(token, Match.Prefix);
            if (Found.Count == 0) AddByName(token, Match.Contains);

            return Found;
        }

        private enum Match
        {
            Exact,
            Prefix,
            Contains
        }

        private static void AddByName(string token, Match mode)
        {
            for (int i = 0; i < AllClasses.Length; i++)
            {
                if (AllClasses[i] == PlayerClass.None) continue;

                string name = AllNames[i];
                bool hit;

                if (mode == Match.Exact) hit = string.Equals(name, token, StringComparison.OrdinalIgnoreCase);
                else if (mode == Match.Prefix) hit = name.StartsWith(token, StringComparison.OrdinalIgnoreCase);
                else hit = name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

                if (hit) Found.Add(AllClasses[i]);
            }
        }

        private static string[] BuildNames()
        {
            string[] names = new string[AllClasses.Length];
            for (int i = 0; i < AllClasses.Length; i++)
            {
                names[i] = AllClasses[i].ToString();
            }
            return names;
        }
    }

    [HarmonyPatch(typeof(UIRoundPlayersControlPanel), "UpdateFilteredLists")]
    internal static class UpdateFilteredListsPatch
    {
        private static bool Prefix(UIRoundPlayersControlPanel __instance, RoundPlayersRefreshNeeded refreshPriority,
            RoundPlayerInfoFilter ___playersInfoFilter, List<ClientRoundPlayer> ___roundPlayers,
            List<ClientRoundPlayer> ___roundPlayersListFiltered, ref RoundPlayersRefreshNeeded ___rowPlayerRefreshNeeded)
        {
            if (___playersInfoFilter == null) return true;

            RoundPlayersQuery query = ___playersInfoFilter.RoundPlayerQuery;
            if (query == null) return true;

            string text = SearchText(__instance);

            List<PlayerClass> classes = ClassFilter.NoMatch;
            if (Enabled(Settings.ClassFilterEnabled)) classes = ClassFilter.Resolve(text);

            if (classes.Count == 1)
            {
                query.PlayerName = null;
                query.PlayerID = null;
                query.PlayerClass = classes[0];
                return true;
            }

            string tag = Enabled(Settings.RegimentSearchEnabled) ? TextSearch.Normalize(text) : string.Empty;

            bool searchTags = tag.Length >= 2;

            if (classes.Count == 0 && !searchTags)
            {
                query.PlayerClass = null;
                return true;
            }

            query.PlayerClass = null;
            if (classes.Count > 0)
            {
                query.PlayerName = null;
                query.PlayerID = null;
            }

            ___roundPlayersListFiltered.Clear();

            for (int i = 0; i < ___roundPlayers.Count; i++)
            {
                ClientRoundPlayer player = ___roundPlayers[i];
                if (player == null || player.PlayerBase == null) continue;

                if (classes.Count > 0)
                {
                    PlayerSpawnData start = player.PlayerStartData;
                    if (start != null && classes.Contains(start.ClassType)) ___roundPlayersListFiltered.Add(player);
                    continue;
                }

                if (player.MatchesQuery(query) || TagMatches(player, tag)) ___roundPlayersListFiltered.Add(player);
            }

            if (__instance.playerCountText != null)
            {
                __instance.playerCountText.text = ___roundPlayersListFiltered.Count.ToString();
            }

            if (refreshPriority == RoundPlayersRefreshNeeded.Immediate)
            {
                ___rowPlayerRefreshNeeded = RoundPlayersRefreshNeeded.Immediate;
            }
            else if (___rowPlayerRefreshNeeded == RoundPlayersRefreshNeeded.None &&
                     refreshPriority == RoundPlayersRefreshNeeded.NoPlayerSelected)
            {
                ___rowPlayerRefreshNeeded = RoundPlayersRefreshNeeded.NoPlayerSelected;
            }

            return false;
        }

        private static bool TagMatches(ClientRoundPlayer player, string tag)
        {
            if (tag.Length == 0) return false;

            RoundPlayerInformation info = player.PlayerRoundInformation;
            if (info == null || info.InitialDetails == null) return false;

            PlayerInitialDetails details = info.InitialDetails;
            return TextSearch.Contains(details.RegimentTag, tag) || TextSearch.Contains(details.DisplayName, tag);
        }

        private static bool Enabled(ConfigEntry<bool> entry)
        {
            return entry != null && entry.Value;
        }

        private static string SearchText(UIRoundPlayersControlPanel panel)
        {
            if (panel == null || panel.searchFilterInputField == null) return null;
            return panel.searchFilterInputField.text;
        }
    }
}
