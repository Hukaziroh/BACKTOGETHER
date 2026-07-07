using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 5f;
    private SpectatorSystem spectatorSystem;

    void LateUpdate()
    {
        // 씬 전환 등으로 시스템이 유실되면 다시 찾음
        if (spectatorSystem == null)
        {
            spectatorSystem = FindFirstObjectByType<SpectatorSystem>();
        }

        if (spectatorSystem != null && spectatorSystem.CurrentTarget != null)
        {
            Vector3 targetPos = spectatorSystem.CurrentTarget.position;
            // Z축은 카메라 위치를 유지
            Vector3 desiredPosition = new Vector3(targetPos.x, targetPos.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}