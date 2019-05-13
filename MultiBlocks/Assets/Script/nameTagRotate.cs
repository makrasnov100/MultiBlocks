using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class nameTagRotate : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if(Camera.main)
            transform.LookAt(Camera.main.transform);
    }
}
