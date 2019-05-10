using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AutoFillInAtStart : MonoBehaviour
{
    public TMP_InputField input;
    public List<string> options = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        int chosenOption = Random.Range(0, options.Count);
        input.text = options[chosenOption];
    }
}
