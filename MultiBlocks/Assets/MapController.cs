using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public AudioSource audioSource;

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
    public int towerSize;
    public int levelsPresentAtOnce;
    public int sizePerBlock;
    public int sizePerLevel;
    public Vector3 towerCenter;
    public float secPerLevel;
    public int peakLevel;

    //Instance Variables
    List<LevelManager> levels = new List<LevelManager>();
    private int curLevel = 0;


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
        }

        //Create the first few levels
        for (int i = 0; i < levelsPresentAtOnce; i++)
        {
            curLevel++;
            CreateNewLevel();
        }


        //Make sure size of tower is even
        if (towerSize % 2 != 0)
            towerSize++;

        //Make sure tower size is large enough for elevators
        towerSize = Mathf.Max(10, towerSize);
    }

    IEnumerator LayerTimer()
    {

        while (isStarted && curLevel-levelsPresentAtOnce < peakLevel)
        {
            levels[curLevel - levelsPresentAtOnce].BeginDegradation();
            yield return new WaitForSeconds(secPerLevel);

            curLevel++;
            if (curLevel <= peakLevel)
            {
                CreateNewLevel();
            }
        }

        isStarted = false;
    }

    public void StartGeneration(bool isStarted)
    {
        this.isStarted = isStarted;
        StartCoroutine(LayerTimer());
    }

    int[] latestOutIdx = { -1, -1 };
    void CreateNewLevel()
    {
        int[] outIdx = { -1, -1 };
        int[] inIdx = { -1, -1 };

        //If not first level calculate incoming points
        if (curLevel != 1)
        {
            if (latestOutIdx[0] == -1 || latestOutIdx[1] == -1)
            {
                inIdx[0] = Random.Range(1, towerSize - 2);
                inIdx[1] = Random.Range(1, towerSize - 2);
            }
            else
            {
                inIdx[0] = latestOutIdx[0];
                inIdx[1] = latestOutIdx[1];
            }
        }

        //If not the last level calculate outgoing points
        if (peakLevel != 1 && curLevel != peakLevel) //first condition protects from infinite loop
        {
            while (Mathf.Abs(outIdx[0] - inIdx[0]) <= 1 || Mathf.Abs(outIdx[1] - inIdx[1]) <= 1 || outIdx[0] == -1 || outIdx[1] == -1)
            {
                outIdx[0] = Random.Range(1, towerSize-2);
                outIdx[1] = Random.Range(1, towerSize-2);
            }
        }

        //Create the level manager and its floors
        GameObject curLMGO = Instantiate(LevelManager, transform);
        LevelManager curLM = curLMGO.GetComponent<LevelManager>();
        curLM.GenerateFloor(towerSize, curLevel, secPerLevel, .5f, towerCenter, sizePerBlock, sizePerLevel, inIdx, outIdx);
        curLM.PlanFloorRemoval(255);
        levels.Add(curLM);

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


}
