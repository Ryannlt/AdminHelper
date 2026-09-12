using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    internal static class MeleeTracker
    {
        public enum State
        {
            Unknown,
            NotInMelee,
            InMelee
        }

        private sealed class Trace
        {
            public float LastTrigger;
            public float LastInMelee;
            public FactionCountry Faction;
            public bool FactionKnown;
        }

        private static readonly Dictionary<int, Trace> Traces = new Dictionary<int, Trace>();

        private static readonly HashSet<int> InMelee = new HashSet<int>();
        private static readonly List<PlayerSnapshot> Players = new List<PlayerSnapshot>();
        private static readonly List<int> Pending = new List<int>();

        private static float _trackingSince = -1f;
        private static float _lastTick;

        public static void Touch(int playerId)
        {
            if (playerId < 0) return;

            Trace trace = Resolve(playerId, create: true);
            trace.LastTrigger = Time.time;
            trace.LastInMelee = Time.time;
            RecordFaction(playerId, trace);
        }

        public static void Reset()
        {
            Traces.Clear();
            InMelee.Clear();
            Players.Clear();
            Pending.Clear();
            _trackingSince = -1f;
            _lastTick = 0f;
        }

        public static void Tick()
        {
            if (_trackingSince < 0f) _trackingSince = Time.time;

            if (Time.time - _lastTick < 0.2f) return;
            _lastTick = Time.time;

            Rebuild();
        }

        public static State Query(int playerId)
        {
            if (playerId < 0) return State.Unknown;
            if (Settings.Enabled == null || !Settings.Enabled.Value || _trackingSince < 0f) return State.Unknown;

            if (Time.time - _trackingSince < Window()) return State.Unknown;

            Trace trace;
            if (!Traces.TryGetValue(playerId, out trace)) return State.NotInMelee;

            if (trace.FactionKnown && FactionChanged(playerId, trace)) return State.Unknown;

            float window = Window();
            if (Time.time - trace.LastTrigger <= window) return State.InMelee;
            if (Time.time - trace.LastInMelee <= window) return State.InMelee;

            return State.NotInMelee;
        }

        private static void Rebuild()
        {
            GameAccess.CollectPlayers(Players);

            InMelee.Clear();
            Pending.Clear();

            float window = Window();

            for (int i = 0; i < Players.Count; i++)
            {
                int id = Players[i].PlayerId;

                Trace trace;
                if (!Traces.TryGetValue(id, out trace)) continue;
                if (Time.time - trace.LastTrigger > window) continue;

                if (InMelee.Add(id)) Pending.Add(id);
            }

            float radius = Radius();
            float radiusSquared = radius * radius;

            int guard = 0;
            while (Pending.Count > 0 && guard++ < 512)
            {
                Vector3 from = PositionOf(Pending[0]);
                Pending.RemoveAt(0);

                for (int i = 0; i < Players.Count; i++)
                {
                    PlayerSnapshot other = Players[i];
                    if (InMelee.Contains(other.PlayerId)) continue;
                    if (HorizontalSquared(other.Position - from) > radiusSquared) continue;

                    InMelee.Add(other.PlayerId);
                    Pending.Add(other.PlayerId);
                }
            }

            for (int i = 0; i < Players.Count; i++)
            {
                PlayerSnapshot snapshot = Players[i];
                if (!InMelee.Contains(snapshot.PlayerId)) continue;

                Trace trace = Resolve(snapshot.PlayerId, create: true);
                trace.LastInMelee = Time.time;
                trace.Faction = snapshot.Faction;
                trace.FactionKnown = true;
            }

            Prune(window);
        }

        private static void Prune(float window)
        {
            if (Traces.Count < 64) return;

            Pending.Clear();
            foreach (KeyValuePair<int, Trace> pair in Traces)
            {
                float newest = Mathf.Max(pair.Value.LastTrigger, pair.Value.LastInMelee);
                if (Time.time - newest > window * 3f) Pending.Add(pair.Key);
            }

            for (int i = 0; i < Pending.Count; i++)
            {
                Traces.Remove(Pending[i]);
            }
            Pending.Clear();
        }

        private static Trace Resolve(int playerId, bool create)
        {
            Trace trace;
            if (Traces.TryGetValue(playerId, out trace)) return trace;
            if (!create) return null;

            trace = new Trace();
            Traces[playerId] = trace;
            return trace;
        }

        private static void RecordFaction(int playerId, Trace trace)
        {
            RoundPlayer player = GameAccess.ResolvePlayer(playerId);
            if (player == null || player.PlayerStartData == null) return;

            trace.Faction = player.PlayerStartData.Faction;
            trace.FactionKnown = true;
        }

        private static bool FactionChanged(int playerId, Trace trace)
        {
            RoundPlayer player = GameAccess.ResolvePlayer(playerId);
            if (player == null || player.PlayerStartData == null) return false;

            return player.PlayerStartData.Faction != trace.Faction;
        }

        private static Vector3 PositionOf(int playerId)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].PlayerId == playerId) return Players[i].Position;
            }
            return Vector3.zero;
        }

        private static float HorizontalSquared(Vector3 delta)
        {
            return delta.x * delta.x + delta.z * delta.z;
        }

        private static float Window()
        {
            return (Settings.MeleeWindowSeconds == null) ? 10f : Mathf.Max(1f, Settings.MeleeWindowSeconds.Value);
        }

        private static float Radius()
        {
            return (Settings.MeleeChainMetres == null) ? 10f : Mathf.Max(0f, Settings.MeleeChainMetres.Value);
        }
    }
}
