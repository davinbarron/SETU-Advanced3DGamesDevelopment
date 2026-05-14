using Fusion;
using UnityEngine;

namespace Example
{
    public static class NetworkUtils
    {
        /// <summary>
        /// Shared logic for requesting state authority when the current authority leaves.
        /// This is intended to be called from OnPlayerLeft in Shared Mode.
        /// </summary>
        public static void RequestAuthorityIfOwnerLeft(NetworkObject networkObject, PlayerRef player, string contextName = "")
        {
            if (networkObject == null) return;
            if (networkObject.Runner.GameMode != GameMode.Shared) return;

            // If the player who left was the state authority, or if the authority is now invalid (PlayerRef.None)
            bool authorityLeft = (networkObject.StateAuthority == player || networkObject.StateAuthority == PlayerRef.None);

            if (authorityLeft && !networkObject.HasStateAuthority)
            {
                Debug.Log($"[NetworkUtils] {contextName}: Requesting authority takeover for {networkObject.name} because player {player} left.");
                networkObject.RequestStateAuthority();
            }
        }

        /// <summary>
        /// Robust lookup for a player's NetworkObject based on their PlayerRef.
        /// Includes a fallback search through active behaviours if the standard mapping is missing.
        /// </summary>
        public static NetworkObject GetPlayerObject(NetworkRunner runner, PlayerRef playerRef)
        {
            if (playerRef == PlayerRef.None) return null;

            // 1. Try standard Fusion mapping
            NetworkObject obj = runner.GetPlayerObject(playerRef);
            if (obj != null) return obj;

            // 2. Fallback: Manually find the player behavior in the scene
            var players = new System.Collections.Generic.List<Player>();
            runner.GetAllBehaviours(players);

            foreach (var p in players)
            {
                if (p.Object != null && p.Object.InputAuthority == playerRef)
                {
                    // Cache it back into the runner for future fast lookup
                    runner.SetPlayerObject(playerRef, p.Object);
                    return p.Object;
                }
            }


            return null;
        }

        /// <summary>
        /// Returns a list of all active player components in the session.
        /// </summary>
        public static System.Collections.Generic.List<Player> GetAllPlayers(NetworkRunner runner)
        {
            var players = new System.Collections.Generic.List<Player>();
            if (runner != null && runner.IsRunning)
            {
                runner.GetAllBehaviours(players);
            }
            return players;
        }
    }
}
