using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(AudioSource))]
public class MapController : MonoBehaviour
{
    public AudioSource audioSource;

    //Obstacle Ref
    public GameObject tilePrefab;
    public GameObject TargetingRockets;
    public GameObject LevelManager;


    //Start Settings
    bool isStarted = false;
    bool lvlCoroutineBegan = false;

    //Map Generation Settings
    public int towerSize;
    public int sizePerBlock;
    public Vector3 towerCenter;
    public float secPerLevel;
    public int peakLevel;


    //Instance Variables
    List<LevelManager> levels = new List<LevelManager>();
    private int curLevel = -1;



    // Start is called before the first frame update
    void Start()
    {
        //TODO: Set Server Seed

        //Make sure size of tower is even
        if (towerSize % 2 != 0)
            towerSize++;
    }

    void Update()
    {
        //UpdateTower();
    }

    //void UpdateTower()
    //{
    //    if (Random.Range(0f,1f) > 0.1) //TODO: Update the graphics to be controlled with audio source
    //    {
    //        int objectsToGenerate = blocksGenerated;//Random.Range(blocksGenerated[0], blocksGenerated[1] + 1);

    //        for (int i = 0; i < objectsToGenerate; i++)
    //        {
    //            int curXIdx = Random.Range(0, towerSize);
    //            int curZIdx = Random.Range(0, towerSize);

    //            if (!cellTaken[curZIdx][curXIdx])
    //            {
    //                Instantiate(BuildUpBlock, towerCenter + new Vector3((curXIdx - (towerSize / 2)) * sizePerBlock, curLevel * sizePerBlock, (curZIdx - (towerSize / 2)) * sizePerBlock), Quaternion.identity, transform);
    //            }

    //        }
    //    }
    //}

    IEnumerator LayerTimer()
    {

        while (isStarted && curLevel < peakLevel)
        {
            curLevel++;
            CreateNewLevel();

            yield return new WaitForSeconds(secPerLevel);
        }

        isStarted = false;
    }

    public void StartGeneration(bool isStarted)
    {
        this.isStarted = isStarted;
        StartCoroutine(LayerTimer());
    }

    void CreateNewLevel()
    {
        int[] outIdx = { -1, -1 };
        int[] inIdx = { -1, -1 };

        //If not first level calculate incoming points
        if (curLevel != 0)
        {
            inIdx[0] = Random.Range(0, towerSize);
            inIdx[1] = Random.Range(0, towerSize);
        }

        //If not the last level calculate outgoing points
        if (curLevel != peakLevel)
        {
            outIdx[0] = Random.Range(0, towerSize);
            outIdx[1] = Random.Range(0, towerSize);
        }

        //Create the level manager and its floors
        GameObject curLMGO = Instantiate(LevelManager, transform);
        LevelManager curLM = curLMGO.GetComponent<LevelManager>();
        curLM.GenerateFloor(towerSize, curLevel, secPerLevel, tilePrefab, towerCenter, sizePerBlock, inIdx, outIdx);
    }
}
