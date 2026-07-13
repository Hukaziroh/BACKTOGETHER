using Mirror;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI 패널")]
    public GameObject lobbyPanel;      // 로비 시작 화면
    public GameObject chapterPanel;    // 챕터 선택 화면
    public GameObject waitPanel;       // 클라이언트 대기 화면

    // 호스트가 챕터 선택창을 열었는지 상태 동기화
    [SyncVar(hook = nameof(OnChapterSelectionOpened))]
    private bool isChapterSelectionOpen = false;

    // 1. [로비] 플레이 버튼 클릭 시 (서버에 요청)
    public void RequestOpenChapter()
    {
        if (isServer) CmdSetChapterSelection(true);
        else Debug.Log("방장만 챕터를 선택할 수 있습니다.");
    }

    // 2. [챕터창] 뒤로가기 버튼 클릭 시 (서버에 요청)
    public void RequestCloseChapter()
    {
        if (isServer) CmdSetChapterSelection(false);
    }

    // 서버에게 상태 변경을 명령
    [Command(requiresAuthority = false)]
    private void CmdSetChapterSelection(bool isOpen)
    {
        isChapterSelectionOpen = isOpen;
    }

    // 상태가 바뀔 때 모든 클라이언트의 UI 업데이트
    private void OnChapterSelectionOpened(bool oldVal, bool newVal)
    {
        UpdateUI(newVal);
    }

    private void UpdateUI(bool isSelecting)
    {
        if (isServer) // 방장
        {
            lobbyPanel.SetActive(!isSelecting);
            chapterPanel.SetActive(isSelecting);
            waitPanel.SetActive(false); // 방장은 대기할 필요 없음
        }
        else // 클라이언트
        {
            lobbyPanel.SetActive(false);
            chapterPanel.SetActive(false);
            waitPanel.SetActive(isSelecting); // 대기창 표시
        }
    }

    // 3. [챕터창] 게임 시작
    public void StartGame(string sceneName)
    {
        if (isServer)
        {
            Debug.Log($"방장이 {sceneName} 씬으로 게임을 시작합니다!");
            NetworkManager.singleton.ServerChangeScene(sceneName);
        }
    }
}