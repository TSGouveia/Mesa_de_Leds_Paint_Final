using UnityEngine;

public class ToolManager : MonoBehaviour
{
    public enum ToolType
    {
        Brush,
        Eraser,
        Bucket,
        Eyedropper
    }

    private ToolType currentTool = ToolType.Brush;
    private DrawingManager drawingManager;

    // Start is called before the first frame update
    void Awake()
    {
        // Set the default tool to Brush
        drawingManager = FindAnyObjectByType<DrawingManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // Example of how to switch tools using keyboard input
        if (Input.GetKeyDown(KeyCode.B))
        {
            SelectTool(ToolType.Brush);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SelectTool(ToolType.Eraser);
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            SelectTool(ToolType.Bucket);
        }
        else if (Input.GetKeyDown(KeyCode.I))
        {
            SelectTool(ToolType.Eyedropper);
        }
    }

    // Method to select the current tool
    public void SelectTool(ToolType tool)
    {
        currentTool = tool;
        drawingManager.SetCurrentTool(tool);
        Debug.Log("Selected tool: " + currentTool);
    }

    // Method to get the current tool
    public ToolType GetCurrentTool()
    {
        return currentTool;
    }
}
