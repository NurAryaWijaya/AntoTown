using UnityEngine;

public class KincirAngin : MonoBehaviour
{
    [Header("Pengaturan Kecepatan")]
    public float kecepatanPutar = 100f; // derajat per detik

    void Update()
    {
        // Memutar baling-baling pada sumbu Z
        transform.Rotate(Vector3.right * kecepatanPutar * Time.deltaTime);
    }
}
