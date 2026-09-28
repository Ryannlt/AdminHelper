using System.Collections.Generic;

namespace AdminHelper
{
    internal sealed class MapOverlay
    {
        private static readonly List<ScoredPlayer> NoPlayers = new List<ScoredPlayer>();

        private readonly IsolationTracker _tracker;
        private readonly FlagTracker _flags;

        public MapOverlay(IsolationTracker tracker, FlagTracker flags)
        {
            _tracker = tracker;
            _flags = flags;
        }

        public void Register(RyLib.HeaderTab header)
        {
            RyLib.Page map = header.MapPage(AdminHelperMod.Guid, "Map", RyLib.StockIcon.Maps, Shown);
            if (map != null) map.MapLayer(AdminHelperMod.Guid, Fill);
        }

        private static bool Shown()
        {
            return Settings.MapEnabled.Value;
        }

        private void Fill(List<RyLib.MapMark> marks)
        {
            if (!AdminHelperMod.CanReveal()) return;

            List<ScoredPlayer> watched = AdminHelperMod.OverlayVisible ? _tracker.Watched : NoPlayers;
            for (int i = 0; i < watched.Count; i++)
            {
                ScoredPlayer scored = watched[i];

                RyLib.MapMark mark = new RyLib.MapMark();
                mark.Key = "player-" + scored.PlayerId;
                mark.OnPlayer = true;
                mark.PlayerId = scored.PlayerId;
                mark.Shape = RyLib.MapShape.Outline;
                mark.Colour = WorldLayer.StateColour(scored);
                mark.Tooltip = WorldLayer.State(scored) + "\nISO " + scored.Isolation + "  DGR " + scored.Danger;
                marks.Add(mark);
            }

            if (!Settings.FlagHighlightEnabled.Value) return;

            List<FlagMark> flags = _flags.Flags;
            for (int i = 0; i < flags.Count; i++)
            {
                FlagMark flag = flags[i];
                if (flag.Follow == null) continue;

                RyLib.MapMark mark = new RyLib.MapMark();
                mark.Key = (flag.Carried ? "carried-" : "flag-") + flag.Key;
                mark.PlayerId = -1;
                mark.Position = flag.Follow.transform.position;
                mark.Shape = RyLib.MapShape.Flag;
                mark.Colour = WorldLayer.FactionColour(flag.Type);
                mark.Tooltip = flag.Carried ? "FLAG: " + flag.Name : flag.Name + " FLAG";
                marks.Add(mark);
            }
        }
    }
}
