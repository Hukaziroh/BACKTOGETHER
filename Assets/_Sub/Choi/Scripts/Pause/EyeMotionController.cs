using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EyeMotionController : MonoBehaviour
{
    [Header("눈동자 스프라이트 설정 (2개)")]
    public Image eyeImage;                // 눈동자 UI Image 컴포넌트
    public Sprite eyeClosedSprite;        // 처음 시작할 때의 감은 눈 스프라이트
    public Sprite eyeOpenSprite;          // 눈을 떴을 때의 뜬 눈 스프라이트

    [Header("연출용 오브젝트 (Fill 방식)")]
    public Image boxImage;                // 스르륵 차오를 박스 Image 컴포넌트
    public List<GameObject> contentElements; // 박스가 다 찬 후 나타날 내부 버튼/텍스트 리스트

    [Header("모션 설정")]
    public float eyeAppearDuration = 0.15f; // 눈이 톡 하고 커지는 시간
    public float fillDuration = 0.25f;      // 박스가 위에서 아래로 채워지는 시간

    private Coroutine activeMotionRoutine;

    // 패널이 켜지는 순간 자동으로 모션 실행
    void OnEnable()
    {
        if (activeMotionRoutine != null)
        {
            StopCoroutine(activeMotionRoutine);
        }
        activeMotionRoutine = StartCoroutine(PlayOpenMotion());
    }

    // 패널이 꺼질 때 초기화
    void OnDisable()
    {
        if (activeMotionRoutine != null)
        {
            StopCoroutine(activeMotionRoutine);
            activeMotionRoutine = null;
        }
        SetContentsActive(false);
    }

    // ==========================================
    // 창 오픈 모션 (감은 눈 등장 -> 눈 뜨기 -> 박스 Fill 채워짐 -> 내부 버튼 등장)
    // ==========================================
    private IEnumerator PlayOpenMotion()
    {
        // 오픈 시작 시 내부 컨텐츠 숨기기
        SetContentsActive(false);

        if (eyeImage == null || boxImage == null) yield break;

        // 1. 눈 초기화: 감은 눈으로 설정하고 크기 0에서 시작
        if (eyeClosedSprite != null) eyeImage.sprite = eyeClosedSprite;
        eyeImage.gameObject.SetActive(true);
        eyeImage.transform.localScale = Vector3.zero;

        // 2. 박스 초기화: Fill Amount를 0으로 설정
        boxImage.type = Image.Type.Filled;
        boxImage.fillAmount = 0f;
        boxImage.gameObject.SetActive(true);

        // Step 1: 감은 눈이 톡 하고 커짐
        float elapsed = 0f;
        while (elapsed < eyeAppearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / eyeAppearDuration;
            float scale = Mathf.Lerp(0f, 1f, t);
            eyeImage.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        eyeImage.transform.localScale = Vector3.one;

        // Step 2: 눈을 번쩍 뜨며 '뜬 눈' 스프라이트로 교체
        if (eyeOpenSprite != null)
        {
            eyeImage.sprite = eyeOpenSprite;
        }

        // Step 3: 박스가 Fill Amount를 이용해 위에서 아래로 스르륵 채워짐 (0 -> 1)
        elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fillDuration;
            t = 1f - (1f - t) * (1f - t); // 부드러운 감속 (EaseOut)

            boxImage.fillAmount = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        boxImage.fillAmount = 1f;

        // Step 4: 박스가 다 찬 후 내부 버튼/컨텐츠 리스트 일괄 활성화
        SetContentsActive(true);
    }

    // 내부 요소 리스트 일괄 제어 함수
    private void SetContentsActive(bool isActive)
    {
        if (contentElements == null) return;
        foreach (GameObject element in contentElements)
        {
            if (element != null)
            {
                element.SetActive(isActive);
            }
        }
    }
}