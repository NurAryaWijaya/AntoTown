using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition; // posisi tile di grid
    public bool isOccupied = false;
    public GameObject currentObject; // jalan atau bangunan

    private Color defaultColor;
    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rend.material = new Material(rend.material);
        defaultColor = rend.material.color;
    }

    public void SetColor(Color color)
    {
        rend.material.color = color;
    }

    public void ResetColor()
    {
        rend.material.color = defaultColor;
    }

    // 🔹 Zone preview (hijau / merah)
    public void SetPreview(bool valid)
    {
        if (rend == null) return;
        rend.material.color = valid ? Color.green : Color.red;
    }

    public void ClearPreview()
    {
        if (rend == null) return;
        rend.material.color = defaultColor;
    }
}
