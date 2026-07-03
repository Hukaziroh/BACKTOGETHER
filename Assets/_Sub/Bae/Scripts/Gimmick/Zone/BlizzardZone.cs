using UnityEngine;

public class BlizzardZone : MonoBehaviour
{
    [Header("바람 설정")]
    [Tooltip("음수면 왼쪽으로, 양수면 오른쪽으로 밀립니다.")]
    public float windStrength = -3f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.windVelocity = windStrength;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerController>(out var player))
            { 
                player.windVelocity = 0f;
            }
        }
    }
}