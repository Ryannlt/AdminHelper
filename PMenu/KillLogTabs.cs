using System;
using HoldfastGame;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AdminHelper
{
    internal static class KillLogTabs
    {
        private const string ObjectName = "AdminHelper_TeamkillToggle";
        private const string TeamkillTitle = "TEAM KILL LOG";

        private static UIAdminPlayerKillLogPanel _panel;
        private static float _side;
        private static GameObject _toggleObject;
        private static Toggle _toggle;
        private static TMP_Text _title;
        private static string _titleText;
        private static bool _failed;
        private static bool _suppress;

        public static void Attach(UIAdminPlayerKillLogPanel panel)
        {
            if (_failed || _toggleObject != null) return;
            if (Settings.TeamkillFilterEnabled == null || !Settings.TeamkillFilterEnabled.Value) return;

            try
            {
                Build(panel);
            }
            catch (Exception error)
            {
                _failed = true;
                Log.Warn("Could not add the teamkill toggle, so type 'tk' in the search box instead. " + error.Message);
            }
        }

        private static void Build(UIAdminPlayerKillLogPanel panel)
        {
            if (panel == null || panel.searchFilterInputField == null) return;

            RectTransform searchRect = panel.searchFilterInputField.transform as RectTransform;
            if (searchRect == null) return;

            Transform parent = panel.transform;
            if (parent == null) return;

            Sweep(parent);

            PMenuChildTabButton source = FindSource();
            if (source == null)
            {
                _failed = true;
                Log.Warn("No child tab button to copy, so the teamkill toggle was skipped. Type 'tk' instead.");
                return;
            }

            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent);
            clone.name = ObjectName;
            clone.SetActive(false);

            PMenuChildTabButton stray = clone.GetComponent<PMenuChildTabButton>();
            if (stray != null) UnityEngine.Object.Destroy(stray);

            StripLocalizers(clone);

            Toggle toggle = clone.GetComponent<Toggle>();
            if (toggle == null) toggle = clone.GetComponentInChildren<Toggle>(true);
            if (toggle == null)
            {
                UnityEngine.Object.Destroy(clone);
                _failed = true;
                Log.Warn("The cloned tab carried no toggle, so the teamkill toggle was skipped.");
                return;
            }

            MuteInspectorListeners(toggle);

            toggle.group = null;
            toggle.interactable = true;

            WhitenIcon(clone);
            BlankText(clone);

            _panel = panel;
            _side = Side(source.transform as RectTransform, searchRect);
            Place(clone.transform as RectTransform, searchRect, _side);
            Clickable(clone, toggle);

            _suppress = true;
            toggle.isOn = KillLogMarks.TeamkillOnly;
            _suppress = false;

            clone.SetActive(true);
            clone.transform.SetAsLastSibling();

            toggle.onValueChanged.AddListener(OnToggled);

            _toggleObject = clone;
            _toggle = toggle;

            ClaimTitle(panel, searchRect, clone.transform);
            SetTitle();
        }

        public static void Warm()
        {
            if (_toggleObject != null || _failed) return;

            ClientComponentReferenceManager client = ClientComponentReferenceManager.ClientInstance;
            if (client == null || client.uiAdminPlayerKillLogPanel == null) return;

            Attach(client.uiAdminPlayerKillLogPanel);
        }

        public static void Show(UIAdminPlayerKillLogPanel panel)
        {
            Attach(panel);
            Reposition();
            Reapply(panel);
        }

        private static void Reposition()
        {
            if (_toggleObject == null || _panel == null || _panel.searchFilterInputField == null) return;

            Place(_toggleObject.transform as RectTransform, _panel.searchFilterInputField.transform as RectTransform, _side);
        }

        private static void Sweep(Transform parent)
        {
            Transform existing = parent.Find(ObjectName);
            while (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
                existing = parent.Find(ObjectName);
            }
        }

        private static PMenuChildTabButton FindSource()
        {
            ClientComponentReferenceManager client = ClientComponentReferenceManager.ClientInstance;
            if (client == null || client.pMenuPanel == null) return null;

            GameObject root = client.pMenuPanel.playerChildTabButtonsRoot;
            if (root == null) return null;

            PMenuChildTabButton[] buttons = root.GetComponentsInChildren<PMenuChildTabButton>(true);
            PMenuChildTabButton first = null;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (first == null) first = buttons[i];
                if (buttons[i].toggle != null && !buttons[i].toggle.isOn) return buttons[i];
            }

            return first;
        }

        private static void MuteInspectorListeners(Toggle toggle)
        {
            int count = toggle.onValueChanged.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                toggle.onValueChanged.SetPersistentListenerState(i, UnityEventCallState.Off);
            }
        }

        private static void WhitenIcon(GameObject clone)
        {
            Image[] images = clone.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) < 0) continue;

                images[i].color = new Color(1f, 1f, 1f, images[i].color.a);
            }
        }

        private static void Clickable(GameObject clone, Toggle toggle)
        {
            Image background = clone.GetComponent<Image>();
            if (background == null)
            {
                background = clone.AddComponent<Image>();
                background.color = new Color(1f, 1f, 1f, 0f);
            }

            background.raycastTarget = true;
            if (toggle.targetGraphic == null) toggle.targetGraphic = background;

            Graphic[] graphics = clone.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == background) continue;
                graphics[i].raycastTarget = false;
            }
        }

        private static void BlankText(GameObject clone)
        {
            TMP_Text[] labels = clone.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].text = string.Empty;
            }
        }

        private static float Side(RectTransform source, RectTransform searchRect)
        {
            float side = 0f;
            if (source != null) side = Mathf.Max(source.rect.width, source.rect.height);
            if (side < 1f) side = searchRect.rect.height;
            if (side < 1f) side = 55f;

            return side;
        }

        private static void Place(RectTransform rect, RectTransform searchRect, float side)
        {
            if (rect == null) return;

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(side, side);

            Vector3[] corners = new Vector3[4];
            searchRect.GetWorldCorners(corners);
            float centreY = (corners[0].y + corners[1].y) * 0.5f;

            float rightEdge = PlayersTabsRightEdge();

            if (float.IsNaN(rightEdge))
            {
                RectTransform tab = rect.parent as RectTransform;
                if (tab == null)
                {
                    rect.position = new Vector3(corners[2].x, centreY, corners[0].z);
                    rect.anchoredPosition += new Vector2(side * 0.5f + 14f, 0f);
                    return;
                }

                Vector3[] tabCorners = new Vector3[4];
                tab.GetWorldCorners(tabCorners);
                rightEdge = tabCorners[2].x;
            }

            rect.position = new Vector3(rightEdge, centreY, corners[0].z);
            rect.anchoredPosition -= new Vector2(side * 0.5f, 0f);
        }

        private static float PlayersTabsRightEdge()
        {
            ClientComponentReferenceManager client = ClientComponentReferenceManager.ClientInstance;
            if (client == null || client.pMenuPanel == null) return float.NaN;

            GameObject root = client.pMenuPanel.playerChildTabButtonsRoot;
            if (root == null) return float.NaN;

            PMenuChildTabButton[] buttons = root.GetComponentsInChildren<PMenuChildTabButton>(true);
            Vector3[] corners = new Vector3[4];
            float edge = float.NaN;

            for (int i = 0; i < buttons.Length; i++)
            {
                RectTransform rect = buttons[i].transform as RectTransform;
                if (rect == null || rect.rect.width < 1f) continue;

                rect.GetWorldCorners(corners);
                if (float.IsNaN(edge) || corners[2].x > edge) edge = corners[2].x;
            }

            return edge;
        }

        private static void StripLocalizers(GameObject target)
        {
            MonoBehaviour[] behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null) continue;
                if (behaviours[i].GetType().Name.IndexOf("Localize", StringComparison.OrdinalIgnoreCase) < 0) continue;

                UnityEngine.Object.Destroy(behaviours[i]);
            }
        }

        private static void ClaimTitle(UIAdminPlayerKillLogPanel panel, RectTransform searchRect, Transform self)
        {
            _title = null;
            _titleText = null;

            Transform searchRoot = searchRect;
            if (searchRoot.parent != null && searchRoot.parent.parent != null) searchRoot = searchRoot.parent.parent;

            TMP_Text best = null;
            TMP_Text[] texts = panel.transform.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || string.IsNullOrEmpty(text.text)) continue;
                if (!text.gameObject.activeSelf) continue;
                if (text.transform.IsChildOf(searchRoot)) continue;
                if (text.transform.IsChildOf(self)) continue;
                if (panel.rowsRootTransform != null && text.transform.IsChildOf(panel.rowsRootTransform)) continue;

                bool named = Named(text);
                if (best == null || (named && !Named(best)) || (named == Named(best) && text.fontSize > best.fontSize))
                {
                    best = text;
                }
            }

            if (best == null)
            {
                Log.Warn("No heading found to rename, so the title will stay as it is.");
                return;
            }

            _title = best;
            _titleText = best.text;
            StripLocalizers(best.gameObject);

            best.raycastTarget = false;
        }

        private static void OnToggled(bool on)
        {
            if (_suppress) return;

            KillLogMarks.TeamkillOnly = on;
            SetTitle();

            UIAdminPlayerKillLogPanel panel = Panel();
            if (panel != null) Reapply(panel);
        }

        public static void Hide()
        {
            if (_title != null && _titleText != null) _title.text = _titleText;
        }

        private static bool Named(TMP_Text text)
        {
            return text.gameObject.name.StartsWith("Title", StringComparison.OrdinalIgnoreCase);
        }

        private static void SetTitle()
        {
            if (_title == null) return;

            _title.text = KillLogMarks.TeamkillOnly ? TeamkillTitle : _titleText;
        }

        public static void Reapply(UIAdminPlayerKillLogPanel panel)
        {
            if (panel == null || panel.searchFilterInputField == null) return;

            panel.searchFilterInputField.onValueChanged.Invoke(panel.searchFilterInputField.text);
        }

        private static UIAdminPlayerKillLogPanel Panel()
        {
            ClientComponentReferenceManager client = ClientComponentReferenceManager.ClientInstance;
            if (client == null) return null;
            return client.uiAdminPlayerKillLogPanel;
        }

        public static void Forget()
        {
            if (_title != null && _titleText != null) _title.text = _titleText;
            if (_toggleObject != null) UnityEngine.Object.Destroy(_toggleObject);

            _toggleObject = null;
            _toggle = null;
            _title = null;
            _titleText = null;
            _failed = false;
            KillLogMarks.TeamkillOnly = false;
        }
    }
}
