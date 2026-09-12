using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
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

    internal struct ScoredPlayer
    {
        public int PlayerId;
        public string Name;
        public Vector3 Position;
        public int Isolation;
        public int Danger;
        public float DwellSeconds;
        public bool Flagged;

        public float MateDistance;
        public float EnemyDistance;
        public int EnemyCount;
        public bool InFormation;
    }
}
