using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Lean.Touch;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public enum DIRECTIONS
    {
        DEFAULT,
        LEFT,
        RIGHT,
        UP,
        DOWN
    }

    public static GameManager instance;
    private const float REQUIRED_ARC = 45.0F;
    private const float TILE_HEIGHT = 0.89F;
    private GameObject selectedObject;
    private GameObject splashParticleSystem;
    private List<MoveData> moveHistory;
    public int unSandwichedElementCount;
    public Action onLevelFailed;
    public Action onLevelCompleted;
    public Action onFinalAnimationsCanFired;
    private GameObject finalSandwich;
    private GameObject plate;

    private bool canMove = true;

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
    }

    void Start()
    {
        moveHistory = new List<MoveData>();
        splashParticleSystem = Resources.Load<GameObject>("SplashParticleSystem");
        onLevelFailed += OnLevelFailed;
        onLevelCompleted += OnLevelCompleted;
        onFinalAnimationsCanFired += OnFinalAnimationsCanFired;
    }

    public void OnLevelDataReady(int sandwichElementCount)
    {
        unSandwichedElementCount = sandwichElementCount;
    }

    private void OnEnable()
    {
        LeanTouch.OnFingerSwipe += OnFingerSwipe;
        LeanSelectable.OnSelectGlobal += OnSelectGlobal;
    }

    private void OnSelectGlobal(LeanSelectable selectable, LeanFinger finger)
    {
        Debug.Log("Selectable: " + selectable.gameObject);
        selectedObject = selectable.gameObject;
    }

    private void OnDisable()
    {
        LeanTouch.OnFingerSwipe -= OnFingerSwipe;
        LeanSelectable.OnSelectGlobal -= OnSelectGlobal;
    }

    private void OnFingerSwipe(LeanFinger finger)
    {
        if (selectedObject == null)
            return;

        var selectedTileNode = selectedObject.GetComponent<TileNodeBridge>().tileNode;
        var finalDelta = finger.ScreenPosition - finger.StartScreenPosition;

        if (selectedTileNode != null)
        {
            if (AngleIsValid(0.0f, finalDelta) || AngleIsValid(45.0f, finalDelta))
            {
                selectedTileNode = selectedTileNode.ParentOfAll;

                if (selectedTileNode.up != null && selectedTileNode.up.isAvailable)
                {
                    MoveToTargetTile(selectedTileNode, selectedTileNode.up, DIRECTIONS.UP);
                }

                Debug.Log("Up");
            }
            else if (AngleIsValid(90.0f, finalDelta) || AngleIsValid(135.0f, finalDelta))
            {
                selectedTileNode = selectedTileNode.ParentOfAll;

                if (selectedTileNode.right != null && selectedTileNode.right.isAvailable)
                {
                    MoveToTargetTile(selectedTileNode, selectedTileNode.right, DIRECTIONS.RIGHT);
                }

                Debug.Log("Right");
            }
            else if (AngleIsValid(180.0f, finalDelta) || AngleIsValid(225.0f, finalDelta))
            {
                selectedTileNode = selectedTileNode.ParentOfAll;

                if (selectedTileNode.down != null && selectedTileNode.down.isAvailable)
                {
                    MoveToTargetTile(selectedTileNode, selectedTileNode.down, DIRECTIONS.DOWN);
                }

                Debug.Log("Down");
            }
            else if (AngleIsValid(270.0f, finalDelta) || AngleIsValid(315.0f, finalDelta))
            {
                selectedTileNode = selectedTileNode.ParentOfAll;

                if (selectedTileNode.left != null && selectedTileNode.left.isAvailable)
                {
                    MoveToTargetTile(selectedTileNode, selectedTileNode.left, DIRECTIONS.LEFT);
                }

                Debug.Log("Left");
            }
            else
            {
                Debug.Log("No Direction");
            }
        }
    }

    private void MoveToTargetTile(TileNode selectedTileNode, TileNode targetTileNode, DIRECTIONS direction)
    {
        if (!canMove)
        {
            return;
        }

        unSandwichedElementCount--;

        canMove = false;
        var targetHeight = CalculateHeight(targetTileNode);
        var selectedHeight = CalculateHeight(selectedTileNode);
        selectedTileNode.isAvailable = false;

        Debug.Log("Target Node : " + targetTileNode.tile.tileState);

        targetTileNode.children.Add(selectedTileNode);
        selectedTileNode.parent = targetTileNode;

        moveHistory.Insert(0, new MoveData
        {
            node = targetTileNode,
            direction = direction,
            previousPosition = selectedTileNode.sceneObject.transform.position
        });

        Debug.LogWarning($"History Direction = ({moveHistory[0].direction})");
        Debug.LogWarning($"History Node = ({moveHistory[0].node.tile.tileState})");
        Debug.LogWarning($"History Prev Position = ({moveHistory[0].previousPosition})");


        var rotateTween = selectedTileNode.sceneObject.transform
            .DORotate(
                selectedTileNode.sceneObject.transform.rotation.eulerAngles + new Vector3(GetRotationAngle(direction).x,
                    0, GetRotationAngle(direction).z), 0.3f, RotateMode.Fast);
        var moveTween = selectedTileNode.sceneObject.transform
            .DOMove(
                new Vector3(targetTileNode.sceneObject.transform.position.x, targetHeight + selectedHeight,
                    targetTileNode.sceneObject.transform.position.z), 0.3f)
            .OnComplete(() =>
            {
                canMove = true;
                
                if (splashParticleSystem != null)
                {
                    var splash = Instantiate(splashParticleSystem, targetTileNode.sceneObject.transform);
                    Destroy(splash, 0.5f);
                }
                    
                selectedTileNode.sceneObject.transform.SetParent(targetTileNode.sceneObject.transform);

                if (unSandwichedElementCount == 1)
                {
                    selectedTileNode.isOnTheTop = true;
                    if (CheckIfTheLevelCompleted(selectedTileNode))
                    {
                        finalSandwich = targetTileNode.sceneObject;
                        onLevelCompleted?.Invoke();
                    }
                    else
                    {
                        onLevelFailed?.Invoke();
                    }
                }
            });
    }

    private bool CheckIfTheLevelCompleted(TileNode tileOnTheTop)
    {
        var tileNodes = GridManager.instance.grid;

        int breadCount = 0;
        foreach (var tileNode in tileNodes)
        {
            if (tileNode.tile.tileState != TileData.TileState.EMPTY && tileNode.isAvailable)
            {
                if (tileNode.tile.tileState != TileData.TileState.BREAD)
                {
                    return false;
                }

                breadCount++;
            }
        }

        if (tileOnTheTop.tile.tileState != TileData.TileState.BREAD)
            return false;

        if (breadCount == 0 || breadCount > 1)
        {
            return false;
        }

        return true;
    }

    private float CalculateHeight(TileNode tileNode)
    {
        var height = TILE_HEIGHT / 2.0f;
        var parentObject = tileNode.sceneObject;
        var childCount = tileNode.ChildCount;
        height += childCount * TILE_HEIGHT;

        Debug.Log($"Target {tileNode.sceneObject} Child Count = {(childCount)}, TILE_HEIGHT = {TILE_HEIGHT}, Calculated Height: {height}");

        return height;
    }

    protected bool AngleIsValid(float requiredAngle, Vector2 vector)
    {
        var angle = Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg;
        var angleDelta = Mathf.DeltaAngle(angle, requiredAngle);

        if (angleDelta < REQUIRED_ARC * -0.5f || angleDelta >= REQUIRED_ARC * 0.5f)
        {
            return false;
        }

        return true;
    }

    public Vector3 GetRotationAngle(DIRECTIONS direction)
    {
        switch (direction)
        {
            case DIRECTIONS.UP:
                return new Vector3(180.0f, 0, 0);
            case DIRECTIONS.RIGHT:
                return new Vector3(0, 0, -180.0f);
            case DIRECTIONS.LEFT:
                return new Vector3(0, 0, 180.0f);
            case DIRECTIONS.DOWN:
                return new Vector3(-180.0f, 0, 0);
        }

        return Vector3.zero;
    }

    public void OnUndo()
    {
        canMove = false;
        ExecuteUndo().OnComplete(() => canMove = true);
    }

    private Tween ExecuteUndo()
    {
        if (moveHistory.Count == 0)
            return null;

        var moveData = moveHistory[0];
        moveHistory.RemoveAt(0);

        return UndoMoveData(moveData);
    }

    private Tween UndoMoveData(MoveData moveData)
    {
        unSandwichedElementCount++;
        var tileNode = moveData.node.children[moveData.node.children.Count - 1];

        var direction = moveData.direction;
        moveData.node.children.Remove(tileNode);
        tileNode.parent = null;
        tileNode.isAvailable = true;

        var rotateTween = tileNode.sceneObject.transform
            .DORotate(
                tileNode.sceneObject.transform.rotation.eulerAngles + new Vector3(GetRotationAngle(direction).x, 0,
                    -GetRotationAngle(direction).z), 0.3f, RotateMode.Fast)
            .SetEase(Ease.Linear);

        var tween = tileNode.sceneObject.transform
            .DOMove(new Vector3(moveData.previousPosition.x, 0, moveData.previousPosition.z), 0.3f)
            .OnStart(() => { tileNode.sceneObject.transform.SetParent(null); });

        return tween;
    }

    public void Restart()
    {
        canMove = false;
        ChainedUndo();
    }

    private void ChainedUndo()
    {
        var tween = ExecuteUndo();
        if (tween != null)
        {
            tween.OnComplete(() => ChainedUndo());
        }
        else
        {
            canMove = true;
        }
    }

    private void OnLevelCompleted()
    {
        Debug.Log("LEVEL COMPLETED");
        InstantiatePlate();
        MoveFinalSandwichToFinalPosition();
    }

    private void OnLevelFailed()
    {
        Debug.Log("LEVEL FAILED");
    }

    private void MoveFinalSandwichToFinalPosition()
    {
        finalSandwich.transform.DOMove(new Vector3(0.0f, 0.3f, -7.0f), .4f).SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                PlayStarParticles();
                finalSandwich.transform.DORotate(new Vector3(0.0f, 360.0f * 2, 0.0f ), 0.5f, RotateMode.FastBeyond360)
                    .OnComplete(() => { 
                        onFinalAnimationsCanFired?.Invoke();
                    }); 
            }); 
    }
    
    private void OnFinalAnimationsCanFired()
    {
        FinalRotationAnimations();
    }
    
    private void PlayStarParticles()
    {
        var successStarParticleSystem = Resources.Load("StarsParticle");
        Instantiate(successStarParticleSystem, finalSandwich.transform.position, Quaternion.identity);
    }
    
    private void InstantiatePlate()
    {
        plate = Instantiate(Resources.Load("Plate") as GameObject, new Vector3(0, 0, -7.0f), Quaternion.identity);
        
        plate.transform.DOScale(new Vector3(1f, 1f, 1.0f), 0.5f)
            .OnComplete(() => { plate.transform.DOPunchScale(new Vector3(.2f, 0, .2f), .5f, 5, 1.0f); });
    }

    private void FinalRotationAnimations()
    {
        var confettiParticle = Resources.Load("ConfettiExplosion");

        finalSandwich.transform
            .DORotate(new Vector3(0.0f, 360.0f, 0.0f), 4.0f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);

        plate.transform.DORotate(new Vector3(0.0f, -360.0f, 0.0f), 4.0f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);

        finalSandwich.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 1f, 2, 2.0f)
            .OnComplete(() =>
            {
                var confetti =Instantiate(confettiParticle, finalSandwich.transform.position, Quaternion.identity) as GameObject;
                confetti.GetComponent<ParticleSystem>().Play();
            });
        
        finalSandwich.transform.DOMove(new Vector3(0, transform.position.y + 2.5f, 0), 1.0f)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() =>
            {
                GUIManager.instance.ShowLevelPassedMessage();
            });
    }
}