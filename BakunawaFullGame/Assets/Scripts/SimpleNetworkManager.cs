using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;

public class SimpleNetworkManager : MonoBehaviour
{
    public static SimpleNetworkManager Instance;

    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    public bool isHost = false;
    public bool isConnected = false;
    public string myRole = "None"; // "Attacker", "Tank", "Support"

    // Queue to run code on the main Unity thread
    private List<string> messageQueue = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this alive when scene changes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Process messages on the main thread
        lock (messageQueue)
        {
            if (messageQueue.Count > 0)
            {
                foreach (string msg in messageQueue)
                {
                    MainMenuManager.Instance.OnMessageReceived(msg);
                }
                messageQueue.Clear();
            }
        }
    }

    // --- HOSTING ---
    public string StartHost()
    {
        try
        {
            isHost = true;
            server = new TcpListener(IPAddress.Any, 7777); // Port 7777
            server.Start();

            // Start listening for clients in background
            receiveThread = new Thread(new ThreadStart(ListenForClients));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            isConnected = true;
            return GetLocalIPAddress();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Host Error: " + e.Message);
            return "Error";
        }
    }

    void ListenForClients()
    {
        while (true)
        {
            TcpClient newClient = server.AcceptTcpClient();
            // For this simple version, we act as both Server and Client 1
            if (client == null)
            {
                client = newClient;
                stream = client.GetStream();
                Debug.Log("Client Connected!");

                // Start reading data
                Thread readThread = new Thread(new ThreadStart(ReceiveData));
                readThread.IsBackground = true;
                readThread.Start();
            }
        }
    }

    // --- JOINING ---
    public bool JoinGame(string ipAddress)
    {
        try
        {
            isHost = false;
            client = new TcpClient();
            client.Connect(ipAddress, 7777);
            stream = client.GetStream();

            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            isConnected = true;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Join Error: " + e.Message);
            return false;
        }
    }

    // --- MESSAGING ---
    public void SendMessageToServer(string msg)
    {
        if (client == null || !client.Connected) return;

        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        stream.Write(bytes, 0, bytes.Length);
    }

    void ReceiveData()
    {
        try
        {
            byte[] bytes = new byte[1024];
            while (client != null)
            {
                int length = stream.Read(bytes, 0, bytes.Length);
                if (length > 0)
                {
                    string msg = Encoding.UTF8.GetString(bytes, 0, length);
                    lock (messageQueue)
                    {
                        messageQueue.Add(msg);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Disconnected: " + e.Message);
            isConnected = false;
        }
    }

    // Helper to find your own IP
    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    void OnApplicationQuit()
    {
        if (server != null) server.Stop();
        if (client != null) client.Close();
        if (receiveThread != null) receiveThread.Abort();
    }
}