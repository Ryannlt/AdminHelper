using UnityEngine;

namespace AdminHelper
{
    internal sealed class Hotkey
    {
        private RamboUiMode _lastShown = RamboUiMode.On;

        public void Poll()
        {
            RamboUiMode mode = Settings.RamboUi.Value;
            if (mode != RamboUiMode.Off) _lastShown = mode;

            if (!Input.GetKeyDown(Settings.ToggleKeyCode) || GameAccess.IsTyping) return;

            Settings.Write(Settings.RamboUi, mode == RamboUiMode.Off ? _lastShown : RamboUiMode.Off);
        }
    }
}
