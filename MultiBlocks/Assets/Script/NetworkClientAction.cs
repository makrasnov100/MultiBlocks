﻿using UnityEngine;
using System;


public abstract class NetworkClientAction
{

    public Client client;

    public NetworkClientAction(Client client)
    {
        this.client = client;
    }

    public abstract void PerformAction(string[] data);

}

public class PlayerMove : NetworkClientAction
{
    public PlayerMove(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT: PlayerMove|cnnID1,x,y,z,rz|cnnID2,...

        for (int i = 1; i < data.Length; i++)
        {
            string[] splitData = data[i].Split(',');
            if (int.Parse(splitData[0]) != client.GetOurClientID())
                client.players[int.Parse(splitData[0])].SetTransform(splitData[1], splitData[2], splitData[3], splitData[4]);
        }
    }
}

public class OnOtherPlayerDisconnect : NetworkClientAction
{
    public OnOtherPlayerDisconnect(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT: OnOtherPlayerDisconnect|disconnected cnnID| amount of players ready


        GameObject.Destroy(client.players[int.Parse(data[1])].playerRef);
        client.players.Remove(int.Parse(data[1]));
        client.uiCont.SetReadyPlayers(data[2]);
    }
}

public class OnPlayerSetup : NetworkClientAction
{
    public OnPlayerSetup(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT: OnPlayerSetup|cnnID,posx,posy,posz,roty,name,model#

        Debug.Log("Starting Player Setup...");

        //Set connection ID
        string[] playerInfo = data[1].Split(',');
        client.SetOurClientID(int.Parse(playerInfo[0]));
        Debug.Log("Our Client ID is : " + client.GetOurClientID());
        client.uiCont.OnServerChange();

        //Sync Time with Server
        DateTime curTime = System.DateTime.Now;
        client.Send("OnSyncTimeWithPlayer|" + curTime.Year + "," + curTime.Month + "," + curTime.Day + "," + curTime.Hour + "," + curTime.Minute + "," + curTime.Second + "," + curTime.Millisecond, client.GetUnreliableChannel());

        //Create and store refernce to player
        GameObject curPlayerRef = GameObject.Instantiate(client.playerPrefab, new Vector3(float.Parse(playerInfo[1]), float.Parse(playerInfo[2]), float.Parse(playerInfo[3])), Quaternion.Euler(new Vector3(0,float.Parse(playerInfo[4]),0)));
        ClientPlayer cp = new ClientPlayer(curPlayerRef, playerInfo[5], int.Parse(playerInfo[6]));
        curPlayerRef.GetComponent<MovementController>().client = client;
        curPlayerRef.SetActive(false);

        cp.playerRef.name = "myPlayer";
        client.SetOurPlayer(cp);
        client.players.Add(int.Parse(playerInfo[0]), cp);
    }
}

public class OnNewPlayers : NetworkClientAction
{
    public OnNewPlayers(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT : OnNewPlayers|cnnId|playerPosX,playerPosY,playerPosZ,playerRot|name|mode#

        if (int.Parse(data[1]) == client.GetOurClientID()) //If it is our player, return
            return;

        string[] playerPos = data[2].Split(',');
        //Add the foreign player object to scene
        ClientPlayer cp = new ClientPlayer(GameObject.Instantiate(client.playerPrefab, new Vector3(float.Parse(playerPos[0]), float.Parse(playerPos[1]), float.Parse(playerPos[2])), new Quaternion()),data[3], int.Parse(data[4]));
        cp.playerRef.GetComponent<MovementController>().enabled = false;
        cp.playerRef.GetComponentInChildren<Camera>().enabled = false;
        GameObject.Destroy(cp.playerRef.GetComponent<Rigidbody>());
        client.players.Add(int.Parse(data[1]), cp);
        cp.playerRef.SetActive(false);
    }
}

