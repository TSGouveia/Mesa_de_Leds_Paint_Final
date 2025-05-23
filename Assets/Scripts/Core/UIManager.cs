using TMPro; // Para usar o TextMeshPro
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private ToolManager toolManager;
    public Button brushButton; // Botão da ferramenta Brush
    public Button eraserButton; // Botão da ferramenta Eraser
    public Button bucketButton; // Botão da ferramenta Bucket
    public Button eyedropperButton; // Botão da ferramenta Eyedropper

    private Image brushButtonImage; // Componente Image do botão Brush
    private Image eraserButtonImage; // Componente Image do botão Eraser
    private Image bucketButtonImage; // Componente Image do botão Bucket
    private Image eyedropperButtonImage; // Componente Image do botão Eyedropper

    public Color selectedColor = Color.green; // Cor do botão selecionado
    public Color defaultColor = Color.white; // Cor padrão do botão

    // TextMeshPro para exibir os valores de RGB e HEX
    public TMP_InputField redInputField; // Para editar o valor de R
    public TMP_InputField greenInputField; // Para editar o valor de G
    public TMP_InputField blueInputField; // Para editar o valor de B
    public TMP_InputField hexInputField; // Para editar o valor de HEX

    [SerializeField] private string helpURL = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    ColorManager colorManager;
    void Awake()
    {
        toolManager = FindAnyObjectByType<ToolManager>();
        colorManager = FindAnyObjectByType<ColorManager>();

        // Obter as referências aos componentes Image uma vez
        brushButtonImage = brushButton.GetComponent<Image>();
        eraserButtonImage = eraserButton.GetComponent<Image>();
        bucketButtonImage = bucketButton.GetComponent<Image>();
        eyedropperButtonImage = eyedropperButton.GetComponent<Image>();
    }
    private void Start()
    {
        // Set default selection to Brush
        SelectBrush();
    }

    // Método para selecionar a ferramenta Brush
    public void SelectBrush()
    {
        toolManager.SelectTool(ToolManager.ToolType.Brush);
        UpdateButtonColors(brushButtonImage);
    }

    // Método para selecionar a ferramenta Eraser
    public void SelectEraser()
    {
        toolManager.SelectTool(ToolManager.ToolType.Eraser);
        UpdateButtonColors(eraserButtonImage);
    }

    // Método para selecionar a ferramenta Bucket
    public void SelectBucket()
    {
        toolManager.SelectTool(ToolManager.ToolType.Bucket);
        UpdateButtonColors(bucketButtonImage);
    }

    // Método para selecionar a ferramenta Eyedropper
    public void SelectEyedropper()
    {
        toolManager.SelectTool(ToolManager.ToolType.Eyedropper);
        UpdateButtonColors(eyedropperButtonImage);
    }

    // Método para atualizar as cores dos botões
    private void UpdateButtonColors(Image selectedButtonImage)
    {
        // Altera a cor de todos os botões para a cor padrão
        brushButtonImage.color = defaultColor;
        eraserButtonImage.color = defaultColor;
        bucketButtonImage.color = defaultColor;
        eyedropperButtonImage.color = defaultColor;

        // Altera a cor do botão selecionado
        selectedButtonImage.color = selectedColor;
    }

    // Método para atualizar os valores nos campos de Input
    public void UpdateColorInputs(Color color)
    {
        // Atualiza os campos de RGB
        redInputField.text = Mathf.FloorToInt(color.r * 255).ToString();
        greenInputField.text = Mathf.FloorToInt(color.g * 255).ToString();
        blueInputField.text = Mathf.FloorToInt(color.b * 255).ToString();

        // Atualiza o campo HEX
        hexInputField.text = ColorUtility.ToHtmlStringRGB(color);
    }

    // Método para converter valores de RGB de volta para a cor
    public void OnRGBValueChanged()
    {
        int r, g, b;

        // Usando TryParse para garantir que os valores inseridos sejam válidos
        bool isRValid = int.TryParse(redInputField.text, out r);
        bool isGValid = int.TryParse(greenInputField.text, out g);
        bool isBValid = int.TryParse(blueInputField.text, out b);

        if (isRValid && isGValid && isBValid)
        {
            // Garante que os valores de RGB fiquem dentro do intervalo de 0 a 255
            r = Mathf.Clamp(r, 0, 255);
            g = Mathf.Clamp(g, 0, 255);
            b = Mathf.Clamp(b, 0, 255);

            Color newBrushColor = new Color(r / 255f, g / 255f, b / 255f);
            UpdateColor(newBrushColor);
        }
    }

    // Método para converter HEX para a cor
    public void OnHexValueChanged()
    {
        string hex = hexInputField.text;

        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        if (hex.Length == 6)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color newColor))
            {
                Color newBrushColor = newColor;
                UpdateColor(newBrushColor);
            }

        }
    }
    // Atualiza os botões com base na cor selecionada
    private void UpdateColor(Color color)
    {
        colorManager.SetSelectedColor(color);
    }
    public void OpenHelp()
    {
        Application.OpenURL(helpURL);
    }
}
