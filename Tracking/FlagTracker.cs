using System;
using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    internal struct FlagMark
    {
        public Vector3 Position;
        public int PlayerId;
        public string Name;
        public bool Carried;
    }

    internal static class FlagTypes
    {
        private static readonly HashSet<CarryableObjectType> Types = Build();

        public static bool IsFlag(CarryableObjectType type)
        {
            return Types.Contains(type);
        }

        public static string Label(CarryableObjectType type)
        {
            string name = type.ToString();
            if (name == "BearingFlag") return "Bearing";
            if (name.StartsWith("Flag") && name.Length > 4) return name.Substring(4);
            return name;
        }

        private static HashSet<CarryableObjectType> Build()
        {
            HashSet<CarryableObjectType> types = new HashSet<CarryableObjectType>();

            Array values = Enum.GetValues(typeof(CarryableObjectType));
            for (int i = 0; i < values.Length; i++)
            {
                CarryableObjectType type = (CarryableObjectType)values.GetValue(i);
                string name = type.ToString();
                if (name.StartsWith("Flag") || name == "BearingFlag") types.Add(type);
            }

            return types;
        }
    }

    internal sealed class FlagTracker
    {
        private const float CarrierScanInterval = 0.25f;
        private const float PickupScanInterval = 5f;

        private struct Carrier
        {
            public RoundPlayer Player;
            public int PlayerId;
            public string Name;
        }

        public readonly List<FlagMark> Flags = new List<FlagMark>();

        private readonly List<PlayerSnapshot> _players = new List<PlayerSnapshot>();
        private readonly List<Carrier> _carriers = new List<Carrier>();
        private readonly List<SimpleCarryInteractableObject> _pickups = new List<SimpleCarryInteractableObject>();

        private float _nextCarrierScan;
        private float _nextPickupScan;
        private int _reported = -1;

        public void Reset()
        {
            Flags.Clear();
            _players.Clear();
            _carriers.Clear();
            _pickups.Clear();
            _nextCarrierScan = 0f;
            _nextPickupScan = 0f;
            _reported = -1;
        }

        public void Tick()
        {
            if (Time.time >= _nextCarrierScan)
            {
                _nextCarrierScan = Time.time + CarrierScanInterval;
                RescanCarriers();
            }

            if (Time.time >= _nextPickupScan)
            {
                _nextPickupScan = Time.time + PickupScanInterval;
                RescanPickups();
            }

            Flags.Clear();

            for (int i = 0; i < _carriers.Count; i++)
            {
                Carrier carrier = _carriers[i];
                RoundPlayer player = carrier.Player;

                if (player == null || player.PlayerTransformData == null) continue;
                if (player.PlayerBase == null || !player.PlayerBase.SpawnedAndAlive) continue;

                CarryableObjectType held;
                if (!GameAccess.IsCarryingFlag(carrier.PlayerId, out held)) continue;

                FlagMark mark;
                mark.Position = player.PlayerTransformData.position;
                mark.PlayerId = carrier.PlayerId;
                mark.Name = carrier.Name;
                mark.Carried = true;
                Flags.Add(mark);
            }

            for (int i = 0; i < _pickups.Count; i++)
            {
                SimpleCarryInteractableObject pickup = _pickups[i];

                if (pickup == null || !pickup.gameObject.activeInHierarchy) continue;

                FlagMark mark;
                mark.Position = pickup.transform.position;
                mark.PlayerId = -1;
                mark.Name = FlagTypes.Label(pickup.carryableObjectType);
                mark.Carried = false;
                Flags.Add(mark);
            }
        }

        private void RescanCarriers()
        {
            _carriers.Clear();

            GameAccess.CollectPlayers(_players);
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerSnapshot player = _players[i];

                CarryableObjectType held;
                if (!GameAccess.IsCarryingFlag(player.PlayerId, out held)) continue;

                Carrier carrier;
                carrier.Player = player.Player;
                carrier.PlayerId = player.PlayerId;
                carrier.Name = player.Name;
                _carriers.Add(carrier);
            }

            _players.Clear();
        }

        private void RescanPickups()
        {
            _pickups.Clear();

            InteractableObjectCollection all = GameAccess.Interactables;
            if (all == null) return;

            int count = all.Count;
            for (int i = 0; i < count; i++)
            {
                SimpleCarryInteractableObject pickup = all[i] as SimpleCarryInteractableObject;
                if (pickup == null || !FlagTypes.IsFlag(pickup.carryableObjectType)) continue;

                _pickups.Add(pickup);
            }

            if (_pickups.Count != _reported)
            {
                _reported = _pickups.Count;
                Log.Info("flag pickups on this map: " + _pickups.Count + " of " + count + " interactables");
            }
        }
    }
}
