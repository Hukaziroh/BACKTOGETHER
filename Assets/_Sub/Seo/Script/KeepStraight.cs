using UnityEngine;

public class KeepStraight : MonoBehaviour
{
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (transform.parent == null) return;

        float parentDirection = Mathf.Sign(transform.parent.localScale.x);

        transform.localScale = new Vector3(
            originalScale.x * parentDirection,
            originalScale.y,
            originalScale.z
        );
    }
}