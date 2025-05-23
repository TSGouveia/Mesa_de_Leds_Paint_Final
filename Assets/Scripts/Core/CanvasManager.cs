using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    private Image[,] imageArray; // Removida inicializa��o aqui, ser� no Awake
    private const int GRID_WIDTH = 32; // Mant�m consist�ncia
    private const int GRID_HEIGHT = 18;

    [Tooltip("Nome da Layer a ser atribu�da aos pixels. Deve existir no Unity Editor.")]
    public string drawingCanvasLayerName = "DrawingCanvas"; // Certifica-te que esta Layer existe

    void Awake()
    {
        imageArray = new Image[GRID_WIDTH, GRID_HEIGHT]; // Inicializa o array aqui
        InitializeImageArray();
    }

    void InitializeImageArray()
    {
        int drawingLayer = LayerMask.NameToLayer(drawingCanvasLayerName);
        if (drawingLayer == -1)
        {
            Debug.LogError($"CanvasManager: Layer '{drawingCanvasLayerName}' n�o existe! Crie-a no Unity Editor.", this);
        }

        if (GetComponent<GridLayoutGroup>() == null)
        {
            Debug.LogWarning("CanvasManager: GameObject n�o tem um GridLayoutGroup. A organiza��o dos pixels pode n�o ser a esperada.", this);
        }

        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                GameObject imageObject = new GameObject($"Pixel_{x}_{y}"); // Nome mais descritivo
                Image image = imageObject.AddComponent<Image>();
                image.color = Color.white;

                // !!!!! CR�TICO PARA DETEC��O DE CLIQUE PELO EVENTSYSTEM !!!!!
                image.raycastTarget = true;

                imageObject.transform.SetParent(this.transform, false); // false para manter worldPositionStays
                imageObject.transform.localScale = Vector3.one; // OK com GridLayoutGroup

                // !!!!! CR�TICO PARA FILTRO DE LAYERMASK NO DRAWINGMANAGER !!!!!
                if (drawingLayer != -1)
                {
                    imageObject.layer = drawingLayer;
                }
                else // Fallback se a layer n�o existir (n�o ideal)
                {
                    Debug.LogWarning($"CanvasManager: Pixel_{x}_{y} n�o p�de ter a layer '{drawingCanvasLayerName}' atribu�da.", imageObject);
                }

                imageArray[x, y] = image;
            }
        }
    }

    public Image[,] GetImageArray()
    {
        return imageArray;
    }

    // Adicionados para consist�ncia e acesso f�cil
    public int GetWidth() { return GRID_WIDTH; }
    public int GetHeight() { return GRID_HEIGHT; }
}