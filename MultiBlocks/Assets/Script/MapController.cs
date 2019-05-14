using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct LevelInfo
{
    public int levelSeed;
    public DateTime endTime;
    public float secForLevel;
    public float proportionGoneByEnd;

    public LevelInfo(int levelSeed, DateTime endTime, float secForLevel, float proportionGoneByEnd)
    {
        this.levelSeed = levelSeed;
        this.endTime = endTime;
        this.secForLevel = secForLevel;
        this.proportionGoneByEnd = proportionGoneByEnd;
    }
}

public class MapController : MonoBehaviour
{
    public Client client;

    //Obstacle Ref
    public GameObject tilePrefab;
    public GameObject TargetingRockets;
    public GameObject LevelManager;

    //Object Pool
    public static MapController Instance;
    Queue<GameObject> tilePool = new Queue<GameObject>();

    //Start Settings
    bool isStarted = false;
    bool lvlCoroutineBegan = false;

    //Map Generation Settings
    private int towerSize;
    public int levelsPresentAtOnce;
    public int sizePerBlock;
    public int sizePerLevel;
    public Vector3 towerCenter;
    public int peakLevel;

    //Instance Variables
    Dictionary<int, LevelInfo> levelCtrl = new Dictionary<int, LevelInfo>();
    List<LevelManager> levels = new List<LevelManager>();
    Queue<float> levelWaitTimes = new Queue<float>();
    private int curLevel = 0;

    //Body List
    public List<GameObject> bodyPrefabs = new List<GameObject>();

    void Awake()
    {
        //Allows other scripts to access pooler by updating the static variable before all Start() methods execute
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        //TODO: Set Server Seed

        //Create object pool
        // - estimate needed amount of items
        int poolCapacity = (levelsPresentAtOnce + 1) * (towerSize * towerSize);
        // - instantiate pooled objects
        tilePool = new Queue<GameObject>();
        for (int a = 0; a < poolCapacity; a++)
        {
            GameObject go = Instantiate(tilePrefab, transform);
            go.SetActive(false);
            tilePool.Enqueue(go);
            TileController tl = tilePool.Peek().GetComponent<TileController>();
        }

        //Make sure tower size is large enough for elevators
        towerSize = Mathf.Max(10, towerSize);
    }

    IEnumerator LayerTimer(DateTime gamServerTime)
    {
        DateTime actualStartTime = gamServerTime.AddMilliseconds(client.timeDifference);

        while (actualStartTime > DateTime.Now)
        {
            client.uiCont.UpdateGameStart(actualStartTime - DateTime.Now);
            yield return new WaitForEndOfFrame();
        }

        //Spawn in all players
        foreach (KeyValuePair<int, ClientPlayer> cp in client.players)
        {
            cp.Value.SpawnIntoGame();

            GameObject playerRef = cp.Value.playerRef;
            //Have to do here because client player cant be monobehavior
            Transform curBody = playerRef.transform.Find("Body");
            float curBodyRotY = curBody.rotation.y;
            if (curBody != null)
                GameObject.Destroy(curBody.gameObject);

            GameObject newBody = GameObject.Instantiate(bodyPrefabs[cp.Value.model], playerRef.transform);
            newBody.name = "Body";
            playerRef.GetComponent<MovementController>().body = newBody;
            if (cp.Value.model == 1)
            {
                newBody.transform.rotation = Quaternion.Euler(new Vector3(-90, curBodyRotY, 0));
            }
            else
            {
                newBody.transform.rotation = Quaternion.Euler(new Vector3(0, curBodyRotY, 0));
            }

        }

        //Disable UI and our nametag on our side
        client.uiCont.InitMenu.SetActive(false);
        client.uiCont.InGameMenu.SetActive(true);
        client.ourPlayer.playerRef.transform.Find("NameTag").gameObject.SetActive(false);

        isStarted = true;

        //Create the first set of levels
        for (int i = 0; i < levelsPresentAtOnce; i++)
        {
            CreateNewLevel();
            curLevel++;
        }

        //TODO: SHOW INSTRUCTIONS
        yield return new WaitForSeconds(5f);

        //Start Despawn of first Level
        levelWaitTimes.Dequeue();
        levels[curLevel - levelsPresentAtOnce].BeginDegradation();
        CreateNewLevel();
        curLevel++;

        //Keep going through levels until none remain
        while (levelWaitTimes.Count != 0)
        {
            yield return new WaitForSeconds(levelWaitTimes.Dequeue());
            levels[curLevel - levelsPresentAtOnce].BeginDegradation();

            if (curLevel < peakLevel)
            {
                CreateNewLevel();
                curLevel++;
            }
        }

        isStarted = false;
    }

