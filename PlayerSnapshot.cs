using HoldfastGame;
using UnityEngine;

namespace AdminEye
{
    // One player as of the current tick. Rebuilt each tick so nothing here outlives a respawn.
    internal struct PlayerSnapshot
    {
        public RoundPlayer Player;
        public int PlayerId;
        public string Name;
        public Vector3 Position;
        public FactionCountry Faction;
        public PlayerClass Class;
        public bool IsCavalry;
        public bool IsArtillery;
    }

    // What the scorer produced for one player, ready to draw.
    internal struct ScoredPlayer
    {
        public int PlayerId;
        public string Name;
        public Vector3 Position;
        public int Isolation;
        public int Danger;
        public float DwellSeconds;
        public bool Flagged;

        // Raw inputs, kept for the diagnostic readout so thresholds can be picked from real rounds.
        public float MateDistance;
        public float EnemyDistance;
        public int EnemyCount;
        public bool InFormation;
    }
}
