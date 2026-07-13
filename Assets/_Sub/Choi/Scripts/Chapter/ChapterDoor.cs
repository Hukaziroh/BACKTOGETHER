using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ChapterDoor : NetworkBehaviour
{
    [Header("설정")]
    public GameObject fanfarePrefab;
    public string ChapterSceneName = "Lobby"; // 다음으로 넘어갈 씬 이름 (인스펙터에서 수정)
    public float floatSpeed = 2f;

    // ★ [추가] 이 문이 몇 번째 챕터 문인지 설정 (인스펙터에서 1, 2, 3 등 입력)
    public int chapterNumber = 1;

    [Header("UI 요소")]
    public GameObject ChapterTextUI;  // 클리어 시 나타날 텍스트 오브젝트
    public TMP_Text countText;      // 현재 도착 인원 표시용 TMP 텍스트

    // 도착한 플레이어들을 저장할 서버 전용 리스트
    private HashSet<uint> arrivedPlayers = new HashSet<uint>();

    void Start()
    {
        // 처음에는 카운트 텍스트를 숨김
        if (countText != null) countText.gameObject.SetActive(false);
        if (ChapterTextUI != null) ChapterTextUI.SetActive(false);
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        NetworkIdentity identity = collision.GetComponent<NetworkIdentity>();
        if (identity != null && collision.CompareTag("Player"))
        {
            if (!arrivedPlayers.Contains(identity.netId))
            {
                arrivedPlayers.Add(identity.netId);

                // 모든 클라이언트에게 숫자 업데이트 명령
                RpcUpdateCount(arrivedPlayers.Count, NetworkServer.connections.Count);

                // 모든 플레이어가 도착했는지 확인
                if (arrivedPlayers.Count >= CoopPlayerIdentity.players.Count)
                {
                    RpcTriggerClearEffect();
                    StartCoroutine(WaitAndLoadScene());
                }
            }
        }
    }

    [ClientRpc]
    private void RpcUpdateCount(int current, int total)
    {
        if (countText != null)
        {
            countText.gameObject.SetActive(true);
            countText.text = $"{current} / {total}";
        }
    }

    [ClientRpc]
    private void RpcTriggerClearEffect()
    {
        // 카운트 텍스트 숨기기
        if (countText != null) countText.gameObject.SetActive(false);

        // 파티클 생성
        if (fanfarePrefab != null)
            Instantiate(fanfarePrefab, transform.position, Quaternion.identity);

        // 클리어 텍스트 띄우기
        if (ChapterTextUI != null)
        {
            // ★ [핵심 추가] clearTextUI 오브젝트나 그 자식들 중에서 TMP_Text 컴포넌트를 찾아 텍스트를 동적으로 변경합니다.
            // (비활성화 상태여도 찾을 수 있도록 true 매개변수 사용)
            TMP_Text textComponent = ChapterTextUI.GetComponentInChildren<TMP_Text>(true);
            if (textComponent != null)
            {
                textComponent.text = $"WARP CHAPTER {chapterNumber}";
            }

            ChapterTextUI.SetActive(true);
            StartCoroutine(FloatTextRoutine());
        }
    }

    private IEnumerator FloatTextRoutine()
    {
        float timer = 0f;
        Vector3 startPos = ChapterTextUI.transform.localPosition;

        while (timer < 1.5f)
        {
            timer += Time.deltaTime;
            ChapterTextUI.transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);
            yield return null;
        }

        ChapterTextUI.SetActive(false);
        ChapterTextUI.transform.localPosition = startPos; // 위치 초기화
    }

    private IEnumerator WaitAndLoadScene()
    {
        yield return new WaitForSeconds(2.0f);
        if (isServer)
        {
            NetworkManager.singleton.ServerChangeScene(ChapterSceneName);
        }
    }
}