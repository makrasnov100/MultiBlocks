using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LevelDesigner : MonoBehaviour
{
    public Server server;

    //Map Generation Settings
    public int towerSize;
    public Vector3 towerCenter;
    public float[] secPerLevelRange;
    public float[] levelGoneRange;
    public int peakLevel;


    //Instance Settings
    DateTime gameStartTime;

    public void StartMapGenerations()
    {
        gameStartTime = DateTime.Now;

        //Level Info Storage
        Stack<string> 


        for (int i = 1; i < towerSize; i++)
        {
            
        }
    }

    public void CancelPlayMode()
    {

    }
}
