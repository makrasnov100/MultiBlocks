using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using ZeroFormatter;
using ZeroFormatter.Formatters;

public enum ConnectionStatus {Connecting, Connected, Disconnected, Failed}

public class Client : MonoBehaviour
{
    //References
    public GameObject playerPrefab;

    //Network Related
    public string ipToConnectTo = "127.0.0.1"; // Default local host
    private const int MAX_CONNECTION = 100;

    private int port = 5701;

    private int hostId;

    private int reliableChannel;
    private int unreliableChannel;
    private byte[] workingBuffer = new byte[1024];

    private float connectionTime;
    private int connectionId = -1;

    private bool isConnected = false;
    private byte error;

    //Controllers
    public MapController mapCont;
    public UIManager uiCont;

    //Players Storage
    private int ourClientID;
    public ClientPlayer ourPlayer;
    public Dictionary<int, ClientPlayer> players = new Dictionary<int, ClientPlayer>();

    //Messages Dictionary
    Dictionary<string, NetworkClientAction> methods = new Dictionary<string, NetworkClientAction>();

    // Start is called before the first frame update
    void Start()
    {
        //Create used data methods
        methods.Add("OnPlayerSetup", new OnPlayerSetup(this));                                 //Server told client their player information and map
        methods.Add("PlayerMove", new PlayerMove(this));                                       //Server told client a player has moved
        methods.Add("OnNewPlayers", new OnNewPlayers(this));                                   //Server told client new player information
        methods.Add("OnLoadExistingPlayers", new OnLoadExistingPlayers(this));                 //Server telling joining player about old players
        methods.Add("OnOtherPlayerDisconnect", new OnOtherPlayerDisconnect(this));             //Server telling others about a leaving player

        Connect();
    }

    //Connect to server with the ip in inspector
    public void Connect()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, 0);
        connectionId = NetworkTransport.Connect(hostId, ipToConnectTo, port, 0, out error);

        connectionTime = Time.time;
        Debug.Log("Attempting to connect to - " + ipToConnectTo);
        isConnected = true;

        StartCoroutine(CheckConnection());
    }

    IEnumerator<WaitForSeconds> CheckConnection()
    {
        yield return new WaitForSeconds(5f);

        if (connectionId == -1)
        {
            uiCont.SetConnnection(ConnectionStatus.Failed);
            Disconnect();
        }
    }

    ///[COMMUNICATION]
    //Message Recieving (checked each frame)
    void Update()
    {
        if (!isConnected)
            return;

        int recHostId;
        int connectionId;
        int channelId;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, workingBuffer, workingBuffer.Length, out dataSize, out error);
        while (recData != NetworkEventType.Nothing)
        {
            switch (recData)
            {
                case NetworkEventType.DataEvent:
                    byte[] recBuffer = new byte[dataSize];
                    if (dataSize > 0)
                    {
                        Buffer.BlockCopy(workingBuffer, 0, recBuffer, 0, dataSize);
                    }
                    else
                    {
                        recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, workingBuffer, workingBuffer.Length, out dataSize, out error);
                        continue;
                    }
                    string msg = ZeroFormatterSerializer.Deserialize<string>(recBuffer);
                    string[] splitData = msg.Split('|');
                    if (splitData[0] != "PlayerMove")
                        Debug.Log("Receving from " + connectionId + " : " + msg);
                    methods[splitData[0]].PerformAction(splitData);
                    break;
                case NetworkEventType.ConnectEvent:
                    connectionTime = Time.time;
                    uiCont.SetConnnection(ConnectionStatus.Connected);
                    Debug.Log("Connected to server... Took " + connectionTime);
                    break;
                case NetworkEventType.DisconnectEvent:
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
                    uiCont.SetConnnection(ConnectionStatus.Disconnected);
                    Debug.LogWarning("Disconnected from the server");
                    break;
            }
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, workingBuffer, workingBuffer.Length, out dataSize, out error);
        }
    }

    public void Disconnect()
    {
        NetworkTransport.Disconnect(hostId, connectionId, out error);
    }

    //Message Sending
    public void Send(string message, int chnlId)
    {
        string[] splitData = message.Split('|');
        if (splitData[0] != "PlayerMove")
            Debug.Log("SENDING: " + message);
        byte[] msg = ZeroFormatterSerializer.Serialize(message);
        if (msg.Length >= 1024 || (message.Length * sizeof(char)) >= 1024)
        {
            Debug.LogError("Byte Quota surpassed with length - " + msg.Length + " message - " + message);
            //return;
        }

        NetworkTransport.Send(hostId, connectionId, chnlId, msg, msg.Length, out error);
    }

    public int GetUnreliableChannel() { return unreliableChannel; }
    public int GetReliableChannel() { return reliableChannel; }
    public int GetOurClientID() { return ourClientID; }
    public void SetOurClientID(int ourClientID) { this.ourClientID = ourClientID; }
    public void SetOurPlayer(ClientPlayer ourPlayer) { this.ourPlayer = ourPlayer; }
}