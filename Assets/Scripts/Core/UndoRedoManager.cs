using System.Collections.Generic;
using UnityEngine;

public class UndoRedoManager : MonoBehaviour
{
    private Stack<Color[,]> undoStack = new Stack<Color[,]>();
    private Stack<Color[,]> redoStack = new Stack<Color[,]>();
    private DrawingManager drawingManager;

    private const int GRID_WIDTH = 32; // Idealmente obtido de CanvasManager/PixelGrid
    private const int GRID_HEIGHT = 18;

    private void Awake()
    {
        drawingManager = FindObjectOfType<DrawingManager>(); // Mudado para FindObjectOfType
        if (drawingManager == null)
        {
            Debug.LogError("UndoRedoManager: DrawingManager não encontrado!", this);
            enabled = false;
        }
        // Considera obter GRID_WIDTH/HEIGHT do CanvasManager aqui para flexibilidade
    }

    /// <summary>
    /// Captura o estado atual do canvas. Usado internamente e pelo DrawingManager.
    /// </summary>
    public Color[,] CaptureCurrentState()
    {
        if (drawingManager == null || !enabled) return null;

        Color[,] snapshot = new Color[GRID_WIDTH, GRID_HEIGHT];
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                snapshot[x, y] = drawingManager.GetPixelColor(new Vector2Int(x, y));
            }
        }
        return snapshot;
    }

    /// <summary>
    /// Restaura o canvas para o estado fornecido. Usado internamente.
    /// </summary>
    private void RestoreState(Color[,] state) // Tornar private se só Undo/Redo usam
    {
        if (drawingManager == null || state == null || !enabled) return;
        if (state.GetLength(0) != GRID_WIDTH || state.GetLength(1) != GRID_HEIGHT) return;

        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                drawingManager.DrawPixelWithoutUndo(new Vector2Int(x, y), state[x, y]); // Assume este método no DrawingManager
            }
        }
        // Debug.Log("RestoreState complete.");
    }

    /// <summary>
    /// Método principal para o DrawingManager registrar um estado para Undo.
    /// Recebe o estado ANTES da ação ter sido concluída.
    /// </summary>
    public void RecordStateForUndo(Color[,] stateBeforeAction)
    {
        if (stateBeforeAction == null || !enabled) return;

        undoStack.Push(stateBeforeAction);
        redoStack.Clear(); // Nova ação limpa o redo
        // Debug.Log($"RecordStateForUndo. Undo: {undoStack.Count}, Redo: {redoStack.Count}");
    }

    public void Undo()
    {
        if (undoStack.Count > 0 && enabled)
        {
            Color[,] stateToRestore = undoStack.Pop();          // Estado anterior (S_n-1)
            Color[,] currentStateForRedo = CaptureCurrentState(); // Estado atual (S_n) ANTES de reverter

            redoStack.Push(currentStateForRedo);    // Salva S_n para Redo
            RestoreState(stateToRestore);           // Restaura para S_n-1
            // Debug.Log($"Undo. Undo: {undoStack.Count}, Redo: {redoStack.Count}");
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0 && enabled)
        {
            Color[,] stateToRestore = redoStack.Pop();          // Estado "futuro" (S_n)
            Color[,] currentStateForUndo = CaptureCurrentState(); // Estado atual (S_n-1) ANTES de refazer

            undoStack.Push(currentStateForUndo);    // Salva S_n-1 para Undo
            RestoreState(stateToRestore);           // Restaura para S_n
            // Debug.Log($"Redo. Undo: {undoStack.Count}, Redo: {redoStack.Count}");
        }
    }

    public void ClearStacks()
    {
        undoStack.Clear();
        redoStack.Clear();
    }
}