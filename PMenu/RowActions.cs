using System;
using System.Collections.Generic;
using HarmonyLib;
using HoldfastGame;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AdminHelper
{
    internal static class RowActions
    {
        private const string TeleportName = "AdminHelperToRegiment";
        private const string ReviveName = "AdminHelperReviveToRegiment";
        private const float ReviveWaitSeconds = 4f;
        private const float Inset = 10f;
        private const string IconSquare = "Button Icon Square";
        private const float RowGap = 25f;

        private struct Pending
        {
            public int PlayerId;
            public float Deadline;
        }

        private struct Column
        {
            public float Min;
            public float Max;
        }

        private static readonly List<Pending> Waiting = new List<Pending>();
        private static readonly List<PlayerSnapshot> Players = new List<PlayerSnapshot>();
        private static readonly List<PlayerSnapshot> Mates = new List<PlayerSnapshot>();
        private static readonly List<Column> Columns = new List<Column>();
        private static readonly List<float> Rows = new List<float>();

        public static void Reset()
        {
            Waiting.Clear();
            Players.Clear();
            Mates.Clear();
        }

        public static void Tick()
        {
            for (int i = Waiting.Count - 1; i >= 0; i--)
            {
                Pending pending = Waiting[i];

                if (IsAlive(pending.PlayerId))
                {
                    Waiting.RemoveAt(i);
                    SendToRegiment(pending.PlayerId);
                    continue;
                }

                if (Time.time < pending.Deadline) continue;

                Waiting.RemoveAt(i);
                Log.Warn("player " + pending.PlayerId + " never came back alive, so the teleport was dropped.");
            }
        }

        public static void Build(UIRoundPlayersControlPanelItemRow row)
        {
            if (Settings.RowActionsEnabled == null || !Settings.RowActionsEnabled.Value) return;
            if (row == null || row.bringButton == null || row.kickButton == null) return;

            RectTransform actions = row.bringButton.transform.parent as RectTransform;
            RectTransform admin = row.kickButton.transform.parent as RectTransform;
            if (actions == null || admin == null || actions == admin) return;
            if (actions.Find(TeleportName) != null) return;

            ReadColumns(actions);
            if (Columns.Count < 2)
            {
                Log.Warn("the actions row has no readable columns, so the regiment buttons were skipped.");
                return;
            }

            RectTransform template = row.bringButton.transform as RectTransform;
            float height = template.sizeDelta.y;
            float pitch = Pitch(admin, height);
            float y = Bottom(actions) - pitch;

            Button teleport = Clone(row.banButton, actions, TeleportName, "Reg TP", Columns[0], y, height, false);
            if (teleport == null) return;

            Button revive = Clone(row.banButton, actions, ReviveName, "Rez + Reg TP", Columns[1], y, height, true);
            if (revive == null) return;

            CopyIcon(teleport.gameObject, row.bringButton);
            CopyIcon(revive.gameObject, row.reviveButton);

            teleport.onClick.AddListener(delegate { OnTeleport(row); });
            revive.onClick.AddListener(delegate { OnRevive(row); });

            Grow(actions, admin, pitch);
        }

        private static void CopyIcon(GameObject host, Button source)
        {
            Transform target = Icon(host.transform);
            Transform origin = (source == null) ? null : Icon(source.transform);

            if (target == null || origin == null)
            {
                Log.Warn("no icon square to copy, so the regiment button kept the one it was cloned from.");
                return;
            }

            int copied = Paint(target, origin);
            if (copied == 0) Log.Warn("no matching icon graphic was found to copy onto a regiment button.");
        }

        private static Transform Icon(Transform button)
        {
            Transform icon = button.Find(IconSquare);
            if (icon != null) return icon;

            for (int i = 0; i < button.childCount; i++)
            {
                Transform child = button.GetChild(i);
                if (child.name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0) return child;
            }

            return null;
        }

        private static int Paint(Transform target, Transform origin)
        {
            int copied = 0;

            for (int i = 0; i < target.childCount; i++)
            {
                Transform child = target.GetChild(i);
                Transform match = origin.Find(child.name);
                if (match == null) continue;

                Image to = child.GetComponent<Image>();
                Image from = match.GetComponent<Image>();
                if (to != null && from != null)
                {
                    to.sprite = from.sprite;
                    to.overrideSprite = from.overrideSprite;
                    to.color = from.color;
                    to.type = from.type;
                    to.preserveAspect = from.preserveAspect;
                    copied++;
                }

                TMP_Text toText = child.GetComponent<TMP_Text>();
                TMP_Text fromText = match.GetComponent<TMP_Text>();
                if (toText != null && fromText != null && !Hotkey(fromText.text))
                {
                    toText.text = fromText.text;
                    copied++;
                }

                copied += Paint(child, match);
            }

            return copied;
        }

        private static void ReadColumns(RectTransform parent)
        {
            Columns.Clear();

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.GetComponent<Button>() == null) continue;

                RectTransform rect = child as RectTransform;
                if (rect == null) continue;

                Column column;
                column.Min = rect.anchorMin.x;
                column.Max = rect.anchorMax.x;

                bool known = false;
                for (int j = 0; j < Columns.Count; j++)
                {
                    if (Math.Abs(Columns[j].Min - column.Min) > 0.001f) continue;
                    known = true;
                    break;
                }

                if (!known) Columns.Add(column);
            }

            Columns.Sort(CompareColumns);
        }

        private static int CompareColumns(Column left, Column right)
        {
            return left.Min.CompareTo(right.Min);
        }

        private static float Bottom(RectTransform parent)
        {
            float bottom = 0f;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.GetComponent<Button>() == null) continue;

                RectTransform rect = child as RectTransform;
                if (rect == null) continue;

                if (rect.anchoredPosition.y < bottom) bottom = rect.anchoredPosition.y;
            }

            return bottom;
        }

        private static float Pitch(RectTransform admin, float height)
        {
            Rows.Clear();

            for (int i = 0; i < admin.childCount; i++)
            {
                Transform child = admin.GetChild(i);
                if (child.GetComponent<Button>() == null) continue;

                RectTransform rect = child as RectTransform;
                if (rect == null) continue;

                float y = rect.anchoredPosition.y;

                bool known = false;
                for (int j = 0; j < Rows.Count; j++)
                {
                    if (Math.Abs(Rows[j] - y) > 1f) continue;
                    known = true;
                    break;
                }

                if (!known) Rows.Add(y);
            }

            if (Rows.Count < 2) return height + RowGap;

            Rows.Sort();
            return Rows[Rows.Count - 1] - Rows[Rows.Count - 2];
        }

        private static void Grow(RectTransform actions, RectTransform admin, float pitch)
        {
            actions.sizeDelta = new Vector2(actions.sizeDelta.x, actions.sizeDelta.y + pitch);
            admin.anchoredPosition = new Vector2(admin.anchoredPosition.x, admin.anchoredPosition.y - pitch);

            RectTransform cell = actions.parent as RectTransform;
            if (cell == null) return;

            LayoutElement element = cell.GetComponent<LayoutElement>();
            if (element == null)
            {
                cell.sizeDelta = new Vector2(cell.sizeDelta.x, cell.sizeDelta.y + pitch);
                return;
            }

            if (element.minHeight > 0f) element.minHeight += pitch;

            if (element.preferredHeight > 0f) element.preferredHeight += pitch;
            else if (element.minHeight <= 0f) element.preferredHeight = cell.rect.height + pitch;
        }

        private static void OnTeleport(UIRoundPlayersControlPanelItemRow row)
        {
            ClientRoundPlayer target = (row == null) ? null : row.clientRoundPlayer;
            if (target == null) return;

            SendToRegiment(target.NetworkPlayerID);
        }

        private static void OnRevive(UIRoundPlayersControlPanelItemRow row)
        {
            ClientRoundPlayer target = (row == null) ? null : row.clientRoundPlayer;
            if (target == null) return;

            int playerId = target.NetworkPlayerID;

            if (IsAlive(playerId))
            {
                SendToRegiment(playerId);
                return;
            }

            ClientComponentReferenceManager client = GameAccess.Client;
            if (client == null || client.clientBannedPlayersManager == null) return;

            client.clientBannedPlayersManager.RequestPlayerRevive(playerId, "AdminHelper revive and teleport");

            for (int i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].PlayerId == playerId) return;
            }

            Pending pending;
            pending.PlayerId = playerId;
            pending.Deadline = Time.time + ReviveWaitSeconds;
            Waiting.Add(pending);
        }

        private static void SendToRegiment(int playerId)
        {
            int destination = ResolveDestination(playerId);
            if (destination < 0)
            {
                Log.Warn("no living regiment mate or teammate to send player " + playerId + " to.");
                return;
            }

            ClientComponentReferenceManager client = GameAccess.Client;
            if (client == null || client.pMenuPanel == null) return;

            client.pMenuPanel.ManualConsoleExecute("rc teleport " + playerId + " " + destination);
        }

        private static int ResolveDestination(int playerId)
        {
            RoundPlayer target = Resolve(playerId);
            if (target == null || target.PlayerStartData == null) return -1;

            FactionCountry faction = target.PlayerStartData.Faction;
            string tag = Tag(target);

            GameAccess.CollectPlayers(Players);

            Mates.Clear();
            PlayerSnapshot nearest = default(PlayerSnapshot);
            float nearestDistance = float.MaxValue;
            bool found = false;

            Vector3 origin = (target.PlayerTransformData == null) ? Vector3.zero : target.PlayerTransformData.position;

            for (int i = 0; i < Players.Count; i++)
            {
                PlayerSnapshot player = Players[i];
                if (player.PlayerId == playerId || player.Faction != faction) continue;

                if (tag.Length > 0 && string.Equals(Tag(player.Player), tag, StringComparison.Ordinal))
                {
                    Mates.Add(player);
                }

                float distance = (player.Position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = player;
                    found = true;
                }
            }

            Players.Clear();

            if (Mates.Count > 0) return Middle(Mates);

            return found ? nearest.PlayerId : -1;
        }

        private static int Middle(List<PlayerSnapshot> mates)
        {
            Vector3 centre = Vector3.zero;
            for (int i = 0; i < mates.Count; i++) centre += mates[i].Position;
            centre /= mates.Count;

            int best = mates[0].PlayerId;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < mates.Count; i++)
            {
                float distance = (mates[i].Position - centre).sqrMagnitude;
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = mates[i].PlayerId;
            }

            mates.Clear();
            return best;
        }

        private static string Tag(RoundPlayer player)
        {
            if (player == null) return string.Empty;

            RoundPlayerInformation info = player.PlayerRoundInformation;
            if (info == null || info.InitialDetails == null) return string.Empty;

            return TextSearch.Normalize(info.InitialDetails.RegimentTag);
        }

        private static RoundPlayer Resolve(int playerId)
        {
            ClientComponentReferenceManager client = GameAccess.Client;
            if (client == null || client.clientRoundPlayerManager == null) return null;

            return client.clientRoundPlayerManager.ResolveRoundPlayer(playerId);
        }

        private static bool IsAlive(int playerId)
        {
            RoundPlayer player = Resolve(playerId);
            return player != null && player.PlayerBase != null && player.PlayerBase.SpawnedAndAlive;
        }

        private static Button Clone(Button template, RectTransform parent, string name, string label,
            Column column, float y, float height, bool last)
        {
            GameObject host = UnityEngine.Object.Instantiate(template.gameObject, parent);
            host.name = name;

            Button button = host.GetComponent<Button>();
            RectTransform rect = host.transform as RectTransform;

            if (button == null || rect == null)
            {
                UnityEngine.Object.Destroy(host);
                return null;
            }

            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            }

            float left = (column.Min <= 0.001f) ? 0f : Inset;
            float right = last ? 0f : Inset;

            rect.anchorMin = new Vector2(column.Min, 1f);
            rect.anchorMax = new Vector2(column.Max, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(-(left + right), height);
            rect.anchoredPosition = new Vector2(left, y);
            rect.localScale = Vector3.one;

            StripComponents(host);
            SetLabel(host, label);

            host.SetActive(true);
            return button;
        }

        private static void StripComponents(GameObject target)
        {
            MonoBehaviour[] behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null) continue;

                string type = behaviours[i].GetType().Name;
                if (type.IndexOf("Localize", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    type == "UIAutoScrollableListItem")
                {
                    UnityEngine.Object.Destroy(behaviours[i]);
                }
            }
        }

        private static void SetLabel(GameObject target, string label)
        {
            TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (Hotkey(texts[i].text)) texts[i].gameObject.SetActive(false);
                else texts[i].text = label;
            }

            Text[] legacy = target.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacy.Length; i++)
            {
                if (Hotkey(legacy[i].text)) legacy[i].gameObject.SetActive(false);
                else legacy[i].text = label;
            }
        }

        private static bool Hotkey(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]) && !char.IsWhiteSpace(text[i])) return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(UIRoundPlayersControlPanelItemRow), "Initialize")]
    internal static class RowActionsInitializePatch
    {
        private static void Postfix(UIRoundPlayersControlPanelItemRow __instance)
        {
            try
            {
                RowActions.Build(__instance);
            }
            catch (Exception error)
            {
                Log.Error("row actions could not be added: " + error.Message);
            }
        }
    }
}
