using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TileNode
{
    public bool isAvailable = true;
    public TileNode left;
    public TileNode right;
    public TileNode up;
    public TileNode down;

    public List<TileNode> children = new List<TileNode>();
    public TileNode parent;

    public GameObject sceneObject;

    public TileData tile;

    public TileNode ParentOfAll
    {
        get
        {
            var node = this;
            while (node.parent != null)
            {
                node = node.parent;
            }

            return node;
        }
    }

    public int ChildCount
    {
        get
        {
            var childCount = children.Count;
            foreach (var childNode in children)
            {
                childCount += childNode.ChildCount;
            }

            return childCount;
        }
    }
}
