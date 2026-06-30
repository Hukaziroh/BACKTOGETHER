using UnityEngine;
using Mirror;

public class LobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    public GameObject lobbyPanel;
    public GameObject chapterPanel;

    // 1. [로비] 플레이 버튼 클릭 시
    public void OpenChapterSelection()
    {
        lobbyPanel.SetActive(false);
        chapterPanel.SetActive(true);
    }

    // 2. [챕터창] 뒤로가기 버튼 클릭 시
    public void CloseChapterSelection()
    {
        chapterPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    // 3. [챕터창] 실제 게임 시작 버튼 클릭 시 (호스트만 가능)
    public void StartGame(string sceneName)
    {
        if (NetworkServer.active)
        {
            Debug.Log($"방장이 {sceneName} 씬으로 게임을 시작합니다!");
            NetworkManager.singleton.ServerChangeScene(sceneName);
        }
        else
        {
            Debug.LogWarning("게임 시작은 방장만 할 수 있습니다!");
        }
    }
}