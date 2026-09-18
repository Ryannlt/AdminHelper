using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    internal sealed class IsolationTracker
    {
        private const float SoloEnemyFloor = 0.5f;

        private const int MaxWatched = 32;

        private sealed class State
        {
            public float Isolation;
            public float Dwell;
            public int LastSeenTick;
            public float GraceUntil;
            public bool WasInMelee;
            public bool WasFlagged;
        }

        private readonly Dictionary<int, State> _states = new Dictionary<int, State>();
        private readonly List<PlayerSnapshot> _players = new List<PlayerSnapshot>();
        private readonly Dictionary<FactionCountry, List<PlayerSnapshot>> _byFaction =
            new Dictionary<FactionCountry, List<PlayerSnapshot>>();
        private readonly List<int> _stale = new List<int>();
        private readonly AfkTracker _afk = new AfkTracker();

        private int _tick;

        public readonly List<ScoredPlayer> Watched = new List<ScoredPlayer>();
        public ScoredPlayer LocalScore;
        public bool HasLocalScore;

        public void Reset()
        {
            _afk.Reset();
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

                if (scorable && isolation >= Settings.RamboThreshold.Value) state.Dwell += dt;
                else state.Dwell = Mathf.Max(0f, state.Dwell - dt * Mathf.Max(1f, Settings.RecoverMultiplier.Value));

                ScoredPlayer scored;
                scored.PlayerId = self.PlayerId;
                scored.Name = self.Name;
                scored.Position = self.Position;
                scored.Isolation = isolation;
                scored.Danger = danger;
                scored.DwellSeconds = state.Dwell;
                bool rawFlag = scorable && state.Dwell >= Settings.RamboHoldSeconds.Value;
                bool graced = UpdateGrace(state, self.PlayerId, rawFlag);
                float still = Settings.AfkMarkEnabled.Value ? _afk.Observe(self) : 0f;

                scored.Flagged = rawFlag && !graced;
                scored.InHonestMelee = graced;
                scored.Afk = Settings.AfkMarkEnabled.Value && still >= Mathf.Max(5f, Settings.AfkSeconds.Value);
                scored.AfkSeconds = still;
                scored.MateDistance = mateDistance;
                scored.EnemyDistance = enemyDistance;
                scored.EnemyCount = enemyCount;
                scored.InFormation = inFormation;

                if (scorable && isolation >= Settings.RingThreshold.Value) Watched.Add(scored);

                if (self.PlayerId == localId)
                {
                    LocalScore = scored;
                    HasLocalScore = true;
                }
            }

            SortAndCapWatched();
            DropStaleStates();
            _afk.Prune();
        }

        private static bool UpdateGrace(State state, int playerId, bool rawFlag)
        {
            bool enabled = Settings.MeleeGraceEnabled != null && Settings.MeleeGraceEnabled.Value;
            bool inMelee = enabled && MeleeTracker.Query(playerId) == MeleeTracker.State.InMelee;

            if (inMelee)
            {
                bool eligible = state.WasInMelee ? state.GraceUntil > 0f : !state.WasFlagged;
                if (eligible) state.GraceUntil = Time.time + GraceSeconds();
            }

            state.WasInMelee = inMelee;
            state.WasFlagged = rawFlag;

            return enabled && Time.time < state.GraceUntil;
        }

        private static float GraceSeconds()
        {
            float window = (Settings.MeleeWindowSeconds == null) ? 10f : Settings.MeleeWindowSeconds.Value;
            return Mathf.Max(1f, window) + Mathf.Max(0f, Settings.RamboHoldSeconds.Value);
        }

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

        private static float Curve(float distance)
        {
            float near = Settings.ClusterNearMetres.Value;
            float far = Mathf.Max(near + 1f, Settings.ClusterFarMetres.Value);
            return Mathf.Clamp01((distance - near) / (far - near));
        }

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

        private static float EnemyThreat(float nearestDistance, int countInRadius)
        {
            float radius = Settings.EnemyRadius.Value;
            if (radius <= 0f || nearestDistance > radius) return 0f;

            float proximity = Mathf.Clamp01((radius - nearestDistance) / radius);
            float crowd = Mathf.Clamp01(countInRadius / Mathf.Max(1f, Settings.EnemyCrowd.Value));
            return proximity * (SoloEnemyFloor + (1f - SoloEnemyFloor) * crowd);
        }

        private static void Integrate(State state, float target, float dt)
        {
            float rise = Mathf.Max(0.05f, Settings.RiseSeconds.Value);
            float k = 1f / rise;
            if (target < state.Isolation) k *= Mathf.Max(1f, Settings.RecoverMultiplier.Value);

            state.Isolation += (target - state.Isolation) * (1f - Mathf.Exp(-k * dt));
        }

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
