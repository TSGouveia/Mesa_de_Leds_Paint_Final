using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // Para Raycasting UI
using UnityEngine.UI;

public class DrawingManager : MonoBehaviour
{
    private CanvasManager canvasManager;
    private PixelGrid pixelGrid;
    private ColorManager colorManager;
    private UndoRedoManager undoRedoManager;

    private Image[,] imageArray;
    private ToolManager.ToolType currentTool;

    [Header("UI Interaction")]
    [SerializeField] private GameObject[] warnings; // Array de pain�is de aviso
    [SerializeField] private LayerMask uiLayerMask; // Layer dos bot�es da UI
    [SerializeField] private LayerMask drawingSurfaceLayerMask; // Layer dos pixels do canvas

    private Color[,] stateBeforeAction; // Para capturar estado antes de uma a��o (MouseDown)

    private int gridWidth;
    private int gridHeight;

    void Awake()
    {
        canvasManager = FindObjectOfType<CanvasManager>();
        pixelGrid = FindObjectOfType<PixelGrid>();
        colorManager = FindObjectOfType<ColorManager>();
        undoRedoManager = FindObjectOfType<UndoRedoManager>();

        if (canvasManager == null) Debug.LogError("DM: CanvasManager n�o encontrado", this);
        if (pixelGrid == null) Debug.LogError("DM: PixelGrid n�o encontrado", this);
        if (colorManager == null) Debug.LogError("DM: ColorManager n�o encontrado", this);
        if (undoRedoManager == null) Debug.LogError("DM: UndoRedoManager n�o encontrado", this);

        if (uiLayerMask.value == 0) Debug.LogWarning("DM: Ui Layer Mask n�o atribu�da.", this);
        if (drawingSurfaceLayerMask.value == 0) Debug.LogWarning("DM: Drawing Surface Layer Mask n�o atribu�da.", this);
    }

    private void Start()
    {
        if (canvasManager != null)
        {
            imageArray = canvasManager.GetImageArray();
            gridWidth = canvasManager.GetWidth();
            gridHeight = canvasManager.GetHeight();
        }
        if (imageArray == null || undoRedoManager == null)
        {
            Debug.LogError("DM: imageArray ou UndoRedoManager nulo no Start.", this);
            enabled = false; return;
        }
        SetCurrentTool(ToolManager.ToolType.Brush);
    }

    public void SetCurrentTool(ToolManager.ToolType tool)
    {
        currentTool = tool;
    }

    void Update()
    {
        if (!enabled) return;

        // Verifica se algum painel de aviso est� ativo
        foreach (var warning in warnings)
        {
            if (warning != null && warning.activeSelf) return;
        }

        bool isPointerOverUI = false;
        bool isPointerOverDrawingSurface = false;
        Vector2Int currentPixelCoords = Vector2Int.one * -1;

        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            int layerHit = result.gameObject.layer;
            if (((1 << layerHit) & uiLayerMask.value) != 0) { isPointerOverUI = true; break; }
            if (((1 << layerHit) & drawingSurfaceLayerMask.value) != 0) { isPointerOverDrawingSurface = true; }
        }

