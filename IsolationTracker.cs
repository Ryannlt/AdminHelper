using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;

namespace AdminEye
{
    // Holds the continuous isolation score for every player and rebuilds the watch list each tick.
    internal sealed class IsolationTracker
    {
        // A single enemy at contact is worth this much of the threat a full crowd carries.
        private const float SoloEnemyFloor = 0.5f;

        // Ceiling on drawn markers, so a collapsing team cannot fill the screen.
        private const int MaxWatched = 32;


        private sealed class State
        {
            public float Isolation;
            public float Dwell;
            public int LastSeenTick;
        }

        private readonly Dictionary<int, State> _states = new Dictionary<int, State>();
        private readonly List<PlayerSnapshot> _players = new List<PlayerSnapshot>();
        private readonly Dictionary<FactionCountry, List<PlayerSnapshot>> _byFaction =
            new Dictionary<FactionCountry, List<PlayerSnapshot>>();
        private readonly List<int> _stale = new List<int>();

        private int _tick;

        // Everyone worth drawing, hottest first. Keyed to the live score, not the dwell timer.
        public readonly List<ScoredPlayer> Watched = new List<ScoredPlayer>();
        public ScoredPlayer LocalScore;
        public bool HasLocalScore;

        public void Reset()
        {
            _states.Clear();
            _byFaction.Clear();
            Watched.Clear();
            HasLocalScore = false;
        }

        public void Tick(float dt)
        {
            _tick++;
            Watched.Clear();
            HasLocalScore = false;

            GameAccess.CollectPlayers(_players);
            if (_players.Count == 0)
            {
                DropStaleStates();
                return;
            }

            BucketByFaction();
            int localId = GameAccess.LocalPlayerId;

            for (int i = 0; i < _players.Count; i++)
            {
                PlayerSnapshot self = _players[i];

                List<PlayerSnapshot> friendlies = _byFaction[self.Faction];
                if (friendlies.Count < 3) continue;

                float mateDistance = MateDistance(self, friendlies);
                bool inFormation = FormationDetector.IsInFormation(self, friendlies);

                float rawIsolation = Curve(mateDistance);
                if (inFormation) rawIsolation *= 1f - Mathf.Clamp01(Settings.FormationSuppression.Value);

                State state = ResolveState(self.PlayerId);
                Integrate(state, rawIsolation, dt);

                float enemyDistance;
                int enemyCount;
                MeasureEnemies(self, out enemyDistance, out enemyCount);

                int isolation = Mathf.RoundToInt(state.Isolation * 100f);
                int danger = Mathf.RoundToInt(isolation * EnemyThreat(enemyDistance, enemyCount));

                bool scorable = IsScorable(self);

                // Dwell decays at the recovery rate, fast enough that returning to the line clears the label.
                if (scorable && isolation >= Settings.RamboThreshold.Value) state.Dwell += dt;
                else state.Dwell = Mathf.Max(0f, state.Dwell - dt * Mathf.Max(1f, Settings.RecoverMultiplier.Value));

                ScoredPlayer scored;
                scored.PlayerId = self.PlayerId;
                scored.Name = self.Name;
                scored.Position = self.Position;
                scored.Isolation = isolation;
                scored.Danger = danger;
                scored.DwellSeconds = state.Dwell;
                scored.Flagged = scorable && state.Dwell >= Settings.RamboHoldSeconds.Value;
                scored.MateDistance = mateDistance;
                scored.EnemyDistance = enemyDistance;
                scored.EnemyCount = enemyCount;
                scored.InFormation = inFormation;

                // Drawn on the live score, so a marker appears the moment someone drifts and clears as they return.
                if (scorable && isolation >= Settings.RingThreshold.Value) Watched.Add(scored);

                if (self.PlayerId == localId)
                {
                    LocalScore = scored;
                    HasLocalScore = true;
                }
            }

            SortAndCapWatched();
            DropStaleStates();
        }

        // Hottest first, so the cap and the label limit both keep the ones that matter.
        private void SortAndCapWatched()
        {
            Watched.Sort(delegate(ScoredPlayer a, ScoredPlayer b) { return b.Isolation.CompareTo(a.Isolation); });
            if (Watched.Count > MaxWatched) Watched.RemoveRange(MaxWatched, Watched.Count - MaxWatched);
        }

        private bool IsScorable(PlayerSnapshot self)
        {
            if (self.IsCavalry && !Settings.ScoreCavalry.Value) return false;
            if (self.IsArtillery) return false;
            return !Settings.IsExempt(self.Class);
        }

        // Maps a distance onto 0..1 across the near and far thresholds.
        private static float Curve(float distance)
        {
            float near = Settings.ClusterNearMetres.Value;
            float far = Mathf.Max(near + 1f, Settings.ClusterFarMetres.Value);
            return Mathf.Clamp01((distance - near) / (far - near));
        }

