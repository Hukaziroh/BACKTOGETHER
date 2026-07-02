using UnityEngine;
using TMPro;
using System.Collections;

public class IntervalTrigger : MonoBehaviour
{
    public TMP_Text timerText;
    private Coroutine timerCoroutine;
    private const float MAX_TIME = 5.0f; // 5초

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

            // 초(Seconds)와 소수점 이하 두 자리(Milliseconds 느낌) 계산
            int seconds = Mathf.FloorToInt(timeLeft);
            int milliSeconds = Mathf.FloorToInt((timeLeft - seconds) * 100);

            if (timerText != null)
            {
                // 앞에는 초, 뒤에는 1/100초 단위를 표시 (05:00 형식)
                timerText.text = string.Format("{0:00}:{1:00}", seconds, milliSeconds);

                // 색상 변화 로직
                if (timeLeft > 2f) timerText.color = Color.green;
                else if (timeLeft > 1f) timerText.color = Color.yellow;
                else timerText.color = Color.red;
            }

            if (timeLeft <= 0)
            {
                Debug.Log("이벤트 실행!");
                timeLeft = MAX_TIME; // 5초로 리셋
            }

            yield return null;
        }
    }
}