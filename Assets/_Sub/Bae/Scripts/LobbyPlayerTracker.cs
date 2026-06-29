using UnityEngine;
using Mirror;

public class LobbyPlayerTracker : NetworkBehaviour
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        UpdateLobbyCount();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        UpdateLobbyCount();
    }

    [Server]
    private void UpdateLobbyCount()
    {
        LobbySyncManager syncManager = FindFirstObjectByType<LobbySyncManager>();

        if (syncManager != null)
        {
            syncManager.RefreshPlayerCount();
        }
    }
}