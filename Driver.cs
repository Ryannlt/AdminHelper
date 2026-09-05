using UnityEngine;

namespace AdminEye
{
    // Owns the frame loop. BepInEx builds its manager object during the chainloader, before any scene exists,
    // where DontDestroyOnLoad does not stick, so the mod keeps a host it can remake instead.
    internal sealed class Driver : MonoBehaviour
    {
        internal AdminEyeMod Owner;

        internal static Driver Attach(AdminEyeMod owner)
        {
            GameObject host = new GameObject("AdminEye_Driver");
            DontDestroyOnLoad(host);

            Driver driver = host.AddComponent<Driver>();
            driver.Owner = owner;
            return driver;
        }

        private void Awake()
        {
            Log.Info("driver awake");
        }

        private void OnDestroy()
        {
            Log.Info("driver destroyed");
        }

        private void Update()
        {
            if (!ReferenceEquals(Owner, null)) Owner.Tick();
        }

        private void OnGUI()
        {
            if (!ReferenceEquals(Owner, null)) Owner.DrawGui();
        }
    }
}
