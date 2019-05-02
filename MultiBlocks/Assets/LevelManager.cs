using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    int curLvl;
    int towerSize;
    int countRemoveIter;
    float timeToComplete;
    float difficultyThreshold; //value from 0 to 1 indicating proportion of blocks to remove before the whole level falls

    List<TileController> floorTiles;

    Stack<TileController> tilesToRemove = new Stack<TileController>();

    public void GenerateFloor(int towerSize, int curLvl, float timeToComplete, float difficultyThreshold, GameObject floorTile, Vector3 layerCenter, int sizePerBlock, int[] inPoint, int[] outPoint)
    {

        this.curLvl = curLvl;
        this.towerSize = towerSize;
        this.timeToComplete = timeToComplete;
        this.difficultyThreshold = difficultyThreshold;
        countRemoveIter = 0;

        StartCoroutine(LevelDeathCounter());

        //Populating initial cell list  
        floorTiles = new List<TileController>();
        for (int z = 0; z < towerSize; z++)
        {
            for (int x = 0; x < towerSize; x++)
            {
                GameObject curGO = Instantiate(floorTile,
                                              layerCenter + new Vector3((x - (towerSize / 2)) * sizePerBlock,
                                                                         curLvl * sizePerBlock,
                                                                        (z - (towerSize / 2)) * sizePerBlock),
                                              Quaternion.identity, transform);

                TileController curGOControl = curGO.AddComponent<TileController>();

                floorTiles.Add(curGOControl);
            }
        }

        //TODO: create incoming and outgoing fields
    }

    public void PlanFloorRemoval(int seed)
    {
        //Set the ranodm seed based on server input, count of despawn commands and other variables?
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

        StartCoroutine(LevelDegradation(despawnsASec));
        countRemoveIter++;
    }

    IEnumerator LevelDeathCounter()
    {
        yield return new WaitForSeconds(timeToComplete);

        foreach (TileController tile in floorTiles)
            tile.Despawn(4f);

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
        while (tilesToRemove.Count != 0)
        {
            for (int i = 0; i < despawnsASec; i++)
            {
                tilesToRemove.Pop().Despawn(3f);
            }
            yield return new WaitForSeconds(1f);
        }
    }
}
