namespace AdminHelper
{
    internal static class HelperTab
    {
        private const string TabName = "AdminHelper";
        private const string MapHeaderName = "Map";

        public static void Register(MapOverlay map)
        {
            RyLib.PlayersBar.Cycle(AdminHelperMod.Guid, "Rambo UI", Settings.RamboUi, RamboText, Showing, RyLib.StockIcon.Players);
            RyLib.KeyHints.Add(AdminHelperMod.Guid, "Spectate Rambos", SpectateKeys, HintVisible);
            RyLib.KillLog.EnableActions(AdminHelperMod.Guid);

            RyLib.HeaderTab mapHeader = RyLib.PMenu.Header(AdminHelperMod.Guid, MapHeaderName, RyLib.StockIcon.Maps);
            if (mapHeader != null) map.Register(mapHeader);

            RyLib.ModTab tab = RyLib.PMenu.ModTab(AdminHelperMod.Guid, TabName, RyLib.StockIcon.Shield);
            if (tab == null) return;

            tab.Section(AdminHelperMod.Guid, "Rambo Detection", BuildRambo);
            tab.Section(AdminHelperMod.Guid, "Flags", BuildFlags);
            tab.Section(AdminHelperMod.Guid, "Players Tab", BuildPlayers);
            tab.Section(AdminHelperMod.Guid, "Kill Log", BuildKillLog);
            tab.Section(AdminHelperMod.Guid, "Map", BuildMap);
            tab.Section(AdminHelperMod.Guid, "Glow Tuning - left click raises, right click lowers", BuildGlow);
        }

        private static string RamboText(RamboUiMode mode)
        {
            switch (mode)
            {
                case RamboUiMode.On:
                    return "Rambo UI Always On";
                case RamboUiMode.FreeflightOnly:
                    return "Rambo UI Freeflight";
                default:
                    return "Rambo UI Off";
            }
        }

        private static bool Showing(RamboUiMode mode)
        {
            return mode != RamboUiMode.Off;
        }

        private static bool HintVisible()
        {
            return Showing(Settings.RamboUi.Value) && AdminHelperMod.CanReveal();
        }

        private static UnityEngine.KeyCode[] SpectateKeys()
        {
            return new[] { Settings.SpectatePrevKeyCode, Settings.SpectateNextKeyCode };
        }

        private static void BuildGlow(RyLib.Section section)
        {
            section.Toggle("Scale with distance", RyLib.GlowStyle.ScaleWithDistance, RyLib.StockIcon.Players);
            section.Stepper("Outline", RyLib.GlowStyle.OutlineWidth, 0.1f, "0.0", RyLib.StockIcon.Players);
            section.Stepper("Brightness", RyLib.GlowStyle.Brightness, 0.2f, "0.0", RyLib.StockIcon.Players);
            section.Stepper("Halo", RyLib.GlowStyle.Glow, 0.1f, "0.0", RyLib.StockIcon.Players);
            section.Stepper("Halo width", RyLib.GlowStyle.GlowWidth, 0.02f, "0.00", RyLib.StockIcon.Players);
        }

        private static void BuildRambo(RyLib.Section section)
        {
            section.Cycle("Rambo UI", Settings.RamboUi, RamboText, Showing, RyLib.StockIcon.Players);
            section.Toggle("Labels", Settings.ShowLabels, RyLib.StockIcon.Players);
            section.Toggle("Rings", Settings.ShowRings, RyLib.StockIcon.Regiment);
            section.Toggle("Glow", Settings.ShowGlow, RyLib.StockIcon.Players);
            section.Toggle("AFK marks", Settings.AfkMarkEnabled, RyLib.StockIcon.Unspawned);
            section.Toggle("Melee grace", Settings.MeleeGraceEnabled, RyLib.StockIcon.Skull);
        }

        private static void BuildFlags(RyLib.Section section)
        {
            section.Toggle("Flag highlight", Settings.FlagHighlightEnabled, RyLib.StockIcon.Maps);
            section.Toggle("Flag labels", Settings.ShowFlagLabels, RyLib.StockIcon.Maps);
            section.Toggle("Flag minimap", Settings.FlagMinimapMarkers, RyLib.StockIcon.Maps);
            section.Toggle("All flags (admin)", Settings.FlagMinimapAlways, RyLib.StockIcon.Maps);
        }

        private static void BuildPlayers(RyLib.Section section)
        {
            section.Toggle("Class search", Settings.ClassFilterEnabled, RyLib.StockIcon.Players);
            section.Toggle("Regiment search", Settings.RegimentSearchEnabled, RyLib.StockIcon.Regiment);
            section.Toggle("Faction search", Settings.FactionSearchEnabled, RyLib.StockIcon.Shield);
            section.Toggle("Row buttons", Settings.RowActionsEnabled, RyLib.StockIcon.Admin);
        }

        private static void BuildKillLog(RyLib.Section section)
        {
            section.Toggle("Kill log marks", Settings.MeleeMarkerEnabled, RyLib.StockIcon.Skull);
            section.Toggle("Teamkill filter", Settings.TeamkillFilterEnabled, RyLib.StockIcon.Skull);
        }

        private static void BuildMap(RyLib.Section section)
        {
            section.Toggle("Map tab", Settings.MapEnabled, RyLib.StockIcon.Maps);
            section.Stepper("Map icon size", RyLib.MapStyle.IconSize, 2f, "0", RyLib.StockIcon.Maps);
        }
    }
}