        if (isPointerOverDrawingSurface && !isPointerOverUI)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currentPixelCoords = pixelGrid.WorldToPixel(worldPosition);
        }

        // L�gica de Input do Rato
        if (Input.GetMouseButtonDown(0))
        {
            if (isPointerOverDrawingSurface && !isPointerOverUI && IsValidPixel(currentPixelCoords))
            {
                stateBeforeAction = undoRedoManager.CaptureCurrentState(); // Captura estado ANTES da modifica��o

                // Aplica a ferramenta no primeiro clique
                ApplyTool(currentPixelCoords, true); // true para isMouseDown
            }
            else
            {
                stateBeforeAction = null; // Clique fora ou na UI, n�o inicia a��o de undo
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (stateBeforeAction != null && isPointerOverDrawingSurface && !isPointerOverUI && IsValidPixel(currentPixelCoords))
            {
                // Aplica a ferramenta durante o arraste (s� para Brush/Eraser)
                ApplyTool(currentPixelCoords, false); // false para isMouseDown
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (stateBeforeAction != null) // Se uma a��o foi iniciada
            {
                // Verifica se o estado mudou e, se sim, regista para undo
                Color[,] currentState = undoRedoManager.CaptureCurrentState();
                if (AreStatesDifferent(stateBeforeAction, currentState))
                {
                    undoRedoManager.RecordStateForUndo(stateBeforeAction);
                }
                stateBeforeAction = null; // Reseta para a pr�xima a��o
            }
        }
    }

    private void ApplyTool(Vector2Int pixelPosition, bool isMouseDown)
    {
        // Eyedropper e Bucket s�o geralmente a��es de clique �nico (MouseDown)
        // Brush e Eraser funcionam em MouseDown e MouseDrag

        switch (currentTool)
        {
            case ToolManager.ToolType.Brush:
                DrawPixelWithLogic(pixelPosition, colorManager.GetSelectedColor());
                break;
            case ToolManager.ToolType.Eraser:
                DrawPixelWithLogic(pixelPosition, Color.white);
                break;
            case ToolManager.ToolType.Bucket:
                if (isMouseDown) // Bucket s� no clique inicial
                {
                    Color targetColor = GetPixelColor(pixelPosition);
                    Color fillColor = colorManager.GetSelectedColor();
                    if (fillColor != targetColor)
                    {
                        BucketFill(pixelPosition, targetColor, fillColor);
                        // O Undo/Redo para o bucket � tratado pelo MouseUp ap�s capturar stateBeforeAction
                    }
                }
                break;
            case ToolManager.ToolType.Eyedropper:
                if (isMouseDown) // Eyedropper s� no clique inicial
                {
                    Color newColor = GetPixelColor(pixelPosition);
                    if (newColor.a > 0) // Considera cor transparente como "n�o selecion�vel"
                        colorManager.SetSelectedColor(newColor);
                    // Eyedropper n�o modifica o canvas, ent�o o stateBeforeAction n�o ser� usado para undo.
                    // O MouseUp n�o vai registar um undo se AreStatesDifferent for falso.
                }
                break;
        }
    }

    /// <summary>
    /// Desenha um pixel. Esta � a fun��o que as ferramentas chamam.
    /// Ela n�o lida diretamente com undo; isso � feito no Update (MouseDown/MouseUp).
    /// </summary>
    private void DrawPixelWithLogic(Vector2Int pos, Color color)
    {
        if (!IsValidPixel(pos) || imageArray[pos.x, pos.y] == null) return;
        if (imageArray[pos.x, pos.y].color == color) return;
        imageArray[pos.x, pos.y].color = color;
    }

    /// <summary>
    /// Desenha um pixel diretamente. Usado pelo UndoRedoManager.RestoreState para
    /// evitar acionar a l�gica de undo novamente.
    /// </summary>
    public void DrawPixelWithoutUndo(Vector2Int pos, Color color)
    {
        if (!IsValidPixel(pos) || imageArray[pos.x, pos.y] == null) return;
        // Aqui n�o verificamos se a cor � a mesma, pois a restaura��o deve sempre aplicar.
        imageArray[pos.x, pos.y].color = color;
    }

    public Color GetPixelColor(Vector2Int pos)
    {
        if (IsValidPixel(pos) && imageArray[pos.x, pos.y] != null)
        {
            return imageArray[pos.x, pos.y].color;
        }
        return Color.clear; // Retorna transparente se fora dos limites ou pixel nulo
    }

    /// <summary>
    /// Usado para carregar um canvas inteiro (ex: de um ficheiro).
    /// Esta a��o DEVE ser registada para Undo.
    /// </summary>
    public void LoadCanvasState(Color[,] newColors) // Renomeado de DrawCanvas
    {
        if (newColors == null || newColors.GetLength(0) != gridWidth || newColors.GetLength(1) != gridHeight)
        {
            Debug.LogError("LoadCanvasState: Estado inv�lido ou dimens�es n�o correspondem.", this);
            return;
        }

        Color[,] stateBeforeLoad = undoRedoManager.CaptureCurrentState();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Usa DrawPixelWithoutUndo para n�o interferir com a l�gica normal de undo do Update.
                // A a��o de "Load" em si � o que ser� guardado para undo.
                DrawPixelWithoutUndo(new Vector2Int(x, y), newColors[x, y]);
            }
        }
        // Regista a a��o de carregar o canvas
        Color[,] stateAfterLoad = undoRedoManager.CaptureCurrentState();
        if (AreStatesDifferent(stateBeforeLoad, stateAfterLoad))
        {
            undoRedoManager.RecordStateForUndo(stateBeforeLoad);
        }
        // Opcional: limpar stacks se carregar um novo ficheiro invalida o hist�rico anterior
        // undoRedoManager.ClearStacks();
    }

    public Color[,] GetCanvas()
    {
        if (undoRedoManager == null) // Fallback se UndoRedoManager n�o estiver pronto
        {
            Debug.LogWarning("DM: GetCanvas chamado, mas UndoRedoManager � nulo. Retornando estado manual.", this);
            Color[,] fallbackState = new Color[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++) for (int y = 0; y < gridHeight; y++)
                {
                    if (imageArray != null && x < imageArray.GetLength(0) && y < imageArray.GetLength(1) && imageArray[x, y] != null)
                        fallbackState[x, y] = imageArray[x, y].color;
                    else
                        fallbackState[x, y] = Color.magenta; // Cor de erro
                }
            return fallbackState;
        }
        return undoRedoManager.CaptureCurrentState();
    }

    public void ClearCanvas()
    {
        Color[,] stateBeforeClear = undoRedoManager.CaptureCurrentState();
        bool changed = false;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (imageArray[x, y] != null && imageArray[x, y].color != Color.white)
                {
                    DrawPixelWithoutUndo(new Vector2Int(x, y), Color.white);
                    changed = true;
                }
                else if (imageArray[x, y] == null) // Seguran�a
                {
                    DrawPixelWithoutUndo(new Vector2Int(x, y), Color.white);
                    changed = true;
                }
            }
        }
        if (changed)
        {
            undoRedoManager.RecordStateForUndo(stateBeforeClear);
        }
    }

    private void BucketFill(Vector2Int startPos, Color targetColor, Color fillColor)
    {
        if (targetColor == fillColor || !IsValidPixel(startPos)) return;
        Queue<Vector2Int> pixels = new Queue<Vector2Int>();
        pixels.Enqueue(startPos);
        bool[,] visited = new bool[gridWidth, gridHeight];

        while (pixels.Count > 0)
        {
            Vector2Int pos = pixels.Dequeue();
            if (!IsValidPixel(pos) || visited[pos.x, pos.y] || GetPixelColor(pos) != targetColor) continue;
            DrawPixelWithLogic(pos, fillColor); // Usa DrawPixelWithLogic para que a mudan�a seja parte da a��o atual
            visited[pos.x, pos.y] = true;
            pixels.Enqueue(new Vector2Int(pos.x + 1, pos.y));
            pixels.Enqueue(new Vector2Int(pos.x - 1, pos.y));
            pixels.Enqueue(new Vector2Int(pos.x, pos.y + 1));
            pixels.Enqueue(new Vector2Int(pos.x, pos.y - 1));
        }
    }

    public bool AreStatesDifferent(Color[,] state1, Color[,] state2)
    {
        if (state1 == null && state2 == null) return false; // Ambos nulos, s�o iguais
        if (state1 == null || state2 == null) return true;  // Um nulo, outro n�o, s�o diferentes

        if (state1.GetLength(0) != state2.GetLength(0) || state1.GetLength(1) != state2.GetLength(1)) return true;
        for (int i = 0; i < state1.GetLength(0); i++)
        {
            for (int j = 0; j < state1.GetLength(1); j++)
            {
                if (state1[i, j] != state2[i, j]) return true;
            }
        }
        return false;
    }

    private bool IsValidPixel(Vector2Int coords)
    {
        return coords.x >= 0 && coords.x < gridWidth &&
               coords.y >= 0 && coords.y < gridHeight;
    }
}