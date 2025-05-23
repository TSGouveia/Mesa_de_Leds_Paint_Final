using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorButtonManager : MonoBehaviour
{
    private string fileName = "palette.txt"; // Path to the .txt palette file
    public GameObject buttonPrefab; // Button prefab to be instantiated
    public Transform buttonPanel; // Panel where buttons will be added

    private List<Color> colorPalette = new List<Color>(); // List to hold the color values

    void Start()
    {
        // Load colors from the palette file
        LoadPalette(fileName);

        // Create buttons for each color in the palette
        CreateButtonsWithColors();
    }

    // Function to load the palette from the .txt file
    // Function to load the palette from a .txt file inside Resources
    void LoadPalette(string fileName)
    {
        // Remove extensão caso seja passada
        if (fileName.EndsWith(".txt"))
        {
            fileName = fileName.Substring(0, fileName.Length - 4);
        }

        // Carregar o ficheiro da pasta Resources
        TextAsset textAsset = Resources.Load<TextAsset>(fileName);

        if (textAsset != null)
        {
            string[] lines = textAsset.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (line.StartsWith(";") || string.IsNullOrWhiteSpace(line))
                    continue;

                string hexColor = line.Trim();
                if (hexColor.Length == 8)
                {
                    byte alpha = byte.Parse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte red = byte.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte green = byte.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    byte blue = byte.Parse(hexColor.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

                    Color color = new Color(red / 255f, green / 255f, blue / 255f, alpha / 255f);
                    colorPalette.Add(color);
                }
                else
                {
                    Debug.LogWarning($"Invalid color code (must be AARRGGBB): {line}");
                }
            }
        }
        else
        {
            Debug.LogError($"Palette file '{fileName}' not found in Resources!");
        }
    }


    // Function to create buttons and apply colors from the palette
    void CreateButtonsWithColors()
    {
        foreach (var color in colorPalette)
        {
            // Instantiate the button prefab
            GameObject newButton = Instantiate(buttonPrefab, buttonPanel);

            // Get the Image component of the button to apply the color
            Image buttonImage = newButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = color; // Set the button color
            }

            // Optionally, set button text (e.g., display the color name or HEX)
            Text buttonText = newButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = ColorUtility.ToHtmlStringRGBA(color); // Display the color in RGBA format (e.g., #RRGGBBAA)
            }
        }
    }
}
