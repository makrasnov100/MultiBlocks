using UnityEngine;

public class ClientPlayer
{
    public GameObject playerRef;

    public ClientPlayer(GameObject playerRef)
    {
        this.playerRef = playerRef;
    }

    public void SetTransform(string x, string y, string z, string rz)
    {
        playerRef.transform.position = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
        playerRef.transform.Find("Body").transform.eulerAngles = new Vector3(0f, float.Parse(rz), 0);
    }
}
