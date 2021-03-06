﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GUIManager : MonoBehaviour
{
    public Text levelNoText;

    public static GUIManager instance;

    public GameObject restartButton;
    public GameObject undoButton;
    public Transform greatMessage;
    public Transform levelPassedMessage;
    public Transform levelFailedMessage;
    public Transform nextLevelButton;
    
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

    void Start()
    {
        GameManager.instance.onLevelCompleted += ShowGreatMessage;
        GameManager.instance.onLevelFailed += ShowLevelFailedMessage;
    }

    public void SetLevelText(int levelNo)
    {
        levelNoText.text = "LEVEL " + levelNo;
    }

    private void ShowGreatMessage()
    {
        SetResetAndUndoButtonDisable();
        greatMessage.DOScale(new Vector3(1.5f, 1.5f, 1.0f), 0.5f)
            .OnComplete(() => { greatMessage.DOPunchScale(new Vector3(.5f, .5f), 1.0f, 5, 1.0f)
                .OnComplete(() => { greatMessage.DOScale(new Vector3(0.0f, 0.0f, 0.0f), 0.2f).SetDelay(0.4f); }); 
            });
    }
    public void ShowLevelPassedMessage()
    {
        levelPassedMessage.gameObject.SetActive(true);
    }
    private void ShowLevelFailedMessage()
    {
        levelFailedMessage.gameObject.SetActive(true);
    }

    private void SetResetAndUndoButtonDisable()
    {
        restartButton.GetComponent<Button>().enabled = false;
        undoButton.GetComponent<Button>().enabled = false;
    }

    public void OnTapToContinueButton()
    {
        LevelManager.instance.LoadNextLevel();
    }

    public void OnTapToReplayButton()
    {
        LevelManager.instance.LoadCurrentLevel();
    }
}
