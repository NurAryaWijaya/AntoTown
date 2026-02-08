using UnityEngine;
using UnityEngine.SceneManagement;

public class GameExitMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject exitPopupPanel;

    private const string SAVE_KEY = "HAS_SAVE";

    private void Start()
    {
        exitPopupPanel.SetActive(false);
    }

    // Dipanggil oleh tombol Exit
    public void OpenExitPopup()
    {
        exitPopupPanel.SetActive(true);
        Time.timeScale = 0f; // pause game
    }

    public void CloseExitPopup()
    {
        exitPopupPanel.SetActive(false);
        Time.timeScale = 1f; // resume game
    }

    // SAVE + EXIT
    public void SaveAndExit()
    {
        SaveGame();
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }

    // EXIT TANPA SAVE
    public void QuitWithoutSave()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void SaveGame()
    {
        // Pastikan ada reference ke BuildingManager & GridManager
        BuildingManager buildingManager = GameObject.FindFirstObjectByType<BuildingManager>();
        GridManager gridManager = GameObject.FindFirstObjectByType<GridManager>();

        if (buildingManager != null && gridManager != null)
        {
            SaveSystem.SaveGame(); // simpan semua
            PlayerPrefs.SetInt(SAVE_KEY, 1); // flag save ada
            PlayerPrefs.Save();
            Debug.Log("Game saved!");
        }
        else
        {
            Debug.LogWarning("BuildingManager/GridManager tidak ditemukan!");
        }
    }


}
