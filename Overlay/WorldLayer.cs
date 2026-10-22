using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    internal enum RamboTag
    {
        CavCharge,
        Afk,
        Rambo,
        MeleeGrace,
        Isolated
    }

    internal sealed class WorldLayer
    {
        private const float LayerHz = 10f;
        private const float PlayerRing = 1.6f;
        private const float HorseRing = 2.6f;
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
                if (scored.PlayerId == local || !Shown(scored)) continue;

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
                mark.RingRadius = rings ? (scored.Mounted ? HorseRing : PlayerRing) : 0f;
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
            switch (Tag(scored))
            {
                case RamboTag.CavCharge: return "CAV CHARGE";
                case RamboTag.Afk: return "AFK " + Mathf.FloorToInt(scored.AfkSeconds) + "s";
                case RamboTag.Rambo: return "RAMBO: " + Mathf.FloorToInt(scored.DwellSeconds) + "s";
                case RamboTag.MeleeGrace: return "MELEE GRACE";
                default: return "ISOLATED";
            }
        }

        internal static RamboTag Tag(ScoredPlayer scored)
        {
            if (scored.CavCharge) return RamboTag.CavCharge;
            if (scored.Afk) return RamboTag.Afk;
            if (scored.Flagged) return RamboTag.Rambo;
            return scored.InHonestMelee ? RamboTag.MeleeGrace : RamboTag.Isolated;
        }

        internal static bool Shown(ScoredPlayer scored)
        {
            switch (Tag(scored))
            {
                case RamboTag.CavCharge: return Settings.ShowCavChargeTag.Value;
                case RamboTag.Afk: return Settings.ShowAfkTag.Value;
                case RamboTag.Rambo: return Settings.ShowRamboTag.Value;
                case RamboTag.MeleeGrace: return Settings.ShowMeleeGraceTag.Value;
                default: return Settings.ShowIsolatedTag.Value;
            }
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
