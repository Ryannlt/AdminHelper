using System.Collections.Generic;
using HarmonyLib;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    internal sealed class KillMark
    {
        public MeleeTracker.State Melee;
        public bool Teamkill;
        public bool Multicharge;
        public string KillerColor = string.Empty;
        public string VictimColor = string.Empty;
    }

    internal static class KillLogMarks
    {
        public const string InMeleeText = "IN MELEE";
        public const string MeleeIcon = "hui-trait-sword-specialisation";
        public const string MeleeIconSize = "125%";
        public const string MultichargeIcon = "hui-trait-mounted-sword-specialist";
        public const string MultichargeText = "OUT OF SPACING";
        public const string MultichargeHex = "E03C31";

        private static readonly List<PlayerSnapshot> Riders = new List<PlayerSnapshot>();
        private static readonly List<PlayerSnapshot> Chain = new List<PlayerSnapshot>();

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

        public static bool MultichargeBroken(ClientRoundPlayer killer, ClientRoundPlayer victim, bool killerIsVictim)
        {
            if (Settings.CavalryRules == null || !Settings.CavalryRules.Value || !Settings.MultichargeCheck.Value) return false;
            if (killerIsVictim || killer == null || victim == null) return false;
            if (killer.PlayerStartData == null || victim.PlayerStartData == null) return false;
            if (killer.PlayerStartData.Faction == victim.PlayerStartData.Faction) return false;
            if (!GameAccess.IsMounted(killer)) return false;

            Vector3 origin = (killer.PlayerTransformData == null) ? Vector3.zero : killer.PlayerTransformData.position;
            int riders = ChainSize(killer.NetworkPlayerID, killer.PlayerStartData.Faction, origin, GameAccess.CompassRange());
            return riders < Mathf.Max(1, Settings.MultichargeMinRiders.Value);
        }

        private static int ChainSize(int killerId, FactionCountry faction, Vector3 origin, float range)
        {
            GameAccess.CollectPlayers(Riders);

            Chain.Clear();
            for (int i = Riders.Count - 1; i >= 0; i--)
            {
                PlayerSnapshot rider = Riders[i];
                if (rider.PlayerId == killerId || rider.Faction != faction || !rider.Mounted) Riders.RemoveAt(i);
            }

            float rangeSquared = range * range;
            int found = 1;
            int next = 0;
            Vector3 from = origin;

            while (true)
            {
                for (int i = Riders.Count - 1; i >= 0; i--)
                {
                    float dx = Riders[i].Position.x - from.x;
                    float dz = Riders[i].Position.z - from.z;
                    if (dx * dx + dz * dz > rangeSquared) continue;

                    Chain.Add(Riders[i]);
                    Riders.RemoveAt(i);
                    found++;
                }

                if (next >= Chain.Count) break;
                from = Chain[next++].Position;
            }

            Riders.Clear();
            Chain.Clear();
            return found;
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
            mark.Multicharge = KillLogMarks.MultichargeBroken(killer, victim, killerIsVictim);
            mark.KillerColor = HexOf(killer);
            mark.VictimColor = HexOf(victim);

            KillLogMarks.Record(info, mark);
            Stamp(info, mark);
        }

        private static string HexOf(ClientRoundPlayer player)
        {
            Color color;
            if (player == null || !AdminHelperMod.TryStateColor(player.NetworkPlayerID, out color)) return string.Empty;
            return ColorUtility.ToHtmlStringRGB(color);
        }

        private static void Stamp(KillLogInfo info, KillMark mark)
        {
            if (mark.Melee == MeleeTracker.State.InMelee)
            {
                string icon = RyLib.TextIcons.Tag(KillLogMarks.MeleeIcon);
                Append(info, (icon == null) ? KillLogMarks.InMeleeText : Large(icon));
            }

            if (mark.Multicharge)
            {
                string icon = RyLib.TextIcons.Tag(KillLogMarks.MultichargeIcon);
                string text = (icon == null) ? KillLogMarks.MultichargeText : Large(icon);
                Append(info, "<color=#" + KillLogMarks.MultichargeHex + ">" + text + "</color>");
            }
        }

        private static string Large(string icon)
        {
            return "</size><size=" + KillLogMarks.MeleeIconSize + ">" + icon + "</size><size=75%>";
        }

        private static void Append(KillLogInfo info, string text)
        {
            info.KillRange = string.IsNullOrEmpty(info.KillRange) ? text : info.KillRange + "  " + text;
        }

        private static bool Marked()
        {
            return Settings.MeleeMarkerEnabled != null && Settings.MeleeMarkerEnabled.Value;
        }
    }

    [HarmonyPatch(typeof(UIAdminPlayerKillLogPanelRow), nameof(UIAdminPlayerKillLogPanelRow.Setup))]
    internal static class KillLogRowColorPatch
    {
        private static void Postfix(UIAdminPlayerKillLogPanelRow __instance, KillLogInfo killLogInfo)
        {
            if (killLogInfo == null || __instance.scoreLogUiEntry == null) return;
            if (!Settings.KillLogColors) return;

            KillMark mark = KillLogMarks.For(killLogInfo);
            if (mark == null || (mark.KillerColor.Length == 0 && mark.VictimColor.Length == 0)) return;

            __instance.scoreLogUiEntry.BuildScoreLogEntry(killLogInfo.KillerIcon, Paint(killLogInfo.KillerName, mark.KillerColor),
                string.Empty, killLogInfo.KillMethod, killLogInfo.VictimIcon, Paint(killLogInfo.VictimName, mark.VictimColor),
                string.Empty, killLogInfo.KillRange, false, killLogInfo.KillerIsVictim);
        }

        private static string Paint(string name, string hex)
        {
            if (string.IsNullOrEmpty(name) || hex.Length == 0) return name;
            return "<color=#" + hex + ">" + name + "</color>";
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
            KillLogTip.Show(__instance);
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
