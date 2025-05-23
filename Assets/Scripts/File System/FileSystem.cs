using SFB; // Namespace para Standalone File Browser
using System.IO;
using UnityEngine;

public class FileSystem : MonoBehaviour
{
    private DrawingManager drawingManager;
    private SaveWarning saveWarning;
    private Color[,] lastSavedCanvas = new Color[32, 18];

    private void Awake()
    {
        drawingManager = FindAnyObjectByType<DrawingManager>();
        saveWarning = FindAnyObjectByType<SaveWarning>();
    }
    private void Start()
    {
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 18; y++)
            {
                lastSavedCanvas[x, y] = Color.white;
            }
        }
    }
    public void OpenImage()
    {
        if (drawingManager.AreStatesDifferent(drawingManager.GetCanvas(), lastSavedCanvas))
        {
            saveWarning.ShowUnsavedChangesWarning(SaveAndOpen, OpenFileExplorer);
        }
        else
        {
            OpenFileExplorer();
        }
    }
    private void OpenFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg") };
        // Usando o StandaloneFileBrowser para abrir a janela de seleção de arquivos
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Choose an image", "", extensions, false);

        if (paths.Length > 0)
        {
            string path = paths[0]; // O caminho do arquivo selecionado
            LoadImage(path); // Carregar a imagem
        }
        else
        {
            Debug.Log("Nenhum arquivo selecionado.");
        }
    }
    private void LoadImage(string path)
    {
        // Carregar a imagem do arquivo
        Texture2D texture = new Texture2D(2, 2); // Inicializar uma textura
        byte[] fileData = File.ReadAllBytes(path);
        texture.LoadImage(fileData); // Carregar a imagem no formato de textura

        // Redimensionar a imagem para 32x18
        Texture2D resizedTexture = ResizeTexture(texture, 32, 18);

        // Converter a imagem para um array de cores
        Color[] pixels = resizedTexture.GetPixels();
        Color[,] colorArray = new Color[32, 18];

        // Preencher o array de cores 2D
        for (int y = 0; y < 18; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                colorArray[x, y] = pixels[y * 32 + x]; // Atribuir as cores corretamente
            }
        }

        // Chamar o método DrawCanvas no outro script e passar o array de cores
        drawingManager.DrawCanvas(colorArray);
        lastSavedCanvas = drawingManager.GetCanvas();
    }

    // Função para redimensionar a textura
    private Texture2D ResizeTexture(Texture2D originalTexture, int targetWidth, int targetHeight)
    {
        Texture2D newTexture = new Texture2D(targetWidth, targetHeight);
        float incX = 1.0f / targetWidth;
        float incY = 1.0f / targetHeight;

        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                newTexture.SetPixel(x, y, originalTexture.GetPixelBilinear(x * incX, y * incY));
            }
        }

        newTexture.Apply();
        return newTexture;
    }
    public void SaveImage()
    {
        int width = 32;
        int height = 18;
        Color[,] pixelColors = drawingManager.GetCanvas();

        // Abrir o explorador de arquivos para o usuário escolher o local e o nome do arquivo
        string path = StandaloneFileBrowser.SaveFilePanel("Save image", "", "image", "jpg");

        // Verificar se o usuário selecionou um caminho válido
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Nenhum caminho selecionado.");
            return;
        }

        // Criar uma nova textura com as dimensões fornecidas
        Texture2D texture = new Texture2D(width, height);

        Color[] flatPixelColors = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flatPixelColors[y * width + x] = pixelColors[x, y]; // Preenchendo o array unidimensional
            }
        }

        // Atribuir os valores do array de cores à textura
        texture.SetPixels(flatPixelColors);

        // Aplicar as mudanças na textura
        texture.Apply();

        // Converter a textura para um formato de imagem (PNG ou JPG)
        byte[] imageBytes = texture.EncodeToPNG();  // Pode usar EncodeToJPG() se preferir JPG

        // Salvar a imagem no caminho especificado
        File.WriteAllBytes(path, imageBytes);

        lastSavedCanvas = pixelColors;
        Debug.Log($"Imagem salva em: {path}");
    }
    public void NewImage()
    {
        if (drawingManager.AreStatesDifferent(drawingManager.GetCanvas(), lastSavedCanvas))
        {
            saveWarning.ShowUnsavedChangesWarning(SaveAndClearCanvas, drawingManager.ClearCanvas);
        }
        else
        {
            drawingManager.ClearCanvas();
        }
    }
    private void SaveAndClearCanvas()
    {
        SaveImage();
        drawingManager.ClearCanvas();
    }
    private void SaveAndOpen()
    {
        SaveImage();
        OpenImage();
    }
    private void SaveAndQuit()
    {
        SaveImage();
        Application.Quit();
    }
    public void Quit()
    {
        if (drawingManager.AreStatesDifferent(drawingManager.GetCanvas(), lastSavedCanvas))
        {
            saveWarning.ShowUnsavedChangesWarning(SaveAndQuit, Application.Quit);
        }
        else
        {
            Application.Quit();
        }
    }
}
