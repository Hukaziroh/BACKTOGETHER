using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EmojiDisplay : MonoBehaviour
{
    public Sprite[] emojiSprites; // 여기에 이모지 이미지들을 드래그해서 넣으세요
    private Image displayImage;

    void Awake()
    {
        displayImage = GetComponent<Image>();
    }

    public void ShowEmoji(int index)
    {
        if (index >= 0 && index < emojiSprites.Length)
        {
            displayImage.sprite = emojiSprites[index];
            StartCoroutine(ShowAndHide());
        }
    }

    IEnumerator ShowAndHide()
    {
        displayImage.enabled = true; // 이미지 켜기
        yield return new WaitForSeconds(2f); // 2초 대기
        displayImage.enabled = false; // 이미지 끄기
    }
}