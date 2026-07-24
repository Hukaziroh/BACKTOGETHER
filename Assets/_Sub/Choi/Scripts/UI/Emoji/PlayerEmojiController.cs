using UnityEngine;
using System.Collections;

public class PlayerEmojiController : MonoBehaviour
{
    [Header("컴포넌트 연결")]
    [Tooltip("플레이어 머리 위에 이모지를 보여줄 SpriteRenderer 오브젝트")]
    [SerializeField] private SpriteRenderer emojiSpriteRenderer;

    [Header("이모지 스프라이트 리스트 (네트워크 동기화용)")]
    [Tooltip("EmojiRadialMenu에 등록된 순서와 동일하게 스프라이트들을 등록해주세요.")]
    [SerializeField] private Sprite[] emojiSprites;

    [Header("크기 및 위치 설정")]
    [Tooltip("이미지 원본 비율과 상관없이 맞출 정사각형 표준 크기 (월드 유닛 기준)")]
    [SerializeField] private float targetSize = 0.5f;

    [Tooltip("캐릭터 머리 위로 띄울 높이 오프셋")]
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.2f, 0f);

    [Header("시간 설정")]
    [Tooltip("이모지가 머리 위에 유지되는 시간 (초)")]
    [SerializeField] private float displayDuration = 2.0f;

    [Header("멀티플레이 설정")]
    [Tooltip("내 캐릭터인지 여부 (EOS 세션 소유자에 맞게 설정)")]
    [SerializeField] private bool isLocalPlayer = true;

    private Transform parentTransform;
    private Transform emojiTransform;
    private Coroutine hideCoroutine;

    private void OnEnable()
    {
        EmojiRadialMenu.OnEmojiIndexSelected += HandleEmojiIndexSelected;
    }

    private void OnDisable()
    {
        EmojiRadialMenu.OnEmojiIndexSelected -= HandleEmojiIndexSelected;
    }

    private void Awake()
    {
        parentTransform = transform;

        if (emojiSpriteRenderer == null)
        {
            emojiSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (emojiSpriteRenderer != null)
        {
            emojiTransform = emojiSpriteRenderer.transform;

            // 부모-자식 관계를 끊어버려 캐릭터의 좌우 반전 시 깜빡임 원천 차단
            emojiTransform.SetParent(null);

            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (parentTransform == null)
        {
            if (emojiSpriteRenderer != null)
                Destroy(emojiSpriteRenderer.gameObject);
            return;
        }

        // 이모지가 켜져있는 동안 위치 실시간 추적
        if (emojiSpriteRenderer != null && emojiSpriteRenderer.gameObject.activeSelf)
        {
            float facingDir = (parentTransform.localScale.x < 0) ? -1f : 1f;
            Vector3 currentOffset = new Vector3(headOffset.x * facingDir, headOffset.y, headOffset.z);
            emojiTransform.position = parentTransform.position + currentOffset;

            // 회전값 고정 (플레이어가 회전해도 이모지는 똑바로 유지)
            emojiTransform.rotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        if (emojiSpriteRenderer != null)
        {
            Destroy(emojiSpriteRenderer.gameObject);
        }
    }

    // 로컬 플레이어가 이모지 메뉴에서 항목을 확정했을 때 호출
    private void HandleEmojiIndexSelected(int emojiIndex)
    {
        if (!isLocalPlayer) return;

        // 1. 내 화면에 즉시 표시
        ShowEmojiByIndex(emojiIndex);

        // 2. EOS 네트워크를 통해 다른 플레이어들에게 선택된 인덱스 전송
        SendEmojiIndexViaEOS(emojiIndex);
    }

    // EOS 네트워크를 통해 다른 클라이언트들에게 이모지 인덱스를 전송하는 함수
    private void SendEmojiIndexViaEOS(int emojiIndex)
    {
        // TODO: 사용 중인 EOS P2P 혹은 네트워크 시스템에 맞춰 바이트 배열 전송 코드를 작성하세요.
        // 예시: 
        // byte[] data = System.BitConverter.GetBytes(emojiIndex);
        // P2PManager.Instance.SendPacketToAll(data);

        Debug.Log($"[EOS 멀티플레이] 내 이모지 인덱스({emojiIndex})를 다른 플레이어들에게 전송함");
    }

    // 다른 플레이어로부터 EOS 패킷을 수신했을 때 외부(네트워크 관리자)에서 호출해 줄 함수
    public void ReceiveEmojiFromEOS(int emojiIndex)
    {
        // 다른 플레이어의 캐릭터에서 이모지를 띄움
        ShowEmojiByIndex(emojiIndex);
    }

    // 인덱스 기반으로 이모지를 화면에 띄우는 공통 로직
    private void ShowEmojiByIndex(int emojiIndex)
    {
        if (emojiSpriteRenderer == null || emojiSprites == null) return;
        if (emojiIndex < 0 || emojiIndex >= emojiSprites.Length) return;
        if (emojiSprites[emojiIndex] == null) return;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        // 1. 스프라이트 교체
        emojiSpriteRenderer.sprite = emojiSprites[emojiIndex];

        // 2. 플레이어 방향과 상관없이 scaleX를 항상 양수(절대값)로 설정하여 좌우 반전 방지
        float scaleX = Mathf.Abs(targetSize);
        float scaleY = Mathf.Abs(targetSize);

        emojiTransform.localScale = new Vector3(scaleX, scaleY, 1f);

        // 3. 활성화
        emojiSpriteRenderer.gameObject.SetActive(true);

        // 4. 타이머 시작
        hideCoroutine = StartCoroutine(HideEmojiRoutine());
    }

    private IEnumerator HideEmojiRoutine()
    {
        yield return new WaitForSeconds(displayDuration);

        if (emojiSpriteRenderer != null)
        {
            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }
}