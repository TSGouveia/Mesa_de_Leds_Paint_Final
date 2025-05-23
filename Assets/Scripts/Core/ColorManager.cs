using UnityEngine;
using UnityEngine.UI;

public class ColorManager : MonoBehaviour
{
    UIManager uiManager;
    private Color selectedColor = Color.black;
    [SerializeField] private Image colorPreview;

    private void Awake()
    {
        uiManager = FindAnyObjectByType<UIManager>();
    }
    void Start()
    {
        SetSelectedColor(selectedColor);
    }

    public void SetSelectedColor(Color color)
    {
        selectedColor = color;
        if (colorPreview != null)
        {
            colorPreview.color = selectedColor;
        }
        uiManager.UpdateColorInputs(selectedColor);
    }

    public Color GetSelectedColor()
    {
        return selectedColor;
    }
}
