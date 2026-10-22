namespace AdminHelper
{
    internal static class HelperTab
    {
        private const string TabName = "AdminHelper";
        private const string MinimapHeaderName = "Minimap";

        public static void Register(MapOverlay map)
        {
            RyLib.PlayersBar.Cycle(AdminHelperMod.Guid, "Rambo UI", Settings.RamboUi, RamboText, Showing, RyLib.StockIcon.Players);
            RyLib.KeyHints.Add(AdminHelperMod.Guid, "Spectate Rambos", SpectateKeys, HintVisible);
            RyLib.KillLog.EnableActions(AdminHelperMod.Guid);
            RyLib.PMenu.StockTabVisible(AdminHelperMod.Guid, RyLib.StockTab.Rules, RulesVisible);

            RyLib.HeaderTab minimapHeader = RyLib.PMenu.Header(AdminHelperMod.Guid, MinimapHeaderName, RyLib.StockIcon.Maps);
            if (minimapHeader != null) map.Register(minimapHeader);

            RyLib.ModTab tab = RyLib.PMenu.ModTab(AdminHelperMod.Guid, TabName, RyLib.StockIcon.Shield, false);
            if (tab == null) return;

            tab.Section(AdminHelperMod.Guid, "Rambo UI", BuildRamboUi);
            tab.Section(AdminHelperMod.Guid, "Tags", BuildTags);
            tab.Section(AdminHelperMod.Guid, "Detection", BuildDetection);
            tab.Section(AdminHelperMod.Guid, "Danger", BuildDanger);
            tab.Section(AdminHelperMod.Guid, "Formation", BuildFormation);
            tab.Section(AdminHelperMod.Guid, "Melee", BuildMelee);
            tab.Section(AdminHelperMod.Guid, "Cavalry", BuildCavalry);
            tab.Section(AdminHelperMod.Guid, "AFK", BuildAfk);
            tab.Section(AdminHelperMod.Guid, "Flags", BuildFlags);
            tab.Section(AdminHelperMod.Guid, "Players Tab", BuildPlayers);
            tab.Section(AdminHelperMod.Guid, "Kill Log", BuildKillLog);
            tab.Section(AdminHelperMod.Guid, "Minimap", BuildMinimap);
            tab.Section(AdminHelperMod.Guid, "Spectate", BuildSpectate);
            tab.Section(AdminHelperMod.Guid, "Glow", BuildGlow);
            tab.Section(AdminHelperMod.Guid, "General", BuildGeneral);
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

        private static string ModeText(RamboUiMode mode)
        {
            switch (mode)
            {
                case RamboUiMode.On:
                    return "Always On";
                case RamboUiMode.FreeflightOnly:
                    return "Freeflight";
                default:
                    return "Off";
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

        private static bool RulesVisible()
        {
            return Settings.ShowRulesTab.Value;
        }

        private static UnityEngine.KeyCode[] SpectateKeys()
        {
            return new[] { Settings.SpectatePrevKeyCode, Settings.SpectateNextKeyCode };
        }

        private static void BuildRamboUi(RyLib.Section section)
        {
            section.Toggle("Compact labels", Settings.CompactLabels, RyLib.StockIcon.Players);
            section.Toggle("Labels", Settings.ShowLabels, RyLib.StockIcon.Players);
            section.Toggle("Rings", Settings.ShowRings, RyLib.StockIcon.Regiment);
            section.Toggle("Glow", Settings.ShowGlow, RyLib.StockIcon.Players);
            section.Dropdown("Rambo UI", Settings.RamboUi, ModeText);
            section.Slider("Most labels", Settings.MaxLabels, 0, 32);
            section.TextField("Toggle key", Settings.ToggleKey);
        }

        private static void BuildTags(RyLib.Section section)
        {
            section.Toggle("Rambo", Settings.ShowRamboTag, RyLib.StockIcon.Players);
            section.Toggle("Isolated", Settings.ShowIsolatedTag, RyLib.StockIcon.Players);
            section.Toggle("Melee Grace", Settings.ShowMeleeGraceTag, RyLib.StockIcon.Skull);
            section.Toggle("Cav Charge", Settings.ShowCavChargeTag, RyLib.StockIcon.Players);
            section.Toggle("AFK", Settings.ShowAfkTag, RyLib.StockIcon.Unspawned);
        }

        private static void BuildDetection(RyLib.Section section)
        {
            section.Slider("Marked at isolation", Settings.RingThreshold, 0, 100);
            section.Slider("Rambo at isolation", Settings.RamboThreshold, 0, 100);
            section.Slider("Rambo hold seconds", Settings.RamboHoldSeconds, 0f, 30f, 0.5f, "0.0");
            section.Slider("Isolation starts (m)", Settings.ClusterNearMeters, 0f, 50f, 1f, "0");
            section.Slider("Isolation full (m)", Settings.ClusterFarMeters, 5f, 100f, 1f, "0");
            section.Slider("Rise seconds", Settings.RiseSeconds, 0.5f, 30f, 0.5f, "0.0");
            section.Slider("Recover multiplier", Settings.RecoverMultiplier, 1f, 10f, 0.5f, "0.0");
            section.TextField("Exempt classes", Settings.ExemptClasses);
        }

        private static void BuildDanger(RyLib.Section section)
        {
            section.Slider("Enemy radius (m)", Settings.EnemyRadius, 5f, 100f, 1f, "0");
            section.Slider("Enemy crowd", Settings.EnemyCrowd, 1, 10);
        }

        private static void BuildFormation(RyLib.Section section)
        {
            section.Slider("Suppression", Settings.FormationSuppression, 0f, 1f, 0.05f, "0.00");
            section.Slider("Line radius (m)", Settings.LineRadius, 2f, 30f, 1f, "0");
            section.Slider("Line min mates", Settings.LineMinMates, 1, 10);
            section.Slider("Line max mates", Settings.LineMaxMates, 2, 12);
            section.Slider("Line spread (m)", Settings.LineResidual, 0.5f, 10f, 0.5f, "0.0");
            section.Slider("Cluster mates", Settings.ClusterMinMates, 1, 10);
            section.Slider("Cluster radius (m)", Settings.ClusterFormationRadius, 2f, 30f, 1f, "0");
        }

        private static void BuildMelee(RyLib.Section section)
        {
            section.Toggle("Melee grace", Settings.MeleeGraceEnabled, RyLib.StockIcon.Skull);
            section.Slider("Chain distance (m)", Settings.MeleeChainMeters, 0f, 30f, 1f, "0");
            section.Slider("Melee window seconds", Settings.MeleeWindowSeconds, 1f, 60f, 1f, "0");
        }

        private static void BuildCavalry(RyLib.Section section)
        {
            section.Toggle("Cavalry rules", Settings.CavalryRules, RyLib.StockIcon.Players);
            section.Toggle("Multicharge check", Settings.MultichargeCheck, RyLib.StockIcon.Skull);
            section.Slider("Compass range (0 = game)", Settings.CompassRangeMeters, 0f, 300f, 5f, "0");
            section.Slider("Isolation full (x range)", Settings.CavalryFarMultiplier, 1f, 5f, 0.25f, "0.00");
            section.Slider("Charge chain (m)", Settings.CavalryChainMeters, 5f, 150f, 5f, "0");
            section.Slider("Engaged within (m)", Settings.CavalryEngageMeters, 5f, 100f, 5f, "0");
            section.Slider("Multicharge riders", Settings.MultichargeMinRiders, 1, 10);
        }

        private static void BuildAfk(RyLib.Section section)
        {
            section.Toggle("AFK marks", Settings.AfkMarkEnabled, RyLib.StockIcon.Unspawned);
            section.Slider("AFK after seconds", Settings.AfkSeconds, 10f, 600f, 10f, "0");
            section.Slider("AFK move (m)", Settings.AfkMoveMeters, 0.1f, 5f, 0.05f, "0.00");
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
            section.Toggle("Rules tab", Settings.ShowRulesTab, RyLib.StockIcon.Rules);
        }

        private static void BuildKillLog(RyLib.Section section)
        {
            section.Toggle("Kill log marks", Settings.MeleeMarkerEnabled, RyLib.StockIcon.Skull);
            section.Toggle("Teamkill filter", Settings.TeamkillFilterEnabled, RyLib.StockIcon.Skull);
            section.Toggle("Color key", Settings.ColorKey, RyLib.StockIcon.Skull);
            section.Toggle("Colors without Rambo UI", Settings.KillLogColorsWithoutUi, RyLib.StockIcon.Skull);
        }

        private static void BuildMinimap(RyLib.Section section)
        {
            section.Toggle("Minimap tab", Settings.MapEnabled, RyLib.StockIcon.Maps);
            section.Toggle("Ships", RyLib.MapStyle.ShowShips, RyLib.StockIcon.Maps);
            section.Toggle("Hide ship crews", RyLib.MapStyle.HideCrew, RyLib.StockIcon.Players);
            section.Slider("Icon size", RyLib.MapStyle.IconSize, 10f, 40f, 1f, "0");
        }

        private static void BuildSpectate(RyLib.Section section)
        {
            section.TextField("Previous key", Settings.SpectatePrevKey);
            section.TextField("Next key", Settings.SpectateNextKey);
        }

        private static void BuildGlow(RyLib.Section section)
        {
            section.Toggle("Scale with distance", RyLib.GlowStyle.ScaleWithDistance, RyLib.StockIcon.Players);
            section.Slider("Outline", RyLib.GlowStyle.OutlineWidth, 0f, 3f, 0.1f, "0.0");
            section.Slider("Brightness", RyLib.GlowStyle.Brightness, 0.5f, 6f, 0.1f, "0.0");
            section.Slider("Halo", RyLib.GlowStyle.Glow, 0f, 3f, 0.1f, "0.0");
            section.Slider("Halo width", RyLib.GlowStyle.GlowWidth, 0f, 1f, 0.01f, "0.00");
        }

        private static void BuildGeneral(RyLib.Section section)
        {
            section.Toggle("Enabled", Settings.Enabled, RyLib.StockIcon.Shield);
            section.ConfirmButton("Reset to defaults", Settings.ResetAll, RyLib.StockIcon.Admin,
                "Puts every AdminHelper setting back to its default, with the glow and map settings shown here. Click twice within four seconds.");
            section.Slider("Scoring per second", Settings.TickHz, 1f, 30f, 1f, "0");
        }
    }
}
