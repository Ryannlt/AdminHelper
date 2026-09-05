using System.Reflection;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

// Kept in step with BepInPlugin below; package.ps1 reads this back off the built DLL.
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace AdminHelper
{
    // Scores how isolated each player is from their own side and draws the result over the ones who stay out.
    [BepInPlugin(Guid, "AdminHelper", "1.0.0")]
    public class AdminHelperMod : BaseUnityPlugin
    {
        // Also names the config file, BepInEx/config/com.ryannlt.adminhelper.cfg.
        public const string Guid = "com.ryannlt.adminhelper";

        private readonly IsolationTracker _tracker = new IsolationTracker();
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

            // BepInEx has no per-scene callback of its own, so the mod takes Unity's.
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureDriver();

            Log.Info("Ready. Toggle key " + Settings.ResolveToggleKey() +
                     ", RequireAdminLogin=" + Settings.RequireAdminLogin.Value);
        }

        // Deliberately does not unsubscribe sceneLoaded. Unity destroys objects before firing that event, so
        // dropping it here removes the only hook that outlives the manager object and the mod never returns.
        private void OnDestroy()
        {
            Log.Info("plugin component destroyed");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.Info("scene loaded " + scene.name + " driverAlive=" + (_driver != null));
            EnsureDriver();

            GameAccess.ClearSceneCache();
            _tracker.Reset();
            _rings.Destroy();
            _accumulator = 0f;
            _wasInRound = false;
        }

        // A host made before the first scene does not survive it, so the driver is remade whenever it has gone.
        private void EnsureDriver()
        {
            if (_driver != null) return;
            _driver = Driver.Attach(this);
        }

        internal void Tick()
        {
            // Ahead of the Enabled test, so the master switch can be turned back on from the cfg.
            Settings.PollForExternalEdits();

            if (!Settings.Enabled.Value)
            {
                _rings.HideAll();
                return;
            }

            _hotkey.Poll();

            bool inRound = GameAccess.InRound;
            if (!inRound)
            {
                if (_wasInRound) _tracker.Reset();
                _wasInRound = false;
                _rings.HideAll();
                return;
            }

            _wasInRound = true;

            _accumulator += Time.deltaTime;

            float interval = 1f / Mathf.Clamp(Settings.TickHz.Value, 1f, 30f);
            if (_accumulator >= interval)
            {
                _tracker.Tick(_accumulator);
                _accumulator = 0f;
            }

            if (CanReveal() && _hotkey.Visible && Settings.ShowRings.Value) _rings.Draw(_tracker.Watched);
            else _rings.HideAll();
        }

        internal void DrawGui()
        {
            if (!Settings.Enabled.Value || !_hotkey.Visible || !GameAccess.InRound) return;

            _hud.Draw(_tracker, CanReveal());
        }

        // The scorer always runs, but nothing about another player is drawn until the server says you are an admin.
        private static bool CanReveal()
        {
            return !Settings.RequireAdminLogin.Value || GameAccess.IsLoggedInAdmin;
        }
    }
}
