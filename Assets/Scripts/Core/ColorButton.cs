using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    private ColorManager colorManager;
    private Image thisImage;
    private Button thisButton;

    private void Awake()
    {
        colorManager = FindAnyObjectByType<ColorManager>();
        thisImage = GetComponent<Image>();
        thisButton = GetComponent<Button>();

        if (thisButton != null)
        {
            thisButton.onClick.AddListener(ChangeColor);
        }
    }

    public void ChangeColor()
    {
        Debug.Log(thisImage.color);
        colorManager.SetSelectedColor(thisImage.color);
    }
}