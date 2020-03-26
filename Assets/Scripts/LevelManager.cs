using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    
    public int levelNo = 2;
    
    
    private GridData levelData;

    private List<GridData> levels;

    void MakeInstance()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Awake()
    {        
        MakeInstance();
        levels = Levels.Instance.levels;
        levelNo = PlayerPrefs.GetInt("LevelNo");        
    }

    private void Start()
    {
        GUIManager.instance.SetLevelText(levelNo);
    }

    public void OnLevelDataReady()
    {
        Debug.Log("Levels data is ready.");
    }
    
    public GridData GetCurrentLevelData()
    {
        CheckLevelNo();
        
        return levels[levelNo-1];
    }

    private void CheckLevelNo()
    {
        if (levelNo <= 0 || levelNo > levels.Count )
            levelNo = 1;
        GUIManager.instance.SetLevelText(levelNo);
    }
    
    public void LoadNextLevel()
    {
        levelNo += 1;
        CheckLevelNo();
        PlayerPrefs.SetInt("LevelNo", levelNo);
        DOTween.KillAll();
        SceneManager.LoadScene(0);
    }
    
    public void LoadPrevLevel()
    {
        levelNo -= 1;
        PlayerPrefs.SetInt("LevelNo", levelNo);
        SceneManager.LoadScene(0);
    }
    

}
