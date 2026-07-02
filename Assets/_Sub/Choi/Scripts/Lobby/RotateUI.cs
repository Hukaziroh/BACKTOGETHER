using UnityEngine;

public class RotateUI : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 200f; // 회전 속도

    void Update()
    {
        // Z축을 기준으로 계속 회전시킵니다.
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}