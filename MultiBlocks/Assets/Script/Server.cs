using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ZeroFormatter;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class Server : MonoBehaviour
{
    //[GLOBAL PARAMETERS]
    //Server Settings
    private const int MAX_CONNECTION = 100;
    private int port = 5701;

    private int hostId;

    private int reliableChannel;
    private int unreliableChannel;
    private byte[] workingBuffer = new byte[1024];

    private bool isStarted = false;
    private byte error;

    //Connected Players
    public List<ServerPlayer> clients = new List<ServerPlayer>();
    public Dictionary<int, int> clientIdxs = new Dictionary<int, int>();

    //Movement update handling
    public Queue<int> pendingMoveUpdates = new Queue<int>();

    //Messages Dictionary
    public Dictionary<string, NetworkServerAction> methods = new Dictionary<string, NetworkServerAction>();

    //Map Settings
    public Vector3 curSpawnPos = new Vector3(-5, 2, 0);

    // Start is called before the first frame update
    void Start()
    {
        //Start server connections
        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, port, null);

        isStarted = true;

        //Populate network action dictionary
        methods.Add("PlayerMove", new OnPlayerMove(this));                                  //Client asked to move/rotate
        methods.Add("OnDisconnect", new OnDisconnect(this));                                //Client has disconnected
        methods.Add("OnConnect", new OnConnect(this));                                      //Client has connected - where player setup is
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMessagePump();

        //If there is player movement, update that to others
        if (pendingMoveUpdates.Count != 0)
            SendTransUpdates();
    }

    void UpdateMessagePump()
    {
        if (!isStarted)
            return;

        //Get New Data
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
                    Debug.Log("Receving from " + connectionId + " : " + msg);
                    methods[splitData[0]].PerformAction(splitData, connectionId);
                    break;
                case NetworkEventType.ConnectEvent:
                    Debug.Log("Player " + connectionId + " has connected.");
                    methods["OnConnect"].PerformAction(null, connectionId);
                    break;
                case NetworkEventType.DisconnectEvent:
                    Debug.Log("Player " + connectionId + " has disconnected.");
                    methods["OnDisconnect"].PerformAction(null, connectionId);
                    break;
            }
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, workingBuffer, workingBuffer.Length, out dataSize, out error);
        }
    }

    //Server Closing Operations
    void OnDestroy()
    {
        Send("OnServerDisconnected|Server is closing down .", reliableChannel);
    }

    ///[COMMUNICATION]
    //Sends message to a SINGLE CLIENT (provided cnnId)
    public void Send(string message, int channelId, int cnnId)
    {
        Debug.Log("SENDING: " + message);
        List<ServerPlayer> c = new List<ServerPlayer>();
        c.Add(clients[clientIdxs[cnnId]]);
        Send(message, channelId, c);
    }
    //Sends a message to ALL CLIENTS (guests and logged in users)
    public void Send(string message, int channelId)
    {
        byte[] msg = ZeroFormatterSerializer.Serialize(message);
        if (msg.Length >= 1024 || (message.Length * sizeof(char)) >= 1024)
        {
            Debug.LogError("Byte Quota surpassed with length - " + msg.Length + " message - " + message);
            //return;
        }
        foreach (ServerPlayer sc in clients)
            NetworkTransport.Send(hostId, sc.connectionId, channelId, msg, msg.Length, out error);
    }
    //Sends message to a LIST OF CLIENTS
    public void Send(string message, int channelId, List<ServerPlayer> c)
    {
        byte[] msg = ZeroFormatterSerializer.Serialize(message);
        foreach (ServerPlayer sc in c)
            NetworkTransport.Send(hostId, sc.connectionId, channelId, msg, msg.Length, out error);
    }

    ///[MOVEMENT UPDATES]
    //Called when someone moved
    void SendTransUpdates()
    {
        string curMessage = "PlayerMove";
        while (pendingMoveUpdates.Count != 0)
        {
            if (clients[clientIdxs[pendingMoveUpdates.Peek()]].GetStringTransform() == "0,0,0")
            {
                Debug.LogWarning("SENT RESET POSITION");
            }


            string posMsg = curMessage + "|" + pendingMoveUpdates.Peek() + "," + clients[clientIdxs[pendingMoveUpdates.Peek()]].GetStringTransform();
            if ((posMsg.Length * sizeof(char)) > 1024)
            {
                Send(curMessage, unreliableChannel, clients);
                curMessage = "PlayerMove";
            }
            else
            {
                curMessage = posMsg;
                pendingMoveUpdates.Dequeue();
            }
        }

        if (curMessage != "PlayerMove") //Send any left over positions
            Send(curMessage, unreliableChannel, clients);
    }

    ///[ACCESORS/MUTATORS]
    public int GetUnreliableChannel() { return unreliableChannel; }
    public int GetReliableChannel() { return reliableChannel; }
}
