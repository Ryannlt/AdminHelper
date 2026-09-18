using System.Collections.Generic;
using UnityEngine;

namespace AdminHelper
{
    internal sealed class AfkTracker
    {
        private sealed class Entry
        {
            public Vector3 Position;
            public float StillSince;
            public float LastSeen;
        }

        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
        private readonly List<int> _stale = new List<int>();

        public void Reset()
        {
            _entries.Clear();
            _stale.Clear();
        }

        public float Observe(PlayerSnapshot player)
        {
            float now = Time.time;

            Entry entry;
            if (!_entries.TryGetValue(player.PlayerId, out entry))
            {
                entry = new Entry();
                entry.Position = player.Position;
                entry.StillSince = now;
                _entries[player.PlayerId] = entry;
            }

            entry.LastSeen = now;

            float moved = Settings.AfkMoveMetres == null ? 0.75f : Mathf.Max(0.05f, Settings.AfkMoveMetres.Value);
            if (Horizontal(player.Position - entry.Position) > moved)
            {
                entry.Position = player.Position;
                entry.StillSince = now;
            }

            return now - entry.StillSince;
        }

        public void Prune()
        {
            if (_entries.Count < 64) return;

            _stale.Clear();
            float now = Time.time;

            foreach (KeyValuePair<int, Entry> pair in _entries)
            {
                if (now - pair.Value.LastSeen > 30f) _stale.Add(pair.Key);
            }

            for (int i = 0; i < _stale.Count; i++) _entries.Remove(_stale[i]);
            _stale.Clear();
        }

        private static float Horizontal(Vector3 delta)
        {
            return Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z);
        }
    }
}
