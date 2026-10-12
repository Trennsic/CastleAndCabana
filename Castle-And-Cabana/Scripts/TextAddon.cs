using UnityEngine;
using TMPro;

public class TextAddon : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Text1;
    [SerializeField] private TextMeshProUGUI Text2;
    [SerializeField] private TextMeshProUGUI Text3;
    [SerializeField] private TextMeshProUGUI Text4;
    [SerializeField] private TextMeshProUGUI Text5;

    public TextMeshProUGUI GetText1()
    {
        return ValidateText(Text1);
    }

    public TextMeshProUGUI GetText2()
    {
        return ValidateText(Text2);
    }

    public TextMeshProUGUI GetText3()
    {
        return ValidateText(Text3);
    }

    public TextMeshProUGUI GetText4()
    {
        return ValidateText(Text4);
    }

    public TextMeshProUGUI GetText5()
    {
        return ValidateText(Text5);
    }

    public void UpdateText(string newText)
    {
        if (Text1 != null)
        {
            Text1.text = newText;
        }
        else
        {
            Debug.LogWarning("Text1 is not assigned in the inspector.");
        }
    }

    private TextMeshProUGUI ValidateText(TextMeshProUGUI text)
    {
        if (text == null)
        {
            Debug.LogWarning("Text object is not assigned in the inspector.");
        }
        return text;
    }
}
