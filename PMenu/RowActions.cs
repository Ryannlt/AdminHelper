using System;
using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    internal static class RowActions
    {
        private const float ReviveWaitSeconds = 4f;

        private struct Pending
        {
            public int PlayerId;
            public float Deadline;
        }

        private static readonly List<Pending> Waiting = new List<Pending>();
        private static readonly List<PlayerSnapshot> Players = new List<PlayerSnapshot>();
        private static readonly List<PlayerSnapshot> Mates = new List<PlayerSnapshot>();

        public static void Register()
        {
            RyLib.PlayerRow.AddAction(AdminHelperMod.Guid, "Reg TP", RyLib.StockRowButton.Bring, SendToRegiment, Enabled);
            RyLib.PlayerRow.AddAction(AdminHelperMod.Guid, "Rez + Reg TP", RyLib.StockRowButton.Revive, ReviveThenSend, Enabled);
        }

        private static bool Enabled()
        {
            return Settings.RowActionsEnabled != null && Settings.RowActionsEnabled.Value;
        }

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

        private static void ReviveThenSend(int playerId)
        {
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
            PlayerClass type = target.PlayerStartData.ClassType;
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

                if (tag.Length > 0 && player.Class == type && string.Equals(Tag(player.Player), tag, StringComparison.Ordinal))
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
    }
}
