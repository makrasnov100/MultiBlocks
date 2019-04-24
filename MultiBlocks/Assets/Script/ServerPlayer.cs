using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class ServerPlayer
{
    public int connectionId;
    private float positionX;
    private float positionY;
    private float positionZ;    
    private float rotationZ;

    public ServerPlayer(int connectionId)
    {
        this.connectionId = connectionId;
        this.positionX = 0f;
        this.positionY = 0f;
        this.positionZ = 0f;
        this.rotationZ = 0f;
    }
    public void SetTransform(string x, string y, string z, string rz)
    {
        positionX = float.Parse(x);
        positionY = float.Parse(y);
        positionZ = float.Parse(z);
        rotationZ = float.Parse(rz);
    }

    public string GetStringTransform()
    {
        string result = positionX + "," + positionY + "," + positionZ + "," + rotationZ;
        return result;
    }

}


