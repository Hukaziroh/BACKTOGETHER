using UnityEngine;

public class BossSmokeSway : MonoBehaviour
{
    [Header("흔들림 강도")]
    public float scaleAmount = 0.04f;
    public float scaleSpeed = 0.6f;
    public float rotationAmount = 2f;
    public float rotationSpeed = 0.4f;

    private Vector3 baseScale;
    private float noiseSeed;

    void Start()
    {
        baseScale = transform.localScale;
        noiseSeed = Random.Range(0f, 100f);
    }

    void Update()
    {
        float t = Time.time;

        float scaleNoise = Mathf.PerlinNoise(noiseSeed, t * scaleSpeed) - 0.5f;
        transform.localScale = baseScale * (1f + scaleNoise * scaleAmount);

        float rotNoise = Mathf.PerlinNoise(noiseSeed + 50f, t * rotationSpeed) - 0.5f;
        transform.localRotation = Quaternion.Euler(0f, 0f, rotNoise * rotationAmount * 2f);
    }
}
