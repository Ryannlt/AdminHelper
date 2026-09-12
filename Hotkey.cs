using UnityEngine;

namespace AdminHelper
{
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
