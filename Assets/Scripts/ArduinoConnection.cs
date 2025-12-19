using UnityEngine;
using System.IO.Ports;
using System;

public class ArduinoConnection : MonoBehaviour
{
    public string portName = "/dev/cu.usbmodem1051DB2CF56C2"; // ¡Recuerda poner tu puerto! /dev/cu.usbmodem1051DB2CF56C2
    public int baudRate = 9600;

    private SerialPort stream;

    // Aquí guardaremos los valores recibidos para usarlos en Unity
    // Range(0, 1023) solo sirve para ver una barrita en el inspector, es visual.
    [Range(0, 1023)] public int val1; 
    [Range(0, 1023)] public int val2;
    [Range(0, 1023)] public int val3;

    void Start()
    {
        stream = new SerialPort(portName, baudRate);
        stream.ReadTimeout = 20; // Bajamos un poco el timeout para mayor fluidez
        try {
            stream.Open();
        } catch (Exception e) {
            Debug.LogError("No se pudo abrir el puerto: " + e.Message);
        }
    }

    void Update()
    {
        if (stream != null && stream.IsOpen)
        {
            try
            {
                // 1. Leemos la línea completa: "512,100,800"
                string data = stream.ReadLine();
                
                // 2. Partimos la línea usando la coma como separador
                string[] splitData = data.Split(',');

                // 3. Verificamos que realmente llegaron 3 datos (para evitar errores)
                if (splitData.Length == 3)
                {
                    // 4. Convertimos de texto a número entero
                    val1 = int.Parse(splitData[0]);
                    val2 = int.Parse(splitData[1]);
                    val3 = int.Parse(splitData[2]);
                    
                    // ¡LISTO! Aquí puedes llamar a una función para mover cosas
                    MoverObjeto(); 
                }
            }
            catch (System.Exception)
            {
                // Si falla la lectura o el parseo, ignoramos este frame
            }
        }
    }

    void MoverObjeto()
    {
        // EJEMPLO DE USO:
        // Convertimos el valor de 0-1023 a algo útil para Unity
        // Por ejemplo, mover en posición X, Y, Z
        
        // Mapeamos 0-1023 a -5.0f y 5.0f (un rango de posición en pantalla)
        float x = Map(val1, 0, 1023, -5f, 5f);
        float y = Map(val2, 0, 1023, -5f, 5f);
        float scale = Map(val3, 0, 1023, 0.5f, 3f);

        // Aplicamos al objeto que tenga este script
        transform.position = new Vector3(x, y, 0);
        transform.localScale = new Vector3(scale, scale, scale);
    }
    
    // Función auxiliar para "mapear" valores (como la función map() de Arduino)
    float Map(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    void OnDestroy()
    {
        if (stream != null && stream.IsOpen) stream.Close();
    }
}