public class OnLoadExistingPlayers : NetworkClientAction
{
    public OnLoadExistingPlayers(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT : OnLoadExistingPlayers|cnnId,posX,posY,posZ,rotationZ,name,model#|Other players...

        //Fix Me: Add a queue of player that are needed to be loaded in
        for (int i = 1; i < data.Length; i++)
        {
            string[] curUser = data[i].Split(',');

            if (int.Parse(curUser[0]) == client.GetOurClientID()) //If it is our player, return
                continue;

            //Add the foreign player object to scene    
            ClientPlayer cp = new ClientPlayer(GameObject.Instantiate(client.playerPrefab), curUser[5], int.Parse(curUser[6]));
            cp.playerRef.GetComponent<MovementController>().enabled = false;
            cp.playerRef.GetComponentInChildren<Camera>().enabled = false;
            GameObject.Destroy(cp.playerRef.GetComponent<Rigidbody>());
            cp.SetTransform(curUser[1], curUser[2], curUser[3], curUser[4]);
            client.players.Add(int.Parse(curUser[0]), cp);
            cp.playerRef.SetActive(false);
        }
    }
}

public class OnChangeReadyPlayers : NetworkClientAction
{
    public OnChangeReadyPlayers(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT : OnChangeReadyPlayers|amountChanged|cnnId(that changed)|new name|new model 

        client.uiCont.ChangeReadyPlayers(int.Parse(data[1]));

        //If new ready information update the referenced player
        if (int.Parse(data[1]) == 1)
        {
            ClientPlayer readyPlayer = client.players[int.Parse(data[2])];
            readyPlayer.name = data[3];
            readyPlayer.model = int.Parse(data[4]);
        }
    }
}


public class OnSyncTimeWithServer : NetworkClientAction
{
    public OnSyncTimeWithServer(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT : OnSyncTimeWithServer|clientTime|serverTime
        //TIME FORMAT : Year,Month,Day,Hours,Minutes,Seconds,Milliseconds

        string[] clientTime = data[1].Split(',');
        string[] serverTime = data[1].Split(',');

        //Calculate Latency
        DateTime curTime = System.DateTime.Now;
        DateTime sentTime = new DateTime(int.Parse(clientTime[0]), int.Parse(clientTime[1]), int.Parse(clientTime[2]), int.Parse(clientTime[3]), int.Parse(clientTime[4]), int.Parse(clientTime[5]), int.Parse(clientTime[6]));
        TimeSpan roundtrip = curTime - sentTime;
        int latency = (int) roundtrip.TotalMilliseconds / 2;
        client.uiCont.SetLatency(latency);

        //Calculate Server/Client Time Difference
        DateTime serverStamp = new DateTime(int.Parse(serverTime[0]), int.Parse(serverTime[1]), int.Parse(serverTime[2]), int.Parse(serverTime[3]), int.Parse(serverTime[4]), int.Parse(serverTime[5]), int.Parse(serverTime[6]));
        TimeSpan serverTimeSpan = serverStamp - sentTime;
        client.timeDifference = serverTimeSpan.TotalMilliseconds + latency;

        Debug.LogWarning("Server to Client Time Difference Calculated AT: " + client.timeDifference);
    }
}

public class OnServerDisconnected : NetworkClientAction
{
    public OnServerDisconnected(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT : OnServerDisconnected|

        client.uiCont.SetConnnection(ConnectionStatus.Disconnected);
    }
}


public class OnLevelInfo : NetworkClientAction
{
    public OnLevelInfo(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT : OnLevelInfo|levelNumber|levelSeed|levelServerEndTime|secForLevel|proportionGoneByEnd|towerSize
        //TIME FORMAT : Year,Month,Day,Hours,Minutes,Seconds,Milliseconds

        string[] timeInfo = data[3].Split(',');
        DateTime levelToDisappearAt = new DateTime(int.Parse(timeInfo[0]), int.Parse(timeInfo[1]), int.Parse(timeInfo[2]), int.Parse(timeInfo[3]), int.Parse(timeInfo[4]), int.Parse(timeInfo[5]), int.Parse(timeInfo[6]));
        client.mapCont.PlanLevel(int.Parse(data[1]), int.Parse(data[2]), levelToDisappearAt, float.Parse(data[4]), float.Parse(data[5]), int.Parse(data[6]));
    }
}

public class OnCancelPlay : NetworkClientAction
{
    public OnCancelPlay(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT : OnCancelPlay|placeholder

        client.mapCont.CancelPlay();
        client.uiCont.UpdateGameStart("Game Starts in alot of seconds");
    }
}

