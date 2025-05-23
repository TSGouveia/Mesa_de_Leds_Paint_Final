using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PixelGrid : MonoBehaviour
{
    public float pixelSize = 1.0f; // Tamanho de cada pixel em unidades do mundo
    public Vector2Int gridSize = new Vector2Int(32, 18); // Tamanho da grade

    // Converte coordenadas do mundo para coordenadas de pixel
    public Vector2Int WorldToPixel(Vector3 worldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        int x = Mathf.RoundToInt((localPosition.x + (gridSize.x * pixelSize) / 2) / pixelSize);
        int y = Mathf.RoundToInt((localPosition.y + (gridSize.y * pixelSize) / 2) / pixelSize);
        return new Vector2Int(x - 1, y - 1);
    }

    // Converte coordenadas de pixel para coordenadas do mundo
    public Vector3 PixelToWorld(Vector2Int pixelPosition)
    {
        float x = (pixelPosition.x * pixelSize) - (gridSize.x * pixelSize) / 2 + pixelSize / 2;
        float y = (pixelPosition.y * pixelSize) - (gridSize.y * pixelSize) / 2 + pixelSize / 2;
        return transform.TransformPoint(new Vector3(x + 1, y + 1, 0));
    }

    // Desenha a grid no Gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green; // Cor das linhas da grid

        // Desenhar linhas verticais
        for (int i = -1; i <= gridSize.x - 1; i++)
        {
            Vector3 start = PixelToWorld(new Vector2Int(i, -1));
            Vector3 end = PixelToWorld(new Vector2Int(i, gridSize.y - 1));
            Gizmos.DrawLine(start, end);
        }

        // Desenhar linhas horizontais
        for (int i = -1; i <= gridSize.y - 1; i++)
        {
            Vector3 start = PixelToWorld(new Vector2Int(-1, i));
            Vector3 end = PixelToWorld(new Vector2Int(gridSize.x - 1, i));
            Gizmos.DrawLine(start, end);
        }
    }
}
