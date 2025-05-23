using UnityEngine;
using UnityEngine.UI; // Required for Image component
using System;
using System.IO.Ports; // Required for SerialPort
using System.Collections;
using System.Collections.Generic;
using TMPro; // Required for TextMeshPro InputField

/// <summary>
/// Manages sending pixel data from a grid of UI Images to an Arduino via Serial Port.
/// Includes dynamic port selection, optional auto-connect, and manual send trigger.
/// Sends pixel data correcting for HORIZONTAL MIRRORING and VERTICAL INVERSION
/// using a ZIGZAG pattern relative to the physical display.
/// Assumes Arduino physical row 0 (bottom) is L->R, row 1 is R->L, etc.
/// Sends data for Unity's bottom row first, top row last.
/// Assumes target Arduino expects data in the format: [START_BYTE_1, START_BYTE_2, R1, G1, B1, ..., Rn, Gn, Bn]
/// where n = WIDTH * HEIGHT, ordered according to the corrected zigzag pattern starting from bottom-left physically.
/// </summary>
public class ArduinoSerialSender_FromImages_ManualZigzagMirror : MonoBehaviour // Keeping name, adding clarity in summary
{
    #region Inspector Variables

    [Header("Serial Port Settings")]
    [Tooltip("Drag the TMP_InputField UI element here for entering the serial port name (e.g., COM3, /dev/ttyACM0).")]
    [SerializeField] private TMP_InputField portNameInputField;

    [Tooltip("Serial communication speed (bits per second). Must match the Arduino sketch.")]
    [SerializeField] private int baudRate = 115200;

    [Tooltip("Enable automatic connection attempts on start and retry on failure?")]
    [SerializeField] private bool enableAutoConnect = true;

    [Tooltip("Time in seconds between automatic connection attempts if the previous one failed.")]
    [SerializeField] private float autoConnectInterval = 3.0f;

    [Header("Canvas Representation")]
    [Tooltip("Drag the Parent GameObject containing all the 'Image_X_Y' GameObjects here.")]
    [SerializeField] private Transform imageParent;

    [Header("Grid Dimensions (Match your setup)")]
    [SerializeField] private int gridWidth = 32;
    [SerializeField] private int gridHeight = 18;

    [SerializeField] private GameObject COMInputGO;

    #endregion

    #region Private Variables

    // Serial Port Object
    private SerialPort _serialPort;
    private volatile bool _isPortOpen = false;
    private string _activePortName = "";

    // Coroutine Handles
    private Coroutine _autoConnectCoroutine;

    // Data Buffers and Protocol
    private byte[] _packetBuffer;
    private const byte START_BYTE_1 = 0xA5;
    private const byte START_BYTE_2 = 0x5A;
    private int _numPixels;
    private int _dataLength;
    private int _packetLength;

    // Image Grid Mapping
    private Image[,] _imageGrid;
    private bool _gridInitialized = false;

    #endregion

    #region Unity Lifecycle Methods

    void Awake()
    {
        _numPixels = gridWidth * gridHeight;
        _dataLength = _numPixels * 3;
        _packetLength = 2 + _dataLength;

        _packetBuffer = new byte[_packetLength];
        _packetBuffer[0] = START_BYTE_1;
        _packetBuffer[1] = START_BYTE_2;

        _imageGrid = new Image[gridWidth, gridHeight];
    }

    void Start()
    {
        InitializeImageGrid(); // Keep using the original grid init

        if (enableAutoConnect && _gridInitialized)
        {
            StartAutoConnectRoutine();
        }
        else if (!_gridInitialized)
        {
            Debug.LogWarning("Auto-connect disabled: Image Grid initialization failed.");
        }
        else if (!enableAutoConnect)
        {
            Debug.Log("Auto-connect is disabled. Use the Connect button.");
        }
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    #endregion

    #region Public Methods (for UI Buttons and External Calls)

    public void ManualConnect()
    {
        StopAutoConnectRoutine();

        if (_isPortOpen)
        {
            Debug.LogWarning("Already connected.");
            return;
        }
        if (!_gridInitialized)
        {
            Debug.LogError("Cannot connect: Image Grid failed to initialize.");
            return;
        }

        Debug.Log("Attempting manual connection...");
        if (AttemptConnectionInternal())
        {
            Debug.Log($"Manual connection successful to '{_activePortName}'! Call SendCurrentFrame() to transmit data.");
        }
        else
        {
            Debug.LogError("Manual connection failed.");
        }
    }

    public void Disconnect()
    {
        // Debug.Log("Disconnect requested."); // Less verbose disconnect
        StopAutoConnectRoutine();

        if (_serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                _serialPort.Close();
                Debug.Log($"Serial port '{_activePortName}' closed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error closing serial port '{_activePortName}': {ex.Message}");
            }
        }

        _isPortOpen = false;
        _serialPort = null;
        _activePortName = "";
    }

