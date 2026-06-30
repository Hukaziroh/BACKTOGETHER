using UnityEngine;
using UnityEngine.UI;

public class EmojiButton : MonoBehaviour
{
    public int emojiIndex; // 이 버튼이 표시할 이모지의 인덱스 (0, 1, 2...)
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        // 씬에서 로컬 플레이어를 찾습니다.
        foreach (var player in FindObjectsOfType<PlayerEmoji>())
        {
            if (player.isLocalPlayer)
            {
                player.SelectEmoji(emojiIndex);
                break;
            }
        }
    }
}