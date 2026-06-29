using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;

public class PlayerEmoji : NetworkBehaviour
{
    public Image emojiDisplay; // 플레이어 머리 위 EmojiDisplay Image 연결
    public Sprite[] emojiSprites; // 사용할 이모지 스프라이트들을 인스펙터에서 할당

    void Start()
    {
        // 평소에는 이모지를 끕니다.
        emojiDisplay.enabled = false;
    }

    // 1. UI에서 이모지를 클릭했을 때 호출되는 함수
    public void SelectEmoji(int emojiIndex)
    {
        if (isLocalPlayer) // 내 플레이어일 때만 서버에 요청
        {
            CmdShowEmoji(emojiIndex);
        }
    }

    // 2. 서버에 이모지 표시를 요청하는 Command
    [Command]
    void CmdShowEmoji(int emojiIndex)
    {
        // 서버에서 모든 클라이언트에게 이모지를 표시하라고 명령 (Rpc 호출)
        RpcShowEmoji(emojiIndex);
    }

    // 3. 모든 클라이언트에서 실행되어 이모지를 보여주는 ClientRpc
    [ClientRpc]
    void RpcShowEmoji(int emojiIndex)
    {
        // 인덱스가 유효한지 확인
        if (emojiIndex >= 0 && emojiIndex < emojiSprites.Length)
        {
            emojiDisplay.sprite = emojiSprites[emojiIndex];
            StopAllCoroutines(); // 혹시 실행 중인 다른 이모지 코루틴이 있다면 정지
            StartCoroutine(ShowEmojiCoroutine());
        }
    }

    // 이모지를 잠시 보여주고 자동으로 끄는 코루틴
    IEnumerator ShowEmojiCoroutine()
    {
        emojiDisplay.enabled = true;
        yield return new WaitForSeconds(3f); // 3초 동안 표시
        emojiDisplay.enabled = false;
    }
}