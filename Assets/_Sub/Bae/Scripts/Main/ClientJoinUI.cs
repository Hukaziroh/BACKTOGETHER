using System.Collections;
using UnityEngine;
using TMPro;
using Mirror;
using EpicTransport;

public class ClientJoinUI : MonoBehaviour
{
    public TMP_InputField roomCodeInput;

    public void OnJoinByCodeButtonClicked()
    {
        string roomCode = roomCodeInput.text.Trim();

        if (!string.IsNullOrEmpty(roomCode))
        {
            StartCoroutine(JoinWhenReady(roomCode));
        }
        else
        {
            Debug.LogWarning("방 코드를 입력해주세요!");
        }
    }

    private IEnumerator JoinWhenReady(string roomCode)
    {
        Debug.Log("에픽 서버 로그인 상태 확인 중...");
        while (string.IsNullOrEmpty(EOSSDKComponent.LocalUserProductIdString))
        {
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);

        Debug.Log($"로그인 확인 완료! 방 코드[{roomCode}]로 접속을 시작합니다.");
        NetworkManager.singleton.networkAddress = roomCode;

        NetworkManager.singleton.StartClient();
    }
}