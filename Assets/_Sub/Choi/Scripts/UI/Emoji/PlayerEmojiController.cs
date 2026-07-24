using UnityEngine;
using System.Collections;
using Mirror;

public class PlayerEmojiController : NetworkBehaviour
{
    [Header("컴포넌트 연결")]
    [Tooltip("플레이어 머리 위에 이모지를 보여줄 SpriteRenderer 오브젝트")]
    [SerializeField] private SpriteRenderer emojiSpriteRenderer;

    [Header("이모지 스프라이트 리스트")]
    [SerializeField] private Sprite[] emojiSprites;

    [Header("크기 및 위치 설정")]
    [SerializeField] private float targetSize = 0.5f;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float displayDuration = 2.0f;

    private Transform parentTransform;
    private Transform emojiTransform;
    private Coroutine hideCoroutine;

    private void OnEnable()
    {
        EmojiRadialMenu.OnEmojiIndexSelected += HandleLocalEmojiIndexSelected;
    }

    private void OnDisable()
    {
        EmojiRadialMenu.OnEmojiIndexSelected -= HandleLocalEmojiIndexSelected;
    }

    private void Awake()
    {
        parentTransform = transform;

        if (emojiSpriteRenderer == null)
        {
            emojiSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (emojiSpriteRenderer == null)
            {
                Debug.LogError($"[{gameObject.name}] EmojiSpriteRenderer가 연결되지 않았습니다! 인스펙터를 확인하세요.");
            }
        }

        if (emojiSpriteRenderer != null)
        {
            emojiTransform = emojiSpriteRenderer.transform;
            emojiTransform.SetParent(null);
            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (parentTransform == null) return;

        if (emojiSpriteRenderer != null && emojiSpriteRenderer.gameObject.activeSelf)
        {
            float facingDir = (parentTransform.localScale.x < 0) ? -1f : 1f;
            Vector3 currentOffset = new Vector3(headOffset.x * facingDir, headOffset.y, headOffset.z);
            emojiTransform.position = parentTransform.position + currentOffset;
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

    // 1단계: 메뉴에서 이모지를 골랐을 때
    private void HandleLocalEmojiIndexSelected(int emojiIndex)
    {
        Debug.Log($"[디버그 1] 이벤트 수신됨! Index: {emojiIndex} | 내 캐릭터인가? ({isLocalPlayer})");

        if (!isLocalPlayer) return;

        Debug.Log($"[디버그 2] 내 캐릭터 확인 완료. Cmd 바둑판(서버)으로 전송 시도...");
        CmdShowEmoji(emojiIndex);
    }

    // 2단계: 서버로 명령 전달
    [Command]
    private void CmdShowEmoji(int emojiIndex)
    {
        Debug.Log($"[디버그 3] 서버(Cmd) 도착! 모든 클라이언트에게 Rpc 명령 방송 중... Index: {emojiIndex}");
        RpcShowEmoji(emojiIndex);
    }

    // 3단계: 모든 클라이언트 화면에 명령 하달
    [ClientRpc]
    private void RpcShowEmoji(int emojiIndex)
    {
        Debug.Log($"[디버그 4] 클라이언트(Rpc) 수신 완료! 오브젝트 이름: {gameObject.name}, Index: {emojiIndex}");
        ShowEmojiByIndex(emojiIndex);
    }

    // 4단계: 실제 화면에 띄우기
    private void ShowEmojiByIndex(int emojiIndex)
    {
        if (emojiSpriteRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] emojiSpriteRenderer가 null입니다! 프리팹 연결을 확인하세요.");
            return;
        }

        if (emojiSprites == null || emojiIndex < 0 || emojiIndex >= emojiSprites.Length)
        {
            Debug.LogError($"[{gameObject.name}] 이모지 스프라이트 배열이 비었거나 인덱스가 범위를 벗어났습니다. (Index: {emojiIndex})");
            return;
        }

        if (emojiSprites[emojiIndex] == null)
        {
            Debug.LogError($"[{gameObject.name}] {emojiIndex}번 이모지 스프라이트 슬롯이 비어있습니다(None).");
            return;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        emojiSpriteRenderer.sprite = emojiSprites[emojiIndex];

        float scaleX = Mathf.Abs(targetSize);
        float scaleY = Mathf.Abs(targetSize);
        emojiTransform.localScale = new Vector3(scaleX, scaleY, 1f);

        emojiSpriteRenderer.gameObject.SetActive(true);
        Debug.Log($"[디버그 성공] {gameObject.name} 머리 위에 이모지 켜기 완료!");

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