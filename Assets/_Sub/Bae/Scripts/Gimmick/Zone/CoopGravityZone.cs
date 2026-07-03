using UnityEngine;
using Mirror;

public class CoopGravityZone : MonoBehaviour
{
    [Header("중력 제한 구역 설정")]
    [Tooltip("이 구역을 벗어날 때 캐릭터의 중력을 원래대로(정상) 돌려놓을지 여부")]
    public bool resetGravityOnExit = true;

    [Tooltip("이 게임의 기본 기본 중력 스케일 값 (보통 1~3 사이)")]
    public float defaultGravityScale = 2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 이 구역에 들어오면 플레이어의 기존 PlayerController나 
            // CombineHandler에게 액션키 활성화 신호.
            Debug.Log($"[{other.name}] 중력 반전 가능 구역 진입.");

            // 예시: other.GetComponent<PlayerController>().canInvertGravity = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[{other.name}] 중력 반전 가능 구역 이탈.");
            // 예시: other.GetComponent<PlayerController>().canInvertGravity = false;
            if (resetGravityOnExit)
            {
                Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
                SpriteRenderer sr = other.GetComponent<SpriteRenderer>();

                if (rb != null) rb.gravityScale = defaultGravityScale;
                if (sr != null) sr.flipY = false;

                // 캐릭터 회전값으로 구현했다면 회전값도 Quaternion.identity로 초기화
            }
        }
    }
}