    /// <summary>
    /// Reads the current color state of the image grid, formats it into a packet
    /// using a ZIGZAG pattern CORRECTED FOR HORIZONTAL MIRRORING **AND VERTICAL INVERSION**,
    /// and sends it ONCE over the serial port (if connected).
    /// Sends data starting from the bottom row of the Unity grid.
    /// </summary>
    public void SendCurrentFrame()
    {
        if (portNameInputField.text == "")
        {
            COMInputGO.SetActive(true);
            return;
        }
        if (!_isPortOpen)
        {
            Debug.LogWarning("Cannot send frame: Not connected.");
            ManualConnect();
            return;
        }
        if (!_gridInitialized)
        {
            Debug.LogError("Cannot send frame: Image Grid not initialized.");
            return;
        }
        PrepareAndSendPacketZigzagMirroredAndVerticalCorrected(); // Call the corrected version
        // Debug.Log("Manual zigzag mirrored & vertically corrected frame data sent."); // Optional log
    }

    #endregion

    #region Core Logic - Initialization, Connection, Sending

    // --- InitializeImageGrid remains the same as before ---
    private void InitializeImageGrid()
    {
        if (imageParent == null) { Debug.LogError("Init Error: 'Image Parent' not assigned!"); _gridInitialized = false; return; }
        int foundCount = 0;
        Image[] allImages = imageParent.GetComponentsInChildren<Image>(true);
        Debug.Log($"Found {allImages.Length} Images under '{imageParent.name}'. Mapping to {gridWidth}x{gridHeight} grid...");
        Array.Clear(_imageGrid, 0, _imageGrid.Length);
        foreach (Image imgComponent in allImages)
        {
            string objectName = imgComponent.gameObject.name;
            string[] nameParts = objectName.Split('_');
            if (nameParts.Length == 3 && nameParts[0].Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(nameParts[1], out int x) && int.TryParse(nameParts[2], out int y))
                {
                    if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    {
                        if (_imageGrid[x, y] == null) { _imageGrid[x, y] = imgComponent; foundCount++; }
                        else { Debug.LogWarning($"Duplicate coord ({x},{y}) for '{objectName}'. Using '{_imageGrid[x, y].gameObject.name}'."); }
                    }
                    else { Debug.LogWarning($"Coords ({x},{y}) from '{objectName}' out of bounds ({gridWidth}x{gridHeight}). Ignoring."); }
                }
                else { Debug.LogWarning($"Could not parse coords from '{objectName}'. Ignoring."); }
            }
        }
        if (foundCount == _numPixels) { Debug.Log($"Image Grid initialized successfully! Mapped {foundCount} images."); _gridInitialized = true; }
        else
        {
            Debug.LogError($"Image Grid init FAILED! Expected {_numPixels}, mapped {foundCount}. Check 'Image_X_Y' naming/hierarchy."); _gridInitialized = false;
            for (int y = 0; y < gridHeight; y++) { for (int x = 0; x < gridWidth; x++) { if (_imageGrid[x, y] == null) Debug.LogWarning($"--> Missing image for [{x},{y}]."); } }
        }
    }


    // --- Start/Stop AutoConnect Routines remain the same ---
    private void StartAutoConnectRoutine()
    {
        if (enableAutoConnect && _autoConnectCoroutine == null && !_isPortOpen)
        {
            Debug.Log("Starting auto-connect routine...");
            _autoConnectCoroutine = StartCoroutine(AutoConnectHandler());
        }
    }

    private void StopAutoConnectRoutine()
    {
        if (_autoConnectCoroutine != null)
        {
            Debug.Log("Stopping auto-connect routine.");
            StopCoroutine(_autoConnectCoroutine);
            _autoConnectCoroutine = null;
        }
    }

    // --- AutoConnectHandler remains the same ---
    private IEnumerator AutoConnectHandler()
    {
        yield return new WaitForSeconds(1.0f); Debug.Log("[AutoConnect] Starting attempts...");
        while (enableAutoConnect && !_isPortOpen)
        {
            if (!_gridInitialized) { Debug.LogError("[AutoConnect] Halting: Grid not initialized."); _autoConnectCoroutine = null; yield break; }
            string targetPort = portNameInputField != null ? portNameInputField.text : "N/A"; Debug.Log($"[AutoConnect] Attempting '{targetPort}'...");
            if (AttemptConnectionInternal()) { Debug.Log("[AutoConnect] Success! Ready for manual sending."); _autoConnectCoroutine = null; yield break; }
            else { Debug.Log($"[AutoConnect] Failed. Retrying in {autoConnectInterval}s..."); yield return new WaitForSeconds(autoConnectInterval); }
        }
        Debug.Log("[AutoConnect] Routine finished."); _autoConnectCoroutine = null;
    }

    // --- AttemptConnectionInternal remains the same ---
    private bool AttemptConnectionInternal()
    {
        if (portNameInputField == null) { Debug.LogError("Connection Error: Input Field not assigned."); return false; }
        string currentPortName = portNameInputField.text;
        if (string.IsNullOrWhiteSpace(currentPortName)) { Debug.LogWarning("Connection Error: Port name empty."); return false; }
        if (_isPortOpen) { /*Debug.LogWarning("Conn attempt skipped: Already connected.");*/ return true; } // Less verbose
        try
        {
            _serialPort = new SerialPort(currentPortName, baudRate) { ReadTimeout = 100, WriteTimeout = 100, Parity = Parity.None, DataBits = 8, StopBits = StopBits.One };
            _serialPort.Open(); _isPortOpen = true; _activePortName = currentPortName;
            Debug.Log($"Serial port '{_activePortName}' opened @ {baudRate} baud."); return true;
        }
        catch (Exception ex) { Debug.LogError($"Connection Error ('{currentPortName}'): {ex.GetType().Name} - {ex.Message}"); }
        _isPortOpen = false; _activePortName = ""; if (_serialPort != null) { if (_serialPort.IsOpen) _serialPort.Close(); _serialPort = null; }
        return false;
    }

    /// <summary>
    /// Reads color data from the grid, applying ZIGZAG row pattern and
    /// REVERSED horizontal order within each row to correct mirroring, AND
    /// processing rows from BOTTOM-TO-TOP to correct vertical inversion.
    /// Populates the buffer and sends the packet.
    /// </summary>
    private void PrepareAndSendPacketZigzagMirroredAndVerticalCorrected() // Renamed method
    {
        if (!_isPortOpen || _serialPort == null || !_serialPort.IsOpen)
        {
            Debug.LogWarning("Send cancelled: Serial port not open/ready.");
            if (_isPortOpen) { Disconnect(); } // Attempt to clean up if state is inconsistent
            return;
        }

        try
        {
            int bufferIndex = 2; // Start after START_BYTE_1 and START_BYTE_2

            // Iterate through UNITY rows from BOTTOM (y = gridHeight - 1) to TOP (y = 0)
            for (int y = gridHeight - 1; y >= 0; y--)
            {
                // Calculate the corresponding PHYSICAL row index on the Arduino/display
                int physicalRowIndex = (gridHeight - 1) - y;

                // Determine row direction based on PHYSICAL row index for standard zigzag
                // Assumes physical row 0 (bottom) goes L->R, physical row 1 R->L, etc.
                bool isPhysicalRowEven = (physicalRowIndex % 2 == 0);

                if (isPhysicalRowEven)
                {
                    // EVEN physical row (0, 2, ...): Send LEFT-TO-RIGHT
                    for (int x = 0; x < gridWidth; x++) // Iterate x from Low up to High
                    {
                        Image currentImage = _imageGrid[x, y]; // Access grid using loop's x,y
                        Color color = (currentImage != null) ? currentImage.color : Color.black;

                        _packetBuffer[bufferIndex++] = (byte)(Mathf.Clamp01(color.r) * 255f);
                        _packetBuffer[bufferIndex++] = (byte)(Mathf.Clamp01(color.g) * 255f);
                        _packetBuffer[bufferIndex++] = (byte)(Mathf.Clamp01(color.b) * 255f);
                    }
                }
                else
                {
                    // ODD physical row (1, 3, ...): Send RIGHT-TO-LEFT
                    for (int x = gridWidth - 1; x >= 0; x--) // Iterate x from High down to Low
                    {
                        Image currentImage = _imageGrid[x, y]; // Access grid using loop's x,y
                        Color color = (currentImage != null) ? currentImage.color : Color.black;

                        _packetBuffer[bufferIndex++] = (byte)(Mathf.Clamp01(color.r) * 255f);
                        _packetBuffer[bufferIndex++] = (byte)(Mathf.Clamp01(color.g) * 255f);
                        _packetBuffer[bufferIndex++] = (byte)(Mathf.Clamp01(color.b) * 255f);
                    }
                }
            } // End of row iteration

            // Write the entire packet
            _serialPort.Write(_packetBuffer, 0, _packetLength);
            // Debug.Log($"Zigzag Mirrored & Vertical Corrected Packet sent ({_packetLength} bytes)"); // Optional

        }
        // --- Handle write errors ---
        catch (TimeoutException) { Debug.LogWarning($"Serial write timeout on '{_activePortName}'. Check Arduino/connection."); Disconnect(); }
        catch (InvalidOperationException ex) { Debug.LogError($"Serial write error (Invalid Op) on '{_activePortName}': {ex.Message}. Port closed? Disconnecting."); Disconnect(); }
        catch (System.IO.IOException ex) { Debug.LogError($"Serial write error (IO Error) on '{_activePortName}': {ex.Message}. Disconnected? Disconnecting."); Disconnect(); }
        catch (Exception ex) { Debug.LogError($"Unexpected serial write error on '{_activePortName}': {ex.GetType().Name} - {ex.Message}. Disconnecting."); Disconnect(); }
    }

    #endregion
}