using System;

[Serializable]
public class TileData
{
    public enum TileState{
        EMPTY,
        BREAD,
        TOMATO,
        LETTUCE,
        CHEESE
    } 
    
    public int row;
    public int column;
    public TileState tileState;
}
