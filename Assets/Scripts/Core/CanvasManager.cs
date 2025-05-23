using UnityEngine;
using UnityEngine.UI;
public class CanvasManager : MonoBehaviour
{
    // Array of 32x18 images
    private Image[,] imageArray = new Image[32, 18];

    // Start is called before the first frame update
    void Awake()
    {
        InitializeImageArray();
    }

    // Initializes the image array with empty images
    void InitializeImageArray()
    {
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 18; y++)
            {
                // Create a new GameObject
                GameObject imageObject = new GameObject($"Image_{x}_{y}");


                // Add Image component to the GameObject
                Image image = imageObject.AddComponent<Image>();

                // Set the image color to white
                image.color = Color.white;

                // Set the parent of the imageObject to the Canvas
                imageObject.transform.SetParent(this.transform);
                imageObject.transform.localScale = Vector3.one;

                // Add the Image component to the array
                imageArray[x, y] = image;
            }
        }
    }

    // Public method to get the image array
    public Image[,] GetImageArray()
    {
        return imageArray;
    }
}
