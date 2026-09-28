using HoldfastGame;
using TMPro;
using UnityEngine;

namespace AdminHelper
{
    internal static class KillLogTip
    {
        private const string ObjectName = "AdminHelper_MeleeTip";
        private const string Explanation = "Legal melee kill: the victim was in a melee with an enemy player";

        private static TMP_Text _tip;

        public static void Show(UIAdminPlayerKillLogPanel panel)
        {
            bool wanted = Settings.MeleeMarkerEnabled != null && Settings.MeleeMarkerEnabled.Value;
            if (_tip == null && wanted) Build(panel);
            if (_tip == null) return;

            if (_tip.gameObject.activeSelf != wanted) _tip.gameObject.SetActive(wanted);
            if (!wanted) return;

            string icon = RyLib.TextIcons.Tag(KillLogMarks.MeleeIcon);
            _tip.text = (icon == null) ? "Crossed swords = " + Explanation : icon + "  " + Explanation;
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

            MonoBehaviour[] behaviours = tip.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is TMP_Text)) Object.DestroyImmediate(behaviours[i]);
            }

            for (int i = tip.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(tip.transform.GetChild(i).gameObject);

            tip.fontSize = 16f;
            tip.enableAutoSizing = false;
            tip.enableWordWrapping = false;
            tip.alignment = TextAlignmentOptions.Center;
            tip.color = new Color(0.78f, 0.79f, 0.8f, 0.9f);
            tip.raycastTarget = false;

            RectTransform rect = tip.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 6f);
            rect.sizeDelta = new Vector2(760f, 24f);

            _tip = tip;
        }
    }
}
