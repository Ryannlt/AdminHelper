using UnityEngine;

namespace AdminHelper
{
    // The HUD toggle. Set once at startup and then session scoped, so a round change does not undo a press.
    internal sealed class Hotkey
    {
        private bool _visible;

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
