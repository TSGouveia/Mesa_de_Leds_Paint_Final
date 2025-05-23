using UnityEngine;

public class InputManager : MonoBehaviour
{
    UndoRedoManager undoRedoManager;
    private void Awake()
    {
        undoRedoManager = FindAnyObjectByType<UndoRedoManager>();
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                undoRedoManager.Undo();
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                undoRedoManager.Redo();
            }
        }
    }
}
