using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class ServerPlayer
{
    public bool isReady;
    public int connectionId;
    private float positionX;
    private float positionY;
    private float positionZ;    
    private float rotationZ;

    public string name;
    public int model;


    public ServerPlayer(int connectionId, Vector3 spawnPoint, string name, int model)
    {
        isReady = false;
        this.connectionId = connectionId;
        this.positionX = spawnPoint.x;
        this.positionY = spawnPoint.y;
        this.positionZ = spawnPoint.z;
        this.rotationZ = 0f;

        this.name = name;
        this.model = model;
    }
    public void SetTransform(string x, string y, string z, string rz)
    {
        positionX = float.Parse(x);
        positionY = float.Parse(y);
        positionZ = float.Parse(z);
        rotationZ = float.Parse(rz);
    }

    public void SetTransform(Vector3 pos, float rz)
    {
        this.positionX = pos.x;
        this.positionY = pos.y;
        this.positionZ = pos.z;
        rotationZ = rz;
    }

    public string GetStringTransform()
    {
        string result = positionX + "," + positionY + "," + positionZ + "," + rotationZ;
        return result;
    }

    public void isReadyUpdate(bool isReady, string name, int model)
    {
        this.isReady = isReady;
        this.name = name;
        this.model = model;
    }
}


