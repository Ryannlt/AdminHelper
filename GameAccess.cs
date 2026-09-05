using System.Collections.Generic;
using HoldfastGame;
using UnityEngine;

namespace AdminHelper
{
    // Single point of contact with the game. Everything here is public client-side state, read only.
    internal static class GameAccess
    {
        private static GameConsolePanel _consolePanel;
        private static float _nextConsoleLookup;

        public static ClientComponentReferenceManager Client
        {
            get { return ClientComponentReferenceManager.ClientInstance; }
        }

        // True once the server itself has authenticated an 'rc login'.
        public static bool IsLoggedInAdmin
        {
            get { return ClientRemoteConsoleAccessManager.loggedOn; }
        }

        public static bool InRound
        {
            get
            {
                ClientComponentReferenceManager client = Client;
                return client != null && client.clientRoundPlayerManager != null;
            }
        }

        public static Camera ActiveCamera
        {
            get
            {
                ClientComponentReferenceManager client = Client;
                if (client == null || client.ownerCameraManager == null) return null;
                Camera camera = client.ownerCameraManager.ActiveCamera;
                if (camera == null) camera = client.ownerCameraManager.ownerCamera;
                return camera;
            }
        }

        public static int LocalPlayerId
        {
            get
            {
                ClientComponentReferenceManager client = Client;
                if (client == null || client.clientRoundPlayerManager == null) return -1;
                ClientRoundPlayerOwner local = client.clientRoundPlayerManager.LocalPlayer;
                if (local == null) return -1;
                return local.NetworkPlayerID;
            }
        }

        // True while a text field owns the keyboard, so hotkeys must be ignored.
        public static bool IsTyping
        {
            get
            {
                ClientComponentReferenceManager client = Client;
                if (client != null && client.clientChatHandler != null && client.clientChatHandler.isChatPaneOpened)
                    return true;

                // FindObjectOfType is expensive, so retry on a timer rather than every frame before it exists.
                if (_consolePanel == null && Time.unscaledTime >= _nextConsoleLookup)
                {
                    _nextConsoleLookup = Time.unscaledTime + 2f;
                    _consolePanel = Object.FindObjectOfType<GameConsolePanel>();
                }

                return _consolePanel != null && _consolePanel.Showing;
            }
        }

        // Dropped on scene change so the next lookup finds the new instance.
        public static void ClearSceneCache()
        {
            _consolePanel = null;
            _nextConsoleLookup = 0f;
        }

        // Fills 'into' with every spawned and alive player, local one included. Allocation free after warmup.
        public static void CollectPlayers(List<PlayerSnapshot> into)
        {
            into.Clear();

            ClientComponentReferenceManager client = Client;
            if (client == null) return;

            ClientRoundPlayerManager players = client.clientRoundPlayerManager;
            if (players == null) return;

            List<ClientRoundPlayerProxy> remotes = players.roundPlayersList;
            for (int i = 0; i < remotes.Count; i++)
            {
                PlayerSnapshot snapshot;
                if (TrySnapshot(remotes[i], out snapshot)) into.Add(snapshot);
            }

            PlayerSnapshot localSnapshot;
            if (TrySnapshot(players.LocalPlayer, out localSnapshot)) into.Add(localSnapshot);
        }

        private static bool TrySnapshot(RoundPlayer player, out PlayerSnapshot snapshot)
        {
            snapshot = default(PlayerSnapshot);

            if (player == null || player.PlayerBase == null || !player.PlayerBase.SpawnedAndAlive) return false;
            if (player.PlayerTransformData == null || player.PlayerStartData == null) return false;

            PlayerBase playerBase = player.PlayerBase;

            snapshot.Player = player;
            snapshot.PlayerId = player.NetworkPlayerID;
            snapshot.Name = ResolveName(player);
            snapshot.Position = player.PlayerTransformData.position;
            snapshot.Faction = player.PlayerStartData.Faction;
            snapshot.Class = player.PlayerStartData.ClassType;
            snapshot.IsCavalry = playerBase.IsCavalry;
            snapshot.IsArtillery = playerBase.IsArty;
            return true;
        }

        private static string ResolveName(RoundPlayer player)
        {
            RoundPlayerInformation info = player.PlayerRoundInformation;
            if (info == null || info.InitialDetails == null) return "Player " + player.NetworkPlayerID;

            PlayerInitialDetails details = info.InitialDetails;
            string name = string.IsNullOrEmpty(details.DisplayName) ? details.Name : details.DisplayName;
            if (string.IsNullOrEmpty(name)) return "Player " + player.NetworkPlayerID;
            return name;
        }

        // Whether an officer or sergeant has placed a form-line order that this player is standing inside.
        public static bool IsInsideOfficerLine(RoundPlayer player)
        {
            ClientComponentReferenceManager client = Client;
            if (client == null || client.clientHighCommandOrderManager == null) return false;

            return client.clientHighCommandOrderManager.IsPlayerInsideAnyOfficerLine(player);
        }
    }
}
