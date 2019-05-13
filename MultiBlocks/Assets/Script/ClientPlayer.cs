using UnityEngine;
using TMPro;

public class ClientPlayer
{
    public GameObject playerRef;
    public string name;
    public int model;

    public ClientPlayer(GameObject playerRef, string name, int model)
    {
        this.playerRef = playerRef;
        this.name = name;
        this.model = model;
    }

    public void SetTransform(string x, string y, string z, string rz)
    {
        playerRef.transform.position = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
        playerRef.transform.Find("Body").transform.eulerAngles = new Vector3(0f, float.Parse(rz), 0);
    }

    public void ChangeReadyState(string name, int model)
    {
        this.name = name;
        this.model = model;
    }

    public void SpawnIntoGame()
    {
        playerRef.SetActive(true);

        playerRef.transform.Find("NameTag").gameObject.GetComponent<TMP_Text>().text = name;
    }
}
