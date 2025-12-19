using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO.Ports;

public class ArduinoUdpListener : MonoBehaviour
{
    public int arduinoValue;   // lo ves en el Inspector

    UdpClient client;
    Thread listenThread;
    bool running;
    object lockObj = new object();

    void Start()
    {
        int port = 7000;
        client = new UdpClient(port);
        running = true;

        listenThread = new Thread(ListenLoop);
        listenThread.IsBackground = true;
        listenThread.Start();

        Debug.Log("Listening UDP on port " + port);
    }

    void ListenLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = client.Receive(ref ep);
                string msg = Encoding.UTF8.GetString(data).Trim();

                if (int.TryParse(msg, out int v))
                {
                    lock (lockObj)
                    {
                        arduinoValue = v;
                    }
                }
            }
            catch
            {
                // ignora errores de red
            }
        }
    }

    void Update()
    {
        // Aquí puedes usar arduinoValue para tu terreno, UI, etc.
        // Ejemplo de debug:
        // Debug.Log(arduinoValue);
    }

    void OnApplicationQuit()
    {
        running = false;
        try { client?.Close(); } catch { }
        if (listenThread != null && listenThread.IsAlive)
            listenThread.Join(100);
    }
}