using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Field_Script : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }


    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public void Draw(Hexa_struct[,] state)
    {
        int width = state.GetLength(0);
        int height = state.GetLength(1);

        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Hexa_struct hexa = state[x, y];
                tilemap.SetTile(hexa.position, GetTile(hexa));
            }
        }      
    }

    private Tile GetTile(Hexa_struct hexa)
    {
        if(hexa.revealed)
        {
            return GetRevealedTile(hexa);
        }
        else
        {
            return tileUnrevealed;
        }
    }

    private Tile GetRevealedTile(Hexa_struct hexa)
    {
        switch(hexa.type)
        {
            case Hexa_struct.Type.Neutral: return tileNeutral;
            case Hexa_struct.Type.Number: return GetNumberTile(hexa);
            case Hexa_struct.Type.Bomb: return tileBomb;
            default: return null;
        }

    }

    private Tile GetNumberTile(Hexa_struct hexa)
    {
        switch(hexa.number)
        {
            case 1: return tileNumber1;
            case 2: return tileNumber2;
            case 3: return tileNumber3;
            case 4: return tileNumber4;
            case 5: return tileNumber5;
            case 6: return tileNumber6;
            default: return null;
        }
    }

    //private void NullRefLog()
    //{
    //    Debug.Log("null ref");
    //}

    public Tile tileUnrevealed;
    public Tile tileBomb;
    public Tile tileNeutral;
    public Tile tileNumber1;
    public Tile tileNumber2;
    public Tile tileNumber3;
    public Tile tileNumber4;
    public Tile tileNumber5;
    public Tile tileNumber6;
    public Tile tileLootTest;
    //public Tile tileDetonated;
}
