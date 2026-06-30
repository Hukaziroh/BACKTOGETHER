using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class HostDisconnectHandler : MonoBehaviour
{
    [Header("돌아갈 로비 씬 이름")]
    public string lobbySceneName = "Test";

    private bool wasConnected = false;

    void Update()
    {
        if (NetworkServer.active) return;

        if (NetworkClient.isConnected && !wasConnected)
        {
            wasConnected = true;
        }

        if (wasConnected && !NetworkClient.isConnected)
        {
            wasConnected = false; 
            OnHostDisconnected();
        }
    }

    private void OnHostDisconnected()
    {
        Debug.LogError("🚨 방장이 게임을 종료했거나 연결이 끊어졌습니다! 로비로 강제 이동합니다.");

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
        }

        SceneManager.LoadScene(lobbySceneName);
    }
}