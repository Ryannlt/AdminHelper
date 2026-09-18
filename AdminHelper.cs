using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: AssemblyVersion("1.1.1.0")]
[assembly: AssemblyFileVersion("1.1.1.0")]

namespace AdminHelper
{
    [BepInPlugin(Guid, "AdminHelper", "1.1.1")]
    public class AdminHelperMod : BaseUnityPlugin
    {
        public const string Guid = "com.ryannlt.adminhelper";

        private readonly IsolationTracker _tracker = new IsolationTracker();
        private readonly FlagTracker _flags = new FlagTracker();
        private readonly MinimapMarkers _minimap = new MinimapMarkers();
        private readonly RingRenderer _rings = new RingRenderer();
        private readonly Hotkey _hotkey = new Hotkey();
        private readonly Hud _hud = new Hud();

        private Driver _driver;
        private float _accumulator;
        private bool _wasInRound;

        private void Awake()
        {
            Settings.Create(Config);
            _hotkey.ResetToDefault();

            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureDriver();
            PatchPMenu();

            Log.Info("Ready. Toggle key " + Settings.ResolveToggleKey() +
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
            _tracker.Reset();
            _flags.Reset();
            _minimap.Reset();
            _rings.Destroy();
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

            if (!Settings.Enabled.Value)
            {
                _rings.HideAll();
                _minimap.Hide();
                return;
            }

            _hotkey.Poll();

            bool inRound = GameAccess.InRound;
            if (!inRound)
            {
                if (_wasInRound) _tracker.Reset();
                _wasInRound = false;
                _flags.Flags.Clear();
                _rings.HideAll();
                _minimap.Hide();
                return;
            }

            _wasInRound = true;

            MeleeTracker.Tick();
            KillLogTabs.Warm();

            _accumulator += Time.deltaTime;

            float interval = 1f / Mathf.Clamp(Settings.TickHz.Value, 1f, 30f);
            if (_accumulator >= interval)
            {
                _tracker.Tick(_accumulator);
                _accumulator = 0f;
            }

            bool showFlags = CanReveal() && _hotkey.Visible && Settings.FlagHighlightEnabled.Value;
            if (showFlags) _flags.Tick();
            else _flags.Flags.Clear();

            if (CanReveal() && _hotkey.Visible && Settings.ShowRings.Value) _rings.Draw(_tracker.Watched, _flags.Flags);
            else _rings.HideAll();

            if (showFlags && Settings.FlagMinimapMarkers.Value) _minimap.Draw(_flags.Flags);
            else _minimap.Hide();
        }

        internal void DrawGui()
        {
            if (!Settings.Enabled.Value || !_hotkey.Visible || !GameAccess.InRound) return;

            _hud.Draw(_tracker, _flags.Flags, CanReveal());
        }

        private static bool CanReveal()
        {
            return !Settings.RequireAdminLogin.Value || GameAccess.IsLoggedInAdmin;
        }
    }
}
