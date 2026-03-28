using UnityEngine;
using Fusion;
using Example;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkObject _playerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            // Find all spawn points in the scene
            var spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (spawnPoints.Length > 0)
            {
                int index = player.AsIndex % spawnPoints.Length;
                spawnPosition = spawnPoints[index].transform.position;
                spawnRotation = spawnPoints[index].transform.rotation;
            }

            Runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, player);
            Debug.Log($"Spawned local player {player} at {spawnPosition}");
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Use PlayerManager to find and despawn the leaving player's object
        PlayerManager<Player>.UpdatePlayerConnections(
            Runner,
            _ => { },
            (playerRef, playerObject) =>
            {
                if (playerRef == player)
                {
                    Runner.Despawn(playerObject.Object);
                    Debug.Log($"Despawned player {player}");
                }
            }
        );
    }
}