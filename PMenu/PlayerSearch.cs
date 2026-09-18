using System;
using System.Collections.Generic;
using HoldfastGame;

namespace AdminHelper
{
    internal sealed class SearchTerms
    {
        public readonly List<PlayerClass> Classes = new List<PlayerClass>();
        public readonly List<FactionCountry> Factions = new List<FactionCountry>();

        public bool Resolved;

        public bool Any
        {
            get { return Classes.Count > 0 || Factions.Count > 0; }
        }

        public bool Matches(PlayerSpawnData start)
        {
            if (start == null) return false;
            if (Classes.Count > 0 && !Classes.Contains(start.ClassType)) return false;
            if (Factions.Count > 0 && !Factions.Contains(start.Faction)) return false;
            return true;
        }

        public void Clear()
        {
            Classes.Clear();
            Factions.Clear();
            Resolved = false;
        }
    }

    internal static class PlayerSearch
    {
        private const int MaxTokens = 3;

        private static readonly char[] Separators = { ' ', ',', '+', '&', '/' };

        private static readonly HashSet<string> Attackers =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "attacker", "attacking", "attack", "att", "atk", "offence", "offense"
            };

        private static readonly HashSet<string> Defenders =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "defender", "defending", "defend", "def", "defence", "defense"
            };

        private static readonly Dictionary<string, FactionCountry> FactionAliases =
            new Dictionary<string, FactionCountry>(StringComparer.OrdinalIgnoreCase)
            {
                { "britain", FactionCountry.British },
                { "england", FactionCountry.British },
                { "redcoat", FactionCountry.British },
                { "france", FactionCountry.French },
                { "prussia", FactionCountry.Prussian },
                { "russia", FactionCountry.Russian },
                { "italy", FactionCountry.Italian },
                { "austria", FactionCountry.Austrian },
                { "spain", FactionCountry.Spanish },
                { "allies", FactionCountry.Allied },
                { "entente", FactionCountry.Allied },
                { "centralpowers", FactionCountry.Central },
                { "pirate", FactionCountry.Privateer },
                { "america", FactionCountry.ARAmerican },
                { "american", FactionCountry.ARAmerican },
                { "usa", FactionCountry.ARAmerican },
                { "colonial", FactionCountry.ARAmerican }
            };

        private static readonly FactionCountry[] AllFactions = (FactionCountry[])Enum.GetValues(typeof(FactionCountry));
        private static readonly string[] FactionNames = BuildNames();

        private static readonly SearchTerms Terms = new SearchTerms();
        private static readonly List<FactionCountry> Found = new List<FactionCountry>();

        public static SearchTerms Resolve(string text, bool classes, bool factions)
        {
            Terms.Clear();

            if (string.IsNullOrEmpty(text)) return Terms;

            string[] tokens = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0 || tokens.Length > MaxTokens) return Terms;

            for (int i = 0; i < tokens.Length; i++)
            {
                if (ResolveToken(tokens[i], classes, factions)) continue;

                Terms.Clear();
                return Terms;
            }

            Terms.Resolved = Terms.Any;
            return Terms;
        }

        private static bool ResolveToken(string token, bool classes, bool factions)
        {
            if (TryToken(token, classes, factions)) return true;

            string singular = Singular(token);
            return singular != null && TryToken(singular, classes, factions);
        }

        private static bool TryToken(string token, bool classes, bool factions)
        {
            if (factions && ResolveFactions(token))
            {
                for (int i = 0; i < Found.Count; i++)
                {
                    if (!Terms.Factions.Contains(Found[i])) Terms.Factions.Add(Found[i]);
                }

                return true;
            }

            if (!classes) return false;

            List<PlayerClass> resolved = ClassFilter.Resolve(token);
            if (resolved.Count == 0) return false;

            for (int i = 0; i < resolved.Count; i++)
            {
                if (!Terms.Classes.Contains(resolved[i])) Terms.Classes.Add(resolved[i]);
            }

            return true;
        }

        private static string Singular(string token)
        {
            if (token.Length < 5) return null;
            if (!token.EndsWith("s", StringComparison.OrdinalIgnoreCase)) return null;
            return token.Substring(0, token.Length - 1);
        }

        private static bool ResolveFactions(string token)
        {
            Found.Clear();

            if (token.Length < 3) return false;

            if (Attackers.Contains(token)) return AddSide(GameAccess.AttackingFaction);
            if (Defenders.Contains(token)) return AddSide(GameAccess.DefendingFaction);

            FactionCountry alias;
            if (FactionAliases.TryGetValue(token, out alias))
            {
                Found.Add(alias);
                return true;
            }

            AddByName(token, true);
            if (Found.Count == 0) AddByName(token, false);

            return Found.Count > 0;
        }

        private static bool AddSide(FactionCountry faction)
        {
            if (faction == FactionCountry.None) return false;

            Found.Add(faction);
            return true;
        }

        private static void AddByName(string token, bool exact)
        {
            for (int i = 0; i < AllFactions.Length; i++)
            {
                if (AllFactions[i] == FactionCountry.None) continue;

                string name = FactionNames[i];
                if (name.StartsWith("Internal_", StringComparison.Ordinal)) continue;

                bool hit = exact
                    ? string.Equals(name, token, StringComparison.OrdinalIgnoreCase)
                    : name.StartsWith(token, StringComparison.OrdinalIgnoreCase);

                if (hit) Found.Add(AllFactions[i]);
            }
        }

        private static string[] BuildNames()
        {
            string[] names = new string[AllFactions.Length];
            for (int i = 0; i < AllFactions.Length; i++)
            {
                names[i] = AllFactions[i].ToString();
            }
            return names;
        }
    }
}
