using System.Collections.Generic;
using UnityEngine;

namespace AdminHelper
{
    internal static class RamboSpectate
    {
        private static readonly List<ScoredPlayer> Candidates = new List<ScoredPlayer>();

        public static void Poll(List<ScoredPlayer> watched)
        {
            bool next = Input.GetKeyDown(Settings.SpectateNextKeyCode);
            bool previous = Input.GetKeyDown(Settings.SpectatePrevKeyCode);
            if (next == previous || GameAccess.IsTyping) return;

            Candidates.Clear();
            Candidates.AddRange(watched);
            if (Candidates.Count == 0) return;

            Candidates.Sort(Worst);

            int index = IndexOf(GameAccess.SpectatedPlayerId);
            int target;
            if (index < 0) target = next ? 0 : Candidates.Count - 1;
            else target = (index + (next ? 1 : Candidates.Count - 1)) % Candidates.Count;

            GameAccess.Spectate(Candidates[target].PlayerId);
        }

        private static int IndexOf(int playerId)
        {
            if (playerId < 0) return -1;

            for (int i = 0; i < Candidates.Count; i++)
            {
                if (Candidates[i].PlayerId == playerId) return i;
            }

            return -1;
        }

        private static int Worst(ScoredPlayer a, ScoredPlayer b)
        {
            int tier = Tier(a).CompareTo(Tier(b));
            if (tier != 0) return tier;

            int danger = b.Danger.CompareTo(a.Danger);
            if (danger != 0) return danger;

            int isolation = b.Isolation.CompareTo(a.Isolation);
            if (isolation != 0) return isolation;

            return a.PlayerId.CompareTo(b.PlayerId);
        }

        private static int Tier(ScoredPlayer player)
        {
            if (player.Afk) return 2;
            return player.Flagged ? 0 : 1;
        }
    }
}
