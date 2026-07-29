using UnityEngine;
using Mirror;

public class ChapterBlocker : NetworkBehaviour
{
    [Header("설정")]
    [Tooltip("이 벽이 가로막고 있는 챕터 번호 (예: 2챕터 입구라면 2)")]
    public int targetChapterNumber;

    [Tooltip("오픈 시 재생할 소리 (선택 사항)")]
    public AudioClip openSound;

    [Tooltip("오픈 시 파티클 이펙트 (선택 사항)")]
    public GameObject openEffectPrefab;
    [SyncVar(hook = nameof(OnDoorStateChanged))]
    public bool isOpen = false;
    public override void OnStartServer()
    {
        if (GameSaveManager.Instance != null)
        {
            int maxCleared = GameSaveManager.Instance.currentData.maxClearedChapter;
            if (maxCleared >= targetChapterNumber - 1)
            {
                Debug.Log($"[ChapterBlocker] {targetChapterNumber} 챕터 개방 조건 달성!");
                isOpen = true;
            }
        }
    }

    private void OnDoorStateChanged(bool oldState, bool newState)
    {
        if (newState == true)
        {
            OpenBlocker();
        }
    }

    private void OpenBlocker()
    {
        if (openEffectPrefab != null)
        {
            Instantiate(openEffectPrefab, transform.position, Quaternion.identity);
        }

        if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position);
        }
        gameObject.SetActive(false);
    }
}