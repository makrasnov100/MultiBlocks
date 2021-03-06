﻿using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class NetworkServerAction
{
    public Server server;

    public NetworkServerAction(Server server)
    {
        this.server = server;
    }

    public virtual void PerformAction(string[] data, int cnnId)
    {
        Debug.Log(data[0] + " from " + cnnId);
    }

    //Guest/Client helper methods
    public void removeUser(List<ServerPlayer> targetCol, Dictionary<int, int> targetIdxs, int cnnId)
    {
        //Special case if only one remains in collection
        if (targetCol.Count == 1)
        {
            targetCol.RemoveAt(targetIdxs[cnnId]);
            targetIdxs.Remove(cnnId);
            return;
        }

        //Special case if the removed cnnId is already in the "last" place of the collection
        if (targetIdxs[cnnId] == (targetCol.Count - 1))
        {
            targetCol.RemoveAt(targetIdxs[cnnId]);
            targetIdxs.Remove(cnnId);
            return;
        }

        int lastCnnId = targetCol[targetCol.Count - 1].connectionId;
        int replacedIdx = targetIdxs[cnnId];
        targetCol[targetIdxs[cnnId]] = targetCol[targetCol.Count - 1];
        targetCol.RemoveAt(targetCol.Count - 1);
        targetIdxs.Remove(cnnId);
        targetIdxs[lastCnnId] = replacedIdx;
    }

    public void addUser(List<ServerPlayer> targetCol, Dictionary<int, int> targetIdxs, ServerPlayer element)
    {
        targetCol.Add(element);
        targetIdxs.Add(element.connectionId, targetCol.Count - 1);
    }
}

public class OnConnect : NetworkServerAction
{

    public OnConnect(Server server) : base(server) { }

    public override void PerformAction(string[] data, int cnnId)
    {
        //DATA FORMAT: OnConnect|

        Debug.Log("Player: (" + cnnId + ") has connected");
        ServerPlayer sp = null;

        //Add player to storage
        server.curSpawnPos = new Vector3(server.curSpawnPos.x + 2, server.curSpawnPos.y, server.curSpawnPos.z);
        sp = new ServerPlayer(cnnId, server.curSpawnPos, "Unnamed", 1);
        addUser(server.clients, server.clientIdxs, sp);

        //Calculate spawn position
        //TODO: Transfer more map generation settings to server so these are not hard coded
        int towerSize = server.levelDesigner.towerSize;
        float zOffset = server.clients.Count * 2f;
        zOffset = zOffset % 30; //Prevents users from being spawned in the air
        int level = (int) Math.Floor(zOffset / 30);
        Vector3 spawnPos = new Vector3(((towerSize / 2) * 2) + 3, 7 + (level * 3), zOffset - 15);
        sp.SetTransform(spawnPos, -90f);

        //Add Ready Players
        server.Send("OnChangeReadyPlayers|" + server.readyClientCount, server.GetReliableChannel(), cnnId);

        //Tells joining player about itself
        string msg = "OnPlayerSetup|" + cnnId + "," + sp.GetStringTransform() + "," + sp.name + "," + sp.model;
        server.Send(msg, server.GetReliableChannel(), cnnId);

        //Tell joining player about old players
        string msg2 = "OnLoadExistingPlayers";
        foreach (ServerPlayer sp1 in server.clients)
            msg2 += "|" + sp1.connectionId + "," + sp1.GetStringTransform() + "," + sp1.name + "," + sp1.model;
        server.Send(msg2, server.GetReliableChannel(), cnnId);

        //Tell old players about new player
        string msg3 = "OnNewPlayers|" + cnnId + "|" + sp.GetStringTransform() + "|" + sp.name + "|" + sp.model;
        server.Send(msg3, server.GetReliableChannel());
    }
}

public class OnDisconnect : NetworkServerAction
{
    public OnDisconnect(Server server) : base(server) { }

    public override void PerformAction(string[] data, int cnnId)
    {
        bool notFound = true;
        //Check if player disconnected
        if (server.clientIdxs.ContainsKey(cnnId))
        {
            //Find if disconnected player was in ready mode
            if (server.clients[server.clientIdxs[cnnId]].isReady)
                server.readyClientCount--;

            removeUser(server.clients, server.clientIdxs, cnnId);

            server.Send("OnOtherPlayerDisconnect|" + cnnId + "|" + server.readyClientCount, server.GetReliableChannel(), server.clients);

            notFound = false;
        }
        if (notFound)
        {
            Debug.LogError("Disconnected player cannot be found in client list!");
        }
    }
}

public class OnPlayerMove : NetworkServerAction
{
    public OnPlayerMove(Server server) : base(server) { }

    public override void PerformAction(string[] data, int cnnId)
    {
        //DATA FORMAT: PlayerMove|x,y,z,rotation
        string[] moveData = data[1].Split(',');

        //Reference current player asking to move
        ServerPlayer player = server.clients[server.clientIdxs[cnnId]];

        //Store the new positions
        player.SetTransform(moveData[0], moveData[1], moveData[2], moveData[3]);

        //Send to every client that players updated transform
        server.pendingMoveUpdates.Enqueue(cnnId);
    }
}

public class OnPlayerReady : NetworkServerAction
{
    public OnPlayerReady(Server server) : base(server) { }

    public override void PerformAction(string[] data, int cnnId)
    {
        //DATA FORMAT: OnPlayerReady|1-true OR 0-false|if 1 - name| if 1 - model idx

        if (int.Parse(data[1]) == 1)
        {
            if (!server.clients[server.clientIdxs[cnnId]].isReady)
            {

                server.clients[server.clientIdxs[cnnId]].isReadyUpdate(true, data[2], int.Parse(data[3]));
                server.readyClientCount++;

                server.Send("OnChangeReadyPlayers|1|" + cnnId + "|" + data[2] + "|" + data[3], server.GetReliableChannel());

                if (server.readyClientCount >= 5 && server.canStartGame && !server.gameLock)
                {
                    server.levelDesigner.StartMapGenerations();
                    server.canStartGame = false;
                }
            }
        }
        else
        {
            if (server.clients[server.clientIdxs[cnnId]].isReady)
            {
                server.clients[server.clientIdxs[cnnId]].isReady = false;
                server.readyClientCount--;

                server.Send("OnChangeReadyPlayers|-1", server.GetReliableChannel());

                if (server.readyClientCount < 3 && !server.gameLock)
                {
                    server.levelDesigner.StopAllCoroutines();
                    server.levelDesigner.CancelPlayMode();
                    server.canStartGame = true;
                }
            }
        }


    }
}

public class OnSyncTimeWithPlayer : NetworkServerAction
{
    public OnSyncTimeWithPlayer(Server server) : base(server) { }

    public override void PerformAction(string[] data, int cnnId)
    {
        //DATA FORMAT: OnSyncTimeWithPlayer|hour,minute,second,millisecond

        DateTime curTime = System.DateTime.Now;
        server.Send("OnSyncTimeWithServer|" + data[1] + "|" + curTime.Year + "," + curTime.Month + "," + curTime.Day + "," + curTime.Hour + "," + curTime.Minute + "," + curTime.Second + "," + curTime.Millisecond, server.GetReliableChannel(), cnnId);

    }
}