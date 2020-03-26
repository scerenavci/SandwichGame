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

    private const float REQUIRED_ARC = 45.0F;
    private const float TILE_HEIGHT = 1.7F;
    private GameObject selectedObject;
    private GameObject splashParticleSystem;
    private List<MoveData> moveHistory;

    private bool canMove=true;

    void Start()
    {
        moveHistory = new List<MoveData>();
        splashParticleSystem = Resources.Load<GameObject>("SplashParticleSystem");
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
        if(!canMove)
            return;

        canMove = false;
        var height = CalculateHeight(targetTileNode);
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

        Sequence sequence = DOTween.Sequence();
        
        var rotateTween = selectedTileNode.sceneObject.transform
            .DORotate(selectedTileNode.sceneObject.transform.rotation.eulerAngles + new Vector3(GetRotationAngle(direction).x,GetRotationAngle(direction).y,GetRotationAngle(direction).z), 0.4f,RotateMode.Fast);
        var moveTween = selectedTileNode.sceneObject.transform
            .DOMove(new Vector3(targetTileNode.sceneObject.transform.position.x, height, targetTileNode.sceneObject.transform.position.z), 0.3f)
            .OnComplete(() =>
            {
                if (splashParticleSystem != null)
                    Instantiate(splashParticleSystem, targetTileNode.sceneObject.transform);
                canMove = true;
                selectedTileNode.sceneObject.GetComponent<SandwichElement>().SetOriginalConstrains();
                selectedTileNode.sceneObject.transform.SetParent(targetTileNode.sceneObject.transform);
            });

        sequence?.Append(moveTween).Append(rotateTween);
    }

    private float CalculateHeight(TileNode targetTileNode)
    {
        var height = TILE_HEIGHT / 2.0f;
        var parentObject = targetTileNode.sceneObject;
        var childCount = targetTileNode.ChildCount;
        height +=  childCount* TILE_HEIGHT;
        
        Debug.Log($"Target {targetTileNode.sceneObject} Child Count = {(childCount)}, TILE_HEIGHT = {TILE_HEIGHT}, Calculated Height: {height}");
        
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
        ExecuteUndo();
    }

    private bool ExecuteUndo(Sequence tweenSequence = null)
    {
        if (moveHistory.Count == 0)
            return false;

        canMove = false;
        var moveData = moveHistory[0];
        moveHistory.RemoveAt(0);
        
        UndoMoveData(moveData, tweenSequence);

        return true;
    }

    private void UndoMoveData(MoveData moveData, Sequence tweenSequence = null)
    {       
        var tileNode = moveData.node.children[moveData.node.children.Count - 1];

        var direction = moveData.direction;
        moveData.node.children.Remove(tileNode);
        tileNode.parent = null;
        tileNode.isAvailable = true;

        var rotateTween = tileNode.sceneObject.transform
            .DORotate(tileNode.sceneObject.transform.rotation.eulerAngles + new Vector3(-GetRotationAngle(direction).x,-GetRotationAngle(direction).y,-GetRotationAngle(direction).z), 0.6f,RotateMode.Fast)
            .OnComplete(() =>
            {
                tileNode.sceneObject.GetComponent<SandwichElement>().SetOriginalConstrains();
                canMove = true;
            });
        
        var tween = tileNode.sceneObject.transform.DOMove(new Vector3(moveData.previousPosition.x, TILE_HEIGHT, moveData.previousPosition.z), 0.5f)
            .OnStart(() =>
            {
                tileNode.sceneObject.transform.SetParent(null);
            });
        
        tweenSequence?.Append(tween).Append(rotateTween).OnComplete(() =>
        {
            tileNode.sceneObject.GetComponent<SandwichElement>().SetOriginalConstrains();
            canMove = true;
        });
    }
    
    public void Restart()
    {
        var sequence = DOTween.Sequence();
        while (ExecuteUndo(sequence)) {}
    }
    
    
}