using UnityEngine;


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
                client.players[int.Parse(splitData[0])].SetTransform(splitData[1], splitData[2], splitData[3], splitData[2]);
        }
    }
}

public class OnOtherPlayerDisconnect : NetworkClientAction
{
    public OnOtherPlayerDisconnect(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        GameObject.Destroy(client.players[int.Parse(data[1])].playerRef);
        client.players.Remove(int.Parse(data[1]));
    }
}

public class OnPlayerSetup : NetworkClientAction
{
    public OnPlayerSetup(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT: OnPlayerSetup|cnnID,posx,posy,posz

        Debug.Log("Starting Player Setup...");

        //Set connection ID
        string[] playerInfo = data[1].Split(',');
        client.SetOurClientID(int.Parse(playerInfo[0]));
        Debug.Log("Our Client ID is : " + client.GetOurClientID());

        //Create and store refernce to player
        GameObject curPlayerRef = GameObject.Instantiate(client.playerPrefab, new Vector3(float.Parse(playerInfo[1]), float.Parse(playerInfo[2]), float.Parse(playerInfo[3])), new Quaternion());
        ClientPlayer cp = new ClientPlayer(curPlayerRef);
        curPlayerRef.GetComponent<MovementController>().client = client;

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
        //DATA FORMAT : OnNewPlayers|cnnId|playerPosX,playerPosY,playerPosZ,playerRot

        if (int.Parse(data[1]) == client.GetOurClientID()) //If it is our player, return
            return;

        string[] playerPos = data[2].Split(',');
        //Add the foreign player object to scene
        ClientPlayer cp = new ClientPlayer(GameObject.Instantiate(client.playerPrefab, new Vector3(float.Parse(playerPos[0]), float.Parse(playerPos[1]), float.Parse(playerPos[2])), new Quaternion()));
        cp.playerRef.GetComponent<MovementController>().enabled = false;
        cp.playerRef.GetComponentInChildren<Camera>().enabled = false;
        client.players.Add(int.Parse(data[1]), cp);
    }
}

public class OnLoadExistingPlayers : NetworkClientAction
{
    public OnLoadExistingPlayers(Client client) : base(client) { }

    public override void PerformAction(string[] data)
    {
        //DATA FORMAT : OnLoadExistingPlayers|cnnId,posX,posY,posZ,rotationZ|Other players...

        //Fix Me: Add a queue of player that are needed to be loaded in
        for (int i = 1; i < data.Length; i++)
        {
            string[] curUser = data[i].Split(',');

            if (int.Parse(curUser[0]) == client.GetOurClientID()) //If it is our player, return
                continue;

            //Add the foreign player object to scene    
            ClientPlayer cp = new ClientPlayer(GameObject.Instantiate(client.playerPrefab));
            cp.playerRef.GetComponent<MovementController>().enabled = false;
            cp.playerRef.GetComponentInChildren<Camera>().enabled = false;
            cp.SetTransform(curUser[1], curUser[2], curUser[3], curUser[4]);
            client.players.Add(int.Parse(curUser[0]), cp);
        }
    }
}