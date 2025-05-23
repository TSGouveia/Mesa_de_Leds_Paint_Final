using TMPro; // Para usar o TextMeshPro
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private ToolManager toolManager;
    public Button brushButton; // Bot�o da ferramenta Brush
    public Button eraserButton; // Bot�o da ferramenta Eraser
    public Button bucketButton; // Bot�o da ferramenta Bucket
    public Button eyedropperButton; // Bot�o da ferramenta Eyedropper

    private Image brushButtonImage; // Componente Image do bot�o Brush
    private Image eraserButtonImage; // Componente Image do bot�o Eraser
    private Image bucketButtonImage; // Componente Image do bot�o Bucket
    private Image eyedropperButtonImage; // Componente Image do bot�o Eyedropper

    public Color selectedColor = Color.green; // Cor do bot�o selecionado
    public Color defaultColor = Color.white; // Cor padr�o do bot�o

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

        // Obter as refer�ncias aos componentes Image uma vez
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

    // M�todo para selecionar a ferramenta Brush
    public void SelectBrush()
    {
        toolManager.SelectTool(ToolManager.ToolType.Brush);
        UpdateButtonColors(brushButtonImage);
    }

    // M�todo para selecionar a ferramenta Eraser
    public void SelectEraser()
    {
        toolManager.SelectTool(ToolManager.ToolType.Eraser);
        UpdateButtonColors(eraserButtonImage);
    }

    // M�todo para selecionar a ferramenta Bucket
    public void SelectBucket()
    {
        toolManager.SelectTool(ToolManager.ToolType.Bucket);
        UpdateButtonColors(bucketButtonImage);
    }

    // M�todo para selecionar a ferramenta Eyedropper
    public void SelectEyedropper()
    {
        toolManager.SelectTool(ToolManager.ToolType.Eyedropper);
        UpdateButtonColors(eyedropperButtonImage);
    }

    // M�todo para atualizar as cores dos bot�es
    private void UpdateButtonColors(Image selectedButtonImage)
    {
        // Altera a cor de todos os bot�es para a cor padr�o
        brushButtonImage.color = defaultColor;
        eraserButtonImage.color = defaultColor;
        bucketButtonImage.color = defaultColor;
        eyedropperButtonImage.color = defaultColor;

        // Altera a cor do bot�o selecionado
        selectedButtonImage.color = selectedColor;
    }

    // M�todo para atualizar os valores nos campos de Input
    public void UpdateColorInputs(Color color)
    {
        // Atualiza os campos de RGB
        redInputField.text = Mathf.FloorToInt(color.r * 255).ToString();
        greenInputField.text = Mathf.FloorToInt(color.g * 255).ToString();
        blueInputField.text = Mathf.FloorToInt(color.b * 255).ToString();

        // Atualiza o campo HEX
        hexInputField.text = ColorUtility.ToHtmlStringRGB(color);
    }

    // M�todo para converter valores de RGB de volta para a cor
    public void OnRGBValueChanged()
    {
        int r, g, b;

        // Usando TryParse para garantir que os valores inseridos sejam v�lidos
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

    // M�todo para converter HEX para a cor
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
    // Atualiza os bot�es com base na cor selecionada
    private void UpdateColor(Color color)
    {
        colorManager.SetSelectedColor(color);
    }
    public void OpenHelp()
    {
        Application.OpenURL(helpURL);
    }
}
