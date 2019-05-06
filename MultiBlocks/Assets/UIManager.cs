using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //UI References
    public GameObject InitMenu;
    public Image bg;


    //Instance Variables
    bool initMenuActive = true;


    //BG animation
    public List<Color> bgColors = new List<Color>();
    Color sourceColor;
    int targetColorIdx = -1;
    double transitionStart;
    public double timePerTransition;

    // Start is called before the first frame update
    void Start()
    {
        //Set up BG Animation
        sourceColor = bg.color;
        targetColorIdx = 1;
        transitionStart = Time.time;
        StartCoroutine(BackgroundAnimation());
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator BackgroundAnimation()
    {
        while (initMenuActive)
        {
            bg.color = Color.Lerp(sourceColor, bgColors[targetColorIdx], (Time.time - (float) transitionStart) / (float) timePerTransition);
            yield return new WaitForFixedUpdate();
        }
    }

    void UpdateTargetBGColor()
    {
        transitionStart = Time.time;
        sourceColor = bg.color;
        if (targetColorIdx == bgColors.Count)
            targetColorIdx = 0;
        else
            targetColorIdx++;
    }
}
