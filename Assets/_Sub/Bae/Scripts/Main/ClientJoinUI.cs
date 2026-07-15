#nullable enable
using System.Collections;
using UnityEngine;
using Mirror;
using EpicTransport;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;

public class ClientJoinUI : MonoBehaviour
{
    // [수정] 6자리 숫자 입력 UI 컨트롤러 연결
    public SixDigitCodeInputUI sixDigitUI;

    private LobbySearch? currentSearchHandle;
    private string foundHostAddress = "";
    private bool searchFinished = false;

    public void OnJoinByCodeButtonClicked()
    {
        if (sixDigitUI == null)
        {
            Debug.LogError("SixDigitCodeInputUI가 연결되지 않았습니다!");
            return;
        }

        // [수정] 6자리 코드를 가져옵니다.
        string roomCode = sixDigitUI.GetCode();

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

    private void OnLobbySearchCompleted(LobbySearchFindCallbackInfo data)
    {
        if (data.ResultCode == Result.Success && currentSearchHandle != null)
        {
            LobbySearchGetSearchResultCountOptions countOptions = new LobbySearchGetSearchResultCountOptions();
            uint count = currentSearchHandle.GetSearchResultCount(countOptions);

            if (count > 0)
            {
                LobbySearchCopySearchResultByIndexOptions copyOptions = new LobbySearchCopySearchResultByIndexOptions();
                copyOptions.LobbyIndex = 0;

                currentSearchHandle.CopySearchResultByIndex(copyOptions, out LobbyDetails lobbyDetails);

                LobbyDetailsCopyInfoOptions infoOptions = new LobbyDetailsCopyInfoOptions();
                lobbyDetails.CopyInfo(infoOptions, out var lobbyInfo);

                foundHostAddress = lobbyInfo?.LobbyOwnerUserId.ToString() ?? "";
            }
        }

        searchFinished = true;
    }
}