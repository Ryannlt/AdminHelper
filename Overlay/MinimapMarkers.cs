using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;
using UnityEngine.UI;

namespace AdminHelper
{
    internal sealed class MinimapMarkers
    {
        private static readonly Color MarkerColour = new Color(0.3f, 0.95f, 1f, 0.95f);

        private readonly List<Image> _markers = new List<Image>();
        private readonly List<UIMinimapPanelPlayerTrackingPointer> _pointers =
            new List<UIMinimapPanelPlayerTrackingPointer>();
        private readonly Dictionary<Image, Color> _tinted = new Dictionary<Image, Color>();

        private UIMinimapPanelTracking _tracking;
        private float _nextLookup;

        public void Reset()
        {
            RestoreTints();

            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] != null) Object.Destroy(_markers[i].gameObject);
            }

            _markers.Clear();
            _pointers.Clear();
            _tracking = null;
            _nextLookup = 0f;
        }

        public void Hide()
        {
            RestoreTints();
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] != null) _markers[i].enabled = false;
            }
        }

        public void Draw(List<FlagMark> flags)
        {
            if (!Resolve() || flags.Count == 0)
            {
                Hide();
                return;
            }

            CollectPointers();
            RestoreTints();

            int used = 0;
            float scale;
            float cos;
            float sin;
            Vector2 origin;
            bool placed = Calibrate(out scale, out cos, out sin, out origin);

            for (int i = 0; i < flags.Count; i++)
            {
                FlagMark flag = flags[i];

                if (flag.Carried)
                {
                    TintCarrier(flag.PlayerId);
                    continue;
                }

                if (!placed) continue;

                Image marker = Resolve(used++);
                if (marker == null) continue;

                Vector2 offset = new Vector2(flag.Position.x - origin.x, flag.Position.z - origin.y) * scale;
                RectTransform rect = (RectTransform)marker.transform;
                rect.anchoredPosition = new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
                marker.enabled = true;
            }

            for (int i = used; i < _markers.Count; i++)
            {
                if (_markers[i] != null) _markers[i].enabled = false;
            }
        }

        private void TintCarrier(int playerId)
        {
            for (int i = 0; i < _pointers.Count; i++)
            {
                UIMinimapPanelPlayerTrackingPointer pointer = _pointers[i];
                if (pointer == null || pointer.trackingPlayer == null) continue;
                if (pointer.trackingPlayer.NetworkPlayerID != playerId) continue;

                Image image = pointer.trackingImage;
                if (image == null) return;

                if (!_tinted.ContainsKey(image)) _tinted[image] = image.color;
                image.color = MarkerColour;
                return;
            }
        }

        // The minimap's own scale and rotation are not exposed, so they are solved from two live player pointers.
        private bool Calibrate(out float scale, out float cos, out float sin, out Vector2 origin)
        {
            scale = 0f;
            cos = 1f;
            sin = 0f;
            origin = Vector2.zero;

            if (_pointers.Count < 2) return false;

            Vector2 firstWorld;
            Vector2 firstMap;
            if (!Sample(_pointers[0], out firstWorld, out firstMap)) return false;

            Vector2 bestWorld = Vector2.zero;
            Vector2 bestMap = Vector2.zero;
            float bestSpread = 0f;

            for (int i = 1; i < _pointers.Count; i++)
            {
                Vector2 world;
                Vector2 map;
                if (!Sample(_pointers[i], out world, out map)) continue;

                float spread = (world - firstWorld).sqrMagnitude;
                if (spread <= bestSpread) continue;

                bestSpread = spread;
                bestWorld = world;
                bestMap = map;
            }

            if (bestSpread < 4f) return false;

            Vector2 worldDelta = bestWorld - firstWorld;
            Vector2 mapDelta = bestMap - firstMap;
            if (mapDelta.sqrMagnitude < 0.0001f) return false;

            scale = mapDelta.magnitude / worldDelta.magnitude;
            if (scale <= 0f || float.IsNaN(scale)) return false;

            float angle = Mathf.Atan2(mapDelta.y, mapDelta.x) - Mathf.Atan2(worldDelta.y, worldDelta.x);
            cos = Mathf.Cos(angle);
            sin = Mathf.Sin(angle);

            Vector2 rotated = new Vector2(firstMap.x * cos + firstMap.y * sin, -firstMap.x * sin + firstMap.y * cos);
            origin = firstWorld - rotated / scale;
            return true;
        }

        private static bool Sample(UIMinimapPanelPlayerTrackingPointer pointer, out Vector2 world, out Vector2 map)
        {
            world = Vector2.zero;
            map = Vector2.zero;

            if (pointer == null || !pointer.gameObject.activeInHierarchy) return false;

            ClientRoundPlayerProxy player = pointer.trackingPlayer;
            if (player == null || player.PlayerTransformData == null) return false;

            RectTransform rect = pointer.transform as RectTransform;
            if (rect == null) return false;

            Vector3 position = player.PlayerTransformData.position;
            world = new Vector2(position.x, position.z);
            map = rect.anchoredPosition;
            return true;
        }

        private void CollectPointers()
        {
            _pointers.Clear();
            if (_tracking == null || _tracking.playerPointParentTransform == null) return;

            _tracking.playerPointParentTransform.GetComponentsInChildren(false, _pointers);
        }

        private bool Resolve()
        {
            if (_tracking != null) return true;
            if (Time.unscaledTime < _nextLookup) return false;

            _nextLookup = Time.unscaledTime + 5f;
            _tracking = Object.FindObjectOfType<UIMinimapPanelTracking>(true);

            if (_tracking != null) Log.Info("minimap tracking panel found");
            return _tracking != null;
        }

        private Image Resolve(int index)
        {
            while (_markers.Count <= index) _markers.Add(Create());

            Image marker = _markers[index];
            if (marker == null)
            {
                marker = Create();
                _markers[index] = marker;
            }

            return marker;
        }

        private Image Create()
        {
            if (_tracking == null || _tracking.playerPointParentTransform == null) return null;

            GameObject host = new GameObject("AdminHelperFlagMarker");
            host.transform.SetParent(_tracking.playerPointParentTransform, false);

            Image image = host.AddComponent<Image>();
            image.color = MarkerColour;
            image.raycastTarget = false;

            RectTransform rect = (RectTransform)host.transform;
            rect.sizeDelta = new Vector2(9f, 9f);
            rect.localScale = Vector3.one;

            return image;
        }

        private void RestoreTints()
        {
            foreach (KeyValuePair<Image, Color> pair in _tinted)
            {
                if (pair.Key != null) pair.Key.color = pair.Value;
            }

            _tinted.Clear();
        }
    }
}
