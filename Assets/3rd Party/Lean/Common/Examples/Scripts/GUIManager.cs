using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIManager : MonoBehaviour
{
    public Text levelNoText;

    public static GUIManager instance;

    private void MakeInstance()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Awake()
    {
        MakeInstance();
    }

    public void SetLevelText(int levelNo)
    {
        levelNoText.text = "Level " + levelNo;
    }
}
