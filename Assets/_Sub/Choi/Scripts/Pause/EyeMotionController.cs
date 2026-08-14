using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EyeMotionController : MonoBehaviour
{
    [Header("눈동자 스프라이트 설정")]
    public Image eyeImage;                // 눈동자 UI Image 컴포넌트
    public Sprite eyeOpenSprite;          // 뜬 눈 스프라이트 (이 스프라이트만 활용하여 접었다 폅니다)

    [Header("연출용 오브젝트 (Fill 방식)")]
    public Image boxImage;                // 스르륵 차오를 박스 Image 컴포넌트
    public List<GameObject> contentElements; // 박스가 다 찬 후 나타날 내부 버튼/텍스트 리스트

    [Header("모션 설정 (속도 및 연출)")]
    public float eyeOpenDuration = 0.45f;    // 눈이 깜빡이며 위아래로 확 떠지는 시간 (속도 조절)
    public float fillDuration = 0.45f;       // 박스가 위에서 아래로 채워지는 시간
    public float eyeCloseDuration = 0.4f;    // 눈이 깜빡이며 닫히는 시간

    private Sequence activeSequence;

    // 패널이 켜지는 순간 자동으로 오픈 모션 실행
    void OnEnable()
    {
        PlayOpenMotion();
    }

    void OnDisable()
    {
        KillSequence();
        SetContentsActive(false);
    }

    // 외부에서 패널을 끌 때 닫히는 모션을 재생하기 위한 함수
    public void ClosePanelWithMotion(System.Action onComplete = null)
    {
        PlayCloseMotion(onComplete);
    }

    private void KillSequence()
    {
        if (activeSequence != null && activeSequence.IsActive())
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        if (eyeImage != null) eyeImage.transform.DOKill();
        if (boxImage != null) boxImage.DOKill();
    }

    // ==========================================
    // 창 오픈 모션 (세로로 접혀있던 눈이 확 떠짐 -> 박스 Fill 채워짐 -> 내부 컨텐츠 등장)
    // ==========================================
    private void PlayOpenMotion()
    {
        KillSequence();
        SetContentsActive(false);

        if (eyeImage == null || boxImage == null) return;

        // 1. 초기 상태 세팅 (뜬 눈 지정, 세로 크기를 0으로 만들어 감긴 눈처럼 압축, 박스 Fill 0)
        if (eyeOpenSprite != null) eyeImage.sprite = eyeOpenSprite;
        eyeImage.gameObject.SetActive(true);
        eyeImage.transform.localScale = new Vector3(1f, 0f, 1f); // 가로는 유지하고 세로만 0으로 접음

        boxImage.type = Image.Type.Filled;
        boxImage.fillAmount = 0f;
        boxImage.gameObject.SetActive(true);

        // 2. 오픈 Sequence 생성
        activeSequence = DOTween.Sequence();

        // Step 1: 눈이 깜빡이며 위아래로 시원하게 떠짐 (OutBack을 주어 살짝 위로 튕겼다 안착)
        activeSequence.Append(eyeImage.transform.DOScaleY(1f, eyeOpenDuration).SetEase(Ease.OutBack));

        // Step 2: 박스가 위에서 아래로 부드럽게 채워짐
        activeSequence.Append(boxImage.DOFillAmount(1f, fillDuration).SetEase(Ease.OutCubic));

        // Step 3: 내부 컨텐츠 등장
        activeSequence.OnComplete(() =>
        {
            SetContentsActive(true);
        });

        activeSequence.SetUpdate(true);
    }

    // ==========================================
    // 창 클로즈 모션 (내부 컨텐츠 숨김 -> 박스 줄어듦 -> 눈이 세로로 접히며 깜빡 감김)
    // ==========================================
    public void PlayCloseMotion(System.Action onComplete = null)
    {
        KillSequence();
        SetContentsActive(false);

        if (eyeImage == null || boxImage == null)
        {
            onComplete?.Invoke();
            return;
        }

        activeSequence = DOTween.Sequence();

        // Step 1: 박스가 아래에서 위로 줄어듦
        activeSequence.Append(boxImage.DOFillAmount(0f, fillDuration * 0.7f).SetEase(Ease.InCubic));

        // Step 2: 눈이 세로로 좁혀지며(ScaleY -> 0) 깜빡 감기는 연출
        activeSequence.Append(eyeImage.transform.DOScaleY(0f, eyeCloseDuration).SetEase(Ease.InBack));

        // Step 3: 완료 콜백
        activeSequence.OnComplete(() =>
        {
            onComplete?.Invoke();
        });

        activeSequence.SetUpdate(true);
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