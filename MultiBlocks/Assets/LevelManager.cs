using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    int curLvl;
    int towerSize;
    int countRemoveIter;
    float timeToComplete;

    List<TileController> floorTiles;

    Stack<TileController> tilesToRemove = new Stack<TileController>();

    public void GenerateFloor(int towerSize, int curLvl, float timeToComplete, GameObject floorTile, Vector3 layerCenter, int sizePerBlock, int[] inPoint, int[] outPoint)
    {

        this.curLvl = curLvl;
        this.towerSize = towerSize;
        this.timeToComplete = timeToComplete;

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

    void PlanFloorRemoval(int seed)
    {
        //Check avaliablility of tiles
        //Set the ranodm seed based on server input count of despawn commands and other variables?

        //Choose blocks to despawn and start their despwn animation with correct time to despawn (should always be >1f seconds) 

        //Make sure to remove tile from the list of avalible ones as sson as it was recorded in the stack
        // - use ConstantTimeTileRemove() to do this efficiently
    }

    IEnumerator LevelDeathCounter()
    {
        yield return new WaitForSeconds(timeToComplete);

        foreach (TileController tile in floorTiles)
        {
            tile.Despawn(4f);
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
}
