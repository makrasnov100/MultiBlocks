using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public GameObject levelElevator;

    int curLvl;
    int towerSize;
    int countRemoveIter;
    float timeToComplete;
    float difficultyThreshold; //value from 0 to 1 indicating proportion of blocks to remove before the whole level falls

    List<TileController> floorTiles;

    Stack<TileController> tilesToRemove = new Stack<TileController>();


    public void GenerateFloor(int towerSize, int curLvl, float timeToComplete, float difficultyThreshold, Vector3 layerCenter, int sizePerBlock, int sizePerLevel, int[] inPoint, int[] outPoint)
    {

        this.curLvl = curLvl;
        this.towerSize = towerSize;
        this.timeToComplete = timeToComplete;
        this.difficultyThreshold = difficultyThreshold;
        countRemoveIter = 0;

        //Populating initial cell list  
        floorTiles = new List<TileController>();
        bool inPointStarted = false;
        bool outPointStarted = false;
        for (int z = 0; z < towerSize; z++)
        {
            for (int x = 0; x < towerSize; x++)
            {
                //Check if hole needed for incoming elevator (since its a 2x2 hole it will 1 block to the right and two below)
                if (inPoint[1] == z && inPoint[0] == x)
                {
                    if (!inPointStarted)
                    {
                        inPointStarted = true;
                        inPoint[1]++;
                    }
                    x += 1;
                    continue;
                }

                // Check if hole needed for outgoing elevator (since its a 2x2 hole it will 1 block to the right and two below)
                if (outPoint[1] == z && outPoint[0] == x)
                {
                    if (!outPointStarted)
                    {
                        outPointStarted = true;
                        outPoint[1]++;
                    }
                    x += 1;
                    continue;
                }

                Vector3 pos = layerCenter + new Vector3((x - (towerSize / 2)) * sizePerBlock,
                                                         curLvl * (sizePerBlock * sizePerLevel),
                                                        (z - (towerSize / 2)) * sizePerBlock);

                floorTiles.Add(MapController.Instance.SpawnFromTilePool(pos));
            }
        }
        //Reset hole positions to original values
        if(outPoint[1] != -1)
            outPoint[1]--;
        if(inPoint[1] != -1)
            inPoint[1]--;

        //Instatiate elevator for level
        if (outPoint[0] != -1 && outPoint[1] != -1)
        {
            Vector3 pos = layerCenter + new Vector3(((outPoint[0] - (towerSize / 2)) * sizePerBlock) + (sizePerBlock * .5f), //+sizePerBlock to position in the middle of hole
                                                     curLvl * (sizePerBlock * sizePerLevel) + ((sizePerBlock * sizePerLevel)/2),
                                                    ((outPoint[1] - (towerSize / 2)) * sizePerBlock) + (sizePerBlock * .5f));

            Vector3 scale = new Vector3(sizePerBlock * 2, sizePerLevel * sizePerBlock, sizePerBlock * 2);

            GameObject curElevator = Instantiate(levelElevator, pos, Quaternion.identity, transform);
            curElevator.transform.localScale = scale;
        }
    }

    public void PlanFloorRemoval(int seed)
    {
        //Set the random seed based on server input, count of despawn commands and other variables?
        //Random.InitState(seed);

        //Choose blocks to despawn and start their despwn animation with correct time to despawn (should always be >1f seconds) 
        int amountToDespawn = (int)((towerSize * towerSize) * Mathf.Clamp(difficultyThreshold, 0, 1));
        int despawnsASec = Mathf.CeilToInt(amountToDespawn / Mathf.Floor(timeToComplete));

        //Remove tile from selection list effeciently
        for (int i = 0; i < amountToDespawn; i++)
        {
            int curIdx = Random.Range(0, floorTiles.Count-1);
            tilesToRemove.Push(floorTiles[curIdx]);
            ConstantTimeTileRemove(curIdx);
        }

        countRemoveIter++;
    }

    IEnumerator LevelDeathCounter()
    {
        //Remove all remaining tiles gradually
        for (int i = floorTiles.Count-1; i >= 0; i--)
        {
            floorTiles[i].Despawn(4f);
        }

        //Wait for blocks to finish despawn animation
        yield return new WaitForSeconds(5f);

        //Destroy the whole level manager when its no longer needed
        Destroy(gameObject);
    }

    //ConstantTimeTileRemove: removes tile component from storage in constant time by removing the last tile
    //Note: This doesn't preserve the order of tiles in the list
    void ConstantTimeTileRemove(int removeIdx)
    {
        //Check if valid index
        if (removeIdx >= floorTiles.Count || removeIdx <= -1)
        {
            Debug.LogError("Failed to remove tile because provided index was out of bounds of storage array!");
            return;
        }

        floorTiles[removeIdx] = floorTiles[floorTiles.Count - 1];
        floorTiles.RemoveAt(floorTiles.Count - 1);
    }

    IEnumerator LevelDegradation(float despawnsASec)
    {
        while (tilesToRemove.Count > despawnsASec)
        {
            for (int i = 0; i < despawnsASec; i++)
            {
                tilesToRemove.Pop().Despawn(4f);
            }
            yield return new WaitForSeconds(1f);
        }
        //On the last round remove remaining tiles
        while (tilesToRemove.Count != 0)
        {
            tilesToRemove.Pop().Despawn(4f);
        }
        StartCoroutine(LevelDeathCounter());
    }

    public void BeginDegradation()
    {
        //Choose blocks to despawn and start their despwn animation with correct time to despawn (should always be >1f seconds) 
        int amountToDespawn = (int)((towerSize * towerSize) * Mathf.Clamp(difficultyThreshold, 0, 1));
        int despawnsASec = Mathf.CeilToInt(amountToDespawn / Mathf.Floor(timeToComplete));

        StartCoroutine(LevelDegradation(despawnsASec));
    }
}
