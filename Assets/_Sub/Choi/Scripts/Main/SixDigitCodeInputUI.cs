using UnityEngine;
using TMPro;

public class SixDigitCodeInputUI : MonoBehaviour
{
    [System.Serializable]
    public class DigitSlotData
    {
        public TMP_Text digitText;
        public int currentValue = 0;
    }

    public DigitSlotData[] slots = new DigitSlotData[6];

    private void OnEnable()
    {
        // 패널/오브젝트가 켜질 때마다 자동 초기화
        ResetCode();
    }

    // ★ 6자리 자릿수 전체를 0으로 초기화하는 함수
    public void ResetCode()
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.currentValue = 0;
                if (slot.digitText != null)
                {
                    slot.digitText.text = "0";
                }
            }
        }
    }

    // 6자리 숫자를 합쳐서 반환하는 함수
    public string GetCode()
    {
        string code = "";
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                code += slot.currentValue.ToString();
            }
        }
        return code;
    }

    public void ChangeValue(int index, int delta)
    {
        if (index < 0 || index >= slots.Length) return;
        if (slots[index] == null) return;

        slots[index].currentValue = (slots[index].currentValue + delta + 10) % 10;

        if (slots[index].digitText != null)
        {
            slots[index].digitText.text = slots[index].currentValue.ToString();
        }
    }

    public void Up(int index)
    {
        ChangeValue(index, 1);
    }

    public void Down(int index)
    {
        ChangeValue(index, -1);
    }
}