using System.Collections.Generic;

namespace AdminHelper
{
    internal sealed class MinimapLayer
    {
        private const float LayerHz = 4f;

        private readonly FlagTracker _flags;

        public bool Visible;

        public MinimapLayer(FlagTracker flags)
        {
            _flags = flags;
        }

        public void Register()
        {
            RyLib.Minimap.Layer(AdminHelperMod.Guid, Fill, LayerHz);
        }

        private void Fill(List<RyLib.MinimapMark> marks)
        {
            if (!Visible) return;

            bool always = Settings.FlagMinimapAlways.Value && AdminHelperMod.CanReveal();

            List<FlagMark> flags = _flags.Flags;
            for (int i = 0; i < flags.Count; i++)
            {
                FlagMark flag = flags[i];
                if (flag.Follow == null) continue;

                RyLib.MinimapMark mark = new RyLib.MinimapMark();
                mark.Key = (flag.Carried ? "carried-" : "flag-") + flag.Key;
                mark.Follow = flag.Follow.transform;
                mark.Shape = RyLib.MapShape.Flag;
                mark.Faction = flag.Side;
                mark.Colour = WorldLayer.FactionColour(flag.Type);
                mark.AlwaysShow = always;
                marks.Add(mark);
            }
        }
    }
}
