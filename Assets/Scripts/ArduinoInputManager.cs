using UnityEngine;
using System.IO.Ports;
using System;

public class ArduinoInputManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static ArduinoInputManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    // -----------------

    [Header("Configuración USB")]
    public string portName = "/dev/cu.usbmodem1051DB2CF56C2";
    public int baudRate = 9600;
    public bool showDebugLogs = true;

    private SerialPort stream;

    [Header("Valores en Tiempo Real (Crudos)")]
    public int rawPot1;
    public int rawPot2;
    public int rawPot3;
    public int rawPot4;

    [Header("Normalizados (sin filtro)")]
    [Range(0f, 1f)] public float Pot1_Normalizado;
    [Range(0f, 1f)] public float Pot2_Normalizado;
    [Range(0f, 1f)] public float Pot3_Normalizado;
    [Range(0f, 1f)] public float Pot4_Normalizado;

    [Header("Filtro anti-jitter (Deadzone)")]
    [Tooltip("Ignora cambios menores a este delta (ej: 0.01 = 1%).")]
    [Range(0f, 50f)] public float minDelta = 0.01f;

    [Tooltip("Valores filtrados: usa estos para controlar tu mundo.")]
    [Range(0f, 1f)] public float Pot1_Filtrado;
    [Range(0f, 1f)] public float Pot2_Filtrado;
    [Range(0f, 1f)] public float Pot3_Filtrado;
    [Range(0f, 1f)] public float Pot4_Filtrado;

    // Internos: “último valor aceptado” por pot
    private float _p1LastAccepted;
    private float _p2LastAccepted;
    private float _p3LastAccepted;
    private float _p4LastAccepted;

    void Start()
    {
        stream = new SerialPort(portName, baudRate);
        stream.ReadTimeout = 50;

        // Handshake / estabilidad
        stream.DtrEnable = true;
        stream.RtsEnable = true;

        try
        {
            stream.Open();
            Debug.Log("Puerto abierto exitosamente: " + portName);

            // Inicializa los filtrados con 0 (o puedes inicializarlos al primer read válido)
            Pot1_Filtrado = _p1LastAccepted = 0f;
            Pot2_Filtrado = _p2LastAccepted = 0f;
            Pot3_Filtrado = _p3LastAccepted = 0f;
            Pot4_Filtrado = _p4LastAccepted = 0f;
        }
        catch (Exception e)
        {
            Debug.LogError("ERROR CRÍTICO: " + e.Message);
        }
    }

    void Update()
    {
        if (stream == null || !stream.IsOpen) return;

        try
        {
            string data = stream.ReadLine();
            if (showDebugLogs) Debug.Log("Recibido: " + data);

            if (string.IsNullOrEmpty(data)) return;

            string[] splitData = data.Split(',');
            if (splitData.Length != 4) return;

            rawPot1 = int.Parse(splitData[0]);
            rawPot2 = int.Parse(splitData[1]);
            rawPot3 = int.Parse(splitData[2]);
            rawPot4 = int.Parse(splitData[3]);

            // Normalizados (0..1)
            Pot1_Normalizado = rawPot1 / 1023.0f;
            Pot2_Normalizado = rawPot2 / 1023.0f;
            Pot3_Normalizado = rawPot3 / 1023.0f;
            Pot4_Normalizado = rawPot4 / 1023.0f;

            // Filtrados (deadzone)
            Pot1_Filtrado = ApplyDeadzone(Pot1_Normalizado, ref _p1LastAccepted);
            Pot2_Filtrado = ApplyDeadzone(Pot2_Normalizado, ref _p2LastAccepted);
            Pot3_Filtrado = ApplyDeadzone(Pot3_Normalizado, ref _p3LastAccepted);
            Pot4_Filtrado = ApplyDeadzone(Pot4_Normalizado, ref _p4LastAccepted);
        }
        catch (TimeoutException)
        {
            // Normal: a veces no llega nada en ese frame
        }
        catch (Exception e)
        {
            if (showDebugLogs) Debug.LogWarning("Error leyendo datos: " + e.Message);
        }
    }

    private float ApplyDeadzone(float current, ref float lastAccepted)
    {
        // Solo acepta cambios “grandes”
        if (Mathf.Abs(current - lastAccepted) >= minDelta)
            lastAccepted = current;

        return lastAccepted;
    }

    void OnDestroy()
    {
        if (stream != null && stream.IsOpen) stream.Close();
    }
}