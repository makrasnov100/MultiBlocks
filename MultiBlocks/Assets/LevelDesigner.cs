using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LevelDesigner : MonoBehaviour
{
    public Server server;

    //Map Generation Settings
    public float[] secPerLevelRange;
    public float[] levelGoneRange;
    public int peakLevel;
    public int towerSize;


    //Instance Settings
    DateTime gameStartTime;

    public void StartMapGenerations()
    {
        gameStartTime = DateTime.Now;
        DateTime levelEnd = DateTime.Now;
        levelEnd = levelEnd.AddSeconds(10.0);
        StartCoroutine(GameLock(10f));

        //Calculate level rate values
        float secPerLevelSlope = (secPerLevelRange[1] - secPerLevelRange[0]) / peakLevel;
        float levelGoneSlope = (levelGoneRange[1] - levelGoneRange[0]) / peakLevel;

        for (int i = 0; i < peakLevel; i++)
        {
            //Current level slope
            int levelSeed = UnityEngine.Random.Range(0, 100000);
            float curSecForLevel = secPerLevelRange[0] + (secPerLevelSlope * i);
            levelEnd = levelEnd.AddSeconds(curSecForLevel);
            string curLevInfo = i + "|";
            curLevInfo += levelSeed + "|";
            curLevInfo += levelEnd.Year + "," + levelEnd.Month + "," + levelEnd.Day + "," + levelEnd.Hour + "," + levelEnd.Minute + "," + levelEnd.Second + "," + levelEnd.Millisecond + "|";
            curLevInfo += secPerLevelRange[0] + (secPerLevelSlope * i) + "|";
            curLevInfo += levelGoneRange[0] + (levelGoneSlope * i) + "|";
            curLevInfo += towerSize;

            server.Send("OnLevelInfo|" + curLevInfo, server.GetUnreliableChannel());
        }
    }

    IEnumerator GameLock(float secToLock)
    {
        yield return new WaitForSeconds(secToLock);
        server.gameLock = true;
    }

    public void CancelPlayMode()
    {
        server.Send("OnCancelPlay|" + 0, server.GetUnreliableChannel());
    }
}
