using UnityEngine;
using EpicTransport;
using Mirror;
using System.Collections;
using UnityEngine.UI; // 버튼 제어를 위해 추가

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 연결")]
    [Tooltip("방 만들기 버튼을 연결하세요 (광클 방지용)")]
    public Button hostCreateButton;

    public void OnStartPrivateHostClicked()
    {
        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();

        if (eosLobby == null)
        {
            Debug.LogError("NetworkManager에서 EOSLobby 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        // 1. 유저 광클 방지 (버튼 비활성화)
        if (hostCreateButton != null)
        {
            hostCreateButton.interactable = false;
        }

        // 2. 안전한 방 생성을 위한 코루틴 시작
        StartCoroutine(CleanAndCreateLobbyRoutine(eosLobby));
    }

    private IEnumerator CleanAndCreateLobbyRoutine(EOSLobby eosLobby)
    {
        Debug.Log("[로비 시스템] 1단계: 기존 네트워크 연결 및 잔류 세션 강제 종료 중...");

        // 기존 Mirror 클라이언트 및 서버 접속 강제 종료
        if (NetworkClient.active) NetworkManager.singleton.StopClient();
        if (NetworkServer.active) NetworkManager.singleton.StopHost();

        // 에픽 서버에 기존 로비 폭파 요청
        eosLobby.LeaveLobby();

        // 🌟 [매우 중요] 에픽 클라우드 서버가 기존 세션을 완전히 삭제할 때까지 기다립니다.
        // 인터넷 환경을 고려해 1.5초 ~ 2초 정도 넉넉히 주는 것이 가장 안전합니다.
        yield return new WaitForSeconds(2f);

        Debug.Log("[로비 시스템] 2단계: 세션 초기화 완료. 새로운 에픽 로비를 생성합니다.");

        // 새 로비 생성 세팅
        uint maxPlayers = 4;
        Epic.OnlineServices.Lobby.LobbyPermissionLevel permissionLevel = Epic.OnlineServices.Lobby.LobbyPermissionLevel.Inviteonly;
        bool presenceEnabled = true;

        // 에픽 서버에 신규 로비 할당 명령 실행
        eosLobby.CreateLobby(maxPlayers, permissionLevel, presenceEnabled);

        // 로비가 에픽 서버에 완전히 등록될 때까지 아주 짧은 대기
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[로비 시스템] 3단계: 새 방 생성 완료! Mirror 호스트를 시작합니다.");
        NetworkManager.singleton.StartHost();

        // 방 생성이 완료되었으므로 다시 버튼 활성화 (혹은 씬이 넘어가면 비활성 상태로 유지됨)
        if (hostCreateButton != null)
        {
            hostCreateButton.interactable = true;
        }
    }
}