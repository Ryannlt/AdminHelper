using System.Collections.Generic;
using UnityEngine;

namespace AdminHelper
{
    internal sealed class Hud
    {
        private static readonly Color WatchColour = new Color(1f, 0.82f, 0.15f);
        private static readonly Color FlagColour = new Color(1f, 0.35f, 0.25f);
        private static readonly Color AfkColour = new Color(0.6f, 0.62f, 0.65f);
        private static readonly Color MeleeColour = new Color(0.55f, 0.85f, 1f);
        private static readonly Color BearingColour = new Color(0.3f, 0.95f, 1f);
        private static readonly Color InfoColour = new Color(0.85f, 0.88f, 0.92f);
        private static readonly Color PanelColour = new Color(0.05f, 0.05f, 0.06f, 0.78f);

        private readonly List<ScoredPlayer> _sorted = new List<ScoredPlayer>();

        private GUIStyle _labelStyle;
        private GUIStyle _listStyle;
        private Texture2D _panelTexture;

        public void Draw(IsolationTracker tracker, List<FlagMark> flags, bool revealOthers)
        {
            EnsureStyles();

            _sorted.Clear();
            if (revealOthers)
            {
                SortByHeat(tracker.Watched);
                if (Settings.ShowLabels.Value)
                {
                    DrawLabels();
                    DrawFlagLabels(flags);
                }
            }

            if (Settings.ShowCornerList.Value) DrawCornerList(revealOthers, flags);

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
                    : (scored.InHonestMelee ? "FIGHT" : "ISOLATED");
                if (scored.Afk) state = "AFK " + Mathf.FloorToInt(scored.AfkSeconds) + "s";

                string text = scored.Name + "\n" + state + "\nISO: " + scored.Isolation + "  DGR: " + scored.Danger;

                GUIContent content = new GUIContent(text);
                Vector2 size = _labelStyle.CalcSize(content);
                size.y = _labelStyle.CalcHeight(content, size.x);

                Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y, size.x, size.y);
                GUI.DrawTexture(rect, _panelTexture);

                _labelStyle.normal.textColor = StateColour(scored);
                GUI.Label(rect, text, _labelStyle);
            }
        }

        private void DrawFlagLabels(List<FlagMark> flags)
        {
            Camera camera = GameAccess.ActiveCamera;
            if (camera == null) return;

            for (int i = 0; i < flags.Count; i++)
            {
                FlagMark flag = flags[i];

                Vector3 screen = camera.WorldToScreenPoint(flag.Position + Vector3.up * 2.6f);
                if (screen.z <= 0f) continue;

                string text = flag.Carried ? "FLAG: " + flag.Name : flag.Name + " FLAG";

                GUIContent content = new GUIContent(text);
                Vector2 size = _labelStyle.CalcSize(content);
                size.y = _labelStyle.CalcHeight(content, size.x);

                Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y, size.x, size.y);
                GUI.DrawTexture(rect, _panelTexture);

                _labelStyle.normal.textColor = BearingColour;
                GUI.Label(rect, text, _labelStyle);
            }
        }

        private static Color StateColour(ScoredPlayer scored)
        {
            if (scored.Afk) return AfkColour;
            if (scored.InHonestMelee) return MeleeColour;
            return scored.Flagged ? FlagColour : WatchColour;
        }

        private void DrawCornerList(bool revealOthers, List<FlagMark> flags)
        {
            Camera camera = GameAccess.ActiveCamera;
            Vector3 eye = (camera != null) ? camera.transform.position : Vector3.zero;

            string header = revealOthers
                ? "AdminHelper  " + _sorted.Count + " watched, " + CountFlagged() + " flagged"
                : "AdminHelper  admin login required";

            float width = 260f;
            float rowHeight = 18f;
            int rows = _sorted.Count + flags.Count;
            Rect panel = new Rect(12f, 12f, width, rowHeight * (rows + 1) + 10f);
            GUI.DrawTexture(panel, _panelTexture);

            _listStyle.normal.textColor = InfoColour;
            GUI.Label(new Rect(panel.x + 6f, panel.y + 5f, width - 12f, rowHeight), header, _listStyle);

            int line = 1;

            for (int i = 0; i < flags.Count; i++)
            {
                FlagMark flag = flags[i];
                float distance = Horizontal(flag.Position - eye);

                Rect row = new Rect(panel.x + 6f, panel.y + 5f + rowHeight * line++, width - 12f, rowHeight);
                _listStyle.normal.textColor = BearingColour;
                GUI.Label(row, "FLAG  " + Mathf.RoundToInt(distance) + "m  " +
                               (flag.Carried ? flag.Name : flag.Name + " dropped"), _listStyle);
            }

            for (int i = 0; i < _sorted.Count; i++)
            {
                ScoredPlayer scored = _sorted[i];
                float distance = Horizontal(scored.Position - eye);

                Rect row = new Rect(panel.x + 6f, panel.y + 5f + rowHeight * line++, width - 12f, rowHeight);
                _listStyle.normal.textColor = StateColour(scored);
                GUI.Label(row, scored.Isolation + "/" + scored.Danger + "  " + Mathf.RoundToInt(distance) + "m  " +
                               scored.Name + (scored.Afk ? "  AFK" : string.Empty), _listStyle);
            }
        }

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
