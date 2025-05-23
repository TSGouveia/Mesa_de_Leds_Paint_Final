using System.Collections.Generic;
using UnityEngine;

public class UndoRedoManager : MonoBehaviour
{
    private Stack<Color[,]> undoStack = new Stack<Color[,]>();
    private Stack<Color[,]> redoStack = new Stack<Color[,]>();
    private DrawingManager drawingManager;

    private void Awake()
    {
        drawingManager = FindAnyObjectByType<DrawingManager>();
    }
    public void SaveState()
    {
        Color[,] snapshot = new Color[32, 18];
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 18; y++)
            {
                snapshot[x, y] = drawingManager.GetPixelColor(new Vector2Int(x, y));
            }
        }
        undoStack.Push(snapshot);
        redoStack.Clear();
        Debug.Log("SaveState");
        Debug.Log(undoStack.Count);
        Debug.Log(redoStack.Count);
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            Color[,] previousState = undoStack.Pop();
            redoStack.Push(CaptureCurrentState());
            RestoreState(previousState);
        }
        Debug.Log("Undo");
        Debug.Log(undoStack.Count);
        Debug.Log(redoStack.Count);
    }

    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            Color[,] nextState = redoStack.Pop();
            undoStack.Push(CaptureCurrentState());
            RestoreState(nextState);
        }
        Debug.Log("Redo");
        Debug.Log(undoStack.Count);
        Debug.Log(redoStack.Count);
    }
    public void SaveCapturedState(Color[,] capturedState)
    {
        undoStack.Push(capturedState);
        redoStack.Clear();
        Debug.Log("SaveState");
        Debug.Log(undoStack.Count);
        Debug.Log(redoStack.Count);
    }

    public Color[,] CaptureCurrentState()
    {
        Color[,] snapshot = new Color[32, 18];
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 18; y++)
            {
                snapshot[x, y] = drawingManager.GetPixelColor(new Vector2Int(x, y));
            }
        }
        return snapshot;
    }

    public void RestoreState(Color[,] state)
    {
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 18; y++)
            {
                drawingManager.DrawPixel(new Vector2Int(x, y), state[x, y]);
            }
        }
        Debug.Log("Restore");
        Debug.Log(undoStack.Count);
        Debug.Log(redoStack.Count);
    }
}