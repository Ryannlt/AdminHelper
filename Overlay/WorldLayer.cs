using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    internal sealed class WorldLayer
    {
        private const float LayerHz = 10f;
        private const float PlayerRing = 1.6f;
        private const float PlayerLabelHeight = 2.1f;
        private const float FlagLabelHeight = 2.6f;

        internal static readonly Color CalmColor = new Color(0.35f, 0.9f, 0.3f);
        internal static readonly Color IsolatedColor = new Color(1f, 0.85f, 0.15f);
        internal static readonly Color DangerColor = new Color(1f, 0.55f, 0.1f);
        internal static readonly Color DeadlyColor = new Color(1f, 0.15f, 0.12f);
        internal static readonly Color AfkColor = new Color(0.6f, 0.62f, 0.65f);
        internal static readonly Color MeleeColor = new Color(0.55f, 0.85f, 1f);
        internal static readonly Color CavChargeColor = new Color(0.78f, 0.6f, 1f);
        private static readonly Color FlagColor = new Color(0.3f, 0.95f, 1f);

        private readonly IsolationTracker _tracker;
        private readonly FlagTracker _flags;

        public bool Visible;
        public bool ShowFlags;

        public WorldLayer(IsolationTracker tracker, FlagTracker flags)
        {
            _tracker = tracker;
            _flags = flags;
        }

        public void Register()
        {
            RyLib.World.Layer(AdminHelperMod.Guid, Fill, LayerHz);
        }

        private void Fill(List<RyLib.WorldMark> marks)
        {
            if (!Visible) return;

            bool rings = Settings.ShowRings.Value;
            bool labels = Settings.ShowLabels.Value;
            bool flagLabels = Settings.ShowFlagLabels.Value;
            bool glow = Settings.ShowGlow.Value;

            List<FlagMark> flags = _flags.Flags;
            for (int i = 0; ShowFlags && i < flags.Count; i++)
            {
                FlagMark flag = flags[i];

                RyLib.WorldMark mark = new RyLib.WorldMark();
                mark.Key = (flag.Carried ? "carried-" : "flag-") + flag.Key;
                mark.Follow = (flag.Follow == null) ? null : flag.Follow.transform;
                mark.Position = flag.Position;
                mark.Glow = glow ? flag.Glow : null;
                mark.Color = FactionColor(flag.Type);
                mark.Label = flagLabels ? (flag.Carried ? "FLAG: " + flag.Name : flag.Name + " FLAG") : null;
                mark.LabelHeight = FlagLabelHeight;
                marks.Add(mark);
            }

            int local = GameAccess.LocalPlayerId;
            int labeled = 0;
            int maxLabels = Mathf.Max(0, Settings.MaxLabels.Value);

            List<ScoredPlayer> watched = _tracker.Watched;
            for (int i = 0; i < watched.Count; i++)
            {
                ScoredPlayer scored = watched[i];
                if (scored.PlayerId == local) continue;

                bool text = labels && labeled < maxLabels;
                if (text) labeled++;

                RyLib.WorldMark mark = new RyLib.WorldMark();
                mark.Key = "player-" + scored.PlayerId;
                mark.Follow = (scored.Body == null) ? null : scored.Body.transform;
                mark.Position = scored.Position;
                mark.Glow = glow ? scored.Body : null;
                mark.Color = StateColor(scored);
                mark.Label = text ? Describe(scored) : null;
                mark.LabelHeight = PlayerLabelHeight;
                mark.RingRadius = rings ? PlayerRing : 0f;
                marks.Add(mark);
            }
        }

        private static string Describe(ScoredPlayer scored)
        {
            if (Settings.CompactLabels.Value) return scored.Name;
            return scored.Name + "\n" + State(scored) + "\nISO: " + scored.Isolation + "  DGR: " + scored.Danger;
        }

        internal static string State(ScoredPlayer scored)
        {
            if (scored.CavCharge) return "CAV CHARGE";
            if (scored.Afk) return "AFK " + Mathf.FloorToInt(scored.AfkSeconds) + "s";
            if (scored.Flagged) return "RAMBO: " + Mathf.FloorToInt(scored.DwellSeconds) + "s";
            return scored.InHonestMelee ? "MELEE GRACE" : "ISOLATED";
        }

        internal static Color FactionColor(CarryableObjectType type)
        {
            FactionCountry faction = FlagTypes.Faction(type);
            if (faction == FactionCountry.None || faction == FactionCountry.Allied || faction == FactionCountry.Central) return FlagColor;
            return RyLib.Factions.Color(faction);
        }

        internal static Color StateColor(ScoredPlayer scored)
        {
            if (scored.CavCharge) return CavChargeColor;
            if (scored.Afk) return AfkColor;
            if (scored.InHonestMelee) return MeleeColor;
            if (scored.Danger > 0) return Color.Lerp(DangerColor, DeadlyColor, Mathf.Clamp01(scored.Danger / 100f));

            float isolated = Mathf.InverseLerp(Settings.RingThreshold.Value, 100f, scored.Isolation);
            return Color.Lerp(CalmColor, IsolatedColor, isolated);
        }
    }
}
