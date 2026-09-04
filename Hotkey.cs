using UnityEngine;

namespace AdminEye
{
    // The HUD toggle. Defaults to a plain letter, so the typing guard is required rather than polite.
    internal sealed class Hotkey
    {
        private bool _visible = true;

        public bool Visible
        {
            get { return _visible; }
        }

        public void ResetToDefault()
        {
            _visible = Settings.StartHudVisible.Value;
        }

        public void Poll()
        {
            if (GameAccess.IsTyping) return;
            if (Input.GetKeyDown(Settings.ResolveToggleKey())) _visible = !_visible;
        }
    }
}
