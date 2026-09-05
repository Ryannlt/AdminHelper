using System.Collections.Generic;
using UnityEngine;

namespace AdminHelper
{
    // Screen-space overlay: floating labels over watched players, a corner list, and your own readout.
    internal sealed class Hud
    {
        private static readonly Color WatchColour = new Color(1f, 0.82f, 0.15f);
        private static readonly Color FlagColour = new Color(1f, 0.35f, 0.25f);
        private static readonly Color InfoColour = new Color(0.85f, 0.88f, 0.92f);
        private static readonly Color PanelColour = new Color(0.05f, 0.05f, 0.06f, 0.78f);

        private readonly List<ScoredPlayer> _sorted = new List<ScoredPlayer>();

        private GUIStyle _labelStyle;
        private GUIStyle _listStyle;
        private Texture2D _panelTexture;

        public void Draw(IsolationTracker tracker, bool revealOthers)
        {
            EnsureStyles();

            _sorted.Clear();
            if (revealOthers)
            {
                SortByHeat(tracker.Watched);
                if (Settings.ShowLabels.Value) DrawLabels();
            }

            // Always drawn, so the panel is proof the mod is alive and the toggle key is doing something.
            if (Settings.ShowCornerList.Value) DrawCornerList(revealOthers);

            if (Settings.ShowOwnScore.Value && tracker.HasLocalScore) DrawOwnScore(tracker.LocalScore);
        }

        private void DrawLabels()
        {
            Camera camera = GameAccess.ActiveCamera;
            if (camera == null) return;

            int limit = Mathf.Min(_sorted.Count, Mathf.Max(0, Settings.MaxLabels.Value));
            for (int i = 0; i < limit; i++)
            {
                ScoredPlayer scored = _sorted[i];

                Vector3 screen = camera.WorldToScreenPoint(scored.Position + Vector3.up * 2.1f);
                if (screen.z <= 0f) continue;

                string state = scored.Flagged
                    ? "RAMBO: " + Mathf.FloorToInt(scored.DwellSeconds) + "s"
                    : "ISOLATED";
                string text = scored.Name + "\n" + state + "\nISO: " + scored.Isolation + "  DGR: " + scored.Danger;

                GUIContent content = new GUIContent(text);
                Vector2 size = _labelStyle.CalcSize(content);
                size.y = _labelStyle.CalcHeight(content, size.x);

                Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y, size.x, size.y);
                GUI.DrawTexture(rect, _panelTexture);

                _labelStyle.normal.textColor = scored.Flagged ? FlagColour : WatchColour;
                GUI.Label(rect, text, _labelStyle);
            }
        }

        private void DrawCornerList(bool revealOthers)
        {
            Camera camera = GameAccess.ActiveCamera;
            Vector3 eye = (camera != null) ? camera.transform.position : Vector3.zero;

            string header = revealOthers
                ? "AdminHelper  " + _sorted.Count + " watched, " + CountFlagged() + " flagged"
                : "AdminHelper  admin login required";

            float width = 260f;
            float rowHeight = 18f;
            Rect panel = new Rect(12f, 12f, width, rowHeight * (_sorted.Count + 1) + 10f);
            GUI.DrawTexture(panel, _panelTexture);

            _listStyle.normal.textColor = InfoColour;
            GUI.Label(new Rect(panel.x + 6f, panel.y + 5f, width - 12f, rowHeight), header, _listStyle);

            for (int i = 0; i < _sorted.Count; i++)
            {
                ScoredPlayer scored = _sorted[i];
                float distance = Horizontal(scored.Position - eye);

                Rect row = new Rect(panel.x + 6f, panel.y + 5f + rowHeight * (i + 1), width - 12f, rowHeight);
                _listStyle.normal.textColor = scored.Flagged ? FlagColour : WatchColour;
                GUI.Label(row, scored.Isolation + "/" + scored.Danger + "  " + Mathf.RoundToInt(distance) + "m  " +
                               scored.Name, _listStyle);
            }
        }

        // The raw inputs sit under the scores, which is what makes the thresholds pickable from a real round.
        private void DrawOwnScore(ScoredPlayer local)
        {
            string text = "ISO: " + local.Isolation + "   DGR: " + local.Danger +
                          "\nmates " + Metres(local.MateDistance) + "   enemy " + Metres(local.EnemyDistance) + " x" + local.EnemyCount +
                          (local.InFormation ? "\nin formation" : "\nout of formation") +
                          "   dwell " + local.DwellSeconds.ToString("0.0") + "s";

            Rect rect = new Rect(Screen.width - 232f, Screen.height - 76f, 220f, 64f);
            GUI.DrawTexture(rect, _panelTexture);

            _labelStyle.normal.textColor = local.Flagged ? FlagColour : InfoColour;
            GUI.Label(rect, text, _labelStyle);
        }

        private static string Metres(float distance)
        {
            if (distance >= 9000f) return "none";
            return distance.ToString("0.0") + "m";
        }

        // The tracker already orders by score, so the worst sit at the top of the panel and survive the label cap.
        private void SortByHeat(List<ScoredPlayer> watched)
        {
            _sorted.Clear();
            _sorted.AddRange(watched);
        }

        private int CountFlagged()
        {
            int flagged = 0;
            for (int i = 0; i < _sorted.Count; i++)
            {
                if (_sorted[i].Flagged) flagged++;
            }
            return flagged;
        }

        private void EnsureStyles()
        {
            if (_panelTexture == null)
            {
                _panelTexture = new Texture2D(1, 1);
                _panelTexture.SetPixel(0, 0, PanelColour);
                _panelTexture.Apply();
                _panelTexture.hideFlags = HideFlags.HideAndDontSave;
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.alignment = TextAnchor.MiddleCenter;
                _labelStyle.fontSize = 13;
                _labelStyle.fontStyle = FontStyle.Bold;
                _labelStyle.padding = new RectOffset(6, 6, 3, 3);
                _labelStyle.normal.textColor = FlagColour;
            }

            if (_listStyle == null)
            {
                _listStyle = new GUIStyle(GUI.skin.label);
                _listStyle.alignment = TextAnchor.MiddleLeft;
                _listStyle.fontSize = 12;
                _listStyle.padding = new RectOffset(0, 0, 0, 0);
                _listStyle.normal.textColor = FlagColour;
            }
        }

        private static float Horizontal(Vector3 delta)
        {
            return Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z);
        }
    }
}