        // Metres from the midpoint of the two nearest living friendlies.
        private float MateDistance(PlayerSnapshot self, List<PlayerSnapshot> friendlies)
        {
            float nearestSquared = float.MaxValue;
            float secondSquared = float.MaxValue;
            Vector3 nearest = Vector3.zero;
            Vector3 second = Vector3.zero;

            for (int i = 0; i < friendlies.Count; i++)
            {
                PlayerSnapshot other = friendlies[i];
                if (other.PlayerId == self.PlayerId) continue;

                float dx = other.Position.x - self.Position.x;
                float dz = other.Position.z - self.Position.z;
                float distanceSquared = dx * dx + dz * dz;

                if (distanceSquared < nearestSquared)
                {
                    secondSquared = nearestSquared;
                    second = nearest;
                    nearestSquared = distanceSquared;
                    nearest = other.Position;
                }
                else if (distanceSquared < secondSquared)
                {
                    secondSquared = distanceSquared;
                    second = other.Position;
                }
            }

            if (secondSquared == float.MaxValue) return float.MaxValue;

            float centreX = (nearest.x + second.x) * 0.5f;
            float centreZ = (nearest.z + second.z) * 0.5f;
            return Horizontal(self.Position.x - centreX, self.Position.z - centreZ);
        }

        private void MeasureEnemies(PlayerSnapshot self, out float nearestDistance, out int countInRadius)
        {
            float radius = Settings.EnemyRadius.Value;
            float radiusSquared = radius * radius;
            float nearestSquared = float.MaxValue;

            countInRadius = 0;

            for (int i = 0; i < _players.Count; i++)
            {
                PlayerSnapshot other = _players[i];
                if (other.Faction == self.Faction) continue;

                float dx = other.Position.x - self.Position.x;
                float dz = other.Position.z - self.Position.z;
                float distanceSquared = dx * dx + dz * dz;

                if (distanceSquared < nearestSquared) nearestSquared = distanceSquared;
                if (distanceSquared <= radiusSquared) countInRadius++;
            }

            nearestDistance = (nearestSquared == float.MaxValue) ? float.MaxValue : Mathf.Sqrt(nearestSquared);
        }

        // How much of a player's isolation is actually a threat: 0 alone in a field, 1 deep in the enemy formation.
        private static float EnemyThreat(float nearestDistance, int countInRadius)
        {
            float radius = Settings.EnemyRadius.Value;
            if (radius <= 0f || nearestDistance > radius) return 0f;

            float proximity = Mathf.Clamp01((radius - nearestDistance) / radius);
            float crowd = Mathf.Clamp01(countInRadius / Mathf.Max(1f, Settings.EnemyCrowd.Value));
            return proximity * (SoloEnemyFloor + (1f - SoloEnemyFloor) * crowd);
        }

        // Exponential approach, frame-rate independent, falling faster than it climbs.
        private static void Integrate(State state, float target, float dt)
        {
            float rise = Mathf.Max(0.05f, Settings.RiseSeconds.Value);
            float k = 1f / rise;
            if (target < state.Isolation) k *= Mathf.Max(1f, Settings.RecoverMultiplier.Value);

            state.Isolation += (target - state.Isolation) * (1f - Mathf.Exp(-k * dt));
        }

        // Built once per tick so the per-player scoring is a lookup rather than another pass.
        private void BucketByFaction()
        {
            foreach (KeyValuePair<FactionCountry, List<PlayerSnapshot>> bucket in _byFaction) bucket.Value.Clear();

            for (int i = 0; i < _players.Count; i++)
            {
                FactionCountry faction = _players[i].Faction;

                List<PlayerSnapshot> bucket;
                if (!_byFaction.TryGetValue(faction, out bucket))
                {
                    bucket = new List<PlayerSnapshot>(150);
                    _byFaction[faction] = bucket;
                }

                bucket.Add(_players[i]);
            }
        }

        private State ResolveState(int playerId)
        {
            State state;
            if (!_states.TryGetValue(playerId, out state))
            {
                state = new State();
                _states[playerId] = state;
            }

            state.LastSeenTick = _tick;
            return state;
        }

        // Forget players who have been dead or gone for a while so a respawn starts clean.
        private void DropStaleStates()
        {
            _stale.Clear();
            foreach (KeyValuePair<int, State> entry in _states)
            {
                if (_tick - entry.Value.LastSeenTick > 60) _stale.Add(entry.Key);
            }

            for (int i = 0; i < _stale.Count; i++) _states.Remove(_stale[i]);
        }

        private static float Horizontal(float dx, float dz)
        {
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
