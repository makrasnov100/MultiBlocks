using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public Client client;
    public List<Texture> modelImages = new List<Texture>();

    //UI References (Initial)
    // - output
    public GameObject InitMenu;
    public Image bg;
    public TMP_Text connectText;
    public TMP_Text readyPlayersCount;
    public TMP_Text gameStartsCountdown;
    public TMP_Text latency;
    public Button readyBtn;
    public Button notReadyBtn;
    public RawImage modelImage;
    // - input
    public TMP_InputField nameInput;
    public Dropdown modelInput;

    //UI References (InGame)
    // - output
    public GameObject InGameMenu;
    public TMP_Text ultraJumpsOutput;
    public TMP_Text place1;
    public TMP_Text place2;
    public TMP_Text place3;
    public TMP_Text place4;
    //Instance Variables
    bool initMenuActive = true;
    int readyPlayers = 0;


    //BG animation
    public List<Color> bgColors = new List<Color>();
    Color sourceColor;
    int targetColorIdx = 1;
    double transitionStart;
    public double timePerTransition;

    // Start is called before the first frame update
    void Start()
    {
        //Set up BG Animation
        sourceColor = bg.color;
        targetColorIdx = 1;
        transitionStart = Time.time;
        StartCoroutine(BackgroundAnimation());
    }

    IEnumerator BackgroundAnimation()
    {
        while (initMenuActive)
        {
            bg.color = Color.Lerp(sourceColor, bgColors[targetColorIdx], (Time.time - (float) transitionStart) / (float) timePerTransition);
            yield return new WaitForFixedUpdate();

            if ((Time.time - (float)transitionStart) > timePerTransition)
                UpdateTargetBGColor();
        }
    }

    //Updates the leaderboards of players in the game
    IEnumerator LeaderBoardsUpdate()
    {
        if (client.players.Count < 4)
            place4.gameObject.SetActive(false);
        if (client.players.Count < 3)
            place3.gameObject.SetActive(false);
        if (client.players.Count < 2)
            place2.gameObject.SetActive(false);


        while (client.mapCont.isStarted)
        {
            List<ClientPlayer> sortedList = new List<ClientPlayer>();
            sortedList.AddRange(client.players.Values);
            sortedList = sortedList.OrderBy(o => o.playerRef.transform.position.y).ToList();

            //Display the top three players
            if (sortedList.Count > 0 && sortedList[sortedList.Count-1].playerRef.transform.position.y > -1)
            {
                place1.text = "1st: " + sortedList[sortedList.Count-1].name + " | " + (int) sortedList[sortedList.Count-1].playerRef.transform.position.y;
                sortedList.RemoveAt(sortedList.Count - 1);
            }
            if (sortedList.Count > 0 && sortedList[sortedList.Count-1].playerRef.transform.position.y > -1)
            {
                place2.text = "2nd: " + sortedList[sortedList.Count-1].name + " | " + (int) sortedList[sortedList.Count-1].playerRef.transform.position.y;
                sortedList.RemoveAt(sortedList.Count - 1);
            }
            if (sortedList.Count > 0 && sortedList[sortedList.Count-1].playerRef.transform.position.y > -1)
            {
                place3.text = "3rd: " + sortedList[sortedList.Count-1].name + " | " + (int) sortedList[sortedList.Count-1].playerRef.transform.position.y;
                sortedList.RemoveAt(sortedList.Count - 1);
            }   

            //Display all other players
            string otherPlayers = "";
            int additionalPlayersCount = 0;
            while (sortedList.Count >= (additionalPlayersCount+1) && additionalPlayersCount < 5 && sortedList[sortedList.Count - additionalPlayersCount - 1].playerRef.transform.position.y > -1)
            {
                otherPlayers += (4 + additionalPlayersCount) + ": " + sortedList[sortedList.Count-additionalPlayersCount-1].name + " | " + (int) sortedList[sortedList.Count - additionalPlayersCount - 1].playerRef.transform.position.y + System.Environment.NewLine;
                sortedList.RemoveAt(sortedList.Count - 1);
                additionalPlayersCount++;
            }
            place4.text = otherPlayers;

            yield return new WaitForSeconds(.5f);
        }
    }

    public void SetConnnection(ConnectionStatus cs)
    {
        if (cs == ConnectionStatus.Connected)
        {
            connectText.text = "Connected to Server!";
            connectText.color = Color.green;
        }
        else if (cs == ConnectionStatus.Connecting)
        {
            connectText.text = "Connecting...";
            connectText.color = Color.yellow;
        }
        else if(cs == ConnectionStatus.Disconnected)
        {
            connectText.text = "Disconnected from Server!";
            connectText.color = Color.red;
        }
        else
        {
            connectText.text = "Conection Timeout!";
            connectText.color = Color.red;
        }
    }

    void UpdateTargetBGColor()
    {
        transitionStart = Time.time;
        sourceColor = bg.color;
        if (targetColorIdx == bgColors.Count - 1)
            targetColorIdx = 0;
        else
            targetColorIdx++;
    }

    public void OnReadyClick()
    {
        if (client.GetOurClientID() == -1)
            return;

        client.isReady = !client.isReady;
        if (client.isReady)
        {
            readyBtn.gameObject.SetActive(false);
            notReadyBtn.gameObject.SetActive(true);

            //Chosen model derivation
            int chosenModel = 0;
            if (modelInput.value == 3)
                chosenModel = UnityEngine.Random.Range(0, 2);
            else
                chosenModel = modelInput.value;
            

            //Send Ready Info to Server
            client.Send("OnPlayerReady|1|" + nameInput.text + "|" + chosenModel, client.GetReliableChannel());

        }
        else
        {
            readyBtn.gameObject.SetActive(true);
            notReadyBtn.gameObject.SetActive(false);

            //Send Ready Info to Server
            client.Send("OnPlayerReady|0", client.GetReliableChannel());
        }
    }

    public void OnServerChange()
    {
        if (client.GetOurClientID() != -1)
        {
            //Alter Ready button
            ColorBlock readyColors = readyBtn.colors;
            readyColors.normalColor = new Color(.01f, 1, 0);
            readyBtn.colors = readyColors;
            readyBtn.gameObject.GetComponentInChildren<Text>().text = "Ready";
        }
        else
        {
            //Alter Ready button
            ColorBlock readyColors = readyBtn.colors;
            readyColors.normalColor = new Color(1, 1, 0);
            readyBtn.colors = readyColors;
            readyBtn.gameObject.GetComponentInChildren<Text>().text = "Wait";
        }
    }

    public void ChangeReadyPlayers(int delta)
    {
        readyPlayers += delta;
        readyPlayersCount.text = readyPlayers + " Players Ready";
    }

    public void SetReadyPlayers(string readyPlayers)
    {
        this.readyPlayers = int.Parse(readyPlayers);
        readyPlayersCount.text = readyPlayers + " Players Ready";
    }

    public void SetLatency(int milliseconds)
    {
        latency.text = milliseconds + " ms";

        if (milliseconds < 100)
            latency.color = Color.green;
        else if (milliseconds < 500)
            latency.color = Color.yellow;
        else
            latency.color = Color.red;
    }

    public void UpdateGameStart(TimeSpan ts)
    {
        gameStartsCountdown.text = "Game Starts in " + Math.Round(ts.TotalSeconds, 2).ToString("#.##") + " seconds";
    }

    public void UpdateGameStart(string ts)
    {
        gameStartsCountdown.text = ts;
    }

    public void UpdateModelImage()
    {
        modelImage.texture = modelImages[modelInput.value];
    }

    public void StartLeaderboards()
    {
        StartCoroutine(LeaderBoardsUpdate());
    }
}

