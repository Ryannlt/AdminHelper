using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]

namespace AdminHelper
{
    [BepInPlugin(Guid, "AdminHelper", "1.2.0")]
    [BepInDependency(RyLib.RyLibPlugin.Guid)]
    public class AdminHelperMod : BaseUnityPlugin
    {
        public const string Guid = "com.ryannlt.adminhelper";

        private const float RememberSeconds = 3f;

        private struct Remembered
        {
            public Color Colour;
            public float Time;
        }

        private static readonly IsolationTracker Tracker = new IsolationTracker();
        private static readonly Dictionary<int, Remembered> Recent = new Dictionary<int, Remembered>();
        private static readonly List<int> Stale = new List<int>();
        private readonly FlagTracker _flags = new FlagTracker();
        private readonly Hotkey _hotkey = new Hotkey();

        private WorldLayer _world;
        private MinimapLayer _map;

        private Driver _driver;
        private float _accumulator;
        private bool _wasInRound;

        private void Awake()
        {
            Settings.Create(Config);
            _world = new WorldLayer(Tracker, _flags);
            _world.Register();
            _map = new MinimapLayer(_flags);
            _map.Register();
            HelperTab.Register(new MapOverlay(Tracker, _flags));
            RowActions.Register();

            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureDriver();
            PatchPMenu();

            Log.Info("Ready. Toggle key " + Settings.ToggleKeyCode + ", overlay " + Settings.RamboUi.Value +
                     ", RequireAdminLogin=" + Settings.RequireAdminLogin.Value);
        }

        private void OnDestroy()
        {
            Log.Info("plugin component destroyed");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.Info("scene loaded " + scene.name + " driverAlive=" + (_driver != null));
            EnsureDriver();

            GameAccess.ClearSceneCache();
            MeleeTracker.Reset();
            RowActions.Reset();
            Tracker.Reset();
            _flags.Reset();
            _accumulator = 0f;
            _wasInRound = false;
        }

        private void EnsureDriver()
        {
            if (_driver != null) return;
            _driver = Driver.Attach(this);
        }

        private static void PatchPMenu()
        {
            try
            {
                Harmony.CreateAndPatchAll(typeof(AdminHelperMod).Assembly, Guid);
            }
            catch (Exception error)
            {
                Log.Error("P menu class filter could not be patched in: " + error.Message);
            }
        }

        internal void Tick()
        {
            Settings.PollForExternalEdits();
            RowActions.Tick();

            if (!Settings.Enabled.Value)
            {
                _world.Visible = false;
                _map.Visible = false;
                return;
            }

            _hotkey.Poll();

            bool inRound = GameAccess.InRound;
            if (!inRound)
            {
                if (_wasInRound) Tracker.Reset();
                _wasInRound = false;
                _flags.Flags.Clear();
                _world.Visible = false;
                _map.Visible = false;
                return;
            }

            _wasInRound = true;

            MeleeTracker.Tick();
            KillLogTabs.Warm();

            _accumulator += Time.deltaTime;

            float interval = 1f / Mathf.Clamp(Settings.TickHz.Value, 1f, 30f);
            if (_accumulator >= interval)
            {
                Tracker.Tick(_accumulator);
                _accumulator = 0f;
                Remember();
            }

            if (CanReveal()) RamboSpectate.Poll(Tracker.Watched);

            bool visible = OverlayVisible;
            bool worldFlags = CanReveal() && visible && Settings.FlagHighlightEnabled.Value;
            bool mapFlags = Settings.FlagHighlightEnabled.Value && Settings.FlagMinimapMarkers.Value;
            if (worldFlags || mapFlags) _flags.Tick();
            else _flags.Flags.Clear();

            _world.Visible = CanReveal() && visible;
            _world.ShowFlags = worldFlags;
            _map.Visible = mapFlags;
        }

        internal static bool OverlayVisible
        {
            get
            {
                switch (Settings.RamboUi.Value)
                {
                    case RamboUiMode.On:
                        return true;
                    case RamboUiMode.FreeflightOnly:
                        return GameAccess.InFreeflight;
                    default:
                        return false;
                }
            }
        }

        internal static bool TryStateColour(int playerId, out Color colour)
        {
            colour = Color.clear;

            for (int i = 0; i < Tracker.Watched.Count; i++)
            {
                ScoredPlayer scored = Tracker.Watched[i];
                if (scored.PlayerId != playerId) continue;

                colour = WorldLayer.StateColour(scored);
                return true;
            }

            Remembered last;
            if (!Recent.TryGetValue(playerId, out last) || Time.time - last.Time > RememberSeconds) return false;

            colour = last.Colour;
            return true;
        }

        private static void Remember()
        {
            float now = Time.time;
            for (int i = 0; i < Tracker.Watched.Count; i++)
            {
                ScoredPlayer scored = Tracker.Watched[i];

                Remembered entry;
                entry.Colour = WorldLayer.StateColour(scored);
                entry.Time = now;
                Recent[scored.PlayerId] = entry;
            }

            Stale.Clear();
            foreach (KeyValuePair<int, Remembered> pair in Recent)
            {
                if (now - pair.Value.Time > RememberSeconds) Stale.Add(pair.Key);
            }

            for (int i = 0; i < Stale.Count; i++) Recent.Remove(Stale[i]);
        }

        internal static bool CanReveal()
        {
            return !Settings.RequireAdminLogin.Value || GameAccess.IsLoggedInAdmin;
        }
    }
}
