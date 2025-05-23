using SFB; // Namespace para Standalone File Browser
using System.IO;
using UnityEngine;

public class FileSystem : MonoBehaviour
{
    private DrawingManager drawingManager;
    private SaveWarning saveWarning; // Assume que este script existe
    private Color[,] lastSavedCanvas;

    private const int GRID_WIDTH = 32;
    private const int GRID_HEIGHT = 18;

    private void Awake()
    {
        drawingManager = FindObjectOfType<DrawingManager>();
        saveWarning = FindObjectOfType<SaveWarning>();

        if (drawingManager == null) Debug.LogError("FS: DrawingManager não encontrado", this);
        if (saveWarning == null) Debug.LogWarning("FS: SaveWarning não encontrado", this);
    }

    private void Start()
    {
        // Inicializa lastSavedCanvas com um estado branco ou o estado atual do canvas
        if (drawingManager != null && drawingManager.enabled) // Verifica se drawingManager está pronto
        {
            lastSavedCanvas = drawingManager.GetCanvas();
        }
        else
        {
            lastSavedCanvas = new Color[GRID_WIDTH, GRID_HEIGHT];
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    lastSavedCanvas[x, y] = Color.white;
                }
            }
            // Tenta obter o canvas novamente se o drawingManager ficar pronto mais tarde
            // Invoke(nameof(InitializeLastSavedCanvas), 0.1f); // Pequeno delay
        }
    }
    // private void InitializeLastSavedCanvas() { if(drawingManager != null) lastSavedCanvas = drawingManager.GetCanvas(); }


    public void OpenImage()
    {
        if (drawingManager == null || !drawingManager.enabled) return;
        if (saveWarning != null && drawingManager.AreStatesDifferent(drawingManager.GetCanvas(), lastSavedCanvas))
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
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Image", "", extensions, false);
        if (paths.Length > 0)
        {
            LoadImageFromFile(paths[0]);
        }
    }

    private void LoadImageFromFile(string path) // Renomeado para clareza
    {
        if (drawingManager == null || !drawingManager.enabled) return;
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2); // Tamanho placeholder
        if (!texture.LoadImage(fileData))
        {
            Debug.LogError($"FS: Falha ao carregar imagem de {path}");
            return;
        }

        Texture2D resizedTexture = ResizeTexture(texture, GRID_WIDTH, GRID_HEIGHT);
        if (resizedTexture == null)
        {
            Debug.LogError($"FS: Falha ao redimensionar textura de {path}");
            Destroy(texture); // Limpa textura original
            return;
        }

        Color[] pixels = resizedTexture.GetPixels();
        Color[,] colorArray = new Color[GRID_WIDTH, GRID_HEIGHT];
        for (int y = 0; y < GRID_HEIGHT; y++)
        {
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                colorArray[x, y] = pixels[y * GRID_WIDTH + x];
            }
        }
        Destroy(texture); // Limpa textura original
        Destroy(resizedTexture); // Limpa textura redimensionada

        drawingManager.LoadCanvasState(colorArray); // USA O MÉTODO CORRETO
        lastSavedCanvas = drawingManager.GetCanvas();
    }

    private Texture2D ResizeTexture(Texture2D originalTexture, int targetWidth, int targetHeight)
    {
        // Usar RenderTexture para melhor qualidade e performance de redimensionamento
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Point; // Para pixel art, Point é melhor
        RenderTexture.active = rt;
        Graphics.Blit(originalTexture, rt);
        Texture2D newTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        newTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        newTexture.Apply(false); // false para não gerar mipmaps
        RenderTexture.active = null; // Libera RenderTexture ativa
        RenderTexture.ReleaseTemporary(rt);
        return newTexture;
    }

    public void SaveImage()
    {
        if (drawingManager == null || !drawingManager.enabled) return;
        Color[,] pixelColors = drawingManager.GetCanvas();
        if (pixelColors == null) { Debug.LogError("FS: Não foi possível obter o canvas para salvar."); return; }

        string path = StandaloneFileBrowser.SaveFilePanel("Save Image", "", "pixel_art_image", "png");
        if (string.IsNullOrEmpty(path)) return;

        Texture2D texture = new Texture2D(GRID_WIDTH, GRID_HEIGHT, TextureFormat.RGBA32, false);
        Color[] flatPixelColors = new Color[GRID_WIDTH * GRID_HEIGHT];
        for (int y = 0; y < GRID_HEIGHT; y++)
        {
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                flatPixelColors[y * GRID_WIDTH + x] = pixelColors[x, y];
            }
        }
        texture.SetPixels(flatPixelColors);
        texture.Apply(false);

        byte[] imageBytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, imageBytes);
        Destroy(texture); // Libera memória

        lastSavedCanvas = drawingManager.GetCanvas(); // Atualiza o estado salvo
        Debug.Log($"Imagem salva em: {path}");
    }

    public void NewImage()
    {
        if (drawingManager == null || !drawingManager.enabled) return;
        if (saveWarning != null && drawingManager.AreStatesDifferent(drawingManager.GetCanvas(), lastSavedCanvas))
        {
            saveWarning.ShowUnsavedChangesWarning(SaveAndClearCanvas, ClearCanvasAndUpdateLastSaved);
        }
        else
        {
            ClearCanvasAndUpdateLastSaved();
        }
    }

    private void ClearCanvasAndUpdateLastSaved()
    {
        if (drawingManager == null || !drawingManager.enabled) return;
        drawingManager.ClearCanvas();
        lastSavedCanvas = drawingManager.GetCanvas();
    }

    private void SaveAndClearCanvas()
    {
        SaveImage();
        ClearCanvasAndUpdateLastSaved();
    }

    private void SaveAndOpen()
    {
        SaveImage();
        OpenFileExplorer(); // Chama OpenFileExplorer diretamente
    }
    private void SaveAndQuit()
    {
        SaveImage();
        Application.Quit();
    }

    public void RequestQuitApplication() // Renomeado
    {
        if (drawingManager == null || !drawingManager.enabled) { Application.Quit(); return; }
        if (saveWarning != null && drawingManager.AreStatesDifferent(drawingManager.GetCanvas(), lastSavedCanvas))
        {
            saveWarning.ShowUnsavedChangesWarning(SaveAndQuit, ForceQuitApplication);
        }
        else
        {
            ForceQuitApplication();
        }
    }
    private void ForceQuitApplication() { Application.Quit(); }
}