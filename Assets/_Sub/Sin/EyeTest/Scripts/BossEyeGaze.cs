using UnityEngine;
using System.Collections;

public class BossEyeGaze : MonoBehaviour
{
    [Header("눈동자 포즈 (Eyeball, Eyemiddle, EyeSide, EyeRight 등)")]
    public SpriteRenderer eyeRenderer;
    public Sprite[] eyePoses;

    [Header("랜덤 시선 전환")]
    public float minGazeDelay = 1f;
    public float maxGazeDelay = 3f;

    [Header("깜빡임 (감은 눈 그림 없이 스케일로 표현)")]
    public bool enableBlinking = true;
    public float minBlinkDelay = 2f;
    public float maxBlinkDelay = 5f;
    public float blinkCloseTime = 0.06f;
    public float blinkHoldTime = 0.03f;
    public float blinkOpenTime = 0.08f;

    void Start()
    {
        if (eyeRenderer == null) eyeRenderer = GetComponent<SpriteRenderer>();

        if (eyePoses != null && eyePoses.Length > 0)
            eyeRenderer.sprite = eyePoses[Random.Range(0, eyePoses.Length)];

        StartCoroutine(GazeLoop());
        StartCoroutine(BlinkLoop());
    }

    IEnumerator GazeLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minGazeDelay, maxGazeDelay));

            if (eyePoses == null || eyePoses.Length == 0) continue;

            eyeRenderer.sprite = eyePoses[Random.Range(0, eyePoses.Length)];
        }
    }

    IEnumerator BlinkLoop()
    {
        Vector3 baseScale = transform.localScale;

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minBlinkDelay, maxBlinkDelay));

            if (!enableBlinking) continue;

            yield return Blink(baseScale);
        }
    }

    IEnumerator Blink(Vector3 baseScale)
    {
        float t = 0f;
        while (t < blinkCloseTime)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / blinkCloseTime);
            transform.localScale = new Vector3(baseScale.x, Mathf.Lerp(baseScale.y, baseScale.y * 0.05f, ratio), baseScale.z);
            yield return null;
        }

        yield return new WaitForSeconds(blinkHoldTime);

        t = 0f;
        while (t < blinkOpenTime)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / blinkOpenTime);
            transform.localScale = new Vector3(baseScale.x, Mathf.Lerp(baseScale.y * 0.05f, baseScale.y, ratio), baseScale.z);
            yield return null;
        }

        transform.localScale = baseScale;
    }
}
