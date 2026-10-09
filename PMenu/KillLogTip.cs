using System.Text;
using HoldfastGame;
using TMPro;
using UnityEngine;

namespace AdminHelper
{
    internal static class KillLogTip
    {
        private const string ObjectName = "AdminHelper_ColorKey";
        private const string Square = "■";
        private const string Gap = "     ";

        private static readonly StringBuilder Text = new StringBuilder();

        private static TMP_Text _tip;

        public static void Show(UIAdminPlayerKillLogPanel panel)
        {
            string key = Compose();
            bool wanted = key.Length > 0;
            if (_tip == null && wanted) Build(panel);
            if (_tip == null) return;

            if (_tip.gameObject.activeSelf != wanted) _tip.gameObject.SetActive(wanted);
            if (wanted) _tip.text = key;
        }

        private static string Compose()
        {
            Text.Length = 0;
            if (Settings.ColorKey == null || !Settings.ColorKey.Value) return string.Empty;

            if (Settings.KillLogColors)
            {
                Entry(Swatch(WorldLayer.CalmColor) + Swatch(WorldLayer.IsolatedColor), "Isolated");
                Entry(Swatch(WorldLayer.DangerColor) + Swatch(WorldLayer.DeadlyColor), "Rambo");
                Entry(Swatch(WorldLayer.MeleeColor), "Melee Grace");
                if (Settings.CavalryRules.Value) Entry(Swatch(WorldLayer.CavChargeColor), "Cav Charge");
                if (Settings.AfkMarkEnabled.Value) Entry(Swatch(WorldLayer.AfkColor), "AFK");
            }

            if (Settings.MeleeMarkerEnabled.Value)
            {
                string swords = RyLib.TextIcons.Tag(KillLogMarks.MeleeIcon);
                Entry(swords ?? KillLogMarks.InMeleeText, "Killed In Melee");
            }

            if (Settings.CavalryRules.Value && Settings.MultichargeCheck.Value)
            {
                string horse = RyLib.TextIcons.Tag(KillLogMarks.MultichargeIcon);
                Entry("<color=#" + KillLogMarks.MultichargeHex + ">" + (horse ?? KillLogMarks.MultichargeText) + "</color>", "OUT OF SPACING");
            }

            return Text.ToString();
        }

        private static string Swatch(Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + (RyLib.TextIcons.Swatch() ?? Square) + "</color>";
        }

        private static void Entry(string mark, string label)
        {
            if (Text.Length > 0) Text.Append(Gap);
            Text.Append(mark).Append(' ').Append(label);
        }

        private static void Build(UIAdminPlayerKillLogPanel panel)
        {
            if (panel == null) return;

            Transform header = panel.transform.Find("Header Background");
            Transform titleObject = (header == null) ? null : header.Find("Title");
            TMP_Text title = (titleObject == null) ? null : titleObject.GetComponent<TMP_Text>();
            if (title == null) return;

            TMP_Text tip = Object.Instantiate(title, header);
            tip.name = ObjectName;

            MonoBehaviour[] behaviors = tip.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviors.Length; i++)
            {
                if (!(behaviors[i] is TMP_Text)) Object.DestroyImmediate(behaviors[i]);
            }

            for (int i = tip.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(tip.transform.GetChild(i).gameObject);

            tip.fontSize = 16f;
            tip.fontSizeMax = 16f;
            tip.fontSizeMin = 10f;
            tip.enableAutoSizing = true;
            tip.enableWordWrapping = false;
            tip.overflowMode = TextOverflowModes.Overflow;
            tip.alignment = TextAlignmentOptions.Center;
            tip.color = new Color(0.78f, 0.79f, 0.8f, 0.9f);
            tip.raycastTarget = false;

            RectTransform rect = tip.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 6f);
            rect.sizeDelta = new Vector2(-40f, 24f);

            _tip = tip;
        }
    }
}
