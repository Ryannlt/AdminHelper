using System.Collections.Generic;
using HarmonyLib;
using HoldfastGame;

namespace AdminHelper
{
    internal sealed class KillMark
    {
        public MeleeTracker.State Melee;
        public bool Teamkill;
    }

    internal static class KillLogMarks
    {
        public const string InMeleeText = "IN MELEE";
        public const string NoMeleeText = "NO MELEE";

        private static readonly Dictionary<KillLogInfo, KillMark> Marks = new Dictionary<KillLogInfo, KillMark>();

        public static bool TeamkillOnly;

        public static KillMark For(KillLogInfo info)
        {
            if (info == null) return null;

            KillMark mark;
            return Marks.TryGetValue(info, out mark) ? mark : null;
        }

        public static void Record(KillLogInfo info, KillMark mark)
        {
            if (info == null || mark == null) return;
            Marks[info] = mark;
        }

        public static void Clear()
        {
            Marks.Clear();
            TeamkillOnly = false;
        }

        public static bool IsTeamkill(ClientRoundPlayer killer, ClientRoundPlayer victim, bool killerIsVictim)
        {
            if (killerIsVictim || killer == null || victim == null) return false;
            if (killer.NetworkPlayerID == victim.NetworkPlayerID) return false;
            if (killer.PlayerStartData == null || victim.PlayerStartData == null) return false;

            return killer.PlayerStartData.Faction == victim.PlayerStartData.Faction;
        }
    }

    [HarmonyPatch(typeof(UIAdminPlayerKillLogPanel), "AddKillLog")]
    internal static class AddKillLogPatch
    {
        private static void Postfix(ClientRoundPlayer killer, ClientRoundPlayer victim, bool killerIsVictim,
            List<KillLogInfo> ___queuedKillLogInfo)
        {
            if (___queuedKillLogInfo == null || ___queuedKillLogInfo.Count == 0) return;

            KillLogInfo info = ___queuedKillLogInfo[0];
            if (info == null) return;

            KillMark mark = new KillMark();
            mark.Teamkill = KillLogMarks.IsTeamkill(killer, victim, killerIsVictim);
            mark.Melee = Marked() && victim != null
                ? MeleeTracker.Query(victim.NetworkPlayerID)
                : MeleeTracker.State.Unknown;

            KillLogMarks.Record(info, mark);
            Stamp(info, mark);
        }

        private static void Stamp(KillLogInfo info, KillMark mark)
        {
            string text;
            if (mark.Melee == MeleeTracker.State.InMelee) text = KillLogMarks.InMeleeText;
            else if (mark.Melee == MeleeTracker.State.NotInMelee) text = KillLogMarks.NoMeleeText;
            else return;

            info.KillRange = string.IsNullOrEmpty(info.KillRange) ? text : info.KillRange + "  " + text;
        }

        private static bool Marked()
        {
            return Settings.MeleeMarkerEnabled != null && Settings.MeleeMarkerEnabled.Value;
        }
    }

    [HarmonyPatch(typeof(PlayerDamageManager), "ProcessPlayerHealthChanged")]
    internal static class MeleeHitPatch
    {
        private static void Postfix(RoundPlayer hitPlayer, PlayerHealthChangedEventArgs args)
        {
            if (hitPlayer == null || args == null) return;

            PlayerHealthChangedReason reason = args.ChangeReason;
            if (reason != PlayerHealthChangedReason.HitByMeleeWeapon &&
                reason != PlayerHealthChangedReason.HitByMeleeSecondaryAttack) return;

            HitByMeleeWeaponPlayerHealthChangedData melee = MeleeData(args);
            if (melee == null)
            {
                MeleeTracker.Touch(hitPlayer.NetworkPlayerID);
                return;
            }

            RoundPlayer attacker = GameAccess.ResolvePlayer(melee.AttackingPlayerID);
            if (!GameAccess.AreEnemies(attacker, hitPlayer)) return;

            MeleeTracker.Touch(hitPlayer.NetworkPlayerID);
            MeleeTracker.Touch(melee.AttackingPlayerID);
        }

        private static HitByMeleeWeaponPlayerHealthChangedData MeleeData(PlayerHealthChangedEventArgs args)
        {
            if (args.Packet == null) return null;
            return args.Packet.HealthChangedData as HitByMeleeWeaponPlayerHealthChangedData;
        }
    }

    [HarmonyPatch(typeof(PlayerDamageManager), nameof(PlayerDamageManager.MeleeAttackBlockedRPC))]
    internal static class MeleeBlockedPatch
    {
        private static void Postfix(PlayerMeleeBlockedData data)
        {
            if (!GameAccess.AreEnemies(data.AttackingPlayerID, data.BlockingPlayerID)) return;

            MeleeTracker.Touch(data.AttackingPlayerID);
            MeleeTracker.Touch(data.BlockingPlayerID);
        }
    }

    [HarmonyPatch(typeof(UIAdminPlayerKillLogPanel), "UpdateFilteredPlayers")]
    internal static class KillLogFilterPatch
    {
        private static bool Prefix(string searchText, UIAdminPlayerKillLogPanelRow[] ___allRows, int ___currentActiveRows)
        {
            if (___allRows == null) return true;

            string text = (searchText == null) ? string.Empty : searchText.Trim();

            bool keyword = text.Equals("tk", System.StringComparison.OrdinalIgnoreCase) ||
                           text.Equals("teamkill", System.StringComparison.OrdinalIgnoreCase) ||
                           text.Equals("teamkills", System.StringComparison.OrdinalIgnoreCase);

            bool teamkillOnly = Enabled() && (KillLogMarks.TeamkillOnly || keyword);
            if (!teamkillOnly) return true;

            string search = keyword ? string.Empty : text;

            for (int i = 0; i < ___currentActiveRows && i < ___allRows.Length; i++)
            {
                UIAdminPlayerKillLogPanelRow row = ___allRows[i];
                if (row == null) continue;

                KillLogInfo info = row.CurrentInfo;
                KillMark mark = KillLogMarks.For(info);

                bool teamkill = mark != null && mark.Teamkill;
                bool matches = teamkill && (search.Length == 0 || NameMatches(info, search));

                row.gameObject.SetActive(matches);
            }

            return false;
        }

        private static bool NameMatches(KillLogInfo info, string search)
        {
            if (info == null) return false;

            return dfStringExtensions.Contains(info.KillerName, search, caseInsensitive: true) ||
                   dfStringExtensions.Contains(info.VictimName, search, caseInsensitive: true);
        }

        private static bool Enabled()
        {
            return Settings.TeamkillFilterEnabled != null && Settings.TeamkillFilterEnabled.Value;
        }
    }

    [HarmonyPatch(typeof(UIAdminPlayerKillLogPanel), nameof(UIAdminPlayerKillLogPanel._ToggleShowing))]
    internal static class KillLogToggleShowingPatch
    {
        private static void Postfix(UIAdminPlayerKillLogPanel __instance, bool show)
        {
            if (!show)
            {
                KillLogTabs.Hide();
                return;
            }

            KillLogTabs.Show(__instance);
        }
    }

    [HarmonyPatch(typeof(UIAdminPlayerKillLogPanel), nameof(UIAdminPlayerKillLogPanel._ResetObject))]
    internal static class KillLogResetPatch
    {
        private static void Postfix()
        {
            KillLogMarks.Clear();
            MeleeTracker.Reset();

            KillLogTabs.Forget();
        }
    }
}
