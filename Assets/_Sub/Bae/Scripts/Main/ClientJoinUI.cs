#nullable enable // 🌟 Nullable(?) 컴파일 에러를 해결하기 위한 구문

using System.Collections;
using UnityEngine;
using TMPro;
using Mirror;
using EpicTransport;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;

public class ClientJoinUI : MonoBehaviour
{
    public TMP_InputField roomCodeInput;

    private LobbySearch? currentSearchHandle;
    private string foundHostAddress = "";
    private bool searchFinished = false;

    public void OnJoinByCodeButtonClicked()
    {
        string roomCode = roomCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
        {
            Debug.LogWarning("올바른 6자리 방 코드를 입력해주세요!");
            return;
        }

        StartCoroutine(SearchAndJoinRoutine(roomCode));
    }

    private IEnumerator SearchAndJoinRoutine(string shortCode)
    {
        Debug.Log("에픽 서버 로그인 상태 확인 중...");
        while (string.IsNullOrEmpty(EOSSDKComponent.LocalUserProductIdString))
        {
            yield return null;
        }

        Debug.Log($"[{shortCode}] 방을 에픽 서버에서 검색합니다...");
        searchFinished = false;
        foundHostAddress = "";

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();

        CreateLobbySearchOptions searchOptions = new CreateLobbySearchOptions();
        searchOptions.MaxResults = 1;

        lobbyInterface.CreateLobbySearch(searchOptions, out currentSearchHandle);

        AttributeData attrData = new AttributeData();
        attrData.Key = "SHORTCODE";
        attrData.Value = new AttributeDataValue() { AsUtf8 = shortCode };

        LobbySearchSetParameterOptions paramOptions = new LobbySearchSetParameterOptions();
        paramOptions.Parameter = attrData;
        paramOptions.ComparisonOp = ComparisonOp.Equal;

        if (currentSearchHandle != null)
        {
            currentSearchHandle.SetParameter(paramOptions);

            LobbySearchFindOptions findOptions = new LobbySearchFindOptions();
            findOptions.LocalUserId = EOSSDKComponent.LocalUserProductId;

            currentSearchHandle.Find(findOptions, null, OnLobbySearchCompleted);
        }

        // 검색이 끝날 때까지 대기
        while (!searchFinished) yield return null;

        if (!string.IsNullOrEmpty(foundHostAddress))
        {
            Debug.Log($"방 검색 성공! 접속합니다. (ID: {foundHostAddress})");
            NetworkManager.singleton.networkAddress = foundHostAddress;
            NetworkManager.singleton.StartClient();
        }
        else
        {
            Debug.LogError("방을 찾을 수 없거나 코드(대/소문자)를 다시 확인해주세요.");
        }
    }

    // 🌟 전용 콜백 함수
    private void OnLobbySearchCompleted(LobbySearchFindCallbackInfo data)
    {
        if (data.ResultCode == Result.Success && currentSearchHandle != null)
        {
            // 🌟 에러 수정: 정확한 구조체 풀네임(LobbySearch...) 적용
            LobbySearchGetSearchResultCountOptions countOptions = new LobbySearchGetSearchResultCountOptions();
            uint count = currentSearchHandle.GetSearchResultCount(countOptions);

            if (count > 0)
            {
                // 🌟 에러 수정: 정확한 구조체 풀네임(LobbySearch...) 적용
                LobbySearchCopySearchResultByIndexOptions copyOptions = new LobbySearchCopySearchResultByIndexOptions();
                copyOptions.LobbyIndex = 0;

                currentSearchHandle.CopySearchResultByIndex(copyOptions, out LobbyDetails lobbyDetails);

                LobbyDetailsCopyInfoOptions infoOptions = new LobbyDetailsCopyInfoOptions();

                // 🌟 에러 수정: var를 사용하여 타입 추론 유도
                lobbyDetails.CopyInfo(infoOptions, out var lobbyInfo);

                foundHostAddress = lobbyInfo?.LobbyOwnerUserId.ToString() ?? "";
            }
        }

        searchFinished = true; // 검색 완료 처리
    }
}