using UnityEngine;
using TMPro;
using System.Collections;

public class IntervalTrigger : MonoBehaviour
{
    public TMP_Text timerText;
    private Coroutine timerCoroutine;
    private const float MAX_TIME = 5.0f; // 5초

    // 상태를 저장할 변수 추가
    private bool isForward = true;

    private void Start()
    {
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (timerText != null) timerText.gameObject.SetActive(true);
            timerCoroutine = StartCoroutine(FiveSecondTimer());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        }
    }

    private IEnumerator FiveSecondTimer()
    {
        float timeLeft = MAX_TIME;

        while (true)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0) timeLeft = 0;

            // 초와 밀리초 계산
            int seconds = Mathf.FloorToInt(timeLeft);

            if (timerText != null)
            {
                // 상태에 따른 문자열 결정
                string arrowChar = isForward ? "->" : "<-";

                // UI 텍스트 업데이트 (상태 + 화살표 + 타이머)
                // 줄바꿈(\n)을 사용하여 가독성을 높였습니다.
                timerText.text = $"{arrowChar}";

                // 색상 변화 로직
                if (timeLeft > 3f) timerText.color = Color.green;
                else if (timeLeft > 2f) timerText.color = Color.yellow;
                else timerText.color = Color.red;
            }

            if (timeLeft <= 0)
            {
                Debug.Log("이벤트 실행!");

                // 5초가 지났으므로 상태 반전
                isForward = !isForward;

                // 타이머 리셋
                timeLeft = MAX_TIME;
            }

            yield return null;
        }
    }
}