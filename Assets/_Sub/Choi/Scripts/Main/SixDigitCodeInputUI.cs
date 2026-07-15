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

    // [추가] 6자리 숫자를 합쳐서 반환하는 함수
    public string GetCode()
    {
        string code = "";
        foreach (var slot in slots)
        {
            code += slot.currentValue.ToString();
        }
        return code;
    }

    public void ChangeValue(int index, int delta)
    {
        if (index < 0 || index >= slots.Length) return;
        slots[index].currentValue = (slots[index].currentValue + delta + 10) % 10;
        slots[index].digitText.text = slots[index].currentValue.ToString();
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