    int[] latestOutIdx = { -1, -1 };
    void CreateNewLevel()
    {
        //Find info for current level
        LevelInfo curLevelInfo;
        if (levelCtrl.ContainsKey(curLevel))
            curLevelInfo = levelCtrl[curLevel];
        else
            return;

        //Set the random seed based on server input, count of despawn commands and other variables?
        UnityEngine.Random.InitState(curLevelInfo.levelSeed);

        int[] outIdx = { -1, -1 };
        int[] inIdx = { -1, -1 };

        //If not first level calculate incoming points
        if (curLevel != 0)
        {
            if (latestOutIdx[0] == -1 || latestOutIdx[1] == -1)
            {
                inIdx[0] = UnityEngine.Random.Range(1, towerSize - 2);
                inIdx[1] = UnityEngine.Random.Range(1, towerSize - 2);
            }
            else
            {
                inIdx[0] = latestOutIdx[0];
                inIdx[1] = latestOutIdx[1];
            }
        }

        //If not the last level calculate outgoing points
        while (Mathf.Abs(outIdx[0] - inIdx[0]) <= 1 || Mathf.Abs(outIdx[1] - inIdx[1]) <= 1 || outIdx[0] == -1 || outIdx[1] == -1)
        {
            outIdx[0] = UnityEngine.Random.Range(1, towerSize-2);
            outIdx[1] = UnityEngine.Random.Range(1, towerSize-2);
        }


        GameObject curLMGO = Instantiate(LevelManager, transform);
        LevelManager curLM = curLMGO.GetComponent<LevelManager>();
        curLM.GenerateFloor(towerSize, curLevel, curLevelInfo.secForLevel, curLevelInfo.proportionGoneByEnd, towerCenter, sizePerBlock, sizePerLevel, inIdx, outIdx);
        curLM.PlanFloorRemoval();
        levels.Add(curLM);

        //Add latest level information to storage
        levelWaitTimes.Enqueue(curLevelInfo.secForLevel);
        latestOutIdx[0] = outIdx[0];
        latestOutIdx[1] = outIdx[1];
    }

    public TileController SpawnFromTilePool(Vector3 pos)
    {

        GameObject go;

        //Add objects if pool doesn't have enough created
        if (tilePool.Count == 0)
            go = Instantiate(tilePrefab, transform);
        else
            go = tilePool.Dequeue();

        go.transform.position = pos;
        go.SetActive(true);

        return go.GetComponent<TileController>();
    }

    public void DespawnIntoTilePool(GameObject go)
    {
        go.SetActive(false);

        tilePool.Enqueue(go);
    }

    public void PlanLevel(int levelNum, int levelSeed, DateTime endTime, float secForLevel, float proportionGoneByEnd, int towerSize)
    {
        //Make sure size of tower is even
        if (towerSize % 2 != 0)
            towerSize++;
        this.towerSize = towerSize;

        if (levelNum == 0)
        {
            DateTime gameBeginTime = endTime;
            gameBeginTime = gameBeginTime.AddSeconds(-secForLevel);
            StartCoroutine(LayerTimer(gameBeginTime));
        }

        levelCtrl.Add(levelNum, new LevelInfo(levelSeed, endTime, secForLevel, proportionGoneByEnd));
    }

    public void CancelPlay()
    {
        //Stop level generation
        StopAllCoroutines();

        //TODO: put all tiles into the pool instead of destroying all levels

        //Remove all created blocks 
        levelCtrl.Clear();
        for(int i = 0; i < levels.Count; i++)
            Destroy(levels[i].gameObject);
        levels.Clear();
    }
}
