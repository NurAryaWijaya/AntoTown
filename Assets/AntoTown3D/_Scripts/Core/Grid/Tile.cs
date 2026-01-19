using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition; // posisi tile di grid
    public bool isOccupied = false;
    public GameObject currentObject; // jalan atau bangunan

    private Renderer rend;
    private Color defaultColor;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        defaultColor = rend.material.color;
    }

    public void Highlight(bool value)
    {
        rend.material.color = value ? Color.yellow : defaultColor;
    }
}
