using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingManager : MonoBehaviour
{
    private CanvasManager canvasManager;
    private PixelGrid pixelGrid;

    private Image[,] imageArray;
    private ToolManager.ToolType currentTool;

    private ColorManager colorManager;
    private UndoRedoManager undoRedoManager;

    [SerializeField] private GameObject[] warnings;
    Color[,] initialState;

    void Awake()
    {
        canvasManager = FindAnyObjectByType<CanvasManager>();
        pixelGrid = FindAnyObjectByType<PixelGrid>();
        colorManager = FindAnyObjectByType<ColorManager>();
        undoRedoManager = FindAnyObjectByType<UndoRedoManager>();
    }
    private void Start()
    {
        imageArray = canvasManager.GetImageArray();
        SetCurrentTool(ToolManager.ToolType.Brush);
    }

    public void SetCurrentTool(ToolManager.ToolType tool)
    {
        currentTool = tool;
    }

    void Update()
    {
        foreach (var warning in warnings)
        {
            if (warning.activeSelf)
            {
                return;
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            initialState = undoRedoManager.CaptureCurrentState();
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int pixelPosition = pixelGrid.WorldToPixel(worldPosition);

            switch (currentTool)
            {
                case ToolManager.ToolType.Brush:
                    if (colorManager.GetSelectedColor() != GetPixelColor(pixelPosition))
                    {
                        DrawPixel(pixelPosition, colorManager.GetSelectedColor());
                    }
                    break;
                case ToolManager.ToolType.Eraser:
                    if (Color.white != GetPixelColor(pixelPosition))
                    {
                        DrawPixel(pixelPosition, Color.white);
                    }
                    break;
                case ToolManager.ToolType.Bucket:
                    if (colorManager.GetSelectedColor() != GetPixelColor(pixelPosition))
                    {
                        Color targetColor = GetPixelColor(pixelPosition);
                        BucketFill(pixelPosition, targetColor, colorManager.GetSelectedColor());
                    }
                    break;
                case ToolManager.ToolType.Eyedropper:
                    Color newColor = GetPixelColor(pixelPosition);
                    if (newColor != Color.clear)
                        colorManager.SetSelectedColor(newColor);
                    break;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            Color[,] currentState = undoRedoManager.CaptureCurrentState();
            if (AreStatesDifferent(initialState, currentState))
                undoRedoManager.SaveCapturedState(initialState);
        }
    }

    public void DrawPixel(Vector2Int pos, Color color)
    {
        if (pos.x >= 0 && pos.x < 32 && pos.y >= 0 && pos.y < 18)
        {
            if (imageArray[pos.x, pos.y].color == color)
                return;
            imageArray[pos.x, pos.y].color = color;
        }
    }
    public void DrawCanvas(Color[,] newColors)
    {
        initialState = undoRedoManager.CaptureCurrentState();
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 18; y++)
            {
                imageArray[x, y].color = newColors[x, y];
            }
        }
        Color[,] currentState = undoRedoManager.CaptureCurrentState();
        if (AreStatesDifferent(initialState, currentState))
            undoRedoManager.SaveCapturedState(initialState);
    }


    public Color GetPixelColor(Vector2Int pos)
    {
        if (pos.x >= 0 && pos.x < 32 && pos.y >= 0 && pos.y < 18)
        {
            return imageArray[pos.x, pos.y].color;
        }
        return Color.clear;
    }

    public Color[,] GetCanvas()
    {
        return undoRedoManager.CaptureCurrentState();
    }

    public void ClearCanvas()
    {
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 18; y++)
            {
                imageArray[x, y].color = Color.white;
            }
        }
    }

    public void BucketFill(Vector2Int startPos, Color targetColor, Color fillColor)
    {
        if (targetColor == fillColor)
            return;

        Queue<Vector2Int> pixels = new Queue<Vector2Int>();
        pixels.Enqueue(startPos);

        while (pixels.Count > 0)
        {
            Vector2Int pos = pixels.Dequeue();

            if (pos.x < 0 || pos.x >= 32 || pos.y < 0 || pos.y >= 18)
                continue;

            if (imageArray[pos.x, pos.y].color != targetColor)
                continue;

            DrawPixel(pos, fillColor);

            pixels.Enqueue(new Vector2Int(pos.x + 1, pos.y));
            pixels.Enqueue(new Vector2Int(pos.x - 1, pos.y));
            pixels.Enqueue(new Vector2Int(pos.x, pos.y + 1));
            pixels.Enqueue(new Vector2Int(pos.x, pos.y - 1));
        }
    }
    public bool AreStatesDifferent(Color[,] state1, Color[,] state2)
    {
        if (state1.GetLength(0) != state2.GetLength(0) || state1.GetLength(1) != state2.GetLength(1))
            return true; // Diferente se as dimensões forem diferentes

        for (int i = 0; i < state1.GetLength(0); i++)
        {
            for (int j = 0; j < state1.GetLength(1); j++)
            {
                if (state1[i, j] != state2[i, j])
                    return true; // Diferente se algum valor for diferente
            }
        }
        return false; // São iguais
    }
}
