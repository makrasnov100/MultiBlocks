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
    public int sizePerLevel;
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
        if (peakLevel != 0 && curLevel != peakLevel) //first condition protects from infinite loop
        {
            while (outIdx[0] == inIdx[0] || outIdx[1] == inIdx[1])
            {
                outIdx[0] = Random.Range(0, towerSize);
                outIdx[1] = Random.Range(0, towerSize);
            }
        }

        //Create the level manager and its floors
        GameObject curLMGO = Instantiate(LevelManager, transform);
        LevelManager curLM = curLMGO.GetComponent<LevelManager>();
        curLM.GenerateFloor(towerSize, curLevel, secPerLevel, .5f ,tilePrefab, towerCenter, sizePerBlock, sizePerLevel, inIdx, outIdx);
        curLM.PlanFloorRemoval(255);
    }
}
